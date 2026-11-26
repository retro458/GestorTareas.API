using GestorTareas.API.Data;
using GestorTareas.API.Models;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.API.Demo;

// ============================================================
// Datos ficticios del modo demo ("Comercial El Roble").
// ============================================================
// Nada de aqui sale del cliente real. Todas las fechas son relativas al dia
// en que se siembra (vence manana, vencio hace 3 dias...), para que la demo
// siempre se vea actual. Sembrar dos veces el mismo dia deja exactamente los
// mismos datos e Ids (solo cambian los hashes de contrasena, que llevan sal).
public static class DemoSembrador
{
    // Cuentas con acceso, que la pantalla de entrada muestra con su contrasena.
    public static readonly IReadOnlyList<DemoCuenta> Cuentas = new[]
    {
        new DemoCuenta("jefe.demo", "Jefe2026!", "Jefe", "Laura Méndez"),
        new DemoCuenta("encargado.demo", "Encargado2026!", "Encargado Departamento", "Carlos Rivas"),
        new DemoCuenta("empleado.demo", "Empleado2026!", "Empleado", "Ana Torres"),
    };

    public static bool EsCuentaProtegida(string? nombreUsuario) =>
        nombreUsuario is not null && Cuentas.Any(c => c.Usuario == nombreUsuario);

    // Empleados sin acceso (contrasena aleatoria que nadie conoce), solo para
    // que las listas no se vean vacias.
    private static readonly (string Usuario, string Nombre, string Depto)[] EmpleadosSinAcceso =
    {
        ("dfuentes", "Diego Fuentes", "Contabilidad"),
        ("snavarro", "Sofía Navarro", "Auditoría"),
        ("mcastillo", "Mario Castillo", "Auditoría"),
        ("pherrera", "Paola Herrera", "Administración"),
        ("jortiz", "Javier Ortiz", "Administración"),
    };

    public static int UsuariosSembrados => Cuentas.Count + EmpleadosSinAcceso.Length;

    // ------------------------------------------------------------
    // Arranque: crea las tablas si la base esta vacia y siembra si no hay datos.
    // La base (y su login/usuario) ya deben existir en el servidor.
    // ------------------------------------------------------------
    public static async Task PrepararBaseAsync(AppDbContext db, ILogger logger)
    {
        // EnsureCreated solo crea el esquema si la base no tiene ninguna tabla;
        // si ya tiene tablas no toca nada.
        if (await db.Database.EnsureCreatedAsync())
            logger.LogInformation("Demo: la base no tenia tablas; esquema creado.");

        if (await db.Usuarios.AnyAsync())
            return;

        logger.LogInformation("Demo: base sin datos; sembrando la demo.");
        await ReiniciarAsync(db, logger);
    }

    // ------------------------------------------------------------
    // demo-reiniciar: borra TODOS los datos y vuelve a sembrar.
    // ------------------------------------------------------------
    public static async Task ReiniciarAsync(AppDbContext db, ILogger logger)
    {
        await db.Database.EnsureCreatedAsync();

        await using var transaccion = await db.Database.BeginTransactionAsync();

        // Hijos antes que padres, por las llaves foraneas.
        await db.ComentariosTarea.ExecuteDeleteAsync();
        await db.Notificaciones.ExecuteDeleteAsync();
        await db.HistorialTareas.ExecuteDeleteAsync();
        await db.Tareas.ExecuteDeleteAsync();
        await db.UsuariosDepartamentos.ExecuteDeleteAsync();
        await db.Usuarios.ExecuteDeleteAsync();
        await db.Departamentos.ExecuteDeleteAsync();
        await db.Estados.ExecuteDeleteAsync();
        await db.Prioridads.ExecuteDeleteAsync();
        await db.Roles.ExecuteDeleteAsync();

        await ReiniciarIdentidadesAsync(db);

        db.ChangeTracker.Clear();
        await SembrarAsync(db, DateTime.UtcNow.Date);

        await transaccion.CommitAsync();
        logger.LogInformation("Demo: datos reiniciados.");
    }

