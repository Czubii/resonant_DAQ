using CncMeasurement.Data;
using CncMeasurement.Engine;
using CncMeasurement.Hardware;
using CncMeasurement.Hardware.Acquisition;
using CncMeasurement.Machine;
using CncMeasurement.Processing;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc;


var builder = WebApplication.CreateBuilder(args);
string dbConnectionString = "Data Source = Measurements.db";

SQLitePCL.Batteries.Init();


// Add services to the container.


// instantiate the singletons for all services
DaqDiscovery DiscoveryProvider = new DaqDiscovery();
DatabaseController DatabaseProvider = new DatabaseController(dbConnectionString);
MachineController MachineProvider = new MachineController();
Processor Processing = new Processor();

Engine engine = new Engine(MachineProvider, DatabaseProvider, Processing);

//Register services:
builder.Services.AddSingleton<IDatabaseController>(DatabaseProvider);
builder.Services.AddSingleton<IDaqDiscovery>(DiscoveryProvider);
builder.Services.AddSingleton<IMachineController>(MachineProvider);
builder.Services.AddSingleton<IProcessing>(Processing);

builder.Services.AddSingleton<IEngine>(engine);

// Use newtonsoft for JSON serialization in HTTP requests
builder.Services.AddControllersWithViews()
        .AddNewtonsoftJson(options =>
        {
            // Configure Newtonsoft.Json options here
            options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
        });

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
