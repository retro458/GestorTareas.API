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
            Activo = nuevoDepartamento.Activo.GetValueOrDefault()
        };
    }

    public async Task<IEnumerable<DepartamentoResponseDto>> ObtenerMisDepartamentosAsync(int usuarioId)
    {
        // Devuelve solo los departamentos activos a los que el usuario autenticado pertenece.
        // Sirve para que Empleados (y cualquier rol) puedan resolver nombres de sus propios departamentos.
        var departamentos = await _context.UsuariosDepartamentos
            .Where(ud => ud.UsuarioId == usuarioId && ud.Departamento!.Activo == true)
            .Select(ud => new DepartamentoResponseDto
            {
                Id = ud.Departamento!.Id,
                Nombre = ud.Departamento.Nombre,
                Descripcion = ud.Departamento.Descripcion,
                Activo = ud.Departamento.Activo.GetValueOrDefault()
            })
            .OrderBy(d => d.Nombre)
            .ToListAsync();

        return departamentos;
    }

    public async Task<IEnumerable<DepartamentoResponseDto>> ObtenerDepartamentosAsync()
    {
        var departamentos = await _context.Departamentos.ToListAsync();
        return departamentos.Select(d => new DepartamentoResponseDto
        {
            Id = d.Id,
            Nombre = d.Nombre,
            Descripcion = d.Descripcion,
            Activo = d.Activo.GetValueOrDefault()
        });
    }

   public async Task CambiarEstadoActivoAsync(int departamentoId, bool nuevoEstado)
{
    var departamento = await _context.Departamentos.FindAsync(departamentoId)
        ?? throw new Exception("El departamento no existe.");

    departamento.Activo = nuevoEstado;
    await _context.SaveChangesAsync();
}

public async Task<IEnumerable<DepartamentoResponseDto>> ObtenerDepartamentosInactivosAsync()
{
    var departamentos = await _context.Departamentos
        .Where(d => d.Activo == false)
        .OrderBy(d => d.Nombre)
        .ToListAsync();

    return departamentos.Select(d => new DepartamentoResponseDto
    {
        Id = d.Id,
        Nombre = d.Nombre,
        Descripcion = d.Descripcion,
        Activo = d.Activo.GetValueOrDefault()
    });
}
    public async Task<DepartamentoResponseDto> EditarDepartamentoAsync(int id, string nombre, string? descripcion)
{
    var departamento = await _context.Departamentos.FindAsync(id)
        ?? throw new Exception("El departamento no existe.");

    // que no choque con el nombre de otro departamento (excluyendose a si mismo)
    var nombreDuplicado = await _context.Departamentos
        .AnyAsync(d => d.Id != id && d.Nombre == nombre);
    if (nombreDuplicado)
        throw new Exception("Ya existe otro departamento con ese nombre.");

    departamento.Nombre = nombre;
    departamento.Descripcion = descripcion;
    await _context.SaveChangesAsync();

    return new DepartamentoResponseDto
    {
        Id = departamento.Id,
        Nombre = departamento.Nombre,
        Descripcion = departamento.Descripcion,
        Activo = departamento.Activo.GetValueOrDefault()
    };
}


  }
}
