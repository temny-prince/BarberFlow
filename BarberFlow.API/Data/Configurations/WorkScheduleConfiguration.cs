using BarberFlow.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BarberFlow.Api.Data.Configurations;

public class WorkScheduleConfiguration : IEntityTypeConfiguration<WorkSchedule>
{
    public void Configure(EntityTypeBuilder<WorkSchedule> builder)
    {
        builder.ToTable("WorkSchedules");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DayOfWeek).IsRequired();

        // У мастера не может быть двух графиков на один и тот же день недели
        builder.HasIndex(x => new { x.MasterId, x.DayOfWeek }).IsUnique();
    }
}