using CncMeasurement.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.OpenApi;
using CncMeasurement.Hardware;
using CncMeasurement.Machine;


var builder = WebApplication.CreateBuilder(args);
string dbConnectionString = "Data Source = Measurements.db";

SQLitePCL.Batteries.Init();


// Add services to the container.
builder.Services.AddControllers();

DaqMeasurement MeasurementProvider = new DaqMeasurement();
DaqDiscovery DiscoveryProvider = new DaqDiscovery();
DatabaseController DatabaseProvider = new DatabaseController(dbConnectionString);
MachineController MachineProvider = new MachineController();


//Register services:
builder.Services.AddSingleton<IDatabaseController>(DatabaseProvider);
builder.Services.AddSingleton<IDaqDiscovery>(DiscoveryProvider);
builder.Services.AddSingleton<IDaqMeasurement>(MeasurementProvider);

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
