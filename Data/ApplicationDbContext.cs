using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using HospitalFlow.Models;

namespace HospitalFlow.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
{
    public DbSet<Room> Rooms { get; set; }
    public DbSet<Equipment> Equipments { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Equipment>(entity =>
        {
            entity.HasOne(d => d.Room)
                  .WithMany(p => p.Equipments)
                  .HasForeignKey(d => d.RoomId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }
}