using System.Reflection;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using HomeGameBot.Commands;
using HomeGameBot.Data;
using HomeGameBot.Interactivity;
using HomeGameBot.Interactivity.Buttons;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HomeGameBot.Services;

internal sealed class BotService : BackgroundService
{
    private readonly ILogger<BotService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly DiscordClient _discordClient;
    private readonly HomeGameContext _homeGameContext;
    private readonly ConfigurationService _configurationService;
    
    public BotService(
        ILogger<BotService> logger,
        IServiceProvider serviceProvider,
        DiscordClient discordClient,
        HomeGameContext homeGameContext,
        ConfigurationService configurationService)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _discordClient = discordClient;
        _homeGameContext = homeGameContext;
        _configurationService = configurationService;

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

        _discordClient.UseInteractivity();

        SlashCommandsExtension slashCommands = _discordClient.UseSlashCommands(new SlashCommandsConfiguration
        {
            Services = _serviceProvider
        });
        
        _logger.LogInformation("Registering commands...");
        slashCommands.RegisterCommands<InfoCommand>();
        slashCommands.RegisterCommands<PodCommand>();
        
        _logger.LogInformation("Connecting to Discord...");
        _discordClient.Ready += OnReady;

        RegisterEvents(slashCommands);
        
        await _discordClient.ConnectAsync();
    }

    private Task OnReady(DiscordClient sender, ReadyEventArgs e)
    {
        _logger.LogInformation("Discord client ready");
        _ = CheckPods();
        return Task.CompletedTask;
    }
    
    private void RegisterEvents(SlashCommandsExtension slashCommands)
    {
        slashCommands.AutocompleteErrored += (_, args) =>
        {
            _logger.LogError(args.Exception, "An exception was thrown when performing an autocomplete");
            if (args.Exception is DiscordException discordException)
            {
                _logger.LogError("API response: {Message}", discordException.JsonMessage);
            }

            return Task.CompletedTask;
        };
        
        slashCommands.SlashCommandInvoked += (_, args) =>
        {
            var optionsString = "";
            if (args.Context.Interaction?.Data?.Options is { } options)
            {
                optionsString = $" {string.Join(" ", options.Select(o => $"{o?.Name}: '{o?.Value}'"))}";
            }

            _logger.LogInformation("{User} ran slash command /{Name}{OptionsString}", args.Context.User, args.Context.CommandName,
                optionsString);
            return Task.CompletedTask;
        };

        slashCommands.ContextMenuInvoked += (_, args) =>
        {
            DiscordInteractionResolvedCollection? resolved = args.Context.Interaction?.Data?.Resolved;
            var properties = new List<string>();
            if (resolved?.Attachments?.Count > 0)
            {
                properties.Add($"attachments: {string.Join(", ", resolved.Attachments.Select(a => a.Value.Url))}");
            }

            if (resolved?.Channels?.Count > 0)
            {
                properties.Add($"channels: {string.Join(", ", resolved.Channels.Select(c => c.Value.Name))}");
            }

            if (resolved?.Members?.Count > 0)
            {
                properties.Add($"members: {string.Join(", ", resolved.Members.Select(m => m.Value.Id))}");
            }

            if (resolved?.Messages?.Count > 0)
            {
                properties.Add($"messages: {string.Join(", ", resolved.Messages.Select(m => m.Value.Id))}");
            }

            if (resolved?.Roles?.Count > 0)
            {
                properties.Add($"roles: {string.Join(", ", resolved.Roles.Select(r => r.Value.Id))}");
            }

            if (resolved?.Users?.Count > 0)
            {
                properties.Add($"users: {string.Join(", ", resolved.Users.Select(r => r.Value.Id))}");
            }

            _logger.LogInformation("{User} invoked context menu '{Name}' with resolved {Properties}", args.Context.User,
                args.Context.CommandName, string.Join("; ", properties));

            return Task.CompletedTask;
        };

        slashCommands.ContextMenuErrored += (_, args) =>
        {
            ContextMenuContext context = args.Context;
            if (args.Exception is ContextMenuExecutionChecksFailedException)
            {
                context.CreateResponseAsync("You do not have permission to use this command.", true);
                return Task.CompletedTask; // no need to log ChecksFailedException
            }

            string? name = context.Interaction.Data.Name;
            _logger.LogError(args.Exception, "An exception was thrown when executing context menu '{Name}'", name);
            if (args.Exception is DiscordException discordException)
            {
                _logger.LogError("API response: {Message}", discordException.JsonMessage);
            }

            return Task.CompletedTask;
        };

        slashCommands.SlashCommandErrored += (_, args) =>
        {
            InteractionContext context = args.Context;
            if (args.Exception is SlashExecutionChecksFailedException)
            {
                context.CreateResponseAsync("You do not have permission to use this command.", true);
                return Task.CompletedTask; // no need to log SlashExecutionChecksFailedException
            }

            string? name = context.Interaction.Data.Name;
            _logger.LogError(args.Exception, "An exception was thrown when executing slash command '{Name}'", name);
            if (args.Exception is DiscordException discordException)
            {
                _logger.LogError("API response: {Message}", discordException.JsonMessage);
            }

            return Task.CompletedTask;
        };
    }

    private async Task CheckPods()
    {
        var pods = _homeGameContext.Pods.Where(p => p.When < DateTime.UtcNow).ToList();
        _logger.LogInformation("Found {Count} expired pods", pods.Count);
        
        foreach (var pod in pods)
        {
            await RemovePodButtonsFromExpiredPod(pod);
        }
        
        _homeGameContext.RemoveRange(pods);
        await _homeGameContext.SaveChangesAsync();
        
        _logger.LogInformation("Removed {Count} expired pods", pods.Count);

        var activePods = _homeGameContext.Pods;
        
        _logger.LogInformation("Found {Count} active pods", activePods.Count());
        foreach (var pod in activePods)
        {
            await UpdateActivePod(pod);
        }
    }

    private async Task RemovePodButtonsFromExpiredPod(Pod pod)
    {
        var guild = _discordClient.Guilds.First().Value;
        _logger.LogInformation("Guild found: {GuildId}", guild.Id);
        
        var guildConfig = _configurationService.GetGuildConfiguration(guild);
        if (guildConfig is null)
        {
            _logger.LogWarning("Guild configuration not found for guild {GuildId}", guild.Id);
            return;
        }

        var channel = guild.Channels.First(c => c.Value.Id == guildConfig.ChannelId).Value;

        var message = await channel.GetMessageAsync(pod.MessageId);
        if (message is null)
        {
            _logger.LogWarning("Message not found for pod {PodId}", pod.Id);
            return;
        }

        var podEmbed = DiscordPodEmbed.GetDiscordPodEmbed(pod, pod.Host.DisplayName);
        var builder = new DiscordMessageBuilder().WithEmbed(podEmbed);

        _logger.LogInformation("Updating pod {PodId} to remove buttons", pod.Id);
        await message.ModifyAsync(builder);
    }

    private async Task UpdateActivePod(Pod pod)
    {
        var guild = _discordClient.Guilds.First().Value;
        var guildConfig = _configurationService.GetGuildConfiguration(guild);
        if (guildConfig is null)
        {
            _logger.LogWarning("Guild configuration not found for guild {GuildId}", guild.Id);
            return;
        }
        
        var channel = guild.Channels.First(c => c.Value.Id == guildConfig.ChannelId).Value;
        
        var message = await channel.GetMessageAsync(pod.MessageId);
        if (message is null)
        {
            _logger.LogWarning("Message not found for pod {PodId}", pod.Id);
            return;
        }
        
        var podEmbed = DiscordPodEmbed.GetDiscordPodEmbed(pod, pod.Host.DisplayName);
        var builder = new DiscordMessageBuilder().WithEmbed(podEmbed);
        builder = DiscordPodButtons.GetPodButtons(_discordClient, _homeGameContext, builder);
        
        _logger.LogInformation("Updating pod {PodId} with buttons", pod.Id);
        await message.ModifyAsync(builder);
    }
}