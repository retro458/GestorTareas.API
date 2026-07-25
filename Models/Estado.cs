using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.API.Models;

[Table("ESTADOS")]
[Index("NombreEstado", Name = "UQ_ESTADOS_NOMBRE", IsUnique = true)]
[Index("NombreEstado", Name = "UQ__ESTADOS__2F8C63751CC433ED", IsUnique = true)]
public partial class Estado
{
    [Key]
    [Column("EstadoID")]
    public int EstadoId { get; set; }

    [Column("nombre_estado")]
    [StringLength(50)]
    [Unicode(false)]
    public string NombreEstado { get; set; } = null!;

    [Column("descripcion_estado")]
    [StringLength(255)]
    [Unicode(false)]
    public string? DescripcionEstado { get; set; }

    public int Orden { get; set; }

    public bool EsEstadoInicial { get; set; }

    public bool Activo { get; set; }

    [InverseProperty("Estado")]
    public virtual ICollection<Tarea> Tareas { get; set; } = new List<Tarea>();
}
