using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.API.Models;

[Table("ROLES")]
public partial class Role
{
    [Key]
    [Column("RolID")]
    public int RolId { get; set; }

    [Column("nombre_rol")]
    [StringLength(50)]
    [Unicode(false)]
    public string NombreRol { get; set; } = null!;

    [Column("descripcion_rol")]
    [StringLength(255)]
    [Unicode(false)]
    public string? DescripcionRol { get; set; }

    [InverseProperty("Rol")]
    public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
