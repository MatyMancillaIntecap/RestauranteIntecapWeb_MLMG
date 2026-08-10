using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RestauranteIntecapWeb_MLMG.Data;
using RestauranteIntecapWeb_MLMG.Models;
using RestauranteIntecapWeb_MLMG.Models.DTOs;
using System.Text.RegularExpressions;

namespace RestauranteIntecapWeb_MLMG.Services
{
    public class EmpleadoService : IEmpleadoService
    {
        private readonly ApplicationDbContext _context;

        public EmpleadoService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<MenuDiario>> ObtenerMenuDisponiblePorFechaAsync(DateTime fecha)
        {
            return await _context.MenusDiarios
                .Where(m => m.fecha.Date == fecha.Date && m.estado == "Disponible" && m.stock > 0)
                .ToListAsync();
        }

        // Procesa la reserva asignando el NIT e incrementando el contador diario
        public async Task<(bool Exito, string Mensaje)> ProcesarReservaAsync(SolicitudReservaDTO solicitud)
        {
            if (solicitud.Platillos == null || !solicitud.Platillos.Any())
            {
                return (false, "Debe seleccionar al menos un platillo para realizar la reserva.");
            }



            // Normalización del NIT
            string nitLimpio = string.IsNullOrWhiteSpace(solicitud.NitFacturacion) ? "C/F" : solicitud.NitFacturacion.Trim();

            // VALIDACIÓN DE NIT CON REGEX (Acepta 'C/F' o exactamente 13 dígitos)
            bool esNitValido = Regex.IsMatch(nitLimpio, @"^(C/F|c/f|\d{13})$");
            if (!esNitValido)
            {
                return (false, "El NIT ingresado no es válido. Debe contener exactamente 13 dígitos numéricos o indicar 'C/F'.");
            }

            int totalSolicitadosEnPeticion = solicitud.Platillos.Sum(p => p.Cantidad);
            if (totalSolicitadosEnPeticion > 2)
            {
                return (false, "Has alcanzado el límite máximo de 2 almuerzos permitidos por solicitud.");
            }

            // Continuación normal del guardado en la base de datos con nitLimpio...
            solicitud.NitFacturacion = nitLimpio.ToUpper();



         

            int reservasPreviasHoy = await _context.Reservas
                .Where(r => r.usuario_id == solicitud.UsuarioId &&
                            r.fecha_consumo.Date == solicitud.FechaConsumo.Date &&
                            r.estado == "Activa")
                .SumAsync(r => r.cantidad);

            if ((reservasPreviasHoy + totalSolicitadosEnPeticion) > 2)
            {
                return (false, $"Límite diario alcanzado: Ya cuentas con {reservasPreviasHoy} almuerzo(s) reservado(s) para hoy. No puedes solicitar más de 2 almuerzos por día.");
            }

            string nitFinal = string.IsNullOrWhiteSpace(solicitud.NitFacturacion) ? "C/F" : solicitud.NitFacturacion.Trim();

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    foreach (var item in solicitud.Platillos)
                    {
                        var menu = await _context.MenusDiarios.FindAsync(item.MenuId);

                        if (menu == null || menu.estado != "Disponible")
                        {
                            await transaction.RollbackAsync();
                            return (false, "Uno de los platillos seleccionados ya no está disponible.");
                        }

                        if (menu.stock < item.Cantidad)
                        {
                            await transaction.RollbackAsync();
                            return (false, $"Stock insuficiente para '{menu.nombre_plato}'. Disponible: {menu.stock}.");
                        }

                        var nuevaReserva = new Reserva
                        {
                            usuario_id = solicitud.UsuarioId,
                            menu_id = item.MenuId,
                            forma_pago_id = item.FormaPagoId,
                            cantidad = item.Cantidad,
                            donde_consume = item.DondeConsume,
                            nit_facturacion = nitFinal,
                            fecha_reserva = DateTime.Now,
                            fecha_consumo = solicitud.FechaConsumo.Date,
                            estado = "Activa"
                        };

                        menu.stock -= item.Cantidad;
                        menu.cantidad_solicitada += item.Cantidad;

                        if (menu.stock == 0)
                        {
                            menu.estado = "Agotado";
                        }

                        _context.Reservas.Add(nuevaReserva);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return (true, "¡Reserva registrada con éxito!");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return (false, "Error interno al procesar la reserva: " + ex.Message);
                }
            }



        }

