using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text.Encodings.Web;
using System.Text.Json;
using TgJobAdAnalytics.Data.Messages;
using TgJobAdAnalytics.Data.Messages.Converters;
using TgJobAdAnalytics.Data.Salaries;
using TgJobAdAnalytics.Data.Stacks;
using TgJobAdAnalytics.Data.Vectors;

namespace TgJobAdAnalytics.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false);

        _configuration = builder.Build();
    }


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        optionsBuilder.UseSqlite(connectionString);
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }        
    
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChatEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TelegramId).IsRequired();
            entity.Property(e => e.Name).IsRequired();
            
            entity.HasIndex(e => e.TelegramId);
        });

        modelBuilder.Entity<MessageEntity>(entity =>
        {                
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TelegramChatId).IsRequired();
            entity.Property(e => e.TelegramMessageId).IsRequired();
            entity.Property(e => e.TelegramMessageDate).IsRequired();
            
            entity.Property(e => e.TextEntries)
                .HasConversion(new TextEntriesConverter(JsonSerializerOptions))
                .IsRequired();
            entity.Property(e => e.Tags)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonSerializerOptions),
                    v => JsonSerializer.Deserialize<List<string>>(v, JsonSerializerOptions) ?? new List<string>())
                .IsRequired();                
            entity.HasIndex(e => new { e.TelegramChatId, e.TelegramMessageId }).IsUnique();
        });

        modelBuilder.Entity<AdEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Date).IsRequired();
            entity.Property(e => e.Text).IsRequired();
            entity.Property(e => e.MessageId).IsRequired();
            entity.Property(e => e.StackId).IsRequired(false);
            
            entity.HasIndex(e => e.MessageId);
            entity.HasIndex(e => e.IsUnique);
            entity.HasIndex(e => e.StackId);
        });
        modelBuilder.Entity<AdEntity>()
            .HasQueryFilter(ad => ad.IsUnique);

        modelBuilder.Entity<SalaryEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AdId).IsRequired();
            entity.Property(e => e.Date).IsRequired();
            entity.Property(e => e.Currency);
            entity.Property(e => e.CurrencyNormalized);
            entity.Property(e => e.LowerBound)
                .HasConversion<DoubleToNullableDoubleConverter>();
            entity.Property(e => e.LowerBoundNormalized);
            entity.Property(e => e.UpperBound)
                .HasConversion<DoubleToNullableDoubleConverter>();
            entity.Property(e => e.UpperBoundNormalized);
            entity.Property(e => e.Level).HasConversion<int>();

            entity.HasIndex(e => e.AdId);
            entity.HasIndex(e => e.Level);
        });

        modelBuilder.Entity<VectorModelVersionEntity>(entity =>
        {
            entity.HasKey(e => e.Version);
            entity.Property(e => e.NormalizationVersion).IsRequired();
            entity.HasIndex(e => e.IsActive);
        });

        modelBuilder.Entity<AdVectorEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AdId).IsRequired();
            entity.Property(e => e.Version).IsRequired();
            entity.Property(e => e.Dim).IsRequired();
            entity.Property(e => e.Signature).IsRequired();
            entity.HasIndex(e => new { e.AdId, e.Version }).IsUnique();
        });

        modelBuilder.Entity<LshBucketEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Version).IsRequired();
            entity.Property(e => e.Band).IsRequired();
            entity.Property(e => e.Key).IsRequired();
            entity.Property(e => e.AdId).IsRequired();
            entity.HasIndex(e => new { e.Version, e.Band, e.Key });
            entity.HasIndex(e => e.AdId);
        });

        modelBuilder.Entity<TechnologyStackEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(64);
            entity.HasIndex(e => e.Name).IsUnique();
        });
    }
    
    
    public DbSet<ChatEntity> Chats { get; set; }

    public DbSet<MessageEntity> Messages { get; set; }

    public DbSet<AdEntity> Ads { get; set; }

    public DbSet<SalaryEntity> Salaries { get; set; }

    public DbSet<VectorModelVersionEntity> VectorModelVersions { get; set; }

    public DbSet<AdVectorEntity> AdVectors { get; set; }

    public DbSet<LshBucketEntity> LshBuckets { get; set; }

    public DbSet<TechnologyStackEntity> TechnologyStacks { get; set; }


    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    private readonly IConfiguration _configuration;
}
