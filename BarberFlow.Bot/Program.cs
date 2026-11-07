using BarberFlow.Bot;
using BarberFlow.Bot.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<Worker>();

builder.Services.AddHttpClient<ApiService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["BotConfiguration:ApiBaseUrl"]!);

    // Наш секретный ключ для доступа к API
    var apiKey = builder.Configuration["BotConfiguration:ApiKey"];
    if (!string.IsNullOrEmpty(apiKey))
    {
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
    }
});

var host = builder.Build();
host.Run();