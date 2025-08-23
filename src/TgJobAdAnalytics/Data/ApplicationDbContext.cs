using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text.Encodings.Web;
using System.Text.Json;
using TgJobAdAnalytics.Data.Messages;
using TgJobAdAnalytics.Data.Messages.Converters;
using TgJobAdAnalytics.Data.Salaries;

namespace TgJobAdAnalytics.Data
{
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
                
                entity.HasIndex(e => e.MessageId);
                entity.HasIndex(e => e.IsUnique);
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

                entity.HasIndex(e => e.AdId);
            });
        }
        
        
        public DbSet<ChatEntity> Chats { get; set; }

        public DbSet<MessageEntity> Messages { get; set; }

        public DbSet<AdEntity> Ads { get; set; }

        public DbSet<SalaryEntity> Salaries { get; set; }


        private static readonly JsonSerializerOptions JsonSerializerOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = false
        };

        private readonly IConfiguration _configuration;
    }
}
