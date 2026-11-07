using BarberFlow.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BarberFlow.Api.Data.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ClientName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.ClientPhone).IsRequired().HasMaxLength(20);

        builder.Property(x => x.Status)
            .HasConversion<string>() // Храним статус в БД как строку (Pending, Confirmed), а не как int
            .HasMaxLength(20);

        builder.Property(x => x.StartTime).IsRequired();
        builder.Property(x => x.EndTime).IsRequired();

        // Составной индекс для быстрого поиска записей мастера на конкретную дату
        builder.HasIndex(x => new { x.MasterId, x.StartTime });
    }
}