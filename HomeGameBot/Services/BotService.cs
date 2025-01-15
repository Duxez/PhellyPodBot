using System.Reflection;
using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity.Extensions;
using HomeGameBot.Commands;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HomeGameBot.Services;

internal sealed class BotService : BackgroundService, IEventHandler<ClientStartedEventArgs>
{
    private readonly ILogger<BotService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly DiscordClient _discordClient;
    
    public BotService(ILogger<BotService> logger, IServiceProvider serviceProvider, DiscordClient discordClient)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _discordClient = discordClient;

        var attribute = typeof(BotService).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        Version = attribute?.InformationalVersion ?? "Unknown";
    }
    
    public DateTimeOffset StartedAt { get; private set; }
    
    public string Version { get; }
    
    public override Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.WhenAll(_discordClient.DisconnectAsync(), base.StopAsync(cancellationToken));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        StartedAt = DateTimeOffset.UtcNow;
        _logger.LogInformation("HomeGameBot v{Version} is starting...", Version);
        
        await _discordClient.ConnectAsync();
    }

    public Task HandleEventAsync(DiscordClient sender, ClientStartedEventArgs eventArgs)
    {
        _logger.LogInformation("Discord client ready");
        return Task.CompletedTask;
    }
}