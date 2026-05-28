using CncMeasurement.Core.Interfaces;
using CncMeasurement.Data;
using CncMeasurement.Engine;
using CncMeasurement.Hardware;
using CncMeasurement.Hardware.Acquisition;
using CncMeasurement.Machine;
using CncMeasurement.Processing;
using CncMeasurement.Web.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;


var builder = WebApplication.CreateBuilder(args);
string dbConnectionString = "Data Source = Measurements.db";

SQLitePCL.Batteries.Init();


// Add services to the container.

builder.Services.AddSingleton<IDaqDiscovery, DaqDiscovery>();
builder.Services.AddSingleton<IMachineController, MachineController>();
builder.Services.AddSingleton<IProcessing, Processor>();
builder.Services.AddSingleton<IDataAcquisitionService, NIDataAcquisitionService>();

// 2. Register the DatabaseController using a Factory pattern.
// Because it needs a specific string (dbConnectionString), we tell the container exactly how to build it.
builder.Services.AddSingleton<IDatabaseController>(serviceProvider =>
{
    return new DatabaseController(dbConnectionString);
});

builder.Services.AddSingleton<IMeasurementBroadcaster, SignalRMeasurementBroadcaster>();


builder.Services.AddSingleton<IEngine, Engine>();


builder.Services.AddSignalR();

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
app.MapHub<LiveMeasurementHub>("/hubs/live-measurements");
app.Run();
