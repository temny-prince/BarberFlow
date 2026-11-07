namespace BarberFlow.Api.Models;

public class Appointment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Внешние ключи
    public Guid MasterId { get; set; }
    public Master Master { get; set; } = null!;

    public Guid ServiceId { get; set; }
    public Service Service { get; set; } = null!;

    // Данные клиента
    public string ClientName { get; set; } = string.Empty;
    public string ClientPhone { get; set; } = string.Empty;
    public long? ClientTelegramChatId { get; set; } // Если записывался через бота

    // Время
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; } // Вычисляется как StartTime + Service.DurationMinutes

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}