using BarberFlow.Api.Data;
using BarberFlow.Api.DTOs;
using BarberFlow.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BarberFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MastersController : ControllerBase
{
    private readonly AppDbContext _context;

    public MastersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MasterResponseDto>>> GetMasters()
    {
        var masters = await _context.Masters
            .Where(m => m.IsActive)
            .Select(m => m.ToDto())
            .ToListAsync();

        return Ok(masters);
    }

    // Генерация свободных слотов (Публичный)
    [HttpGet("{id}/slots")]
    public async Task<ActionResult<List<string>>> GetAvailableSlots(Guid id, [FromQuery] DateTime date)
    {
        var master = await _context.Masters.FindAsync(id);
        if (master == null) return NotFound("Мастер не найден");

        var schedule = await _context.WorkSchedules
            .FirstOrDefaultAsync(w => w.MasterId == id && w.DayOfWeek == date.DayOfWeek);

        if (schedule == null || schedule.IsDayOff)
            return Ok(new List<string>());

        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        var appointments = await _context.Appointments
            .Where(a => a.MasterId == id
                     && a.StartTime >= startOfDay
                     && a.StartTime < endOfDay
                     && a.Status != AppointmentStatus.Canceled)
            .ToListAsync();

        var availableSlots = new List<string>();
        var slotDuration = TimeSpan.FromMinutes(30);

        var currentTime = schedule.StartTime;

        while (currentTime + slotDuration <= schedule.EndTime)
        {
            var slotStartTime = startOfDay.Add(currentTime);
            var slotEndTime = slotStartTime.Add(slotDuration);

            bool isBusy = appointments.Any(a =>
                slotStartTime < a.EndTime && slotEndTime > a.StartTime);

            if (!isBusy && slotStartTime > DateTime.UtcNow)
            {
                availableSlots.Add(currentTime.ToString(@"hh\:mm"));
            }

            currentTime = currentTime.Add(slotDuration);
        }

        return Ok(availableSlots);
    }

    // Только для админа
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<MasterResponseDto>> CreateMaster([FromBody] Master master)
    {
        _context.Masters.Add(master);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetMasters), new { id = master.Id }, master.ToDto());
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateMaster(Guid id, [FromBody] Master updated)
    {
        var master = await _context.Masters.FindAsync(id);
        if (master == null) return NotFound();

        master.Name = updated.Name;
        master.Phone = updated.Phone;
        // TelegramChatId тут не меняем, он привяжется через бота

        await _context.SaveChangesAsync();
        return Ok(master.ToDto());
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteMaster(Guid id)
    {
        var master = await _context.Masters.FindAsync(id);
        if (master == null) return NotFound();

        // Мягкое удаление
        master.IsActive = false;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id}/schedule")]
    public async Task<ActionResult<IEnumerable<ScheduleDto>>> GetSchedule(Guid id)
    {
        var schedules = await _context.WorkSchedules
            .Where(w => w.MasterId == id)
            .Select(w => new ScheduleDto
            {
                DayOfWeek = w.DayOfWeek,
                StartTime = w.StartTime,
                EndTime = w.EndTime,
                IsDayOff = w.IsDayOff
            })
            .ToListAsync();

        return Ok(schedules);
    }

    // POST: api/masters/{id}/schedule
    [HttpPost("{id}/schedule")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateSchedule(Guid id, [FromBody] List<ScheduleDto> dtos)
    {
        var master = await _context.Masters.FindAsync(id);
        if (master == null) return NotFound("Мастер не найден");

        // Удаляем старый график
        var oldSchedules = await _context.WorkSchedules.Where(w => w.MasterId == id).ToListAsync();
        _context.WorkSchedules.RemoveRange(oldSchedules);

        // Добавляем новый
        foreach (var dto in dtos)
        {
            _context.WorkSchedules.Add(new WorkSchedule
            {
                MasterId = id,
                DayOfWeek = dto.DayOfWeek,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                IsDayOff = dto.IsDayOff
            });
        }

        await _context.SaveChangesAsync();
        return Ok();
    }
}