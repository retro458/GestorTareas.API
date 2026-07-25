using GestorTareas.API.DTOs.Departamento;
using GestorTareas.API.Services;
using Microsoft.AspNetCore.Mvc;
using GestorTareas.API.Data;
using GestorTareas.API.Models;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.API.Services
{
    public class DepartamentoService : IDepartamentoService
    {
        private readonly AppDbContext _context;
        public DepartamentoService(AppDbContext context)
        {
            _context = context;
        }
    

    public async Task<DepartamentoResponseDto> CrearDepartamentoAsync(string nombre, string descripcion)
    {
        // Verificar si el departamento ya existe
        var departamentoExistente = await _context.Departamentos.FirstOrDefaultAsync(d => d.Nombre == nombre);
        if (departamentoExistente != null)
        {
            throw new Exception("El departamento ya existe.");
        }

        // Crear el nuevo departamento
        var nuevoDepartamento = new Departamento
        {
            Nombre = nombre,
            Descripcion = descripcion,
        };

        // Agregar el departamento a la base de datos
        _context.Departamentos.Add(nuevoDepartamento);
        await _context.SaveChangesAsync();

        return new DepartamentoResponseDto
        {
            Id = nuevoDepartamento.Id,
            Nombre = nuevoDepartamento.Nombre,
            Descripcion = nuevoDepartamento.Descripcion,
            Estado = nuevoDepartamento.Activo.GetValueOrDefault() ? "Activo" : "Inactivo"
        };
    }

    public async Task<IEnumerable<DepartamentoResponseDto>> ObtenerDepartamentosAsync()
    {
        var departamentos = await _context.Departamentos.ToListAsync();
        return departamentos.Select(d => new DepartamentoResponseDto
        {
            Id = d.Id,
            Nombre = d.Nombre,
            Descripcion = d.Descripcion
        });
    }
  }
}