using IPLockScreenService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// Configure for Windows Service
builder.Services.AddWindowsService(options =>
{
	options.ServiceName = "IP Lock Screen Background Service";
});

// Add the worker service
builder.Services.AddHostedService<Worker>();

// Configure logging
builder.Services.AddLogging(configure => configure.AddConsole());

var host = builder.Build();
await host.RunAsync();
