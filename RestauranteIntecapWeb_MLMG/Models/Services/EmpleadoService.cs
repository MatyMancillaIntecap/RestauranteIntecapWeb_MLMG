using Microsoft.EntityFrameworkCore;
using RestauranteIntecapWeb_MLMG.Data;
using RestauranteIntecapWeb_MLMG.Models;
using RestauranteIntecapWeb_MLMG.Models.DTOs;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;


namespace RestauranteIntecapWeb_MLMG.Services
{
    public class EmpleadoService : IEmpleadoService
    {
        private readonly ApplicationDbContext _context;

        public EmpleadoService(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Obtiene platillos disponibles para una fecha específica respetando el contrato exacto
        public async Task<List<MenuDiario>> ObtenerMenuDisponiblePorFechaAsync(DateTime fecha)
        {
            return await _context.MenusDiarios
                .Where(m => m.fecha.Date == fecha.Date && m.estado == "Disponible" && m.stock > 0)
                .OrderBy(m => m.nombre_plato) // Ordenamiento alfabético A-Z
                .ToListAsync();
        }

        // 2. Procesa la reserva evaluando el límite dinámico asignado al Rol del usuario (Empleado, Cocina, Admin)
        public async Task<(bool Exito, string Mensaje)> ProcesarReservaAsync(SolicitudReservaDTO solicitud)
        {
            if (solicitud.Platillos == null || !solicitud.Platillos.Any())
            {
                return (false, "Debe seleccionar al menos un platillo para realizar la reserva.");
            }

            // Consultar el usuario e incluir la información de su Rol desde SQL Server
            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.id == solicitud.UsuarioId);

            if (usuario == null)
            {
                return (false, "El usuario no existe en la base de datos.");
            }

            // Obtener el límite del rol de la base de datos
            int limitePermitido = usuario.Rol != null ? usuario.Rol.max_almuerzos : 2;
            int totalSolicitados = solicitud.Platillos.Sum(p => p.Cantidad);

            // Si el límite es mayor a 0, se valida que no lo supere
            if (limitePermitido > 0 && totalSolicitados > limitePermitido)
            {
                return (false, $"Has superado el límite de {limitePermitido} almuerzos diarios para tu rol ({usuario.Rol?.nombre}).");
            }

            // Validar formato del NIT
            string nitLimpio = string.IsNullOrWhiteSpace(solicitud.NitFacturacion) ? "C/F" : solicitud.NitFacturacion.Trim();
            if (!Regex.IsMatch(nitLimpio, @"^(C/F|c/f|\d{13})$"))
            {
                return (false, "El NIT ingresado no es válido. Debe contener 13 dígitos numéricos o 'C/F'.");
            }

            // Inserción de la reserva
            foreach (var item in solicitud.Platillos)
            {
                var menu = await _context.MenusDiarios.FindAsync(item.MenuId);
                if (menu == null || menu.stock < item.Cantidad)
                {
                    return (false, "No hay suficiente stock para uno de los platillos seleccionados.");
                }

                menu.stock -= item.Cantidad;
                menu.cantidad_solicitada += item.Cantidad;

                var reserva = new Reserva
                {
                    usuario_id = solicitud.UsuarioId,
                    menu_id = item.MenuId,
                    forma_pago_id = item.FormaPagoId,
                    cantidad = item.Cantidad,
                    donde_consume = item.DondeConsume,
                    fecha_reserva = DateTime.Now,
                    fecha_consumo = menu.fecha,
                    estado = "Activa",
                    nit_facturacion = nitLimpio.ToUpper()
                };

                _context.Reservas.Add(reserva);
            }

            await _context.SaveChangesAsync();
            return (true, "¡Reserva realizada exitosamente!");
        }

        // 3. Cancela un platillo de una reserva y devuelve el stock
        public async Task<(bool Exito, string Mensaje)> CancelarReservaAsync(int reservaId, int usuarioId)
        {
            var reserva = await _context.Reservas
                .Include(r => r.MenuDiario)
                .FirstOrDefaultAsync(r => r.id == reservaId && r.usuario_id == usuarioId);

            if (reserva == null)
            {
                return (false, "La reserva no existe o no pertenece al usuario.");
            }

            if (reserva.estado != "Activa")
            {
                return (false, "Solo se pueden cancelar reservas que se encuentren activas.");
            }

            // Devolver el stock a la cocina
            if (reserva.MenuDiario != null)
            {
                reserva.MenuDiario.stock += reserva.cantidad;
                reserva.MenuDiario.cantidad_solicitada -= reserva.cantidad;
            }

            reserva.estado = "Cancelada";
            await _context.SaveChangesAsync();

            return (true, "Reserva cancelada correctamente y stock devuelto.");
        }

