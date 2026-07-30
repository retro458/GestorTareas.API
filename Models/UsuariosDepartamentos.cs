using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.API.Models;

public partial class UsuariosDepartamentos
{
    [Key]
    public int Id {get;set;}
    public int UsuarioId {get;set;}
    public int DepartamentoId {get;set;}

    public virtual Usuario? Usuarios {get;set;} 
    public virtual Departamento? Departamento {get;set;}
}