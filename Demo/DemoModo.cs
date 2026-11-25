namespace GestorTareas.API.Demo;

// Interruptor del modo demo publico. Se lee una sola vez al arrancar desde
// la configuracion (variable de entorno Demo__Activo=true). Con false (por
// defecto) nada de la carpeta Demo/ cambia el comportamiento de la app.
public sealed class DemoModo
{
    public bool Activo { get; }

    public DemoModo(bool activo) => Activo = activo;

    public static DemoModo DesdeConfiguracion(IConfiguration configuracion)
    {
        // Program.cs carga el .env con DotNetEnv despues de construir la
        // configuracion, asi que tambien se mira la variable de entorno directa.
        var valor = configuracion["Demo:Activo"] ?? Environment.GetEnvironmentVariable("Demo__Activo");
        return new DemoModo(bool.TryParse(valor, out var activo) && activo);
    }
}