        // 4. Obtiene el historial del usuario filtrado respetando el nombre exacto de la interfaz
        public async Task<List<HistorialEmpleadoDTO>> ObtenerHistorialUsuarioFiltradoAsync(int usuarioId, DateTime? fechaInicio, DateTime? fechaFin, string? estado)
        {
            var query = _context.Reservas
                .Include(r => r.MenuDiario)
                .Include(r => r.FormaPago)
                .Where(r => r.usuario_id == usuarioId)
                .AsQueryable();

            if (fechaInicio.HasValue)
                query = query.Where(r => r.fecha_consumo.Date >= fechaInicio.Value.Date);

            if (fechaFin.HasValue)
                query = query.Where(r => r.fecha_consumo.Date <= fechaFin.Value.Date);

            if (!string.IsNullOrWhiteSpace(estado) && estado != "Todos")
                query = query.Where(r => r.estado == estado);

            return await query
                .OrderByDescending(r => r.fecha_consumo)
                .ThenBy(r => r.MenuDiario!.nombre_plato)
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

        // Métodos de reporte Excel y PDF
        // MÉTODO PARA GENERAR EL EXCEL DEL EMPLEADO
        public async Task<byte[]> GenerarExcelHistorialFiltradoAsync(int usuarioId, DateTime? fechaInicio, DateTime? fechaFin)
        {
            // 1. Obtenemos los datos reutilizando nuestra propia consulta ya filtrada
            var historial = await ObtenerHistorialUsuarioFiltradoAsync(usuarioId, fechaInicio, fechaFin, "Todos");

            // 2. Creamos un libro de Excel en blanco en la memoria RAM
            using (var workbook = new XLWorkbook())
            {
                // 3. Agregamos una hoja llamada "Mi Historial"
                var worksheet = workbook.Worksheets.Add("Mi Historial");

                // 4. Dibujamos los encabezados de las columnas (Fila 1)
                worksheet.Cell(1, 1).Value = "# Reserva";
                worksheet.Cell(1, 2).Value = "Fecha Consumo";
                worksheet.Cell(1, 3).Value = "Platillo";
                worksheet.Cell(1, 4).Value = "Cantidad";
                worksheet.Cell(1, 5).Value = "Precio Unitario";
                worksheet.Cell(1, 6).Value = "Total (Q)";
                worksheet.Cell(1, 7).Value = "Forma Pago";
                worksheet.Cell(1, 8).Value = "NIT";

                // 5. Le damos estilo visual al encabezado (Azul con letras blancas)
                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#0D6EFD");
                headerRow.Style.Font.FontColor = XLColor.White;

                // 6. Llenamos las filas con los datos (Empezando en la Fila 2)
                int row = 2;
                foreach (var item in historial)
                {
                    worksheet.Cell(row, 1).Value = item.ReservaId;
                    worksheet.Cell(row, 2).Value = item.FechaConsumo.ToString("dd/MM/yyyy");
                    worksheet.Cell(row, 3).Value = item.NombrePlato;
                    worksheet.Cell(row, 4).Value = item.Cantidad;
                    worksheet.Cell(row, 5).Value = item.PrecioUnitario;
                    worksheet.Cell(row, 6).Value = item.Cantidad * item.PrecioUnitario;
                    worksheet.Cell(row, 7).Value = item.FormaPago;
                    worksheet.Cell(row, 8).Value = item.NitFacturacion;
                    row++;
                }

                // 7. Auto-ajustamos el ancho de las columnas para que el texto quepa bien
                worksheet.Columns().AdjustToContents();

                // 8. Guardamos el archivo en un "río de memoria" (Stream) y lo convertimos a un arreglo de bytes
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        // MÉTODO PARA GENERAR EL PDF DEL EMPLEADO
        public async Task<byte[]> GenerarPdfHistorialFiltradoAsync(int usuarioId, DateTime? fechaInicio, DateTime? fechaFin)
        {
            // 1. Obtenemos los datos
            var historial = await ObtenerHistorialUsuarioFiltradoAsync(usuarioId, fechaInicio, fechaFin, "Todos");

            // 2. Creamos la estructura vectorial del documento PDF
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    // Configuramos hoja tamaño carta con márgenes
                    page.Size(PageSizes.Letter);
                    page.Margin(1, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    // 3. Encabezado del documento
                    page.Header().Column(col =>
                    {
                        col.Item().Text("RESTAURANTE ESCUELA INTECAP").Bold().FontSize(18).FontColor(Colors.Blue.Medium);
                        col.Item().Text($"Mi Historial Personal de Reservas - Fecha de emisión: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(11).FontColor(Colors.Grey.Darken2);
                        col.Item().PaddingBottom(10);
                    });

                    // 4. Contenido Principal: La Tabla
                    page.Content().Table(table =>
                    {
                        // Definimos los anchos relativos de las columnas
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(40); // # Reserva
                            cols.RelativeColumn(2);  // Fecha
                            cols.RelativeColumn(4);  // Platillo
                            cols.RelativeColumn(1);  // Cantidad
                            cols.RelativeColumn(2);  // Total
                            cols.RelativeColumn(2);  // Pago
                        });

                        // Dibujamos la fila de encabezados
                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Blue.Medium).Padding(4).Text("#").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Medium).Padding(4).Text("Fecha").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Medium).Padding(4).Text("Platillo").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Medium).Padding(4).Text("Cant.").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Medium).Padding(4).Text("Total (Q)").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Medium).Padding(4).Text("Pago").FontColor(Colors.White).Bold();
                        });

                        // Llenamos las filas con los datos del usuario
                        foreach (var item in historial)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.ReservaId.ToString());
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.FechaConsumo.ToString("dd/MM/yy"));
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.NombrePlato);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.Cantidad.ToString());
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"Q {item.Cantidad * item.PrecioUnitario:F2}");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.FormaPago);
                        }
                    });

                    // 5. Pie de página (Paginación)
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Página ");
                        x.CurrentPageNumber();
                        x.Span(" de ");
                        x.TotalPages();
                    });
                });
            });

            // 6. Compilamos el documento a bytes y lo devolvemos
            return document.GeneratePdf();
        }

        // OBTIENE EL NIT DEL USUARIO PARA PRECARGARLO EN EL CARRITO
        public async Task<string> ObtenerNitUsuarioAsync(int usuarioId)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);

            // Si el usuario existe y tiene un NIT configurado, lo devuelve.
            // Si está vacío o nulo, devuelve "C/F" por defecto.
            if (usuario != null && !string.IsNullOrWhiteSpace(usuario.nit_facturacion))
            {
                return usuario.nit_facturacion;
            }

            return "C/F";
        }
    }
}