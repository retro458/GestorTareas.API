using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.API.Models;

public partial class Departamento
{
    [Key]
    public int Id { get; set; }

    [StringLength(100)]
    public string Nombre { get; set; } = null!;

    [StringLength(255)]
    public string? Descripcion { get; set; }

    public bool? Activo { get; set; }

    [InverseProperty("Departamento")]
    public virtual ICollection<Tarea> Tareas { get; set; } = new List<Tarea>();

    [InverseProperty("Departamento")]
    public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();

    public virtual ICollection<UsuariosDepartamentos> UsuariosDepartamentos {get;set;} = new List<UsuariosDepartamentos>();
}
