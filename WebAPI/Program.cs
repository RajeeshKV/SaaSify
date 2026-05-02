using Serilog;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;
using Application.Common.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(Path.Combine("logs", "log.txt"), rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "MultiTenantSaaS")
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT bearer token."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("ClientApp", corsBuilder =>
    {
        corsBuilder
            .WithOrigins(
                "https://saasify.rajeesh.online",
                "http://localhost:3000",
                "http://localhost:5173")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Add infrastructure services
builder.Services.AddInfrastructure(builder.Configuration);

// Add JWT authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// Add rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy<string>("tenant", context =>
    {
        var tenantIdClaim = context.User?.FindFirst("TenantId")?.Value;
        var partitionKey = string.IsNullOrEmpty(tenantIdClaim) ? "anonymous" : tenantIdClaim;
        
        // For now, use standard rate limiting. Plan-based limits can be implemented
        // with a custom rate limiter or middleware that checks subscription plans
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100, // Default limit
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10
            });
    });
});


// Register handlers
builder.Services.AddScoped<GetProjectByIdQueryHandler>();
builder.Services.AddScoped<GetAllProjectsQueryHandler>();
builder.Services.AddScoped<CreateProjectCommandHandler>();
builder.Services.AddScoped<UpdateProjectCommandHandler>();
builder.Services.AddScoped<UpdateManyProjectCommandHandler>();
builder.Services.AddScoped<DeleteProjectCommandHandler>();
builder.Services.AddScoped<DeleteManyProjectCommandHandler>();
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<CorrelationMiddleware>();
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors("ClientApp");
app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>();
app.UseRateLimiter();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapControllers();

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
