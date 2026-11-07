using BarberFlow.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BarberFlow.Api.Data.Configurations;

public class MasterConfiguration : IEntityTypeConfiguration<Master>
{
    public void Configure(EntityTypeBuilder<Master> builder)
    {
        builder.ToTable("Masters");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Phone).IsRequired().HasMaxLength(20);

        // Уникальный индекс, чтобы один и тот же ChatId не привязался к двум мастерам
        builder.HasIndex(x => x.TelegramChatId).IsUnique();

        // Связь 1-ко-многим с записями
        builder.HasMany(x => x.Appointments)
            .WithOne(x => x.Master)
            .HasForeignKey(x => x.MasterId)
            .OnDelete(DeleteBehavior.Restrict); // Нельзя удалить мастера, если у него есть записи

        // Связь 1-ко-многим с графиком
        builder.HasMany(x => x.WorkSchedules)
            .WithOne(x => x.Master)
            .HasForeignKey(x => x.MasterId)
            .OnDelete(DeleteBehavior.Cascade); // Если удаляем мастера, его график удаляется
    }
}