using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.API.Models;

public partial class Notificaciones
{
    [Key]
    public int Id { get; set; }

    public int? UsuarioId { get; set; }
    public int? TareaId { get; set; }
    public string Mensaje { get; set; } = null!;
    public bool Leida { get; set; }
    public DateTime FechaCreacion { get; set; }
    public virtual Tarea? Tarea { get; set; }
    public virtual Usuario? Usuario { get; set; }
}