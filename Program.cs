using Microsoft.EntityFrameworkCore;
using phoenix_sangam_api.Data;
using phoenix_sangam_api.Configuration;
using phoenix_sangam_api.Extensions;
using phoenix_sangam_api.Services;
using phoenix_sangam_api.Middleware;
using phoenix_sangam_api.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using phoenix_sangam_api;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.MaxDepth = 32;
    });
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "PhoenixSangamApi", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid JWT token. Example: Bearer eyJhbGci..."
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Add generic database support
builder.Services.AddGenericDatabase(builder.Configuration);

// Register services
builder.Services.AddScoped<ILoanService, LoanService>();
builder.Services.AddScoped<IValidationService, ValidationService>();
builder.Services.AddScoped<ICacheService, MemoryCacheService>();





// Configure email settings
var emailSettings = builder.Configuration.GetSection("EmailSettings").Get<EmailSettings>();
if (emailSettings == null)
{
    // Fallback to default settings if configuration is missing
    emailSettings = new EmailSettings
    {
        SmtpServer = "smtp.gmail.com",
        SmtpPort = 587,
        SmtpUsername = "your-email@gmail.com",
        SmtpPassword = "your-app-password",
        SenderName = "Phoenix Sangam",
        SenderEmail = "noreply@phoenixsangam.com",
        EnableSsl = true
    };
}
builder.Services.AddSingleton(emailSettings);
builder.Services.AddScoped<IEmailService, EmailService>();

// Register repositories
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

// Register user activity service
builder.Services.AddScoped<IUserActivityService, UserActivityService>();

// Add memory cache
builder.Services.AddMemoryCache();

// Configure CORS from appsettings.json
var corsSettings = builder.Configuration.GetSection("CorsSettings").Get<CorsSettings>();
if (corsSettings == null)
{
    // Fallback to default settings if configuration is missing
    corsSettings = new CorsSettings
    {
        AllowedOrigins = new[] { "http://localhost:3000" },
        AllowedMethods = new[] { "GET", "POST", "PUT", "DELETE", "OPTIONS" },
        AllowedHeaders = new[] { "*" },
        AllowCredentials = true
    };
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowConfiguredOrigins", policy =>
    {
        policy.WithOrigins(corsSettings.AllowedOrigins)
              .WithHeaders(corsSettings.AllowedHeaders)
              .WithMethods(corsSettings.AllowedMethods);
        
        if (corsSettings.AllowCredentials)
        {
            policy.AllowCredentials();
        }
    });
});

// JWT Authentication configuration
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["Secret"];
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!))
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Add static files support for wwwroot
app.UseStaticFiles();

// Add custom middleware
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCors("AllowConfiguredOrigins");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
