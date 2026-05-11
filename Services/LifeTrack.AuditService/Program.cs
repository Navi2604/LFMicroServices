using LifeTrack.AuditService.Data;
using LifeTrack.Shared.Extensions;
using LifeTrack.Shared.Middleware;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// AuditService has its OWN database: LifeTrackAuditDB
builder.Services.AddDbContext<AuditDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration
            .GetConnectionString("DefaultConnection")));

builder.Services.AddJwtAuthentication(
    builder.Configuration);
builder.Services.AddAngularCors();
builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Auto create LifeTrackAuditDB on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<AuditDbContext>();
    db.Database.EnsureCreated();
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseAuthorization();
// NOTE: AuditService does NOT use AuditMiddleware
// That would cause infinite loop!
app.MapControllers();
app.Run();