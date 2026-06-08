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
string dbConnectionString = "Data Source = Measurements.db";

SQLitePCL.Batteries.Init();


// Add services to the container.

//builder.Services.AddSingleton<IDaqDiscovery, DaqDiscovery>();
Console.WriteLine("[Startup] Registering services in DI container...");
builder.Services.AddSingleton<IMachineController, MachineController>();
builder.Services.AddSingleton<IDataAcquisitionService, ModalAcquisitionService>();
builder.Services.AddSingleton<IModalAnalyzer, ModalAnalyzer>();
builder.Services.AddSingleton<IModalExcelReportBuilder, ModalExcelReportBuilder>();
builder.Services.AddSingleton<ITriggerWindowCapture, SingleTriggerWindowCapture>();
builder.Services.AddSingleton<IModalAnalysisService, ModalAnalysisService>();

// 2. Register the DatabaseController using a Factory pattern.
// Because it needs a specific string (dbConnectionString), we tell the container exactly how to build it.
builder.Services.AddSingleton<IDatabaseController>(serviceProvider =>
{
    return new DatabaseController(dbConnectionString);
});

builder.Services.AddSingleton<IMeasurementBroadcaster, SignalRMeasurementBroadcaster>();


builder.Services.AddSingleton<IEngine, Engine>();
Console.WriteLine("[Startup] Services registered.");

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


var app = builder.Build();

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

app.MapHub<LiveMeasurementHub>("/live-measurements");

Console.WriteLine("[Startup] Startup successful. Running the application");
app.Run();
