using CncMeasurement.Data;
using Microsoft.Extensions.DependencyInjection;


var builder = WebApplication.CreateBuilder(args);
var services = new ServiceCollection();
string dbConnectionString = "Data Source = Measurements.db";

// Add all the components
services.AddSingleton<IDatabaseController>(provider => 
new DatabaseController(dbConnectionString)
);


// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
