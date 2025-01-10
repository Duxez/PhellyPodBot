using DSharpPlus;
using DSharpPlus.Entities;
using HomeGameBot.Data;

namespace HomeGameBot.Interactivity;

internal sealed class DiscordPodButtons
{
    public static DiscordMessageBuilder GetPodButtons(DiscordClient client, HomeGameContext _dbContext, DiscordMessageBuilder messageBuilder)
    {
        var joinButton = new DiscordButton(client, _dbContext, "Join");
        
        messageBuilder.AddComponents(joinButton.ButtonComponent, new DiscordButtonComponent(ButtonStyle.Primary, "leave_button", null, false,
            new DiscordComponentEmoji(DiscordEmoji.FromName(client, ":x:"))), new DiscordButtonComponent(ButtonStyle.Primary, "edit_button", "Edit"), new DiscordButtonComponent(ButtonStyle.Danger, "delete_button", "Delete"));

        return messageBuilder;
    }
}