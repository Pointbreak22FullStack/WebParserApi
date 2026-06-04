using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class SystemSettingsConfiguration : IEntityTypeConfiguration<SystemSettings>
    {
        public void Configure(EntityTypeBuilder<SystemSettings> builder)
        {
            builder.ToTable("SystemSettings");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.DelayBetweenActionsMs).HasDefaultValue(800);
            builder.Property(x => x.MaxParallelTasks).HasDefaultValue(10);
            builder.Property(x => x.FingerprintEngine).HasMaxLength(50).IsRequired();
            builder.Property(x => x.GenerationMode).HasMaxLength(20).IsRequired();
            builder.Property(x => x.TelegramBotToken).HasMaxLength(255);
            builder.Property(x => x.TelegramChatId).HasMaxLength(100);

            builder.HasData(new SystemSettings
            {
                Id = 1,
                DelayBetweenActionsMs = 800,
                MaxParallelTasks = 10,
                RetryOnError = true,
                HeadlessMode = true,
                FingerprintEngine = "Собственный",
                RotateUserAgent = true,
                SimulateHumanBehavior = true,
                GenerationMode = "LLM",
                CheckIntervalMinutes = 30,
                AutoLikeEnabled = true,
                ReportCompetitors = false,
                UpdatedAt = new DateTime(2026, 4, 21, 0, 0, 0, DateTimeKind.Utc)
            });
        }
    }
}
