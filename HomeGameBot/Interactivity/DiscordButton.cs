using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using HomeGameBot.Data;
using Microsoft.EntityFrameworkCore;

namespace HomeGameBot.Interactivity;

internal sealed class DiscordButton
{
    private readonly DiscordClient _discordClient;
    private readonly HomeGameContext _dbContext;
    public string CustomId { get; private set; } = Guid.NewGuid().ToString("N");
    public DiscordButtonComponent ButtonComponent { get; private set; }
    
    internal DiscordButton(DiscordClient discordClient, HomeGameContext dbContext, string label)
    {
        _discordClient = discordClient;
        _dbContext = dbContext;
        _discordClient.ComponentInteractionCreated += OnButtonClicked;
        
        ButtonComponent = new DiscordButtonComponent(ButtonStyle.Primary, CustomId, label);
    }
    
    private async Task OnButtonClicked(DiscordClient sender, ComponentInteractionCreateEventArgs e)
    {
        if (e.Id != CustomId) return;
        
        var messageId = e.Message.Id;
        await JoinButtonClicked(messageId, e);
    }

    private async Task JoinButtonClicked(ulong messageId, ComponentInteractionCreateEventArgs e)
    {
        var pod = _dbContext.Pods.Include(pod => pod.Users).FirstOrDefault(p => p.MessageId == messageId);

        if (pod is null)
        {
            await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
            await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent("Pod not found! It might have expired.").AsEphemeral());
            return;   
        }
        
        if (pod.IsFull)
        {
            await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
            await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent("Pod is full!").AsEphemeral());
            return;
        }
        
        DiscordEmbedBuilder podEmbed = new DiscordEmbedBuilder();
        var builder = new DiscordMessageBuilder();
        
        var user = _dbContext.Users.FirstOrDefault(u => u.UserId == e.User.Id);
        if (user is null)
        {
            user = new User
            {
                UserId = e.User.Id
            };
            await _dbContext.Users.AddAsync(user);
        } 
        else if (pod.Users.Contains(user))
        {
            await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
            await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent("You've already joined this pod!").AsEphemeral());
            return;
        }
        else if (pod.HasExpired)
        {
            await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

            podEmbed = DiscordPodEmbed.GetDiscordPodEmbed(pod, pod.Host.DisplayName);
            builder.WithEmbed(podEmbed);
            await e.Message.ModifyAsync(builder);
            
            await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent("Pod has expired!").AsEphemeral());
            return;
        }
        
        pod.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        podEmbed = DiscordPodEmbed.GetDiscordPodEmbed(pod, pod.Host.DisplayName);
        builder.WithEmbed(podEmbed);
        builder = DiscordPodButtons.GetPodButtons(_discordClient, _dbContext, builder);
        
        await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
        await e.Message.ModifyAsync(builder);
        await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent("You've successively joined the pod!").AsEphemeral());
    }
}