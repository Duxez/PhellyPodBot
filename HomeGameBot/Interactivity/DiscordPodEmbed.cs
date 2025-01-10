using DSharpPlus.Entities;
using HomeGameBot.Data;

namespace HomeGameBot.Interactivity;

internal sealed class DiscordPodEmbed
{
    private static readonly DiscordColor[] _colors = new[]
    {
        new DiscordColor("#63009c"),
        new DiscordColor("#a0cf05"),
    };
    public static DiscordEmbedBuilder GetDiscordPodEmbed(Pod pod, string hostName)
    {
        DiscordColor color = _colors[new Random().Next(_colors.Length)];
        var embed = new DiscordEmbedBuilder();
        embed.WithColor(color);
        embed.WithTitle($"New kitchen table pod by {hostName}");
        embed.AddField("Max players", pod.MaxPlayers.ToString(), true);
        embed.AddField("Game type", pod.Type, true);
        embed.AddField("Location", pod.Location);
        embed.AddField("When", pod.When.ToString("dd-MM-yyyy HH:mm"));
        embed.AddField($"Current players ({pod.CurrentPlayers}/{pod.MaxPlayers})", string.Join("\n", pod.Users.Select(u => "> " + u.DisplayName)));
        return embed;
    }
}