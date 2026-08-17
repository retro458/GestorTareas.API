using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.API.Models;

[Index("AsignadoA", Name = "IX_Tareas_AsignadoA")]
[Index("DepartamentoId", Name = "IX_Tareas_Departamento")]
public partial class Tarea
{
    [Key]
    public int Id { get; set; }

    [StringLength(200)]
    public string Titulo { get; set; } = null!;

    public string? Descripcion { get; set; }

    public int? DepartamentoId { get; set; }

    public int? AsignadoA { get; set; }

    public int? CreadoPor { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? FechaCreacion { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? FechaVencimiento { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? FechaFinalizacion { get; set; }

    public int EstadoId { get; set; }

    public int PrioridadId { get; set; }

    [ForeignKey("AsignadoA")]
    [InverseProperty("TareaAsignadoANavigations")]
    public virtual Usuario? AsignadoANavigation { get; set; }

    [ForeignKey("CreadoPor")]
    [InverseProperty("TareaCreadoPorNavigations")]
    public virtual Usuario CreadoPorNavigation { get; set; } = null!;

    [ForeignKey("DepartamentoId")]
    [InverseProperty("Tareas")]
    public virtual Departamento Departamento { get; set; } = null!;

    [ForeignKey("EstadoId")]
    [InverseProperty("Tareas")]
    public virtual Estado Estado { get; set; } = null!;

    [ForeignKey("PrioridadId")]
    [InverseProperty("Tareas")]
    public virtual Prioridad Prioridad { get; set; } = null!;

    
    //coleccion de comentarios
    //
    public virtual ICollection<ComentariosTarea> ComentariosTarea {get;set;} = new List<ComentariosTarea>();
    public virtual ICollection<HistorialTarea> HistorialTareas { get; set; } = new List<HistorialTarea>();

    public virtual ICollection<Notificaciones> Notificaciones { get; set; } = new List<Notificaciones>();
}
