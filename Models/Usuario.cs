using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.API.Models;

[Index("Email", Name = "UQ__Usuarios__A9D105348B6ED798", IsUnique = true)]
public partial class Usuario
{
    [Key]
    public int Id { get; set; }

    [StringLength(150)]
    public string Nombre { get; set; } = null!;
    [StringLength(50)]
    public string? NombreUsuario {get;set;}
    [StringLength(150)]
    public string Email { get; set; } = null!;

    [StringLength(255)]
    public string PasswordHash { get; set; } = null!;

    public int? DepartamentoId { get; set; }

    public bool? Activo { get; set; }

    public bool? EmailVerificacion { get; set; }

    [StringLength(255)]
    public string? TokenVerificacion { get; set; }

    [Column("RolID")]
    public int? RolId { get; set; }

    [ForeignKey("DepartamentoId")]
    [InverseProperty("Usuarios")]
    public virtual Departamento? Departamento { get; set; }

    [InverseProperty("Usuario")]
    public virtual ICollection<HistorialTarea> HistorialTareas { get; set; } = new List<HistorialTarea>();

    [ForeignKey("RolId")]
    [InverseProperty("Usuarios")]
    public virtual Role? Rol { get; set; }

    [InverseProperty("AsignadoANavigation")]
    public virtual ICollection<Tarea> TareaAsignadoANavigations { get; set; } = new List<Tarea>();

    [InverseProperty("CreadoPorNavigation")]
    public virtual ICollection<Tarea> TareaCreadoPorNavigations { get; set; } = new List<Tarea>();
    public virtual ICollection<UsuariosDepartamentos> UsuariosDepartamentos {get;set;} = new List<UsuariosDepartamentos>();
    public virtual ICollection<Notificaciones> Notificaciones { get; set; } = new List<Notificaciones>();
}
