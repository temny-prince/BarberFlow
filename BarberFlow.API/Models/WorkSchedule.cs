namespace BarberFlow.Api.Models;

public class WorkSchedule
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid MasterId { get; set; }
    public Master Master { get; set; } = null!;

    public DayOfWeek DayOfWeek { get; set; } // Понедельник, Вторник и т.д.

    // Время работы (храним только часы и минуты)
    public TimeSpan StartTime { get; set; } // Например, 10:00
    public TimeSpan EndTime { get; set; }   // Например, 20:00

    public bool IsDayOff { get; set; } = false; // Если мастер отдыхает в этот день
}