// Importamos los espacios de nombres necesarios para la base de datos y la generación de documentos
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RestauranteIntecapWeb_MLMG.Data;
using RestauranteIntecapWeb_MLMG.Models;
using RestauranteIntecapWeb_MLMG.Models.DTOs;

namespace RestauranteIntecapWeb_MLMG.Services
{
    public class CocinaService : ICocinaService
    {
        private readonly ApplicationDbContext _context;

        // Configuración estática para la licencia comunitaria de QuestPDF
        static CocinaService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        // Constructor que recibe el DbContext mediante Inyección de Dependencias
        public CocinaService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Obtiene la lista de platillos registrados para una fecha específica
        public async Task<List<MenuDiario>> ObtenerMenusPorFechaAsync(DateTime fecha)
        {
            return await _context.MenusDiarios
                .Where(m => m.fecha.Date == fecha.Date)
                .ToListAsync();
        }

        // Obtiene un platillo individual por su ID
        public async Task<MenuDiario?> ObtenerMenuPorIdAsync(int id)
        {
            return await _context.MenusDiarios.FindAsync(id);
        }

        // Agrega un nuevo menú o actualiza las propiedades de uno existente
        public async Task<bool> ActualizarMenuAsync(MenuDiario menu)
        {
            if (menu.id == 0)
            {
                // Nuevo registro
                _context.MenusDiarios.Add(menu);
            }
            else
            {
                // Edición de registro existente
                var existente = await _context.MenusDiarios.FindAsync(menu.id);
                if (existente == null) return false;

                existente.nombre_plato = menu.nombre_plato;
                existente.descripcion = menu.descripcion;
                existente.precio = menu.precio;
                existente.stock = menu.stock;
                existente.es_dieta = menu.es_dieta;
                existente.estado = menu.estado;

                if (!string.IsNullOrEmpty(menu.imagen_url))
                {
                    existente.imagen_url = menu.imagen_url;
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        // Cambia el estado (Disponible / Agotado / Inactivo - Soft Delete)
        public async Task<bool> CambiarEstadoMenuAsync(int id, string nuevoEstado)
        {
            var menu = await _context.MenusDiarios.FindAsync(id);
            if (menu == null) return false;

            menu.estado = nuevoEstado;
            await _context.SaveChangesAsync();
            return true;
        }

        // Elimina un platillo de la BD sólo si no posee reservas asociadas
        public async Task<(bool Exito, string Mensaje)> EliminarMenuSinReservasAsync(int id)
        {
            bool tieneReservas = await _context.Reservas.AnyAsync(r => r.menu_id == id);

            if (tieneReservas)
            {
                return (false, "No se puede eliminar el platillo porque ya cuenta con reservas registradas. Cambie su estado a Inactivo.");
            }

            var menu = await _context.MenusDiarios.FindAsync(id);
            if (menu == null) return (false, "El platillo especificado no existe.");

            _context.MenusDiarios.Remove(menu);
            await _context.SaveChangesAsync();
            return (true, "Platillo eliminado exitosamente.");
        }

        // Obtiene el detalle de personas que reservaron ordenado alfabéticamente
        public async Task<List<ReservaDetalleDTO>> ObtenerReservasDetalladasPorFechaAsync(DateTime fecha)
        {
            return await _context.Reservas
                .Where(r => r.fecha_consumo.Date == fecha.Date && r.estado == "Activa")
                .Include(r => r.Usuario)
                .Include(r => r.MenuDiario)
                .Include(r => r.FormaPago)
                .OrderBy(r => r.Usuario!.nombre)
                .Select(r => new ReservaDetalleDTO
                {
                    ReservaId = r.id,
                    NombreEmpleado = r.Usuario!.nombre,
                    EmailEmpleado = r.Usuario.email,
                    NombrePlato = r.MenuDiario!.nombre_plato,
                    Cantidad = r.cantidad,
                    DondeConsume = r.donde_consume,
                    FormaPago = r.FormaPago!.nombre,
                    FechaReserva = r.fecha_reserva,
                    Estado = r.estado
                })
                .ToListAsync();
        }

        // Agrupa las solicitudes por platillo para calcular el recuento total
        public async Task<List<PlatilloConsolidadoDTO>> ObtenerConsolidadoPorFechaAsync(DateTime fecha)
        {
            return await _context.Reservas
                .Where(r => r.fecha_consumo.Date == fecha.Date && r.estado == "Activa")
                .GroupBy(r => new { r.menu_id, r.MenuDiario!.nombre_plato, r.MenuDiario.precio, r.MenuDiario.es_dieta })
                .Select(g => new PlatilloConsolidadoDTO
                {
                    MenuId = g.Key.menu_id,
                    NombrePlato = g.Key.nombre_plato,
                    Precio = g.Key.precio,
                    EsDieta = g.Key.es_dieta,
                    TotalSolicitado = g.Sum(r => r.cantidad),
                    TotalRecaudado = g.Sum(r => r.cantidad * g.Key.precio)
                })
                .ToListAsync();
        }

        // Genera el reporte en Excel (.xlsx) con ClosedXML
        public async Task<byte[]> GenerarReporteExcelReservasAsync(DateTime fecha)
        {
            var detalles = await ObtenerReservasDetalladasPorFechaAsync(fecha);

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Reservas del Día");

                worksheet.Cell(1, 1).Value = "#";
                worksheet.Cell(1, 2).Value = "Empleado";
                worksheet.Cell(1, 3).Value = "Plato Elegido";
                worksheet.Cell(1, 4).Value = "Cantidad";
                worksheet.Cell(1, 5).Value = "¿Dónde consume?";
                worksheet.Cell(1, 6).Value = "Forma de Pago";
                worksheet.Cell(1, 7).Value = "Hora Reserva";

                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#0D6EFD");
                headerRow.Style.Font.FontColor = XLColor.White;

                int row = 2;
                foreach (var item in detalles)
                {
                    worksheet.Cell(row, 1).Value = item.ReservaId;
                    worksheet.Cell(row, 2).Value = item.NombreEmpleado;
                    worksheet.Cell(row, 3).Value = item.NombrePlato;
                    worksheet.Cell(row, 4).Value = item.Cantidad;
                    worksheet.Cell(row, 5).Value = item.DondeConsume;
                    worksheet.Cell(row, 6).Value = item.FormaPago;
                    worksheet.Cell(row, 7).Value = item.FechaReserva.ToString("HH:mm:ss");
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

        // Genera el documento PDF utilizando QuestPDF
        public async Task<byte[]> GenerarReportePdfReservasAsync(DateTime fecha)
        {
            var reservas = await ObtenerReservasDetalladasPorFechaAsync(fecha);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("RESTAURANTE ESCUELA INTECAP").Bold().FontSize(16).FontColor(Colors.Blue.Medium);
                        col.Item().Text($"Reporte de Reservas del Día - {fecha:dd/MM/yyyy}").FontSize(12).FontColor(Colors.Grey.Darken2);
                    });

                    page.Content().PaddingVertical(10).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(25);
                            cols.RelativeColumn(3);
                            cols.RelativeColumn(3);
                            cols.ConstantColumn(35);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Blue.Medium).Padding(4).Text("#").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Medium).Padding(4).Text("Empleado").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Medium).Padding(4).Text("Platillo").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Medium).Padding(4).Text("Cant").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Medium).Padding(4).Text("Consumo").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Medium).Padding(4).Text("Pago").FontColor(Colors.White).Bold();
                        });

                        int i = 1;
                        foreach (var res in reservas)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(i++.ToString());
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(res.NombreEmpleado);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(res.NombrePlato);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(res.Cantidad.ToString());
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(res.DondeConsume);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(res.FormaPago);
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