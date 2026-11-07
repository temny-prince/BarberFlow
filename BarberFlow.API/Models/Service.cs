namespace BarberFlow.Api.Models;

public class Service
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; } // Длительность услуги

    public bool IsActive { get; set; } = true;

    // Навигационное свойство
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}