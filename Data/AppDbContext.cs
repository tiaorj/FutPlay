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
        public DbSet<Time> Times { get; set; }
        public DbSet<Jogo> Jogos { get; set; }
        public DbSet<Liga> Ligas { get; set; }
        public DbSet<LigaParticipante> LigaParticipantes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Campeonato>().ToTable("FutPlay_Campeonatos");
            modelBuilder.Entity<Time>().ToTable("FutPlay_Times");
            modelBuilder.Entity<Jogo>().ToTable("FutPlay_Jogos");
            modelBuilder.Entity<Liga>().ToTable("FutPlay_Ligas");
            modelBuilder.Entity<LigaParticipante>().ToTable("FutPlay_LigaParticipantes");

            modelBuilder.Entity<LigaParticipante>()
                .HasOne(lp => lp.Liga)
                .WithMany()
                .HasForeignKey(lp => lp.LigaId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Liga>()
                .HasOne(l => l.Campeonato)
                .WithMany()
                .HasForeignKey(l => l.CampeonatoId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Jogo>()
                .HasOne(j => j.Campeonato)
                .WithMany()
                .HasForeignKey(j => j.CampeonatoId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Jogo>()
                .HasOne(j => j.TimeCasa)
                .WithMany()
                .HasForeignKey(j => j.TimeCasaId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Jogo>()
                .HasOne(j => j.TimeVisitante)
                .WithMany()
                .HasForeignKey(j => j.TimeVisitanteId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}