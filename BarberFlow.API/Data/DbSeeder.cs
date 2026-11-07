using BarberFlow.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BarberFlow.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(
        AppDbContext context,
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        // ==========================================
        // 1. СОЗДАНИЕ РОЛЕЙ И АДМИНА
        // ==========================================

        // Создаем роль Admin, если её нет
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }

        // Создаем базового админа
        var adminEmail = "admin@barberflow.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new AppUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                DisplayName = "Главный Админ",
                EmailConfirmed = true
            };

            // Пароль должен соответствовать политике безопасности (заглавные, цифры и т.д.)
            var createResult = await userManager.CreateAsync(adminUser, "Admin123!");

            if (createResult.Succeeded)
            {
                // Выдаем роль
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        // ==========================================
        // 2. ЗАПОЛНЕНИЕ БАЗЫ (УСЛУГИ, МАСТЕРА)
        // ==========================================

        // Если услуги уже есть, значит база не пустая — выходим
        if (await context.Services.AnyAsync()) return;

        var haircut = new Service { Name = "Мужская стрижка", Price = 2500m, DurationMinutes = 60 };
        var beard = new Service { Name = "Оформление бороды", Price = 1500m, DurationMinutes = 30 };
        var complex = new Service { Name = "Стрижка + борода", Price = 3500m, DurationMinutes = 90 };

        context.Services.AddRange(haircut, beard, complex);

        var master1 = new Master { Name = "Александр", Phone = "+79991112233" };
        var master2 = new Master { Name = "Дмитрий", Phone = "+79994445566" };

        context.Masters.AddRange(master1, master2);
        await context.SaveChangesAsync(); // Сохраняем, чтобы получить Guid'ы

        // ... (дальше идет создание графика WorkSchedule и тестового Appointment, 
        // код который мы писали ранее, можешь оставить без изменений) ...

        var schedules = new List<WorkSchedule>();
        var workingDays = new[] { DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday };
        var daysOff = new[] { DayOfWeek.Sunday, DayOfWeek.Monday };

        foreach (var master in new[] { master1, master2 })
        {
            foreach (var day in workingDays)
            {
                schedules.Add(new WorkSchedule { MasterId = master.Id, DayOfWeek = day, StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(20, 0, 0), IsDayOff = false });
            }
            foreach (var day in daysOff)
            {
                schedules.Add(new WorkSchedule { MasterId = master.Id, DayOfWeek = day, IsDayOff = true });
            }
        }
        context.WorkSchedules.AddRange(schedules);

        var nextTuesday = DateTime.UtcNow.Date.AddDays(((int)DayOfWeek.Tuesday - (int)DateTime.UtcNow.DayOfWeek + 7) % 7);
        if (nextTuesday == DateTime.UtcNow.Date) nextTuesday = nextTuesday.AddDays(7);

        var testAppointment = new Appointment
        {
            MasterId = master1.Id,
            ServiceId = haircut.Id,
            ClientName = "Тестовый Клиент",
            ClientPhone = "+70000000000",
            StartTime = nextTuesday.AddHours(12),
            EndTime = nextTuesday.AddHours(13),
            Status = AppointmentStatus.Confirmed
        };
        context.Appointments.Add(testAppointment);

        await context.SaveChangesAsync();
    }
}