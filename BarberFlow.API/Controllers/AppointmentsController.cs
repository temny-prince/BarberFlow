using BarberFlow.Api.Data;
using BarberFlow.Api.DTOs;
using BarberFlow.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BarberFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly AppDbContext _context;

    public AppointmentsController(AppDbContext context)
    {
        _context = context;
    }

    // Публичный метод для создания записи
    [HttpPost]
    public async Task<ActionResult<AppointmentResponseDto>> CreateAppointment([FromBody] CreateAppointmentDto dto)
    {
        var service = await _context.Services.FindAsync(dto.ServiceId);
        var master = await _context.Masters.FindAsync(dto.MasterId);

        if (service == null || master == null)
            return BadRequest("Услуга или мастер не найдены");

        var startTime = dto.StartTime;
        var endTime = startTime.AddMinutes(service.DurationMinutes);

        // Проверка на пересечение (защита от двойной записи)
        var isBusy = await _context.Appointments
            .AnyAsync(a => a.MasterId == dto.MasterId
                        && a.Status != AppointmentStatus.Canceled
                        && startTime < a.EndTime
                        && endTime > a.StartTime);

        if (isBusy)
            return Conflict("Это время уже занято другим клиентом");

        var appointment = new Appointment
        {
            MasterId = dto.MasterId,
            ServiceId = dto.ServiceId,
            ClientName = dto.ClientName,
            ClientPhone = dto.ClientPhone,
            StartTime = startTime,
            EndTime = endTime,
            Status = AppointmentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        // Подгружаем навигационные свойства для DTO
        appointment.Master = master;
        appointment.Service = service;

        return CreatedAtAction(nameof(GetAll), new { id = appointment.Id }, appointment.ToDto());
    }

    // Только для админа: получить все записи (для админ-панели)
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<AppointmentResponseDto>>> GetAll()
    {
        var appointments = await _context.Appointments
            .Include(a => a.Master)
            .Include(a => a.Service)
            .OrderByDescending(a => a.StartTime)
            .Select(a => a.ToDto())
            .ToListAsync();

        return Ok(appointments);
    }

    // Только для админа: изменить статус (подтвердить/отменить)
    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] AppointmentStatus newStatus)
    {
        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment == null) return NotFound();

        appointment.Status = newStatus;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // Для телеграм-бота: получить записи по номеру телефона
    [HttpGet("phone/{phone}")]
    public async Task<ActionResult<IEnumerable<AppointmentResponseDto>>> GetByPhone(string phone)
    {
        var appointments = await _context.Appointments
            .Include(a => a.Master)
            .Include(a => a.Service)
            .Where(a => a.ClientPhone == phone)
            .OrderByDescending(a => a.StartTime)
            .Select(a => a.ToDto())
            .ToListAsync();

        return Ok(appointments);
    }
}