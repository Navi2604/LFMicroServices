// ============================================================
// AuditService.API / Program.cs — WITH CACHING
// ============================================================

using LifeTrack.AuditService.Repositories;
using LifeTrack.AuditService.Repositories.Interfaces;
using LifeTrack.AuditService.Services;
using LifeTrack.AuditService.Services.Interfaces;
using LifeTrack.Shared.Data;
using LifeTrack.Shared.Extensions;
using LifeTrack.Shared.Filters;
using LifeTrack.Shared.Middleware;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AuditDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("AuditConnection")));

// ✅ ADD MEMORY CACHE
builder.Services.AddMemoryCache();

builder.Services.AddControllers(options =>
{
    options.Filters.Add<GlobalExceptionFilter>();
    options.Filters.Add<ValidationFilter>();
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy =
        System.Text.Json.JsonNamingPolicy.CamelCase;
});

builder.Services.AddJwtAuth(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddScoped<GlobalExceptionFilter>();
builder.Services.AddScoped<ValidationFilter>();

// ✅ REGISTER REPOSITORIES & SERVICES WITH CACHE
builder.Services.AddScoped<IAuditRepository, AuditRepository>();
builder.Services.AddScoped<IAuditService, AuditService>();

builder.Services.AddCors(options =>
    options.AddPolicy("AllowAngular", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your JWT token here"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UseGlobalExceptionHandler();
app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();