using Microsoft.EntityFrameworkCore;
using MovieAPI.Models;

namespace MovieAPI.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) 
    : DbContext(options)
{

    public DbSet<Movie> Movies => Set<Movie>();
    
    public DbSet<SeatingPlan> SeatingPlans => Set<SeatingPlan>();
    
    public DbSet<Auditorium> Auditoriums => Set<Auditorium>();
    
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<IdempotencyRecord>()
            .HasIndex(i => i.ExpiresAt);

        modelBuilder.Entity<Auditorium>()
            .HasOne(a => a.SeatingPlan)
            .WithMany(s => s.Auditoriums)
            .HasForeignKey(a => a.SeatingPlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
    
}
