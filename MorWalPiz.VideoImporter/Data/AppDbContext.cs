using Microsoft.EntityFrameworkCore;
using MorWalPiz.VideoImporter.Models;
using MorWalPiz.VideoImporter.Services;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MorWalPiz.VideoImporter.Data
{
  public class AppDbContext : DbContext
  {
    private readonly ITenantContext _tenantContext;

    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<Language> Languages { get; set; }
    public DbSet<Disclaimer> Disclaimers { get; set; }
    public DbSet<Settings> Settings { get; set; }
    public DbSet<PublishSchedule> PublishSchedules { get; set; }

    public AppDbContext()
    {
    }

    public AppDbContext(ITenantContext tenantContext)
    {
      _tenantContext = tenantContext;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      // Configura il database SQLite, salvandolo nella cartella dell'applicazione
      string dbPath = Path.Combine(Directory.GetCurrentDirectory(), "VideoImporter.db");
      optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);

      // Global Query Filters for Multi-Tenancy
      if (_tenantContext != null)
      {
        modelBuilder.Entity<Language>()
            .HasQueryFilter(l => l.TenantId == _tenantContext.CurrentTenantId);

        modelBuilder.Entity<Disclaimer>()
            .HasQueryFilter(d => d.TenantId == _tenantContext.CurrentTenantId);

        modelBuilder.Entity<Settings>()
            .HasQueryFilter(s => s.TenantId == _tenantContext.CurrentTenantId);

        modelBuilder.Entity<PublishSchedule>()
            .HasQueryFilter(ps => ps.TenantId == _tenantContext.CurrentTenantId);
      }

      // Configurazione della relazione tra Language e Disclaimer
      modelBuilder.Entity<Language>()
          .HasMany(l => l.Disclaimers)
          .WithOne(d => d.Language)
          .HasForeignKey(d => d.LanguageId)
          .OnDelete(DeleteBehavior.Cascade);

      // Configurazione per garantire che ci sia solo una lingua predefinita per tenant
      modelBuilder.Entity<Language>()
          .HasIndex(l => new { l.IsDefault, l.TenantId })
          .HasFilter("[IsDefault] = 1")
          .IsUnique();

      // Dati predefiniti per i tenant
      modelBuilder.Entity<Tenant>().HasData(
          new Tenant { Id = 1, Name = "MorWalPiz", CreatedDate = new DateTime(2025, 1, 1), IsActive = true },
          new Tenant { Id = 2, Name = "ShootingIta", CreatedDate = new DateTime(2025, 1, 1), IsActive = true }
      );

      // Dati predefiniti per le lingue (con TenantId = 1)
      modelBuilder.Entity<Language>().HasData(
          new Language { Id = 1, Code = "it", Name = "Italiano", IsDefault = true, IsSelected = true, TenantId = 1 },
          new Language { Id = 2, Code = "en", Name = "English", IsDefault = false, IsSelected = true, TenantId = 1 },
          new Language { Id = 3, Code = "fr", Name = "Français", IsDefault = false, IsSelected = false, TenantId = 1 },
          new Language { Id = 4, Code = "de", Name = "Deutsch", IsDefault = false, IsSelected = false, TenantId = 1 },
          new Language { Id = 5, Code = "es", Name = "Español", IsDefault = false, IsSelected = true, TenantId = 1 }
      );

      // Configurazione per Settings (con TenantId = 1)
      modelBuilder.Entity<Settings>().HasData(
          new Settings { Id = 1, DefaultHashtags = "video, hashtag", ApplicationName = "MorWalPiz Site", ApiEndpoint = "https://localhost:7221", TenantId = 1 },
          new Settings { Id = 2, DefaultHashtags = "video, hashtag", ApplicationName = "ShootingITA Site", ApiEndpoint = "https://localhost:7221", TenantId = 2 }
      );

      // Configurazione per PublishSchedules - Pianificazioni predefinite (con TenantId = 1)
      modelBuilder.Entity<PublishSchedule>().HasData(
          new PublishSchedule 
          { 
              Id = 1, 
              Name = "Giorni feriali", 
              DaysOfWeek = 31, // Monday(1) + Tuesday(2) + Wednesday(4) + Thursday(8) + Friday(16) = 31
              PublishTime = new System.TimeSpan(19, 0, 0), // 19:00
              IsActive = true,
              CreatedDate = new System.DateTime(2025, 1, 1),
              TenantId = 1
          },
          new PublishSchedule
          {
              Id = 3, 
              Name = "Giorni feriali 1", 
              DaysOfWeek = 31, // Monday(1) + Tuesday(2) + Wednesday(4) + Thursday(8) + Friday(16) = 31
              PublishTime = new System.TimeSpan(12, 0, 0), // 12:00
              IsActive = true,
              CreatedDate = new System.DateTime(2025, 1, 1),
              TenantId = 1
          },
          new PublishSchedule 
          { 
              Id = 2, 
              Name = "Weekend", 
              DaysOfWeek = 96, // Saturday(32) + Sunday(64) = 96
              PublishTime = new System.TimeSpan(13, 0, 0), // 13:00
              IsActive = true,
              CreatedDate = new System.DateTime(2025, 1, 1),
              TenantId = 1
          }
      );
    }

    public override int SaveChanges()
    {
      SetTenantId();
      return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
      SetTenantId();
      return await base.SaveChangesAsync(cancellationToken);
    }

    private void SetTenantId()
    {
      if (_tenantContext == null) return;

      var entities = ChangeTracker.Entries()
          .Where(e => e.State == EntityState.Added)
          .Select(e => e.Entity);

      foreach (var entity in entities)
      {
        switch (entity)
        {
          case Language language:
            language.TenantId = _tenantContext.CurrentTenantId;
            break;
          case Disclaimer disclaimer:
            disclaimer.TenantId = _tenantContext.CurrentTenantId;
            break;
          case Settings settings:
            settings.TenantId = _tenantContext.CurrentTenantId;
            break;
          case PublishSchedule schedule:
            schedule.TenantId = _tenantContext.CurrentTenantId;
            break;
        }
      }
    }
  }
}
