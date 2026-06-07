using CncMeasurement.Core.Interfaces;
using CncMeasurement.Data;
using CncMeasurement.Engine;
//using CncMeasurement.Hardware;
//using CncMeasurement.Hardware.Acquisition;
using CncMeasurement.Machine;
using CncMeasurement.Processing;
using CncMeasurement.Web.Hubs;
using CncMeasurement.MockHardware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;


Console.WriteLine("[Startup] Creating WebApplication builder...");
Console.WriteLine($"[Startup] ProcessId={Environment.ProcessId} ThreadId={Environment.CurrentManagedThreadId} creating WebApplication builder...");
var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
Console.WriteLine("[Startup] Builder created and logging configured.");
Console.WriteLine("[Startup] Builder created.");
string dbConnectionString = "Data Source = Measurements.db";

SQLitePCL.Batteries.Init();


// Add services to the container.

//builder.Services.AddSingleton<IDaqDiscovery, DaqDiscovery>();
builder.Services.AddSingleton<IMachineController, MachineController>();
builder.Services.AddSingleton<ILiveSignalProcessor, LiveSignalProcessor>();
builder.Services.AddSingleton<IDataAcquisitionService, MockDataAcquisitionService >();

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

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        // For local development only!
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

Console.WriteLine("[Startup] Calling builder.Build() (wrapped in Task with 10s probe)...");
WebApplication app = null!
;
var buildTask = Task.Run(() => builder.Build());
if (!buildTask.Wait(TimeSpan.FromSeconds(10)))
{
    Console.WriteLine("[Startup] builder.Build() is taking longer than 10s. Listing loaded assemblies for diagnosis:");
    foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
    {
        Console.WriteLine($"  Assembly: {a.GetName().Name} {a.FullName}");
    }
    Console.WriteLine("[Startup] Still waiting for builder.Build() to complete...");
}
app = buildTask.Result; // will re-throw any exceptions
Console.WriteLine("[Startup] builder.Build() returned.");

app.UseCors("AllowAll");

try
{
    using (var scope = app.Services.CreateScope())
    {
        Console.WriteLine("[Startup] Resolving IDatabaseController from DI...");
        var dbController = scope.ServiceProvider.GetRequiredService<IDatabaseController>();
        Console.WriteLine("[Startup] IDatabaseController resolved.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[Startup] Exception while resolving services: {ex}");
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapHub<LiveMeasurementHub>("/hubs/live-measurements");
app.Run();
