using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.API.Models;

public partial class HistorialTarea
{
    [Key]
    public int Id { get; set; }

    public int TareaId { get; set; }

    public int UsuarioId { get; set; }

    [StringLength(255)]
    public string Accion { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime? Fecha { get; set; }

    [ForeignKey("TareaId")]
    [InverseProperty("HistorialTareas")]
    public virtual Tarea Tarea { get; set; } = null!;

    [ForeignKey("UsuarioId")]
    [InverseProperty("HistorialTareas")]
    public virtual Usuario Usuario { get; set; } = null!;
}
