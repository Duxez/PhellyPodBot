using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using HomeGameBot.Data;
using Microsoft.EntityFrameworkCore;

namespace HomeGameBot.Interactivity.Buttons;

internal sealed class DiscordDeleteButton : DiscordButton
{
    internal DiscordDeleteButton(DiscordClient discordClient, HomeGameContext dbContext, string label) : base(discordClient, dbContext, label)
    {
        ButtonComponent = new DiscordButtonComponent(ButtonStyle.Danger, CustomId, label);
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
        
        if (e.User.Id != pod.Host.UserId)
        {
            await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
            await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent("Only the host can delete the pod!").AsEphemeral());
            return;
        }

        DbContext.Remove(pod);
        await DbContext.SaveChangesAsync();
        
        await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
        await e.Message.DeleteAsync();
        await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent("The pod has been removed!").AsEphemeral());
    }
}