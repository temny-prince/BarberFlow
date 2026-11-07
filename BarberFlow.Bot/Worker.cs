using BarberFlow.Bot.Models;
using BarberFlow.Bot.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace BarberFlow.Bot;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ApiService _api;
    private readonly string _token;
    private readonly string _adminUsername;
    private TelegramBotClient _botClient = null!;

    private readonly Dictionary<long, BookingState> _userStates = new();

    public Worker(ILogger<Worker> logger, ApiService api, IConfiguration config)
    {
        _logger = logger;
        _api = api;
        _token = config["BotConfiguration:Token"]!;
        _adminUsername = config["BotConfiguration:AdminUsername"]!;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _botClient = new TelegramBotClient(_token);

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery }
        };

        _botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandleErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken
        );

        _logger.LogInformation("Бот запущен.");
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
    {
        try
        {
            if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
                await HandleCallback(botClient, update.CallbackQuery, ct);
            else if (update.Type == UpdateType.Message && update.Message != null)
                await HandleMessage(botClient, update.Message, ct);
        }
        catch (Exception ex) { _logger.LogError(ex, "Ошибка обработки"); }
    }

    private async Task HandleMessage(ITelegramBotClient botClient, Message message, CancellationToken ct)
    {
        var text = message.Text;
        var username = message.From?.Username;
        var chatId = message.Chat.Id;

        // --- АДМИН ---
        if (text == "/start" && username == _adminUsername)
        {
            await botClient.SendMessage(chatId, "👋 Привет, Админ! Вот панель управления.",
                replyMarkup: GetAdminKeyboard(), cancellationToken: ct);
            return;
        }
        if (text == "📋 Записи" && username == _adminUsername)
        {
            await SendAppointmentsList(botClient, chatId, ct);
            return;
        }

        // --- КЛИЕНТ ---
        if (text == "/start" || text == "🔄 Начать заново")
        {
            await StartBooking(botClient, chatId, ct);
            return;
        }

        // --- ОТПРАВКА КОНТАКТА ---
        if (_userStates.TryGetValue(chatId, out var state) && state.Step == "AwaitingContact")
        {
            if (message.Contact != null)
            {
                var clientPhone = message.Contact.PhoneNumber;
                var clientName = message.From?.FirstName ?? "Клиент";

                var appointmentData = new
                {
                    masterId = state.MasterId,
                    serviceId = state.ServiceId,
                    clientName = clientName,
                    clientPhone = clientPhone,
                    startTime = state.SelectedTime
                };

                bool success = await _api.CreateAppointmentAsync(appointmentData);

                if (success)
                {
                    _userStates.Remove(chatId);
                    await botClient.SendMessage(chatId, "✅ Вы успешно записаны! Ждем вас.",
                        replyMarkup: new ReplyKeyboardMarkup(new[] { new KeyboardButton[] { "🔄 Начать заново" } }) { ResizeKeyboard = true },
                        cancellationToken: ct);
                }
                else
                {
                    await botClient.SendMessage(chatId, "❌ Ошибка: это время уже занято. Попробуйте другое.", cancellationToken: ct);
                }
            }
            else
            {
                await botClient.SendMessage(chatId, "Пожалуйста, нажмите кнопку ниже, чтобы отправить номер телефона.",
                    replyMarkup: new ReplyKeyboardMarkup(new[] { new KeyboardButton("📱 Отправить мой телефон") { RequestContact = true } }) { ResizeKeyboard = true },
                    cancellationToken: ct);
            }
        }
    }

    private async Task HandleCallback(ITelegramBotClient botClient, CallbackQuery callback, CancellationToken ct)
    {
        var chatId = callback.Message!.Chat.Id;
        var data = callback.Data;
        var username = callback.From.Username;

        if (data == null) return;

        // --- АДМИН (Подтвердить/Отменить) ---
        if (data.StartsWith("status_"))
        {
            if (username != _adminUsername) return;
            var parts = data.Split('_');
            var appId = Guid.Parse(parts[1]);
            var newStatus = parts[2];

            if (await _api.UpdateStatusAsync(appId, newStatus))
            {
                await botClient.AnswerCallbackQuery(callback.Id, "Статус обновлен!", cancellationToken: ct);
                await botClient.EditMessageText(chatId, callback.Message.MessageId,
                    callback.Message.Text + $"\n\n✅ Статус изменен на: {newStatus}", replyMarkup: null, cancellationToken: ct);
            }
            return;
        }

        // --- КЛИЕНТ (Шаги записи) ---
        if (!_userStates.ContainsKey(chatId)) _userStates[chatId] = new BookingState();
        var state = _userStates[chatId];

        // 1. Выбор Услуги (svc_)
        if (data.StartsWith("svc_"))
        {
            state.ServiceId = Guid.Parse(data.Substring(4));

            var masters = await _api.GetMastersAsync();
            if (masters.Count == 0)
            {
                await botClient.EditMessageText(chatId, callback.Message.MessageId, "К сожалению, сейчас нет доступных мастеров.", cancellationToken: ct);
                return;
            }

            var keyboard = new InlineKeyboardMarkup(masters.Select(m => new[] {
                InlineKeyboardButton.WithCallbackData(m.Name, $"mst_{m.Id}")
            }));

            await botClient.EditMessageText(chatId, callback.Message.MessageId, "Выберите мастера:", replyMarkup: keyboard, cancellationToken: ct);
        }
        // 2. Выбор Мастера (mst_)
        else if (data.StartsWith("mst_"))
        {
            state.MasterId = Guid.Parse(data.Substring(4));

            var keyboard = new List<InlineKeyboardButton[]>();
            for (int i = 0; i < 5; i++) // 5 дней
            {
                var date = DateTime.Today.AddDays(i);
                keyboard.Add(new[] { InlineKeyboardButton.WithCallbackData(date.ToString("dd.MM.yyyy"), $"dt_{date:yyyy-MM-dd}") });
            }
            await botClient.EditMessageText(chatId, callback.Message.MessageId, "Выберите дату:", replyMarkup: new InlineKeyboardMarkup(keyboard), cancellationToken: ct);
        }
        // 3. Выбор Даты (dt_)
        else if (data.StartsWith("dt_"))
        {
            state.SelectedDate = DateTime.Parse(data.Substring(3));
            var slots = await _api.GetSlotsAsync(state.MasterId, state.SelectedDate.Value);

            if (slots.Count == 0)
            {
                await botClient.AnswerCallbackQuery(callback.Id, "На эту дату нет мест", showAlert: true, cancellationToken: ct);
                return;
            }

            var keyboard = slots.Select(slot => new[] {
                InlineKeyboardButton.WithCallbackData(slot, $"tm_{slot}")
            });
            await botClient.EditMessageText(chatId, callback.Message.MessageId, "Выберите время:", replyMarkup: new InlineKeyboardMarkup(keyboard), cancellationToken: ct);
        }
        // 4. Выбор Времени (tm_)
        else if (data.StartsWith("tm_"))
        {
            var timeStr = data.Substring(3);
            state.SelectedTime = state.SelectedDate.Value.Date.Add(TimeSpan.Parse(timeStr));
            state.Step = "AwaitingContact";

            await botClient.EditMessageText(chatId, callback.Message.MessageId,
                $"Выбрано время: {state.SelectedDate:dd.MM.yyyy} в {timeStr}.\n\nПоследний шаг — отправьте свой номер телефона.",
                replyMarkup: null, cancellationToken: ct);

            await botClient.SendMessage(chatId, "Нажмите на кнопку:",
                replyMarkup: new ReplyKeyboardMarkup(new[] { new KeyboardButton("📱 Отправить мой телефон") { RequestContact = true } }) { ResizeKeyboard = true },
                cancellationToken: ct);
        }
    }

    // ==========================================
    // СТАРТ ЗАПИСИ (ГЛАВНОЕ МЕНЮ)
    // ==========================================
    private async Task StartBooking(ITelegramBotClient botClient, long chatId, CancellationToken ct)
    {
        _userStates[chatId] = new BookingState { Step = "ChooseService" };

        // Запрашиваем услуги у API
        var services = await _api.GetServicesAsync();

        if (services.Count == 0)
        {
            await botClient.SendMessage(chatId, "❌ Ошибка: в базе нет доступных услуг. Добавьте их через админ-панель на сайте.", cancellationToken: ct);
            return;
        }

        // Создаем кнопки из полученных услуг
        var keyboard = new InlineKeyboardMarkup(services.Select(s => new[] {
            InlineKeyboardButton.WithCallbackData($"{s.Name} ({s.Price}₽)", $"svc_{s.Id}")
        }));

        await botClient.SendMessage(chatId, "💈 Давайте запишемся!\nШаг 1. Выберите услугу:", replyMarkup: keyboard, cancellationToken: ct);
    }

    private async Task SendAppointmentsList(ITelegramBotClient botClient, long chatId, CancellationToken ct)
    {
        var appointments = await _api.GetAllAppointmentsAsync();
        var activeAppointments = appointments.Where(a => a.Status == "Pending" || a.Status == "Confirmed").ToList();

        if (activeAppointments.Count == 0)
        {
            await botClient.SendMessage(chatId, "Нет активных записей.", cancellationToken: ct);
            return;
        }

        foreach (var app in activeAppointments)
        {
            InlineKeyboardMarkup? keyboard = null;
            if (app.Status == "Pending")
                keyboard = new InlineKeyboardMarkup(new[] { new[] {
                    InlineKeyboardButton.WithCallbackData("✅ Подтвердить", $"status_{app.Id}_Confirmed"),
                    InlineKeyboardButton.WithCallbackData("❌ Отменить", $"status_{app.Id}_Canceled") } });
            else if (app.Status == "Confirmed")
                keyboard = new InlineKeyboardMarkup(new[] { new[] {
                    InlineKeyboardButton.WithCallbackData("❌ Отменить", $"status_{app.Id}_Canceled") } });

            var message = $"📅 {app.StartTime:dd.MM.yyyy HH:mm}\n" +
                          $"👤 Клиент: {app.ClientName} ({app.ClientPhone})\n" +
                          $"💇‍♂️ Мастер: {app.MasterName}\n" +
                          $"✂️ Услуга: {app.ServiceName}\n" +
                          $"Статус: {app.Status}";

            await botClient.SendMessage(chatId, message, replyMarkup: keyboard, cancellationToken: ct);
        }
    }

    private ReplyKeyboardMarkup GetAdminKeyboard() => new(new[] { new KeyboardButton[] { "📋 Записи" } }) { ResizeKeyboard = true };
    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception ex, CancellationToken ct) { _logger.LogError(ex, "TG Error"); return Task.CompletedTask; }

    private class BookingState
    {
        public string Step { get; set; } = "";
        public Guid ServiceId { get; set; }
        public Guid MasterId { get; set; }
        public DateTime? SelectedDate { get; set; }
        public DateTime SelectedTime { get; set; }
    }
}