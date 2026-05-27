using CncMeasurement.Data;
using CncMeasurement.Hardware;
using CncMeasurement.Hardware.Acquisition;
using CncMeasurement.Machine;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;


var builder = WebApplication.CreateBuilder(args);
string dbConnectionString = "Data Source = Measurements.db";

SQLitePCL.Batteries.Init();


// Add services to the container.
builder.Services.AddControllers();


DaqDiscovery DiscoveryProvider = new DaqDiscovery();
DatabaseController DatabaseProvider = new DatabaseController(dbConnectionString);
MachineController MachineProvider = new MachineController();


//Register services:
builder.Services.AddSingleton<IDatabaseController>(DatabaseProvider);
builder.Services.AddSingleton<IDaqDiscovery>(DiscoveryProvider);
builder.Services.AddSingleton<ImachineController>(MachineProvider);

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
