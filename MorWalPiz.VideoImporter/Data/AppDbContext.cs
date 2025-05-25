using Microsoft.EntityFrameworkCore;
using MorWalPiz.VideoImporter.Models;
using System.IO;

namespace MorWalPiz.VideoImporter.Data
{
  public class AppDbContext : DbContext
  {
    public DbSet<Language> Languages { get; set; }
    public DbSet<Disclaimer> Disclaimers { get; set; }
    public DbSet<Settings> Settings { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      // Configura il database SQLite, salvandolo nella cartella dell'applicazione
      string dbPath = Path.Combine(Directory.GetCurrentDirectory(), "VideoImporter.db");
      optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);

      // Configurazione della relazione tra Language e Disclaimer
      modelBuilder.Entity<Language>()
          .HasMany(l => l.Disclaimers)
          .WithOne(d => d.Language)
          .HasForeignKey(d => d.LanguageId)
          .OnDelete(DeleteBehavior.Cascade);

      // Configurazione per garantire che ci sia solo una lingua predefinita
      modelBuilder.Entity<Language>()
          .HasIndex(l => l.IsDefault)
          .HasFilter("[IsDefault] = 1")
          .IsUnique();

      // Dati predefiniti per le lingue
      modelBuilder.Entity<Language>().HasData(
          new Language { Id = 1, Code = "it", Name = "Italiano", IsDefault = true, IsSelected = true },
          new Language { Id = 2, Code = "en", Name = "English", IsDefault = false, IsSelected = true },
          new Language { Id = 3, Code = "fr", Name = "Français", IsDefault = false, IsSelected = false },
          new Language { Id = 4, Code = "de", Name = "Deutsch", IsDefault = false, IsSelected = false },
          new Language { Id = 5, Code = "es", Name = "Español", IsDefault = false, IsSelected = true }
      );

      // Configurazione per Settings
      modelBuilder.Entity<Settings>().HasData(
          new Settings { Id = 1, DefaultHashtags = "#video #hashtag", ApiEndpoint = "https://localhost:7221" , DefaultPublishTime = new System.TimeSpan(12, 0, 0) }
      );
    }
  }
}