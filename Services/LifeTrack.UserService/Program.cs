using LifeTrack.Shared.Data;
using LifeTrack.Shared.Extensions;
using LifeTrack.Shared.Middleware;
using LifeTrack.UserService.Services;
using LifeTrack.UserService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Same LifeTrack DB
builder.Services.AddDbContext<LifeTrackDbContext>(
    options => options.UseSqlServer(
        builder.Configuration
            .GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddJwtAuthentication(
    builder.Configuration);
builder.Services.AddAngularCors();
builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AuditMiddleware>(
    app.Services.GetRequiredService<IConfiguration>(),
    "UserService");
app.MapControllers();
app.Run();