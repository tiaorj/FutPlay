using FutPlay.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FutPlay.Data
{
    public class AppDbContext : IdentityDbContext
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
        public DbSet<Palpite> Palpites { get; set; }
        public DbSet<Classificacao> Classificacoes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Campeonato>().ToTable("FutPlay_Campeonatos");
            modelBuilder.Entity<Time>().ToTable("FutPlay_Times");
            modelBuilder.Entity<Jogo>().ToTable("FutPlay_Jogos");
            modelBuilder.Entity<Liga>().ToTable("FutPlay_Ligas");
            modelBuilder.Entity<LigaParticipante>().ToTable("FutPlay_LigaParticipantes");
            modelBuilder.Entity<Palpite>().ToTable("FutPlay_Palpites");
            modelBuilder.Entity<Classificacao>().ToTable("FutPlay_Classificacoes");

            modelBuilder.Entity<Classificacao>()
                .HasOne(c => c.Campeonato)
                .WithMany()
                .HasForeignKey(c => c.CampeonatoId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Classificacao>()
                .HasOne(c => c.Time)
                .WithMany()
                .HasForeignKey(c => c.TimeId)
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

            modelBuilder.Entity<Liga>()
                .HasOne(l => l.Campeonato)
                .WithMany()
                .HasForeignKey(l => l.CampeonatoId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<LigaParticipante>()
                .HasOne(lp => lp.Liga)
                .WithMany()
                .HasForeignKey(lp => lp.LigaId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<LigaParticipante>()
                .HasOne(lp => lp.Usuario)
                .WithMany()
                .HasForeignKey(lp => lp.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Palpite>()
                .HasOne(p => p.Liga)
                .WithMany()
                .HasForeignKey(p => p.LigaId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Palpite>()
                .HasOne(p => p.LigaParticipante)
                .WithMany()
                .HasForeignKey(p => p.LigaParticipanteId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Palpite>()
                .HasOne(p => p.Jogo)
                .WithMany()
                .HasForeignKey(p => p.JogoId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
