using BluewardSentinel.Core.Interfaces;
using BluewardSentinel.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Nexus Sentinel API", Version = "v1", Description = "Enterprise Real-time AI Log Gathering & Anomaly Diagnostic Platform" });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

builder.Services.AddHttpClient();
builder.Services.AddSingleton<ISentinelRepository, SentinelSqliteRepository>();
builder.Services.AddSingleton<IClusteringEngine, ClusteringEngine>();
builder.Services.AddSingleton<IAiDiagnosticEngine, GeminiAiDiagnosticEngine>();
builder.Services.AddSingleton<IWebhookService, WebhookService>();

var app = builder.Build();

var repo = app.Services.GetRequiredService<ISentinelRepository>();
await repo.InitializeAsync();

app.UseCors("AllowAll");
app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment() || true)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Nexus Sentinel v1");
    });
}

app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