    // Deja los contadores IDENTITY en cero para que los Ids sean siempre los
    // mismos (1, 2, 3...). En una tabla recien creada que nunca tuvo filas no
    // se toca: ahi el primer Id ya es 1 y un RESEED 0 lo dejaria en 0.
    private static async Task ReiniciarIdentidadesAsync(AppDbContext db)
    {
        foreach (var entidad in db.Model.GetEntityTypes())
        {
            var tabla = entidad.GetTableName();
            if (tabla is null) continue;
            var nombre = $"[{entidad.GetSchema() ?? "dbo"}].[{tabla}]";

            // Los nombres salen del modelo de EF (constantes del codigo), no de
            // datos del usuario.
#pragma warning disable EF1002
            await db.Database.ExecuteSqlRawAsync($@"
                IF EXISTS (SELECT 1 FROM sys.identity_columns
                           WHERE object_id = OBJECT_ID(N'{nombre}') AND last_value IS NOT NULL)
                    DBCC CHECKIDENT (N'{nombre}', RESEED, 0) WITH NO_INFOMSGS;");
#pragma warning restore EF1002
        }
    }

    private static async Task SembrarAsync(AppDbContext db, DateTime hoy)
    {
        // Fecha/hora relativa al dia de la siembra (UTC).
        DateTime Dia(int dias, int hora = 15) => hoy.AddDays(dias).AddHours(hora);

        // --- Catalogos (mismos nombres que usa el codigo de la app) ---
        var roles = new Dictionary<string, Role>
        {
            ["Jefe"] = new() { NombreRol = "Jefe", DescripcionRol = "Administra toda la empresa" },
            ["Encargado Departamento"] = new() { NombreRol = "Encargado Departamento", DescripcionRol = "Administra sus departamentos" },
            ["Empleado"] = new() { NombreRol = "Empleado", DescripcionRol = "Trabaja sus tareas asignadas" },
        };
        foreach (var r in roles.Values) { db.Roles.Add(r); await db.SaveChangesAsync(); }

        var estados = new Dictionary<string, Estado>();
        var definicionEstados = new[] { "Pendiente", "En Progreso", "En Revisión", "Completada", "Cancelada" };
        for (var i = 0; i < definicionEstados.Length; i++)
        {
            var e = new Estado { NombreEstado = definicionEstados[i], Orden = i + 1, EsEstadoInicial = i == 0, Activo = true };
            estados[e.NombreEstado] = e;
            db.Estados.Add(e);
            await db.SaveChangesAsync(); // uno por uno: el orden de insercion fija los Ids
        }

        // El front usa Baja=1, Media=2, Alta=3.
        var prioridades = new Dictionary<string, Prioridad>();
        var definicionPrioridades = new[] { "Baja", "Media", "Alta" };
        for (var i = 0; i < definicionPrioridades.Length; i++)
        {
            var p = new Prioridad { NombrePrioridad = definicionPrioridades[i], Orden = i + 1 };
            prioridades[p.NombrePrioridad] = p;
            db.Prioridads.Add(p);
            await db.SaveChangesAsync();
        }

        var deptos = new Dictionary<string, Departamento>();
        foreach (var (nombre, descripcion) in new[]
        {
            ("Contabilidad", "Registros contables, cierres y conciliaciones"),
            ("Auditoría", "Revisión interna de procesos y controles"),
            ("Administración", "Compras, proveedores y servicios generales"),
        })
        {
            var d = new Departamento { Nombre = nombre, Descripcion = descripcion, Activo = true };
            deptos[nombre] = d;
            db.Departamentos.Add(d);
            await db.SaveChangesAsync();
        }

        // --- Usuarios ---
        var usuarios = new Dictionary<string, Usuario>();
        async Task<Usuario> CrearUsuario(string usuario, string nombre, string rol, string password, params string[] departamentos)
        {
            var u = new Usuario
            {
                NombreUsuario = usuario,
                Nombre = nombre,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                RolId = roles[rol].RolId,
                Activo = true,
            };
            db.Usuarios.Add(u);
            await db.SaveChangesAsync();
            foreach (var d in departamentos)
                db.UsuariosDepartamentos.Add(new UsuariosDepartamentos { UsuarioId = u.Id, DepartamentoId = deptos[d].Id });
            await db.SaveChangesAsync();
            usuarios[usuario] = u;
            return u;
        }

        var jefe = await CrearUsuario("jefe.demo", "Laura Méndez", "Jefe", Cuentas[0].Password);
        var encargado = await CrearUsuario("encargado.demo", "Carlos Rivas", "Encargado Departamento", Cuentas[1].Password, "Contabilidad");
        var empleado = await CrearUsuario("empleado.demo", "Ana Torres", "Empleado", Cuentas[2].Password, "Contabilidad");
        foreach (var (u, nombre, depto) in EmpleadosSinAcceso)
            await CrearUsuario(u, nombre, "Empleado", Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24)), depto);

