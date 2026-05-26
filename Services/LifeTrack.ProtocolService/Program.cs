// ============================================================
// ProtocolService.API / Program.cs
// ADDED: DocumentService + DocumentRepository registration
// ============================================================

using LifeTrack.ProtocolService.Repositories;
using LifeTrack.ProtocolService.Repositories.Interfaces;
using LifeTrack.ProtocolService.Services;
using LifeTrack.ProtocolService.Services.Interfaces;
using LifeTrack.Shared.Data;
using LifeTrack.Shared.Extensions;
using LifeTrack.Shared.Filters;
using LifeTrack.Shared.Helpers;
using LifeTrack.Shared.Middleware;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<LifeTrackDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

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

// Repositories & Services
builder.Services.AddScoped<IProtocolRepository, ProtocolRepository>();
builder.Services.AddScoped<IProtocolService, ProtocolService>();
builder.Services.AddScoped<IKPIReportRepository, KPIReportRepository>();
builder.Services.AddScoped<IKPIReportService, KPIReportService>();
// NEW: Documents
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IDocumentService, DocumentService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<AuditHttpClient>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5008/");
    client.Timeout = TimeSpan.FromSeconds(3);
});

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