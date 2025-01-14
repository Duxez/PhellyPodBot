using DSharpPlus;
using DSharpPlus.Entities;
using HomeGameBot.Data;

namespace HomeGameBot.Interactivity.Buttons;

internal sealed class DiscordPodButtons
{
    public static DiscordMessageBuilder GetPodButtons(DiscordClient client, HomeGameContext _dbContext, DiscordMessageBuilder messageBuilder)
    {
        var joinButton = new DiscordJoinButton(client, _dbContext, "Join");
        var leaveButton = new DiscordLeaveButton(client, _dbContext, "Leave");
        var editButton = new DiscordEditButton(client, _dbContext, "Edit");
        var deleteButton = new DiscordDeleteButton(client, _dbContext, "Delete");
        
        messageBuilder.AddComponents(joinButton.ButtonComponent, leaveButton.ButtonComponent, editButton.ButtonComponent, deleteButton.ButtonComponent);

        return messageBuilder;
    }
}