        // --- Tareas ---
        // (titulo, descripcion, depto, asignado, creador, estado, prioridad, creadaHaceDias, venceEnDias)
        // venceEnDias negativo = ya vencio; null = sin fecha limite.
        var definicionTareas = new (string Titulo, string Descripcion, string Depto, string Asignado, string Creador, string Estado, string Prioridad, int CreadaHace, int? VenceEn)[]
        {
            // Contabilidad: la ve el encargado.demo, y el empleado.demo tiene varias.
            ("Conciliación bancaria de octubre", "Cuadrar los movimientos de la cuenta corriente con el estado de cuenta del banco.", "Contabilidad", "empleado.demo", "encargado.demo", "En Progreso", "Alta", 6, 1),
            ("Registrar facturas de proveedores", "Ingresar al sistema las facturas recibidas esta semana.", "Contabilidad", "empleado.demo", "encargado.demo", "Pendiente", "Media", 2, 3),
            ("Revisar retenciones de renta", "Verificar que las retenciones del mes estén bien calculadas.", "Contabilidad", "empleado.demo", "encargado.demo", "Pendiente", "Alta", 8, -3),
            ("Archivar comprobantes de caja chica", "Escanear y archivar los comprobantes del último trimestre.", "Contabilidad", "empleado.demo", "encargado.demo", "Pendiente", "Baja", 1, 10),
            ("Preparar declaración de IVA", "Consolidar ventas y compras del mes para la declaración.", "Contabilidad", "empleado.demo", "encargado.demo", "En Revisión", "Alta", 9, 2),
            ("Cuadre de cuentas por cobrar", "Comparar el auxiliar de clientes con el mayor general.", "Contabilidad", "empleado.demo", "encargado.demo", "Completada", "Media", 14, -7),
            ("Actualizar catálogo de cuentas", "Agregar las subcuentas nuevas pedidas por auditoría.", "Contabilidad", "empleado.demo", "jefe.demo", "Completada", "Baja", 20, -12),
            ("Depreciación de activos fijos", "Calcular la depreciación mensual de mobiliario y equipo.", "Contabilidad", "empleado.demo", "encargado.demo", "Cancelada", "Baja", 18, -5),
            ("Cierre contable del mes", "Revisar asientos de ajuste y emitir los estados financieros.", "Contabilidad", "encargado.demo", "jefe.demo", "Pendiente", "Alta", 3, 5),
            ("Presentar informe a gerencia", "Resumen de resultados del trimestre para la reunión mensual.", "Contabilidad", "encargado.demo", "jefe.demo", "En Progreso", "Media", 5, 0),
            ("Revisar planilla de horas extra", "Validar las horas extra reportadas antes del pago.", "Contabilidad", "encargado.demo", "jefe.demo", "Completada", "Media", 12, -6),
            ("Registrar pagos a proveedores", "Aplicar los pagos de la semana en cuentas por pagar.", "Contabilidad", "dfuentes", "encargado.demo", "En Progreso", "Media", 4, 2),
            ("Conciliar tarjeta corporativa", "Verificar cargos de la tarjeta contra los recibos entregados.", "Contabilidad", "dfuentes", "encargado.demo", "Pendiente", "Media", 3, -1),
            ("Ajustar inventario contable", "Registrar las diferencias del último conteo físico.", "Contabilidad", "dfuentes", "encargado.demo", "Completada", "Alta", 16, -9),
            ("Calcular provisión de aguinaldo", "Estimar la provisión acumulada al cierre del mes.", "Contabilidad", "dfuentes", "encargado.demo", "En Revisión", "Media", 7, 4),

            // Auditoria
            ("Auditoría de caja chica", "Arqueo sorpresa y revisión de comprobantes.", "Auditoría", "snavarro", "jefe.demo", "En Progreso", "Alta", 4, 2),
            ("Revisar control de accesos", "Verificar que solo el personal autorizado tenga acceso al sistema contable.", "Auditoría", "snavarro", "jefe.demo", "Pendiente", "Media", 2, 7),
            ("Informe de hallazgos del trimestre", "Redactar el informe con las observaciones encontradas.", "Auditoría", "mcastillo", "jefe.demo", "Pendiente", "Alta", 6, -2),
            ("Muestreo de facturas de compra", "Seleccionar una muestra y verificar soportes.", "Auditoría", "mcastillo", "jefe.demo", "Completada", "Media", 15, -8),
            ("Seguimiento de recomendaciones", "Confirmar que se corrigieron las observaciones anteriores.", "Auditoría", "snavarro", "jefe.demo", "Completada", "Baja", 21, -14),
            ("Revisión de contratos vigentes", "Validar fechas y montos de los contratos con proveedores.", "Auditoría", "mcastillo", "jefe.demo", "En Revisión", "Media", 8, 3),
            ("Evaluar riesgos de inventario", "Análisis de riesgo sobre bodegas y existencias.", "Auditoría", "snavarro", "jefe.demo", "Cancelada", "Baja", 13, -4),

            // Administracion
            ("Cotizar papelería del semestre", "Pedir al menos tres cotizaciones a proveedores distintos.", "Administración", "pherrera", "jefe.demo", "Pendiente", "Baja", 1, 6),
            ("Renovar póliza de seguro", "Comparar ofertas y renovar la póliza de la flota.", "Administración", "jortiz", "jefe.demo", "En Progreso", "Alta", 5, 1),
            ("Mantenimiento de aires acondicionados", "Coordinar la visita del técnico en las tres oficinas.", "Administración", "jortiz", "jefe.demo", "Pendiente", "Media", 3, -2),
            ("Actualizar directorio de proveedores", "Depurar proveedores inactivos y completar datos de contacto.", "Administración", "pherrera", "jefe.demo", "Completada", "Baja", 17, -10),
            ("Organizar inventario de mobiliario", "Etiquetar y registrar sillas, escritorios y archivos.", "Administración", "pherrera", "jefe.demo", "En Progreso", "Media", 6, 4),
            ("Gestionar pago de servicios básicos", "Pagar agua, luz e internet antes del vencimiento.", "Administración", "jortiz", "jefe.demo", "Completada", "Alta", 10, -5),
            ("Contratar servicio de limpieza", "Evaluar propuestas para el nuevo contrato anual.", "Administración", "pherrera", "jefe.demo", "Cancelada", "Media", 11, -3),
            ("Plan de compras del próximo año", "Borrador con las necesidades de cada departamento.", "Administración", "jortiz", "jefe.demo", "Pendiente", "Media", 2, null),
        };

