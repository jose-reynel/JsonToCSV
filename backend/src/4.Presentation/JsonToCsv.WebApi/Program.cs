using JsonToCsv.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddInfrastructure();

// Orígenes permitidos configurables (Cors:AllowedOrigins); por defecto el frontend local.
var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
              ?? new[] { "http://localhost:5200", "https://localhost:7200" };
builder.Services.AddCors(o => o.AddPolicy("AllowBlazorApp", p =>
    p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("Content-Disposition")));

var app = builder.Build();

app.UseCors("AllowBlazorApp");
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

public partial class Program { }
