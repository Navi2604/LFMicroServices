using LifeTrack.AuthService.Services;
using LifeTrack.AuthService.Services.Interfaces;
using LifeTrack.Shared.Data;
using LifeTrack.Shared.Extensions;
using LifeTrack.Shared.Middleware;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Shared DB: LifeTrackDB
builder.Services.AddDbContext<LifeTrackDbContext>(
    options => options.UseSqlServer(
        builder.Configuration
            .GetConnectionString("DefaultConnection")));

// Services
builder.Services.AddScoped<IAuthService, AuthService>();

// JWT + CORS
builder.Services.AddJwtAuthentication(
    builder.Configuration);
builder.Services.AddAngularCors();
builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Auto create LifeTrackDB on startup
// AuthService owns the shared DB
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<LifeTrackDbContext>();
    db.Database.EnsureCreated();
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AuditMiddleware>(
    app.Services.GetRequiredService<IConfiguration>(),
    "AuthService");
app.MapControllers();
app.Run();