        // Camino de estados que recorrio cada tarea hasta su estado actual.
        static string[] Camino(string estadoFinal) => estadoFinal switch
        {
            "En Progreso" => new[] { "En Progreso" },
            "En Revisión" => new[] { "En Progreso", "En Revisión" },
            "Completada" => new[] { "En Progreso", "En Revisión", "Completada" },
            "Cancelada" => new[] { "Cancelada" },
            _ => Array.Empty<string>(),
        };

        var tareas = new List<Tarea>();
        foreach (var t in definicionTareas)
        {
            var creada = Dia(-t.CreadaHace, 14);
            var tarea = new Tarea
            {
                Titulo = t.Titulo,
                Descripcion = t.Descripcion,
                DepartamentoId = deptos[t.Depto].Id,
                AsignadoA = usuarios[t.Asignado].Id,
                CreadoPor = usuarios[t.Creador].Id,
                EstadoId = estados[t.Estado].EstadoId,
                PrioridadId = prioridades[t.Prioridad].PrioridadId,
                FechaCreacion = creada,
                FechaVencimiento = t.VenceEn.HasValue ? hoy.AddDays(t.VenceEn.Value) : null,
            };
            db.Tareas.Add(tarea);
            await db.SaveChangesAsync();
            tareas.Add(tarea);

            // Historial coherente con el estado actual, repartido entre la
            // creacion y hoy (la ultima transicion de una completada cae
            // varios dias atras: "se completo la semana pasada").
            db.HistorialTareas.Add(new HistorialTarea
            {
                TareaId = tarea.Id,
                UsuarioId = tarea.CreadoPor!.Value,
                Accion = $"Tarea creada y asignada a {usuarios[t.Asignado].Nombre}.",
                Fecha = creada,
            });

            var pasos = Camino(t.Estado);
            var anterior = "Pendiente";
            var finCamino = t.Estado == "Completada" && t.VenceEn.HasValue ? Math.Max(-t.CreadaHace + 1, t.VenceEn.Value) : 0;
            for (var i = 0; i < pasos.Length; i++)
            {
                // Dias repartidos de forma uniforme entre la creacion y finCamino.
                var dia = -t.CreadaHace + (int)Math.Round((finCamino + t.CreadaHace) * (i + 1.0) / (pasos.Length + 1));
                var quien = pasos[i] == "Cancelada" ? usuarios[t.Creador] : usuarios[t.Asignado];
                db.HistorialTareas.Add(new HistorialTarea
                {
                    TareaId = tarea.Id,
                    UsuarioId = quien.Id,
                    Accion = $"Estado cambiado de '{anterior}' a '{pasos[i]}'.",
                    Fecha = Dia(dia, 10 + i),
                });
                anterior = pasos[i];
            }
            await db.SaveChangesAsync();
        }

