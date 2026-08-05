using GestorTareas.API.DTOs.Usuarios;
using GestorTareas.API.Models;

namespace GestorTareas.API.Services;

    public interface IUsuarioService
    {
        Task<UsuarioResponseDto> RegisterAsync(string nombre, string nombreUsuario, string password, string nombreRol, List<int> departamentosIds, string rolCreador, List<int> departamentosCreadorIds);
        Task<UsuarioResponseDto> GetUsuarioByIdAsync(int id);
        Task<IEnumerable<UsuarioResponseDto>> GetEmpleadosPorDepartamentoAsync(List<int> departamentosIds,int usuarioActualId);
        Task<IEnumerable<UsuarioResponseDto>> GetTodosLosEmpleadosAsync();
    }
