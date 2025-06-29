using System.Globalization;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using HomeGameBot.Data;
using HomeGameBot.Extensions;
using HomeGameBot.Interactivity;
using HomeGameBot.Interactivity.Buttons;
using HomeGameBot.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HomeGameBot.Commands;

internal sealed class PodCommand(HomeGameContext dbContext) : ApplicationCommandModule
{
    private readonly HomeGameContext _dbContext;
    private readonly ConfigurationService _configurationService;
    private readonly ILogger<PodCommand> _logger;
    
    public PodCommand(HomeGameContext dbContext, ConfigurationService configurationService, ILogger<PodCommand> logger)
    {
        _dbContext = dbContext;
        _configurationService = configurationService;
        _logger = logger;
    }
    
    [SlashCommand("pod", "Create a new at home kitchentable pod.")]
    [SlashRequireGuild]
    public async Task PodAsync(InteractionContext context)
    {
        var modal = new DiscordModalBuilder(context.Client);
        modal.WithTitle("Create a new pod");
        
        var timezoneInfo = TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");
        var now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, timezoneInfo);
        
        DiscordModalTextInput podSizeInput = modal.AddInput(
            "Number of players", 
            "How many players are in the pod?", 
            null,
            true,
            TextInputStyle.Short,
            1, 2);
        DiscordModalTextInput podTypeInput = modal.AddInput(
            "What MTG format?", 
            "What MTG format will be played?", 
            null,
            true,
            TextInputStyle.Short,
            1, 255);
        DiscordModalTextInput podLocationInput = modal.AddInput(
            "Where? -> City + Area", 
            "Example: Tilburg Zuid (do not leave your address here!)", 
            null,
            true,
            TextInputStyle.Short,
            1, 255);
        DiscordModalTextInput podWhenInput = modal.AddInput(
            "Date", 
            "When is the pod being held?", 
            now.ToString("dd MMM"),
            true,
            TextInputStyle.Short,
            1, 255);
        DiscordModalTextInput podWhenTimeInput = modal.AddInput(
            "Time", 
            "At what time is the pod being held?", 
            "12:00",
            true,
            TextInputStyle.Short,
            1, 255);

        DiscordModalResponse response =
            await modal.Build().RespondToAsync(context.Interaction, TimeSpan.FromMinutes(5))
                .ConfigureAwait(false);
        if (response == DiscordModalResponse.Timeout)
        {
            return;
        }
        
        if(string.IsNullOrWhiteSpace(podSizeInput.Value) || string.IsNullOrWhiteSpace(podTypeInput.Value) || string.IsNullOrWhiteSpace(podLocationInput.Value) || string.IsNullOrWhiteSpace(podWhenInput.Value) || string.IsNullOrWhiteSpace(podWhenTimeInput.Value))
        {
            return;
        }
        
        var client = context.Client;
        DiscordMessageBuilder messageBuilder = new();

        messageBuilder = DiscordPodButtons.GetPodButtons(client, _dbContext, messageBuilder);
        
        var pod = new Pod
        {
            Location = podLocationInput.Value,
            MaxPlayers = int.Parse(podSizeInput.Value),
            Type = podTypeInput.Value,
            When = podWhenInput.Value,
            Time = podWhenTimeInput.Value
        };

        if (pod.MaxPlayers < 2)
        {
            await context.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Pod must have at least 2 players!").AsEphemeral());
            return;
        }

        var discordMember = await context.Guild.GetMemberAsync(context.User.Id);
        var displayName = discordMember.DisplayName;
        
        var hostUser = await dbContext.Users.Where(u => u.UserId == context.User.Id).FirstOrDefaultAsync();
        if(hostUser == null)
        {
            hostUser = new User
            {
                UserId = context.User.Id,
                DisplayName = displayName
            };
            await dbContext.Users.AddAsync(hostUser);
        }
        pod.Users.Add(hostUser);
        
        var embed = DiscordPodEmbed.GetDiscordPodEmbed(pod,
            displayName);
                
        messageBuilder.WithEmbed(embed);
        
        var guildConfig = _configurationService.GetGuildConfiguration(context.Guild);
        if (guildConfig is null)
        {
            _logger.LogWarning("Guild configuration not found for guild {GuildId}", context.Guild.Id);
            return;
        }
        
        var message = await context.Guild.Channels.First(c => c.Value.Id == guildConfig.ChannelId).Value.SendMessageAsync(messageBuilder);
        pod.MessageId = message.Id;
        
        await dbContext.Pods.AddAsync(pod);
        await dbContext.SaveChangesAsync();
        
        await context.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Pod created!").AsEphemeral());
        
        DmAlerts(pod, context);
    }

    private async void DmAlerts(Pod pod, InteractionContext context)
    {
        var users = dbContext.Users.Where(u => u.AlertEnabled == true).ToList();
        
        foreach (var user in users)
        {
            if (user.UserId == pod.HostId)
            {
                continue;
            }
            
            var discordUser = await context.Guild.GetMemberAsync(user.UserId);
            var dmChannel = await discordUser.CreateDmChannelAsync();
            if (dmChannel == null)
            {
                continue;
            }

            var embed = DiscordPodEmbed.GetDiscordPodEmbed(pod, user.DisplayName);
            var messageBuilder = new DiscordMessageBuilder()
                .WithContent($"A new pod has been created by {user.DisplayName}!")
                .AddEmbed(embed);

            dmChannel.SendMessageAsync(messageBuilder);
        }
    }
}