using CncMeasurement.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.OpenApi;
using CncMeasurement.Hardware;

var builder = WebApplication.CreateBuilder(args);
string dbConnectionString = "Data Source = Measurements.db";

SQLitePCL.Batteries.Init();


// Add services to the container.
builder.Services.AddControllers();

//Register services:
builder.Services.AddSingleton<IDatabaseController>(provider =>
new DatabaseController(dbConnectionString)
);
builder.Services.AddSingleton<IDaqDiscovery>(provider => new DaqDiscovery());

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbController = scope.ServiceProvider.GetRequiredService<IDatabaseController>();
    dbController.InitializeCollections();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
