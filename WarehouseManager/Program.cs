using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using WarehouseManager.Conventions;
using WarehouseManager.Data;
using WarehouseManager.Middleware;
using WarehouseManager.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddMvc();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions => 
        npgsqlOptions.EnableRetryOnFailure()));

builder.Services.AddScoped<IItemService, ItemsService>();
builder.Services.AddScoped<IZoneService, ZoneService>();
builder.Services.AddScoped<IDefectService, DefectService>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddControllers(options =>
{
    options.Conventions.Add(new ApiPrefixConvention(new RouteAttribute("api/v{version:apiVersion}")));
});

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();

    app.MapOpenApi();
    app.MapScalarApiReference(); 
}

app.UseExceptionHandler();
app.UseMiddleware<LoggingMiddleware>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();