        Tarea Tarea(string titulo) => tareas.Single(x => x.Titulo == titulo);

        // Una reasignacion, para que el historial muestre ese tipo de evento.
        db.HistorialTareas.Add(new HistorialTarea
        {
            TareaId = Tarea("Registrar pagos a proveedores").Id,
            UsuarioId = encargado.Id,
            Accion = $"Tarea reasignada de {empleado.Nombre} a {usuarios["dfuentes"].Nombre}.",
            Fecha = Dia(-3, 9),
        });
        db.HistorialTareas.Add(new HistorialTarea
        {
            TareaId = Tarea("Preparar declaración de IVA").Id,
            UsuarioId = encargado.Id,
            Accion = "Tarea editada",
            Fecha = Dia(-5, 11),
        });

        // --- Comentarios ---
        var comentarios = new (string Tarea, string Autor, string Texto, int HaceDias, int Hora)[]
        {
            ("Conciliación bancaria de octubre", "encargado.demo", "Ana, prioriza las partidas mayores a $500; el resto puede ir la próxima semana.", 4, 9),
            ("Conciliación bancaria de octubre", "empleado.demo", "Entendido. Encontré dos depósitos sin identificar, los estoy consultando con el banco.", 3, 11),
            ("Conciliación bancaria de octubre", "encargado.demo", "Perfecto, avísame cuando tengas respuesta.", 3, 15),
            ("Revisar retenciones de renta", "encargado.demo", "Esta ya venció, por favor revísala hoy.", 1, 8),
            ("Preparar declaración de IVA", "empleado.demo", "Lista para revisión. Adjunté el detalle en la carpeta compartida.", 2, 16),
            ("Preparar declaración de IVA", "encargado.demo", "Gracias, la reviso mañana a primera hora.", 2, 17),
            ("Cuadre de cuentas por cobrar", "encargado.demo", "Buen trabajo, cuadró a la primera.", 7, 10),
            ("Depreciación de activos fijos", "jefe.demo", "La cancelamos: el cálculo lo hará el despacho externo este año.", 5, 12),
            ("Cierre contable del mes", "jefe.demo", "Carlos, necesito los estados financieros antes del día 5.", 2, 9),
            ("Presentar informe a gerencia", "jefe.demo", "Incluye la comparación con el trimestre anterior, por favor.", 3, 14),
            ("Conciliar tarjeta corporativa", "encargado.demo", "Faltan dos recibos de combustible; pídelos a Administración.", 1, 10),
            ("Auditoría de caja chica", "jefe.demo", "Sofía, el arqueo debe hacerse sin aviso previo.", 3, 8),
            ("Informe de hallazgos del trimestre", "jefe.demo", "Mario, ¿cómo vas con el informe? Ya pasó la fecha.", 1, 9),
            ("Renovar póliza de seguro", "jefe.demo", "Javier, compara al menos con dos aseguradoras más.", 4, 11),
            ("Contratar servicio de limpieza", "jefe.demo", "Se pospone para el próximo año por presupuesto.", 3, 15),
        };
        foreach (var c in comentarios)
        {
            db.ComentariosTarea.Add(new ComentariosTarea
            {
                TareaId = Tarea(c.Tarea).Id,
                UsuarioId = usuarios[c.Autor].Id,
                Contenido = c.Texto,
                FechaCreacion = Dia(-c.HaceDias, c.Hora),
            });
        }
        await db.SaveChangesAsync();

