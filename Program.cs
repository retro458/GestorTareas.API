using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Scalar.AspNetCore;
using GestorTareas.API.Data;
using Microsoft.EntityFrameworkCore;
using GestorTareas.API.Hubs;
var builder = WebApplication.CreateBuilder(args);

// ============================================================
// 1. CARGAR VARIABLES DE ENTORNO (.env)
// ============================================================
Env.Load();

var connectionString = Env.GetString("DB_CONNECTION_STRING");
var jwtSecretKey = Env.GetString("JWT_SECRET_KEY");
var jwtIssuer = Env.GetString("JWT_ISSUER");
var jwtAudience = Env.GetString("JWT_AUDIENCE");

if (string.IsNullOrEmpty(jwtSecretKey))
    throw new Exception("JWT_SECRET_KEY no está configurada en el archivo .env");
if (string.IsNullOrEmpty(connectionString))
    throw new Exception("DB_CONNECTION_STRING no está configurada en el archivo .env");
builder.Configuration["ConnectionStrings:DB_CONNECTION_STRING"] = connectionString;

// ============================================================
// 2. CORS
// ============================================================
var AllowVueApp = "_allowVueApp";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: AllowVueApp,
        policy =>
        {
            policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); // necesario para que viajen las cookies HttpOnly
        });
});

// ============================================================
// 3. AUTENTICACIÓN JWT (cookie HttpOnly + fallback a header Authorization)
// ============================================================
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // En desarrollo local sin HTTPS, evita que falle por metadata HTTPS
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
            ClockSkew = TimeSpan.FromMinutes(1) // por defecto son 5 min de margen; lo acortamos un poco
        };

        // Si existe la cookie, se usa. Si no, JwtBearerHandler ya revisa
        // automáticamente el header "Authorization: Bearer ..." sin código extra
        // (esto es lo que permite que el botón "Authorize" de Swagger funcione).
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.ContainsKey("X-Access-Token"))
                {
                    context.Token = context.Request.Cookies["X-Access-Token"];
                }

                var path = context.HttpContext.Request.Path;
                if(string.IsNullOrEmpty(context.Token) && path.StartsWithSegments("/hubs"))
                {
                    var accessToken = context.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        context.Token = accessToken;
                    }
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

// ============================================================
// 4. SWAGGER (una sola vez, con seguridad JWT configurada)
// ============================================================

builder.Services.AddOpenApi(c =>
{
    c.AddDocumentTransformer((document,context,cancellationToken) =>
    {
        document.Info.Title = "API Gestor de Tareas";
        document.Info.Version = "v1";
        document.Info.Description = "API backend para la gestión de tareas departamentales";
  
        document.Components ??= new Microsoft.OpenApi.Models.OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, OpenApiSecurityScheme>();
        
    var securityScheme = new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "token JWT. No es necesario escribir 'Bearer ' antes."
    };
    document.Components.SecuritySchemes.Add("Bearer", securityScheme);

    var requirement = new OpenApiSecurityRequirement
    {
        { securityScheme, new string[] { } }
    };
    document.SecurityRequirements = new List<OpenApiSecurityRequirement> { requirement };

    return Task.CompletedTask;
    });
});

// ============================================================
// SignalR Hub
// ============================================================
builder.Services.AddSignalR();
// ============================================================
// 5. DbContext 
// ============================================================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// ============================================================
// 6. Servicios propios
// ============================================================
builder.Services.AddScoped<GestorTareas.API.Services.IAuthService, GestorTareas.API.Services.AuthService>();
builder.Services.AddScoped<GestorTareas.API.Services.IUsuarioService, GestorTareas.API.Services.UsuarioService>();
builder.Services.AddScoped<GestorTareas.API.Services.IDepartamentoService, GestorTareas.API.Services.DepartamentoService>();
builder.Services.AddScoped<GestorTareas.API.Services.ITareaService, GestorTareas.API.Services.TareaService>();
builder.Services.AddScoped<GestorTareas.API.Services.INotificacionService, GestorTareas.API.Services.NotificacionService>();
var app = builder.Build();


//===========================================================
// para insertar usuario maestro(admin) en la base de datos
// ============================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();

        // 1. LEER CREDENCIALES DESDE EL .ENV
        var adminEmail = Environment.GetEnvironmentVariable("SEED_ADMIN_EMAIL");
        var adminPassword = Environment.GetEnvironmentVariable("SEED_ADMIN_PASSWORD");

        if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminPassword))
        {
            throw new InvalidOperationException("Las variables SEED_ADMIN_EMAIL o SEED_ADMIN_PASSWORD no están configuradas en el archivo .env");
        }

        // 2. Verificar si el usuario administrador ya existe en la base de datos
        if (!context.Usuarios.Any(u => u.Email == adminEmail))
        {
            // 3. Buscar el rol "Jefe" que ya existe
            var adminRole = context.Roles.FirstOrDefault(r => r.NombreRol == "Jefe");

            if (adminRole == null)
            {
                throw new Exception("El rol 'Jefe' no se encontró en la base de datos. Asegúrate de que los roles preexistentes estén cargados.");
            }

            // 4. Crear el usuario administrador con el rol "Jefe"
            var nuevoAdmin = new GestorTareas.API.Models.Usuario
            {
                Email = adminEmail,
                Nombre = "Administrador Global",
                // Encriptación segura con BCrypt utilizando el valor del .env
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword), 
                RolId = adminRole.RolId,
                Activo = true
            };

            context.Usuarios.Add(nuevoAdmin);
            context.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error al verificar o inyectar el usuario semilla basado en roles existentes.");
    }
}


// 6. MIDDLEWARE PIPELINE
// ============================================================
if (app.Environment.IsDevelopment())
{
   app.MapOpenApi();
   app.MapScalarApiReference(options =>
   {
      options.WithOpenApiRoutePattern("/openapi/v1.json");
   });
}

app.UseHttpsRedirection();

// CORS debe ir antes de Authentication/Authorization
app.UseCors(AllowVueApp);

app.UseAuthentication();
app.UseStaticFiles();
app.UseAuthorization();

app.MapControllers();
app.MapHub<TareasHub>("/hubs/tareas");
app.Run();