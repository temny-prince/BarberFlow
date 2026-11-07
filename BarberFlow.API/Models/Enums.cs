namespace BarberFlow.Api.Models;

public enum AppointmentStatus
{
    Pending = 0,    // Ожидает подтверждения
    Confirmed = 1,  // Подтверждена
    Completed = 2,  // Завершена (клиент пришел)
    Canceled = 3    // Отменена
}