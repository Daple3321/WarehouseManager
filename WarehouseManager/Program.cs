using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WarehouseManager.Conventions;
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

builder.Services.AddScoped<IItemService, ItemsService>();

// builder.Services.AddExceptionHandler(options =>
// {
//     
// });

builder.Services.AddControllers(options =>
{
    options.Conventions.Add(new ApiPrefixConvention(new RouteAttribute("api/v{version:apiVersion}")));
});

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler("/error");
app.UseMiddleware<LoggingMiddleware>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();