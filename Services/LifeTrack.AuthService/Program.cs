// ============================================================

// AuthService.API / Program.cs

// FIXED: AuditHttpClient timeout set to 3 seconds so a

// down AuditService never delays login responses.

// ============================================================

using System.Text;

using LifeTrack.AuthService.Repositories;

using LifeTrack.AuthService.Repositories.Interfaces;

using LifeTrack.AuthService.Services;

using LifeTrack.AuthService.Services.Interfaces;

using LifeTrack.Shared.Data;

using LifeTrack.Shared.Helpers;

using Microsoft.AspNetCore.Authentication.JwtBearer;

using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;

using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services

builder.Services.AddControllers()

    .ConfigureApiBehaviorOptions(options =>

    {

        options.InvalidModelStateResponseFactory = context =>

        {

            var errors = context.ModelState

                .Where(e => e.Value?.Errors.Count > 0)

                .SelectMany(e => e.Value!.Errors.Select(er => er.ErrorMessage))

                .ToList();

            return new BadRequestObjectResult(new

            {

                success = false,

                message = string.Join(" ", errors),

                errors = errors

            });

        };

    });

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

builder.Services.AddMemoryCache();

builder.Services.AddHttpContextAccessor();

// Add DbContext

builder.Services.AddDbContext<LifeTrackDbContext>(options =>

    options.UseSqlServer(

        builder.Configuration.GetConnectionString("DefaultConnection")));

// Register AuditHttpClient with a SHORT timeout (3 s).

// AuditHttpClient is fire-and-forget — if AuditService is down the

// Task.Run call will fail silently. Without this timeout the underlying

// HttpClient socket waits up to 100 s (OS default) before giving up,

// which was causing the first login attempt to appear slow.

builder.Services.AddHttpClient<AuditHttpClient>(client =>

{

    client.BaseAddress = new Uri("http://localhost:5008/");

    client.Timeout = TimeSpan.FromSeconds(3);

});

// Register repositories and services

builder.Services.AddScoped<IAuthRepository, AuthRepository>();

builder.Services.AddScoped<IAuthService, LifeTrack.AuthService.Services.AuthService>();

// Add CORS

builder.Services.AddCors(options =>

{

    options.AddPolicy("AllowAll", policy =>

        policy.AllowAnyOrigin()

              .AllowAnyMethod()

              .AllowAnyHeader());

});

// Add JWT Authentication

var jwtKey = builder.Configuration["Jwt:Key"] ?? "LifeTrackSuperSecretKey2024!@#$%^&*()";

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "LifeTrack";

var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "LifeTrack";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)

    .AddJwtBearer(options =>

    {

        options.TokenValidationParameters = new TokenValidationParameters

        {

            ValidateIssuer = true,

            ValidateAudience = true,

            ValidateLifetime = true,

            ValidateIssuerSigningKey = true,

            ValidIssuer = jwtIssuer,

            ValidAudience = jwtAudience,

            IssuerSigningKey = new SymmetricSecurityKey(

                                           Encoding.UTF8.GetBytes(jwtKey))

        };

    });

builder.Services.AddAuthorization();

var app = builder.Build();

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
