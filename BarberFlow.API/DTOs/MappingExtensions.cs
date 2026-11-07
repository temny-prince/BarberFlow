using BarberFlow.Api.Models;

namespace BarberFlow.Api.DTOs;

public static class MappingExtensions
{
    public static AppointmentResponseDto ToDto(this Appointment a) => new()
    {
        Id = a.Id,
        MasterName = a.Master?.Name ?? "Неизвестно",
        ServiceName = a.Service?.Name ?? "Неизвестно",
        ClientName = a.ClientName,
        ClientPhone = a.ClientPhone,
        StartTime = a.StartTime,
        EndTime = a.EndTime,
        Status = a.Status.ToString(),
        Price = a.Service?.Price ?? 0
    };

    public static MasterResponseDto ToDto(this Master m) => new()
    {
        Id = m.Id,
        Name = m.Name,
        Phone = m.Phone
    };

    public static ServiceResponseDto ToDto(this Service s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        Price = s.Price,
        DurationMinutes = s.DurationMinutes
    };
}