// ============================================================
// SiteService.API / Program.cs — WITH CACHING
// ============================================================

using LifeTrack.SiteService.Repositories;
using LifeTrack.SiteService.Repositories.Interfaces;
using LifeTrack.SiteService.Services;
using LifeTrack.SiteService.Services.Interfaces;
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
builder.Services.AddScoped<ISiteRepository, SiteRepository>();
builder.Services.AddScoped<ISiteProtocolRepository, SiteProtocolRepository>();
builder.Services.AddScoped<IDeviationRepository, DeviationRepository>();
builder.Services.AddScoped<ISiteService, SiteService>();
builder.Services.AddScoped<ISiteProtocolService, SiteProtocolService>();
builder.Services.AddScoped<IDeviationService, DeviationService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<AuditHttpClient>();

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