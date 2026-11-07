namespace BarberFlow.Api.DTOs;

public class AppointmentResponseDto
{
    public Guid Id { get; set; }
    public string MasterName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ClientPhone { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Price { get; set; }
}