        // --- Notificaciones sin leer para las cuentas de demo ---
        var notificaciones = new (string Usuario, string Tarea, string Mensaje, int HaceDias)[]
        {
            ("empleado.demo", "Registrar facturas de proveedores", "Se te asignó una nueva tarea: 'Registrar facturas de proveedores'.", 2),
            ("empleado.demo", "Archivar comprobantes de caja chica", "Se te asignó una nueva tarea: 'Archivar comprobantes de caja chica'.", 1),
            ("empleado.demo", "Registrar pagos a proveedores", "Tu tarea 'Registrar pagos a proveedores' fue reasignada a Diego Fuentes.", 3),
            ("encargado.demo", "Cierre contable del mes", "Se te asignó una nueva tarea: 'Cierre contable del mes'.", 3),
            ("encargado.demo", "Preparar declaración de IVA", "Ana Torres cambió el estado de 'Preparar declaración de IVA' a 'En Revisión'.", 2),
        };
        foreach (var n in notificaciones)
        {
            db.Notificaciones.Add(new Notificaciones
            {
                UsuarioId = usuarios[n.Usuario].Id,
                TareaId = Tarea(n.Tarea).Id,
                Mensaje = n.Mensaje,
                Leida = false,
                FechaCreacion = Dia(-n.HaceDias, 9),
            });
        }
        await db.SaveChangesAsync();
    }
}

public sealed record DemoCuenta(string Usuario, string Password, string Rol, string Nombre);
