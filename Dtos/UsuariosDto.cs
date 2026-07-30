namespace GestorTareas.API.DTOs.Usuarios;

    public class CrearUsuariosDto
    {
        public string? Nombre { get; set; }
        public string? NombreUsuario {get;set;}
        public string? Password { get; set; }
        public string? NombreRol { get; set; }
        public string? Departamento { get; set; }
    }


    public class UsuarioResponseDto
    {
        public int Id { get; set; }
        public string? Nombre { get; set; }
        public string? NombreUsuario {get;set;}
        public string? NombreRol { get; set; }
        public string? Departamento { get; set; }
    }
