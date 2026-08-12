// DIRECTIVAS DE IMPORTACIÓN (Van siempre al inicio del archivo)
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
    public class AdminService : IAdminService
    {
        private readonly ApplicationDbContext _context;

        public AdminService(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. OBTENER TODOS LOS USUARIOS (Ordenados predeterminadamente de la A a la Z por Nombre)
        public async Task<List<UsuarioAdminDTO>> ObtenerTodosLosUsuariosAsync()
        {
            return await _context.Usuarios
                .Include(u => u.Rol)
                .OrderBy(u => u.nombre) // ORDENAMIENTO PREDETERMINADO A-Z POR NOMBRE COMPLETO
                .Select(u => new UsuarioAdminDTO
                {
                    Id = u.id,
                    Nombre = u.nombre,
                    Email = u.email,
                    RolId = u.rol_id,
                    NombreRol = u.Rol!.nombre,
                    Activo = u.activo,
                    NitFacturacion = u.nit_facturacion,
                    MaxAlmuerzosPermitidos = u.Rol.max_almuerzos,
                    FechaCreacion = u.fecha_creacion // Mantiene visible la fecha de registro
                })
                .ToListAsync();
        }

        // 2. OBTENER USUARIO POR ID
        public async Task<UsuarioEdicionDTO?> ObtenerUsuarioPorIdAsync(int id)
        {
            var user = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.id == id);

            if (user == null) return null;

            return new UsuarioEdicionDTO
            {
                Id = user.id,
                Nombre = user.nombre,
                Email = user.email,
                RolId = user.rol_id,
                Activo = user.activo,
                NitFacturacion = user.nit_facturacion,
                MaxAlmuerzos = user.Rol != null ? user.Rol.max_almuerzos : 2
            };
        }

        // 3. GUARDAR O ACTUALIZAR USUARIO
        public async Task<(bool Exito, string Mensaje)> GuardarUsuarioAsync(UsuarioEdicionDTO dto)
        {
            bool correoExiste = await _context.Usuarios
                .AnyAsync(u => u.email == dto.Email && u.id != dto.Id);

            if (correoExiste)
            {
                return (false, "El correo electrónico ya está registrado por otro usuario.");
            }

            if (dto.Id == 0)
            {
                if (string.IsNullOrWhiteSpace(dto.Password))
                {
                    return (false, "La contraseña es obligatoria para nuevos usuarios.");
                }

                var nuevoUsuario = new Usuario
                {
                    nombre = dto.Nombre,
                    email = dto.Email,
                    password = dto.Password,
                    rol_id = dto.RolId,
                    activo = dto.Activo,
                    nit_facturacion = string.IsNullOrWhiteSpace(dto.NitFacturacion) ? "C/F" : dto.NitFacturacion.Trim(),
                    fecha_creacion = DateTime.Now
                };

                _context.Usuarios.Add(nuevoUsuario);
            }
            else
            {
                var usuarioExistente = await _context.Usuarios.FindAsync(dto.Id);
                if (usuarioExistente == null) return (false, "El usuario no existe.");

                usuarioExistente.nombre = dto.Nombre;
                usuarioExistente.email = dto.Email;
                usuarioExistente.rol_id = dto.RolId;
                usuarioExistente.activo = dto.Activo;
                usuarioExistente.nit_facturacion = string.IsNullOrWhiteSpace(dto.NitFacturacion) ? "C/F" : dto.NitFacturacion.Trim();

                if (!string.IsNullOrWhiteSpace(dto.Password))
                {
                    usuarioExistente.password = dto.Password;
                }

                var rolAsociado = await _context.Roles.FindAsync(dto.RolId);
                if (rolAsociado != null)
                {
                    rolAsociado.max_almuerzos = dto.MaxAlmuerzos;
                }
            }

            await _context.SaveChangesAsync();
            return (true, "Usuario y límites guardados correctamente.");
        }

        // 4. CAMBIAR ESTADO ACTIVO/INACTIVO
        public async Task<bool> CambiarEstadoUsuarioAsync(int id, bool activo)
        {
            var user = await _context.Usuarios.FindAsync(id);
            if (user == null) return false;

            user.activo = activo;
            await _context.SaveChangesAsync();
            return true;
        }

        // 5. OBTENER LISTA DE ROLES
        public async Task<List<Rol>> ObtenerRolesAsync()
        {
            return await _context.Roles.ToListAsync();
        }

        // 6. MÉTRICAS PARA DASHBOARD
        public async Task<DashboardDTO> ObtenerMétricasDashboardAsync()
        {
            var hoy = DateTime.Today;

            var totalUsuarios = await _context.Usuarios.CountAsync();
            var usuariosActivos = await _context.Usuarios.CountAsync(u => u.activo);
            var usuariosInactivos = totalUsuarios - usuariosActivos;

            var reservasHoy = await _context.Reservas
                .Where(r => r.fecha_consumo.Date == hoy)
                .Include(r => r.MenuDiario)
                .ToListAsync();

            var totalReservasHoy = reservasHoy.Sum(r => r.cantidad);
            var activasHoy = reservasHoy.Where(r => r.estado == "Activa").Sum(r => r.cantidad);
            var canceladasHoy = reservasHoy.Where(r => r.estado == "Cancelada").Sum(r => r.cantidad);

            var montoRecaudadoHoy = reservasHoy
                .Where(r => r.estado == "Activa" && r.MenuDiario != null)
                .Sum(r => r.cantidad * r.MenuDiario!.precio);

            var masVendidoGroup = reservasHoy
                .Where(r => r.estado == "Activa")
                .GroupBy(r => r.MenuDiario!.nombre_plato)
                .OrderByDescending(g => g.Sum(r => r.cantidad))
                .FirstOrDefault();

            string masVendidoNombre = masVendidoGroup != null ? masVendidoGroup.Key : "Sin ventas";
            int masVendidoCant = masVendidoGroup != null ? masVendidoGroup.Sum(r => r.cantidad) : 0;

            var stockDisponiblesHoy = await _context.MenusDiarios
                .Where(m => m.fecha.Date == hoy && m.estado == "Disponible")
                .SumAsync(m => m.stock);

            return new DashboardDTO
            {
                TotalUsuarios = totalUsuarios,
                UsuariosActivos = usuariosActivos,
                UsuariosInactivos = usuariosInactivos,
                TotalReservasHoy = totalReservasHoy,
                ReservasActivasHoy = activasHoy,
                ReservasCanceladasHoy = canceladasHoy,
                TotalMontoRecaudadoHoy = montoRecaudadoHoy,
                PlatilloMasVendido = masVendidoNombre,
                CantidadPlatilloMasVendido = masVendidoCant,
                TotalPlatillosDisponibles = stockDisponiblesHoy
            };
        }

        // 7. DETALLE COMPLETO DE USUARIO
        public async Task<DetalleUsuarioCompletoDTO?> ObtenerDetalleCompletoUsuarioAsync(int usuarioId)
        {
            var usuarios = await ObtenerTodosLosUsuariosAsync();
            var userInfo = usuarios.FirstOrDefault(u => u.Id == usuarioId);

            if (userInfo == null) return null;

            var reservas = await _context.Reservas
                .Where(r => r.usuario_id == usuarioId)
                .Include(r => r.MenuDiario)
                .Include(r => r.FormaPago)
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

            decimal totalGastado = reservas.Where(r => r.Estado == "Activa").Sum(r => r.TotalPagado);
            int totalPlatillos = reservas.Where(r => r.Estado == "Activa").Sum(r => r.Cantidad);
            int totalCanceladas = reservas.Count(r => r.Estado == "Cancelada");

            return new DetalleUsuarioCompletoDTO
            {
                InfoUsuario = userInfo,
                HistorialReservas = reservas,
                TotalGastadoAcumulado = totalGastado,
                TotalPlatillosReservados = totalPlatillos,
                TotalReservasCanceladas = totalCanceladas
            };
        }

        // 8. REPORTE EXCEL GLOBAL (Ordenado por Nombre A-Z y luego por Fecha Descendente)
        public async Task<byte[]> GenerarReporteGlobalExcelAsync(FiltroReporteAdminDTO filtro)
        {
            var query = _context.Reservas
                .Include(r => r.Usuario)
                .Include(r => r.MenuDiario)
                .Include(r => r.FormaPago)
                .AsQueryable();

            if (filtro.FechaInicio.HasValue)
                query = query.Where(r => r.fecha_consumo.Date >= filtro.FechaInicio.Value.Date);

            if (filtro.FechaFin.HasValue)
                query = query.Where(r => r.fecha_consumo.Date <= filtro.FechaFin.Value.Date);

            if (filtro.UsuarioId.HasValue && filtro.UsuarioId > 0)
                query = query.Where(r => r.usuario_id == filtro.UsuarioId.Value);

            if (!string.IsNullOrWhiteSpace(filtro.Estado) && filtro.Estado != "Todos")
                query = query.Where(r => r.estado == filtro.Estado);

            if (filtro.MenuId.HasValue && filtro.MenuId > 0)
                query = query.Where(r => r.menu_id == filtro.MenuId.Value);

            // CAMBIO CLAVE DE ORDENAMIENTO: Primero Nombre del Usuario (A-Z), luego Fecha Reserva
            var lista = await query
                .OrderBy(r => r.Usuario!.nombre)
                .ThenByDescending(r => r.fecha_reserva)
                .ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Reporte General Reservas");

                worksheet.Cell(1, 1).Value = "# Reserva";
                worksheet.Cell(1, 2).Value = "Fecha Reserva";
                worksheet.Cell(1, 3).Value = "Fecha Consumo";
                worksheet.Cell(1, 4).Value = "Usuario / Empleado (A-Z)";
                worksheet.Cell(1, 5).Value = "Platillo";
                worksheet.Cell(1, 6).Value = "Cantidad";
                worksheet.Cell(1, 7).Value = "Precio Unit.";
                worksheet.Cell(1, 8).Value = "Total (Q)";
                worksheet.Cell(1, 9).Value = "Forma Pago";
                worksheet.Cell(1, 10).Value = "NIT";
                worksheet.Cell(1, 11).Value = "Estado";

                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#0D6EFD");
                headerRow.Style.Font.FontColor = XLColor.White;

                int row = 2;
                foreach (var item in lista)
                {
                    worksheet.Cell(row, 1).Value = item.id;
                    worksheet.Cell(row, 2).Value = item.fecha_reserva.ToString("dd/MM/yyyy HH:mm");
                    worksheet.Cell(row, 3).Value = item.fecha_consumo.ToString("dd/MM/yyyy");
                    worksheet.Cell(row, 4).Value = item.Usuario!.nombre;
                    worksheet.Cell(row, 5).Value = item.MenuDiario!.nombre_plato;
                    worksheet.Cell(row, 6).Value = item.cantidad;
                    worksheet.Cell(row, 7).Value = item.MenuDiario.precio;
                    worksheet.Cell(row, 8).Value = item.cantidad * item.MenuDiario.precio;
                    worksheet.Cell(row, 9).Value = item.FormaPago!.nombre;
                    worksheet.Cell(row, 10).Value = item.nit_facturacion;
                    worksheet.Cell(row, 11).Value = item.estado;
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

        // 9. REPORTE PDF GLOBAL (Ordenado por Nombre A-Z y luego por Fecha Descendente)
        public async Task<byte[]> GenerarReporteGlobalPdfAsync(FiltroReporteAdminDTO filtro)
        {
            var query = _context.Reservas
                .Include(r => r.Usuario)
                .Include(r => r.MenuDiario)
                .Include(r => r.FormaPago)
                .AsQueryable();

            if (filtro.FechaInicio.HasValue)
                query = query.Where(r => r.fecha_consumo.Date >= filtro.FechaInicio.Value.Date);

            if (filtro.FechaFin.HasValue)
                query = query.Where(r => r.fecha_consumo.Date <= filtro.FechaFin.Value.Date);

            if (filtro.UsuarioId.HasValue && filtro.UsuarioId > 0)
                query = query.Where(r => r.usuario_id == filtro.UsuarioId.Value);

            if (!string.IsNullOrWhiteSpace(filtro.Estado) && filtro.Estado != "Todos")
                query = query.Where(r => r.estado == filtro.Estado);

            // CAMBIO CLAVE DE ORDENAMIENTO: Primero Nombre del Usuario (A-Z), luego Fecha Reserva
            var lista = await query
                .OrderBy(r => r.Usuario!.nombre)
                .ThenByDescending(r => r.fecha_reserva)
                .ToListAsync();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("RESTAURANTE ESCUELA INTECAP").Bold().FontSize(16).FontColor(Colors.Blue.Medium);
                        col.Item().Text($"Reporte General de Administración - Generado: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(10).FontColor(Colors.Grey.Darken2);
                    });

                    page.Content().PaddingVertical(10).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(25);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(3);
                            cols.RelativeColumn(3);
                            cols.ConstantColumn(30);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Blue.Medium).Padding(3).Text("#").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Medium).Padding(3).Text("Fecha").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Medium).Padding(3).Text("Usuario (A-Z)").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Medium).Padding(3).Text("Platillo").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Medium).Padding(3).Text("Cant").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Medium).Padding(3).Text("Total").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Medium).Padding(3).Text("Estado").FontColor(Colors.White).Bold();
                        });

                        foreach (var item in lista)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(item.id.ToString());
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(item.fecha_consumo.ToString("dd/MM/yy"));
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(item.Usuario!.nombre);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(item.MenuDiario!.nombre_plato);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(item.cantidad.ToString());
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"Q {item.cantidad * item.MenuDiario.precio:F2}");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(item.estado);
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

        // 10. GENERAR EXCEL DE USUARIOS
        public async Task<byte[]> GenerarExcelUsuariosAsync()
        {
            var usuarios = await ObtenerTodosLosUsuariosAsync(); // Ya viene ordenado por Nombre A-Z desde la función 1

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Padrón de Usuarios");

                worksheet.Cell(1, 1).Value = "# ID";
                worksheet.Cell(1, 2).Value = "Nombre Completo (A-Z)";
                worksheet.Cell(1, 3).Value = "Correo Electrónico";
                worksheet.Cell(1, 4).Value = "Rol Asignado";
                worksheet.Cell(1, 5).Value = "Límite Almuerzos";
                worksheet.Cell(1, 6).Value = "NIT Predeterminado";
                worksheet.Cell(1, 7).Value = "Estado";
                worksheet.Cell(1, 8).Value = "Fecha Creación";

                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#198754");
                headerRow.Style.Font.FontColor = XLColor.White;

                int row = 2;
                foreach (var u in usuarios)
                {
                    worksheet.Cell(row, 1).Value = u.Id;
                    worksheet.Cell(row, 2).Value = u.Nombre;
                    worksheet.Cell(row, 3).Value = u.Email;
                    worksheet.Cell(row, 4).Value = u.NombreRol;
                    worksheet.Cell(row, 5).Value = u.MaxAlmuerzosPermitidos == 0 ? "Ilimitado" : u.MaxAlmuerzosPermitidos.ToString();
                    worksheet.Cell(row, 6).Value = u.NitFacturacion;
                    worksheet.Cell(row, 7).Value = u.Activo ? "Activo" : "Inactivo";
                    worksheet.Cell(row, 8).Value = u.FechaCreacion.ToString("dd/MM/yyyy HH:mm");
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

        // 11. GENERAR PDF DE USUARIOS
        public async Task<byte[]> GenerarPdfUsuariosAsync()
        {
            var usuarios = await ObtenerTodosLosUsuariosAsync(); // Ya viene ordenado por Nombre A-Z desde la función 1

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("RESTAURANTE ESCUELA INTECAP").Bold().FontSize(16).FontColor(Colors.Blue.Medium);
                        col.Item().Text($"Padrón General de Usuarios del Sistema - Generado: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(10).FontColor(Colors.Grey.Darken2);
                    });

                    page.Content().PaddingVertical(10).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(25);
                            cols.RelativeColumn(3);
                            cols.RelativeColumn(3);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Blue.Medium).Padding(4).Text("#").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Medium).Padding(4).Text("Nombre (A-Z)").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Medium).Padding(4).Text("Correo").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Medium).Padding(4).Text("Rol").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Medium).Padding(4).Text("NIT").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Medium).Padding(4).Text("Estado").FontColor(Colors.White).Bold();
                        });

                        foreach (var u in usuarios)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(u.Id.ToString());
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(u.Nombre);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(u.Email);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(u.NombreRol);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(u.NitFacturacion);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(u.Activo ? "Activo" : "Inactivo");
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