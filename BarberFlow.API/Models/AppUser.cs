using Microsoft.AspNetCore.Identity;

namespace BarberFlow.Api.Models;

public class AppUser : IdentityUser
{
    // Можно добавить специфичные для админа поля
    public string? DisplayName { get; set; }
}