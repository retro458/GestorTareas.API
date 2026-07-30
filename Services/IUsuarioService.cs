using GestorTareas.API.DTOs.Usuarios;
using GestorTareas.API.Models;

namespace GestorTareas.API.Services;

    public interface IUsuarioService
    {
        Task<UsuarioResponseDto> RegisterAsync(string nombre, string email, string password, string nombreRol, string departamento,
        string rolCreador, int? departamentoCreadorId);
        Task<UsuarioResponseDto> GetUsuarioByIdAsync(int id);
        Task<IEnumerable<UsuarioResponseDto>> GetEmpleadosPorDepartamentoAsync(int departamentoId,int usuarioActualId);
        Task<IEnumerable<UsuarioResponseDto>> GetTodosLosEmpleadosAsync();
    }
