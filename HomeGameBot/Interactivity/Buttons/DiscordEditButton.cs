using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using HomeGameBot.Data;
using Microsoft.EntityFrameworkCore;

namespace HomeGameBot.Interactivity.Buttons;

internal sealed class DiscordEditButton : DiscordButton
{
    internal DiscordEditButton(DiscordClient discordClient, HomeGameContext dbContext, string label) : base(discordClient, dbContext, label)
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

        if (pod.HasExpired)
        {
            await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
            await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent("Pod has expired!").AsEphemeral());
            return;
        }
        
        if (e.User.Id != pod.Host.UserId)
        {
            await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
            await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent("Only the host can edit the pod!").AsEphemeral());
            return;
        }
        
        var modal = new DiscordModalBuilder(DiscordClient);
        modal.WithTitle("Create a new pod");
        
        DiscordModalTextInput podSizeInput = modal.AddInput(
            "Number of players", 
            "How many players are in the pod?", 
            pod.MaxPlayers.ToString(),
            true,
            TextInputStyle.Short,
            1, 2);
        DiscordModalTextInput podTypeInput = modal.AddInput(
            "Type of pod", 
            "What type of pod is this?", 
            pod.Type,
            true,
            TextInputStyle.Short,
            1, 255);
        DiscordModalTextInput podLocationInput = modal.AddInput(
            "Location", 
            "Where is the pod being held?", 
            pod.Location,
            true,
            TextInputStyle.Short,
            1, 255);
        DiscordModalTextInput podWhenInput = modal.AddInput(
            "When", 
            "When is the pod being held?", 
            pod.When.ToString("dd-MM-yyyy HH:mm"),
            true,
            TextInputStyle.Short,
            1, 255);

        DiscordModalResponse response =
            await modal.Build().RespondToAsync(e.Interaction, TimeSpan.FromMinutes(5))
                .ConfigureAwait(false);
        
        if (response == DiscordModalResponse.Timeout)
        {
            return;
        }
        
        if(string.IsNullOrWhiteSpace(podSizeInput.Value) || string.IsNullOrWhiteSpace(podTypeInput.Value) || string.IsNullOrWhiteSpace(podLocationInput.Value) || string.IsNullOrWhiteSpace(podWhenInput.Value))
        {
            return;
        }
        
        if(int.Parse(podSizeInput.Value) < pod.CurrentPlayers)
        {
            await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent("Can't reduce the number of players below the current amount!").AsEphemeral());
            return;
        }
        
        if(pod.When < DateTime.Now)
        {
            await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent("A pod can't be created in the past!").AsEphemeral());
            return;
        }

        if (pod.MaxPlayers < 2)
        {
            await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent("A pod can't have less than 2 players!").AsEphemeral());
            return;
        }
        
        pod.MaxPlayers = int.Parse(podSizeInput.Value);
        pod.Type = podTypeInput.Value;
        pod.Location = podLocationInput.Value;
        pod.When = DateTime.Parse(podWhenInput.Value);
        
        await DbContext.SaveChangesAsync();
        
        var builder = new DiscordMessageBuilder();
        var podEmbed = DiscordPodEmbed.GetDiscordPodEmbed(pod, pod.Host.DisplayName);
        builder.WithEmbed(podEmbed);
        builder = DiscordPodButtons.GetPodButtons(DiscordClient, DbContext, builder);
        
        await e.Message.ModifyAsync(builder);
        await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent("You've successively joined the pod!").AsEphemeral());
    }
}