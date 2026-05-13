using FutPlay.Models;
using Microsoft.EntityFrameworkCore;

namespace FutPlay.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Campeonato> Campeonatos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Campeonato>().ToTable("FutPlay_Campeonatos");
        }
    }
}