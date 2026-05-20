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
        public DbSet<Grupo> Grupos { get; set; }
        public DbSet<CampeonatoTime> CampeonatoTimes { get; set; }
        public DbSet<LigaConvite> LigaConvites { get; set; }
        public DbSet<TimeFavorito> TimeFavoritos { get; set; }
        public DbSet<CampeonatoFavorito> CampeonatoFavoritos { get; set; }

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
            modelBuilder.Entity<Grupo>().ToTable("FutPlay_Grupos");
            modelBuilder.Entity<CampeonatoTime>().ToTable("FutPlay_CampeonatoTimes");
            modelBuilder.Entity<LigaConvite>().ToTable("FutPlay_LigaConvites");
            modelBuilder.Entity<TimeFavorito>().ToTable("FutPlay_TimeFavoritos");
            modelBuilder.Entity<CampeonatoFavorito>().ToTable("FutPlay_CampeonatoFavoritos");

            modelBuilder.Entity<TimeFavorito>()
                .HasIndex(f => new { f.UserId, f.TimeId })
                .IsUnique();

            modelBuilder.Entity<TimeFavorito>()
                .HasIndex(f => f.TimeId);

            modelBuilder.Entity<CampeonatoFavorito>()
                .HasIndex(f => new { f.UserId, f.CampeonatoId })
                .IsUnique();

            modelBuilder.Entity<CampeonatoFavorito>()
                .HasIndex(f => f.CampeonatoId);

            modelBuilder.Entity<Grupo>()
                .HasIndex(g => g.CampeonatoId);

            modelBuilder.Entity<CampeonatoTime>()
                .HasIndex(ct => new { ct.CampeonatoId, ct.TimeId })
                .IsUnique()
                .HasDatabaseName("UX_FutPlay_CampeonatoTimes_Campeonato_Time");

            modelBuilder.Entity<CampeonatoTime>()
                .HasIndex(ct => ct.GrupoId);

            modelBuilder.Entity<LigaConvite>()
                .HasIndex(c => c.LigaId);

            modelBuilder.Entity<LigaConvite>()
                .HasIndex(c => c.Email);

            modelBuilder.Entity<LigaConvite>()
                .HasIndex(c => c.TokenConvite)
                .IsUnique();

            modelBuilder.Entity<LigaConvite>()
                .HasIndex(c => c.Status);

            modelBuilder.Entity<LigaParticipante>()
                .HasIndex(lp => lp.LigaId);

            modelBuilder.Entity<LigaParticipante>()
                .HasIndex(lp => lp.UserId);

            modelBuilder.Entity<LigaParticipante>()
                .HasIndex(lp => lp.Email);

            modelBuilder.Entity<Palpite>()
                .HasIndex(p => p.LigaId);

            modelBuilder.Entity<Palpite>()
                .HasIndex(p => p.LigaParticipanteId);

            modelBuilder.Entity<Palpite>()
                .HasIndex(p => p.JogoId);

            modelBuilder.Entity<Jogo>()
                .HasIndex(j => j.CampeonatoId);

            modelBuilder.Entity<Jogo>()
                .HasIndex(j => j.Status);

            modelBuilder.Entity<Jogo>()
                .HasIndex(j => j.ApiFixtureId);

            modelBuilder.Entity<Campeonato>()
                .HasIndex(c => c.ApiLeagueId);

            modelBuilder.Entity<Campeonato>()
                .Property(c => c.Formato)
                .HasDefaultValue(CampeonatoFormato.PontosCorridos);

            modelBuilder.Entity<Campeonato>()
                .HasIndex(c => c.Formato);

            modelBuilder.Entity<Time>()
                .HasIndex(t => t.ApiTeamId);

            modelBuilder.Entity<Classificacao>()
                .HasIndex(c => c.CampeonatoId);

            modelBuilder.Entity<Classificacao>()
                .HasIndex(c => c.TimeId);

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

            modelBuilder.Entity<Grupo>()
                .HasOne(g => g.Campeonato)
                .WithMany()
                .HasForeignKey(g => g.CampeonatoId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<CampeonatoTime>()
                .HasOne(ct => ct.Campeonato)
                .WithMany()
                .HasForeignKey(ct => ct.CampeonatoId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<CampeonatoTime>()
                .HasOne(ct => ct.Time)
                .WithMany()
                .HasForeignKey(ct => ct.TimeId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<CampeonatoTime>()
                .HasOne(ct => ct.Grupo)
                .WithMany(g => g.CampeonatoTimes)
                .HasForeignKey(ct => ct.GrupoId)
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

            modelBuilder.Entity<TimeFavorito>()
                .HasOne(f => f.Usuario)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TimeFavorito>()
                .HasOne(f => f.Time)
                .WithMany()
                .HasForeignKey(f => f.TimeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CampeonatoFavorito>()
                .HasOne(f => f.Usuario)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CampeonatoFavorito>()
                .HasOne(f => f.Campeonato)
                .WithMany()
                .HasForeignKey(f => f.CampeonatoId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
