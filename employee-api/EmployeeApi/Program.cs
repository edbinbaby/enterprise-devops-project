using EmployeeApi.Data;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();

// SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect("redis:6379"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Prometheus middleware
app.UseHttpMetrics();

app.MapControllers();

// Prometheus endpoint
app.MapMetrics();

app.Run();
