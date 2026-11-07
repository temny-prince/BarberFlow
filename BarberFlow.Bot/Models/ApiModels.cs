namespace BarberFlow.Bot.Models;

public class AppointmentResponseDto
{
    public Guid Id { get; set; }
    public string MasterName { get; set; } = "";
    public string ServiceName { get; set; } = "";
    public string ClientName { get; set; } = "";
    public string ClientPhone { get; set; } = "";
    public DateTime StartTime { get; set; }
    public string Status { get; set; } = "";
}

public class ServiceResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
}

public class MasterResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
}