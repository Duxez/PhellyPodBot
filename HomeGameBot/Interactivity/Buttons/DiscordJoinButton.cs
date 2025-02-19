using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using HomeGameBot.Data;
using Microsoft.EntityFrameworkCore;

namespace HomeGameBot.Interactivity.Buttons;

internal sealed class DiscordJoinButton : DiscordButton
{
    internal DiscordJoinButton(DiscordClient discordClient, HomeGameContext dbContext, string label) : base(discordClient, dbContext, label)
    {
    }
    
    protected override async Task ButtonClicked(ulong messageId, ComponentInteractionCreateEventArgs e)
    {
        var pod = DbContext.Pods.Include(pod => pod.Users).FirstOrDefault(p => p.MessageId == messageId);

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
        
        DiscordEmbedBuilder podEmbed;
        var builder = new DiscordMessageBuilder();
        
        var user = DbContext.Users.FirstOrDefault(u => u.UserId == e.User.Id);
        if (user is null)
        {
            user = new User
            {
                UserId = e.User.Id,
                DisplayName = e.Guild.GetMemberAsync(e.User.Id).Result.DisplayName
            };
            await DbContext.Users.AddAsync(user);
        } 
        else if (pod.Users.Contains(user))
        {
            await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
            await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent("You've already joined this pod!").AsEphemeral());
            return;
        }
        
        pod.Users.Add(user);
        await DbContext.SaveChangesAsync();

        podEmbed = DiscordPodEmbed.GetDiscordPodEmbed(pod, pod.Host.DisplayName);
        builder.WithEmbed(podEmbed);
        builder = DiscordPodButtons.GetPodButtons(DiscordClient, DbContext, builder);
        
        await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
        await e.Message.ModifyAsync(builder);
        await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent("You've successively joined the pod!").AsEphemeral());
    }
}