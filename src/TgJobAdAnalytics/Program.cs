using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using TgJobAdAnalytics.Services;
using TgJobAdAnalytics.Services.Stacks;
using TgJobAdAnalytics.Utils;


var host = HostHelper.BuildHost(args);

using var scope = host.Services.CreateScope();
var services = scope.ServiceProvider;

await HostHelper.ApplyDatabaseMigrations(services);

//var stackValidator = services.GetRequiredService<StackMappingStartupValidator>();
//stackValidator.ValidateOrThrow();

var orchestrator = services.GetRequiredService<ProcessOrchestrator>();
var logger = services.GetRequiredService<ILogger<Program>>();

using var cancellationToken = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cancellationToken.Cancel();
};

var startTime = Stopwatch.GetTimestamp();
await orchestrator.Run(["assign-dotnet-to-chats"], cancellationToken.Token);

LogExecutionDuration(logger, Stopwatch.GetElapsedTime(startTime));


static void LogExecutionDuration(ILogger logger, TimeSpan elapsed)
{ 
    string elapsedMessage;

    if (elapsed.TotalHours >= 1)
        elapsedMessage = $"{(int)elapsed.TotalHours} hours {elapsed.Minutes} minutes {elapsed.Seconds} seconds";
    else if (elapsed.TotalMinutes >= 1)
        elapsedMessage = $"{(int)elapsed.TotalMinutes} minutes {elapsed.Seconds} seconds";
    else
        elapsedMessage = $"{elapsed.TotalSeconds:F3} seconds";

    logger.LogInformation("Completed in {Elapsed}", elapsedMessage);    
}
