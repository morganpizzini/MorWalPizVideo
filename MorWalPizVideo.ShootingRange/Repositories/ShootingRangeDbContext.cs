using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MorWalPizVideo.ShootingRange.Models;

namespace MorWalPizVideo.ShootingRange.Repositories;

public sealed class ShootingRangeDbContext(DbContextOptions<ShootingRangeDbContext> options) : DbContext(options)
{
    private static readonly ValueConverter<DateTime, DateTime> UtcDateTimeConverter = new(
        value => DateTime.SpecifyKind(value, DateTimeKind.Unspecified),
        value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

    public DbSet<ShootingRangeConfig> Configs => Set<ShootingRangeConfig>();
    public DbSet<ShootingRangeBay> Bays => Set<ShootingRangeBay>();
    public DbSet<ShootingRangeMechanismType> MechanismTypes => Set<ShootingRangeMechanismType>();
    public DbSet<ShootingRangeException> Exceptions => Set<ShootingRangeException>();
    public DbSet<ShootingRangeUser> Users => Set<ShootingRangeUser>();
    public DbSet<ShootingRangeLocalSession> LocalSessions => Set<ShootingRangeLocalSession>();
    public DbSet<ShootingRangeBooking> Bookings => Set<ShootingRangeBooking>();
    public DbSet<ShootingRangeThread> Threads => Set<ShootingRangeThread>();
    public DbSet<ShootingRangeMutationGuard> MutationGuards => Set<ShootingRangeMutationGuard>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureBase<ShootingRangeConfig>(modelBuilder, "shooting_range_configs");
        ConfigureBase<ShootingRangeBay>(modelBuilder, "shooting_range_bays");
        ConfigureBase<ShootingRangeMechanismType>(modelBuilder, "shooting_range_mechanism_types");
        ConfigureBase<ShootingRangeException>(modelBuilder, "shooting_range_exceptions");
        ConfigureBase<ShootingRangeUser>(modelBuilder, "shooting_range_users");
        ConfigureBase<ShootingRangeLocalSession>(modelBuilder, "shooting_range_local_sessions");
        ConfigureBase<ShootingRangeBooking>(modelBuilder, "shooting_range_bookings");
        ConfigureBase<ShootingRangeThread>(modelBuilder, "shooting_range_threads");

        modelBuilder.Entity<ShootingRangeConfig>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.TimeZone).HasMaxLength(100).IsRequired();
            entity.Property(x => x.SessionMode).HasConversion<int>();
            ConfigureJson(entity.Property(x => x.OpeningDays));
            entity.Property(x => x.MorningStart).HasColumnType("time");
            entity.Property(x => x.MorningEnd).HasColumnType("time");
            entity.Property(x => x.AfternoonStart).HasColumnType("time");
            entity.Property(x => x.AfternoonEnd).HasColumnType("time");
            entity.Property(x => x.ContinuousStart).HasColumnType("time");
            entity.Property(x => x.ContinuousEnd).HasColumnType("time");
        });

        modelBuilder.Entity<ShootingRangeBay>(entity =>
        {
            entity.Property(x => x.Code).HasMaxLength(100).IsRequired();
            ConfigureJson(entity.Property(x => x.Mechanisms));
            entity.Property(x => x.Pepper).IsRequired();
            entity.Property(x => x.Status).HasConversion<int>();
            entity.Property(x => x.Description).IsRequired();
            entity.Property(x => x.ReservedUntilUtc).HasColumnType("datetime2")
                .HasConversion(UtcDateTimeConverter);
            ConfigureJson(entity.Property(x => x.WhitelistUserIds));
        });

        modelBuilder.Entity<ShootingRangeMechanismType>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(80).IsRequired();
            entity.Property(x => x.NormalizedName).HasMaxLength(80).IsRequired().UseCollation("Latin1_General_100_BIN2");
            entity.HasIndex(x => x.NormalizedName).IsUnique().HasDatabaseName("ux_shooting_range_mechanism_types_normalized_name");
        });

        modelBuilder.Entity<ShootingRangeException>(entity =>
        {
            entity.Property(x => x.LocalDate).HasColumnType("date");
            entity.Property(x => x.BayId).HasMaxLength(128);
            entity.Property(x => x.Reason).IsRequired();
        });

        modelBuilder.Entity<ShootingRangeUser>(entity =>
        {
            entity.Property(x => x.Username).HasMaxLength(200).IsRequired();
            entity.Property(x => x.NormalizedUsername).HasMaxLength(200).IsRequired();
            entity.Property(x => x.NormalizedUsername).UseCollation("Latin1_General_100_BIN2");
            entity.Property(x => x.FirstName).IsRequired();
            entity.Property(x => x.LastName).IsRequired();
            entity.Property(x => x.PasswordHash).IsRequired();
            entity.Property(x => x.Status).HasConversion<int>();
            entity.Property(x => x.SecurityVersion).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => x.NormalizedUsername).IsUnique().HasDatabaseName("ux_shooting_range_users_normalized_username");
        });

        modelBuilder.Entity<ShootingRangeLocalSession>(entity =>
        {
            entity.Property(x => x.UserId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.SecurityVersion).HasMaxLength(64).IsRequired();
            entity.Property(x => x.ExpiresUtc).HasColumnType("datetime2")
                .HasConversion(UtcDateTimeConverter);
            entity.HasIndex(x => new { x.UserId, x.Revoked, x.ExpiresUtc }).HasDatabaseName("ix_shooting_range_sessions_user_expiry");
        });

        modelBuilder.Entity<ShootingRangeBooking>(entity =>
        {
            entity.Property(x => x.UserId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.BayId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.LocalDate).HasColumnType("date");
            entity.Property(x => x.PeriodKey).HasMaxLength(40).IsRequired();
            entity.Property(x => x.StartUtc).HasColumnType("datetime2")
                .HasConversion(UtcDateTimeConverter);
            entity.Property(x => x.EndUtc).HasColumnType("datetime2")
                .HasConversion(UtcDateTimeConverter);
            entity.Property(x => x.Request).IsRequired();
            entity.Property(x => x.Status).HasConversion<int>();
            entity.HasIndex(x => new { x.BayId, x.StartUtc, x.EndUtc, x.Status }).HasDatabaseName("ix_shooting_range_bookings_interval");
            entity.HasIndex(x => new { x.UserId, x.StartUtc }).HasDatabaseName("ix_shooting_range_bookings_user_start");
        });

        modelBuilder.Entity<ShootingRangeThread>(entity =>
        {
            entity.Property(x => x.UserId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Subject).HasMaxLength(500).IsRequired();
            ConfigureJson(entity.Property(x => x.Messages));
        });

        modelBuilder.Entity<ShootingRangeMutationGuard>(entity =>
        {
            entity.ToTable("shooting_range_mutation_guards");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.HasData(new ShootingRangeMutationGuard { Id = 1 });
        });

        foreach (var entity in modelBuilder.Model.GetEntityTypes())
            foreach (var property in entity.GetProperties())
                property.SetColumnName(ToSnakeCase(property.Name));
    }

    private static void ConfigureBase<TEntity>(ModelBuilder modelBuilder, string tableName)
        where TEntity : BaseEntity
    {
        var entity = modelBuilder.Entity<TEntity>();
        entity.ToTable(tableName);
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Id).HasMaxLength(128).ValueGeneratedNever();
        entity.Property(x => x.CreationDateTime).HasColumnType("datetime2");
    }

    private static void ConfigureJson<TValue>(PropertyBuilder<List<TValue>> property)
    {
        property.HasConversion(JsonListConverter<TValue>.Converter)
            .HasColumnType("nvarchar(max)")
            .Metadata.SetValueComparer(JsonListConverter<TValue>.Comparer);
    }

    private static string ToSnakeCase(string value)
    {
        var result = new System.Text.StringBuilder(value.Length + 8);
        for (var index = 0; index < value.Length; index++)
        {
            if (char.IsUpper(value[index]) && index > 0) result.Append('_');
            result.Append(char.ToLowerInvariant(value[index]));
        }
        return result.ToString();
    }
}

public sealed class ShootingRangeMutationGuard
{
    public int Id { get; set; }
}

internal static class JsonListConverter<T>
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    internal static readonly ValueConverter<List<T>, string> Converter = new(
        value => JsonSerializer.Serialize(value, Options),
        value => JsonSerializer.Deserialize<List<T>>(value, Options) ?? new List<T>());

    internal static readonly ValueComparer<List<T>> Comparer = new(
        (left, right) => left != null && right != null && left.SequenceEqual(right),
        value => value.Aggregate(0, (hash, item) => HashCode.Combine(hash, item)),
        value => value.ToList());
}
