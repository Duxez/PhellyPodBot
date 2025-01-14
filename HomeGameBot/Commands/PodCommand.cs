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

namespace HomeGameBot.Commands;

internal sealed class PodCommand: ApplicationCommandModule
{
    private readonly HomeGameContext _dbContext;
    
    public PodCommand(HomeGameContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    [SlashCommand("pod", "Create a new at home kitchentable pod.")]
    [SlashRequireGuild]
    public async Task PodAsync(InteractionContext context)
    {
        var modal = new DiscordModalBuilder(context.Client);
        modal.WithTitle("Create a new pod");
        
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
            "When", 
            "When is the pod being held?", 
            DateTime.Now.ToString("dd-MM-yyyy HH:mm"),
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
        
        if(string.IsNullOrWhiteSpace(podSizeInput.Value) || string.IsNullOrWhiteSpace(podTypeInput.Value) || string.IsNullOrWhiteSpace(podLocationInput.Value) || string.IsNullOrWhiteSpace(podWhenInput.Value))
        {
            return;
        }
        
        var client = context.Client;
        DiscordMessageBuilder messageBuilder = new();

        messageBuilder = DiscordPodButtons.GetPodButtons(client, _dbContext, messageBuilder);

        var cultureInfo = CultureInfo.InvariantCulture;
        var pod = new Pod
        {
            Location = podLocationInput.Value,
            MaxPlayers = int.Parse(podSizeInput.Value),
            Type = podTypeInput.Value,
            When = DateTime.ParseExact(podWhenInput.Value, "dd-MM-yyyy HH:mm", cultureInfo)
        };
        
        if(pod.When < DateTime.Now)
        {
            await context.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Pod can't be in the past!").AsEphemeral());
            return;
        }

        if (pod.MaxPlayers < 2)
        {
            await context.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Pod must have at least 2 players!").AsEphemeral());
            return;
        }

        var discordMember = await context.Guild.GetMemberAsync(context.User.Id);
        var displayName = discordMember.DisplayName;
        
        var hostUser = await _dbContext.Users.Where(u => u.UserId == context.User.Id).FirstOrDefaultAsync();
        if(hostUser == null)
        {
            hostUser = new User
            {
                UserId = context.User.Id,
                DisplayName = displayName
            };
            await _dbContext.Users.AddAsync(hostUser);
        }
        pod.Users.Add(hostUser);
        
        var embed = DiscordPodEmbed.GetDiscordPodEmbed(pod,
            displayName);
                
        messageBuilder.WithEmbed(embed);
        var message = await context.Channel.SendMessageAsync(messageBuilder);
        pod.MessageId = message.Id;
        
        await _dbContext.Pods.AddAsync(pod);
        await _dbContext.SaveChangesAsync();
        
        await context.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Pod created!").AsEphemeral());
    }
}