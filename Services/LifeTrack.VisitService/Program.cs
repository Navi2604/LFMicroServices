// ============================================================
// VisitService.API / Program.cs — WITH CACHING (FIXED)
// ============================================================

using LifeTrack.Shared.Data;
using LifeTrack.VisitService.Repositories;
using LifeTrack.VisitService.Repositories.Interfaces;
using LifeTrack.VisitService.Services;
using LifeTrack.VisitService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ✅ ADD MEMORY CACHE
builder.Services.AddMemoryCache();

// Add DbContext
builder.Services.AddDbContext<LifeTrackDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ✅ REGISTER REPOSITORIES & SERVICES WITH CACHE
// ✅ FIXED: Use full namespace to avoid conflict with VisitService namespace
builder.Services.AddScoped<IVisitRepository, VisitRepository>();
builder.Services.AddScoped<IVisitService, LifeTrack.VisitService.Services.VisitService>();

// Add CORS if needed
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Add Authorization
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = builder.Configuration["Auth:Authority"];
        options.Audience = builder.Configuration["Auth:Audience"];
        options.RequireHttpsMetadata = false;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();