        // Cancela una reserva activa, devuelve el stock al platillo y actualiza contadores
        public async Task<(bool Exito, string Mensaje)> CancelarReservaAsync(int reservaId, int usuarioId)
        {
            var reserva = await _context.Reservas
                .Include(r => r.MenuDiario)
                .FirstOrDefaultAsync(r => r.id == reservaId && r.usuario_id == usuarioId);

            if (reserva == null)
            {
                return (false, "La reserva no existe o no pertenece a tu usuario.");
            }

            if (reserva.estado != "Activa")
            {
                return (false, $"No se puede cancelar una reserva que se encuentra en estado '{reserva.estado}'.");
            }

            // Regla de Negocio: No se pueden cancelar reservas de días pasados
            if (reserva.fecha_consumo.Date < DateTime.Today)
            {
                return (false, "No es posible cancelar reservas de fechas pasadas.");
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Cambiamos el estado a Cancelada
                    reserva.estado = "Cancelada";

                    // Devolvemos el stock al menú
                    if (reserva.MenuDiario != null)
                    {
                        reserva.MenuDiario.stock += reserva.cantidad;
                        reserva.MenuDiario.cantidad_solicitada -= reserva.cantidad;

                        if (reserva.MenuDiario.cantidad_solicitada < 0)
                        {
                            reserva.MenuDiario.cantidad_solicitada = 0;
                        }

                        // Si estaba agotado y devolvimos stock, vuelve a estar Disponible
                        if (reserva.MenuDiario.estado == "Agotado" && reserva.MenuDiario.stock > 0)
                        {
                            reserva.MenuDiario.estado = "Disponible";
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return (true, "La reserva ha sido cancelada exitosamente y el stock fue restituido.");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return (false, "Error al cancelar la reserva: " + ex.Message);
                }
            }
        }

        // Consulta el historial aplicando filtros dinámicos con LINQ
        public async Task<List<HistorialEmpleadoDTO>> ObtenerHistorialUsuarioFiltradoAsync(int usuarioId, DateTime? fechaInicio, DateTime? fechaFin, string? estado)
        {
            var query = _context.Reservas
                .Where(r => r.usuario_id == usuarioId)
                .Include(r => r.MenuDiario)
                .Include(r => r.FormaPago)
                .AsQueryable();

            // Filtro por fecha de inicio
            if (fechaInicio.HasValue)
            {
                query = query.Where(r => r.fecha_consumo.Date >= fechaInicio.Value.Date);
            }

            // Filtro por fecha de fin
            if (fechaFin.HasValue)
            {
                query = query.Where(r => r.fecha_consumo.Date <= fechaFin.Value.Date);
            }

            // Filtro por estado especifico
            if (!string.IsNullOrWhiteSpace(estado) && estado != "Todos")
            {
                query = query.Where(r => r.estado == estado);
            }

            return await query
                .OrderByDescending(r => r.fecha_reserva)
                .Select(r => new HistorialEmpleadoDTO
                {
                    ReservaId = r.id,
                    FechaConsumo = r.fecha_consumo,
                    NombrePlato = r.MenuDiario!.nombre_plato,
                    ImagenUrl = r.MenuDiario.imagen_url ?? "",
                    Cantidad = r.cantidad,
                    PrecioUnitario = r.MenuDiario.precio,
                    FormaPago = r.FormaPago!.nombre,
                    DondeConsume = r.donde_consume,
                    NitFacturacion = r.nit_facturacion,
                    Estado = r.estado,
                    FechaReserva = r.fecha_reserva
                })
                .ToListAsync();
        }

        // Genera Excel filtrado
        public async Task<byte[]> GenerarExcelHistorialFiltradoAsync(int usuarioId, DateTime? fechaInicio, DateTime? fechaFin, string? estado)
        {
            var historial = await ObtenerHistorialUsuarioFiltradoAsync(usuarioId, fechaInicio, fechaFin, estado);

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Mi Historial Filtrado");

                worksheet.Cell(1, 1).Value = "# Reserva";
                worksheet.Cell(1, 2).Value = "Fecha Consumo";
                worksheet.Cell(1, 3).Value = "Platillo";
                worksheet.Cell(1, 4).Value = "Cantidad";
                worksheet.Cell(1, 5).Value = "Precio Unitario";
                worksheet.Cell(1, 6).Value = "Total";
                worksheet.Cell(1, 7).Value = "Método Pago";
                worksheet.Cell(1, 8).Value = "NIT";
                worksheet.Cell(1, 9).Value = "Estado";

                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#0D6EFD");
                headerRow.Style.Font.FontColor = XLColor.White;

                int row = 2;
                foreach (var item in historial)
                {
                    worksheet.Cell(row, 1).Value = item.ReservaId;
                    worksheet.Cell(row, 2).Value = item.FechaConsumo.ToString("dd/MM/yyyy");
                    worksheet.Cell(row, 3).Value = item.NombrePlato;
                    worksheet.Cell(row, 4).Value = item.Cantidad;
                    worksheet.Cell(row, 5).Value = item.PrecioUnitario;
                    worksheet.Cell(row, 6).Value = item.TotalPagado;
                    worksheet.Cell(row, 7).Value = item.FormaPago;
                    worksheet.Cell(row, 8).Value = item.NitFacturacion;
                    worksheet.Cell(row, 9).Value = item.Estado;
                    row++;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        // Genera PDF filtrado
        public async Task<byte[]> GenerarPdfHistorialFiltradoAsync(int usuarioId, DateTime? fechaInicio, DateTime? fechaFin, string? estado)
        {
            var historial = await ObtenerHistorialUsuarioFiltradoAsync(usuarioId, fechaInicio, fechaFin, estado);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("RESTAURANTE ESCUELA INTECAP").Bold().FontSize(16).FontColor(Colors.Blue.Medium);
                        col.Item().Text("Comprobante de Historial de Reservas").FontSize(12).FontColor(Colors.Grey.Darken2);
                    });

                    page.Content().PaddingVertical(10).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(30);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(3);
                            cols.ConstantColumn(30);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Blue.Medium).Padding(4).Text("#").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Medium).Padding(4).Text("Fecha").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Medium).Padding(4).Text("Platillo").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Medium).Padding(4).Text("Cant").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Medium).Padding(4).Text("Total").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Medium).Padding(4).Text("NIT").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Medium).Padding(4).Text("Estado").FontColor(Colors.White).Bold();
                        });

                        foreach (var item in historial)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.ReservaId.ToString());
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.FechaConsumo.ToString("dd/MM/yyyy"));
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.NombrePlato);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.Cantidad.ToString());
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"Q {item.TotalPagado:F2}");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.NitFacturacion);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.Estado);
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Página ");
                        x.CurrentPageNumber();
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}