using BarberFlow.Api.Data;
using BarberFlow.Api.DTOs;
using BarberFlow.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BarberFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServicesController : ControllerBase
{
    private readonly AppDbContext _context;

    public ServicesController(AppDbContext context)
    {
        _context = context;
    }

    // Публичный метод (для сайта)
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ServiceResponseDto>>> GetServices()
    {
        var services = await _context.Services
            .Where(s => s.IsActive)
            .Select(s => s.ToDto())
            .ToListAsync();

        return Ok(services);
    }

    // Только для админа
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ServiceResponseDto>> CreateService([FromBody] Service service)
    {
        _context.Services.Add(service);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetServices), new { id = service.Id }, service.ToDto());
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteService(Guid id)
    {
        var service = await _context.Services.FindAsync(id);
        if (service == null) return NotFound();

        // Мы не удаляем, а деактивируем, чтобы не сломать старые записи
        service.IsActive = false;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateService(Guid id, [FromBody] Service updated)
    {
        var service = await _context.Services.FindAsync(id);
        if (service == null) return NotFound();

        service.Name = updated.Name;
        service.Price = updated.Price;
        service.DurationMinutes = updated.DurationMinutes;

        await _context.SaveChangesAsync();
        return Ok(service.ToDto());
    }
}