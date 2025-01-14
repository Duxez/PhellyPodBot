using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using HomeGameBot.Data;
using Microsoft.EntityFrameworkCore;

namespace HomeGameBot.Interactivity.Buttons;

internal sealed class DiscordLeaveButton : DiscordButton
{
    internal DiscordLeaveButton(DiscordClient discordClient, HomeGameContext dbContext, string label) : base(discordClient, dbContext, label)
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
        
        DiscordEmbedBuilder podEmbed;
        var builder = new DiscordMessageBuilder();
        
        var user = DbContext.Users.FirstOrDefault(u => u.UserId == e.User.Id);
        if (user is null)
        {
            await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
            await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent("Couldn't find user! This shouldn't happen.").AsEphemeral());
            return;  
        }
        
        if (pod.HasExpired)
        {
            await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

            podEmbed = DiscordPodEmbed.GetDiscordPodEmbed(pod, pod.Host.DisplayName);
            builder.WithEmbed(podEmbed);
            await e.Message.ModifyAsync(builder);

            await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder()
                .WithContent("Pod has expired!").AsEphemeral());
            return;
        }
        
        if (pod.Host == user)
        {
            await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
            await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent("You can't leave your own pod!").AsEphemeral());
            return;
        }

        if (!pod.Users.Contains(user))
        {
            await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
            await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent("You've not joined this pod!").AsEphemeral());
            return;
        }
        
        pod.Users.Remove(user);
        await DbContext.SaveChangesAsync();

        podEmbed = DiscordPodEmbed.GetDiscordPodEmbed(pod, pod.Host.DisplayName);
        builder.WithEmbed(podEmbed);
        builder = DiscordPodButtons.GetPodButtons(DiscordClient, DbContext, builder);
        
        await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
        await e.Message.ModifyAsync(builder);
        await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent("You've successively joined the pod!").AsEphemeral());
    }
}