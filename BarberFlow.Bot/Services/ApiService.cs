using System.Net.Http.Json;
using BarberFlow.Bot.Models;

namespace BarberFlow.Bot.Services;

public class ApiService
{
    private readonly HttpClient _http;

    public ApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<ServiceResponseDto>> GetServicesAsync()
    {
        try
        {
            // Убираем слэш в начале, чтобы он не дублировался с BaseAddress
            var url = "services";
            Console.WriteLine($"[API] Запрос к: {_http.BaseAddress}{url}");

            var result = await _http.GetFromJsonAsync<List<ServiceResponseDto>>(url);
            return result ?? new List<ServiceResponseDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[API ERROR] GetServices: {ex.Message}");
            return new List<ServiceResponseDto>();
        }
    }

    public async Task<List<MasterResponseDto>> GetMastersAsync()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<MasterResponseDto>>("masters");
            return result ?? new List<MasterResponseDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[API ERROR] GetMasters: {ex.Message}");
            return new List<MasterResponseDto>();
        }
    }

    public async Task<List<string>> GetSlotsAsync(Guid masterId, DateTime date)
    {
        try
        {
            var dateStr = date.ToString("yyyy-MM-dd");
            var result = await _http.GetFromJsonAsync<List<string>>($"masters/{masterId}/slots?date={dateStr}");
            return result ?? new List<string>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[API ERROR] GetSlots: {ex.Message}");
            return new List<string>();
        }
    }

    public async Task<bool> CreateAppointmentAsync(object data)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("appointments", data);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[API ERROR] CreateAppointment: {ex.Message}");
            return false;
        }
    }

    public async Task<List<AppointmentResponseDto>> GetAllAppointmentsAsync()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<AppointmentResponseDto>>("appointments");
            return result ?? new List<AppointmentResponseDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[API ERROR] GetAllAppointments: {ex.Message}");
            return new List<AppointmentResponseDto>();
        }
    }

    public async Task<bool> UpdateStatusAsync(Guid id, string status)
    {
        try
        {
            var response = await _http.PatchAsJsonAsync($"appointments/{id}/status", status);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[API ERROR] UpdateStatus: {ex.Message}");
            return false;
        }
    }
}