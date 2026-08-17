using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.API.Models;

public partial class ComentariosTarea
{
    public int Id {get;set;}
    public int TareaId {get;set;}
    public int UsuarioId{get;set;}
    public string Contenido{get;set;} = string.Empty;
    public DateTime FechaCreacion{get;set;}
    public DateTime? FechaEdicion{get;set;}

    // lado muchos esto solo a punto a un solo objeto, pues un Comentario
    // pertence a una tarea y usuario
    //
    public virtual Tarea Tarea {get;set;} = null!;
    public virtual Usuario Usuario {get;set;} = null!;

}
