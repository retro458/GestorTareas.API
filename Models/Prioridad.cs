using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.API.Models;

[Table("Prioridad")]
[Index("NombrePrioridad", Name = "UQ_PRIORIDAD_NOMBRE", IsUnique = true)]
[Index("NombrePrioridad", Name = "UQ__Priorida__C81D80B627AE0058", IsUnique = true)]
public partial class Prioridad
{
    [Key]
    [Column("PrioridadID")]
    public int PrioridadId { get; set; }

    [Column("nombre_prioridad")]
    [StringLength(50)]
    [Unicode(false)]
    public string NombrePrioridad { get; set; } = null!;

    public int Orden { get; set; }

    [InverseProperty("Prioridad")]
    public virtual ICollection<Tarea> Tareas { get; set; } = new List<Tarea>();
}
