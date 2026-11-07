var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

// Включаем раздачу статических файлов из папки wwwroot
app.UseDefaultFiles(); // Ищет index.html по умолчанию
app.UseStaticFiles();  // Разрешает отдавать css, js, картинки

app.Run();