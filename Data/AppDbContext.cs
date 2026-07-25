using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using GestorTareas.API.Models;  

namespace GestorTareas.API.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Departamento> Departamentos { get; set; }

    public virtual DbSet<Estado> Estados { get; set; }

    public virtual DbSet<HistorialTarea> HistorialTareas { get; set; }

    public virtual DbSet<Prioridad> Prioridads { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Tarea> Tareas { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    public virtual DbSet<Notificaciones> Notificaciones { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Departamento>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Departam__3214EC0790089CE0");

            entity.Property(e => e.Activo).HasDefaultValue(true);
        });

        modelBuilder.Entity<Estado>(entity =>
        {
            entity.HasKey(e => e.EstadoId).HasName("PK__ESTADOS__FEF86B608B41A75F");

            entity.HasIndex(e => e.EsEstadoInicial, "UQ_ESTADO_INICIAL")
                .IsUnique()
                .HasFilter("([EsEstadoInicial]=(1))");

            entity.Property(e => e.Activo).HasDefaultValue(true);
        });

        modelBuilder.Entity<HistorialTarea>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Historia__3214EC07E5144D3B");

            entity.Property(e => e.Fecha).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Tarea).WithMany(p => p.HistorialTareas)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Historial__Tarea__5AEE82B9");

            entity.HasOne(d => d.Usuario).WithMany(p => p.HistorialTareas)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Historial__Usuar__5BE2A6F2");
        });

        modelBuilder.Entity<Prioridad>(entity =>
        {
            entity.HasKey(e => e.PrioridadId).HasName("PK__Priorida__393917CEA5DE0C6B");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RolId).HasName("PK__ROLES__F92302D1CBE65EEB");
        });

        modelBuilder.Entity<Tarea>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Tareas__3214EC07042BD65D");

            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.PrioridadId).HasDefaultValue(2);

            entity.HasOne(d => d.AsignadoANavigation).WithMany(p => p.TareaAsignadoANavigations).HasConstraintName("FK__Tareas__Asignado__5629CD9C");

            entity.HasOne(d => d.CreadoPorNavigation).WithMany(p => p.TareaCreadoPorNavigations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tareas__CreadoPo__571DF1D5");

            entity.HasOne(d => d.Departamento).WithMany(p => p.Tareas)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tareas__Departam__5535A963");

            entity.HasOne(d => d.Estado).WithMany(p => p.Tareas)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tareas_Estados");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Usuarios__3214EC070CAAE474");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.EmailVerificacion).HasDefaultValue(false);

            entity.HasOne(d => d.Departamento).WithMany(p => p.Usuarios).HasConstraintName("FK__Usuarios__Depart__4F7CD00D");

            entity.HasOne(d => d.Rol).WithMany(p => p.Usuarios).HasConstraintName("FK__Usuarios__RolID__6FE99F9F");
        });

        modelBuilder.Entity<Notificaciones>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Notificac__3214EC07CBBB7C9B");
            entity.HasOne(d => d.Tarea).WithMany(p => p.Notificaciones)
                .HasForeignKey(d => d.TareaId)
                .HasConstraintName("FK__Notificaci__IdTar__6C190EBB");
            entity.HasOne(d => d.Usuario).WithMany(p => p.Notificaciones)
                .HasForeignKey(d => d.UsuarioId)
                .HasConstraintName("FK__Notificaci__IdUsu__6D0D32F4");
            entity.Property(e => e.Leida).HasDefaultValue(false);

            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getdate())");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
