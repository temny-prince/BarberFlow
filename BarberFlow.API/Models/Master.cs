namespace BarberFlow.Api.Models;

public class Master
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    // Для связки с ботом (когда мастер авторизуется в боте)
    public long? TelegramChatId { get; set; }

    public bool IsActive { get; set; } = true;

    // Навигационные свойства
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<WorkSchedule> WorkSchedules { get; set; } = new List<WorkSchedule>();
}