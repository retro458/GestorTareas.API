namespace GestorTareas.API.DTOs.Usuarios;

    public class CrearUsuariosDto
    {
        public string? Nombre { get; set; }
        public string? NombreUsuario {get;set;}
        public string? Password { get; set; }
        public string? NombreRol { get; set; }
         public List<int> DepartamentosIds { get; set; } = new List<int>();
    }


    public class UsuarioResponseDto
    {
        public int Id { get; set; }
        public string? Nombre { get; set; }
        public string? NombreUsuario {get;set;}
        public string? NombreRol { get; set; }
        public List<DepartamentoResumenDto> Departamentos { get; set; } = new List<DepartamentoResumenDto>();
    }

    public class DepartamentoResumenDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}