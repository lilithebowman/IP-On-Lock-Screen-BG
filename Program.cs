using IPLockScreenService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// .NET 6 hosting pattern
IHost host = Host.CreateDefaultBuilder(args)
	.UseWindowsService(options =>
	{
		options.ServiceName = "IP Lock Screen Background Service";
	})
	.ConfigureServices(services =>
	{
		services.AddHostedService<Worker>();
		services.AddLogging(configure => configure.AddConsole());
	})
	.Build();

await host.RunAsync();
