using System.ComponentModel.DataAnnotations;

namespace BarberFlow.Api.DTOs;

public class CreateAppointmentDto
{
    [Required]
    public Guid MasterId { get; set; }

    [Required]
    public Guid ServiceId { get; set; }

    [Required]
    [MaxLength(100)]
    public string ClientName { get; set; } = string.Empty;

    [Required]
    [Phone]
    public string ClientPhone { get; set; } = string.Empty;

    [Required]
    public DateTime StartTime { get; set; }
}