using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using TgJobAdAnalytics.Data.Messages;
using TgJobAdAnalytics.Data.Messages.Converters;

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
        }        protected override void OnModelCreating(ModelBuilder modelBuilder)
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
                    .HasConversion(new TextEntriesConverter())
                    .IsRequired();
                entity.Property(e => e.Tags)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                    .IsRequired();
                
                entity.HasIndex(e => new { e.TelegramChatId, e.TelegramMessageId }).IsUnique();
            });
        }

          public DbSet<ChatEntity> Chats { get; set; }

        public DbSet<MessageEntity> Messages { get; set; }


        private readonly IConfiguration _configuration;
    }
}
