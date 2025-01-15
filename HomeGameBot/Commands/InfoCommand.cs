using System.Collections.Immutable;
using System.Text;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using HomeGameBot.Extensions;
using HomeGameBot.Services;
using Humanizer;

namespace HomeGameBot.Commands;

internal sealed class InfoCommand
{
    private readonly BotService _botService;
    
    public InfoCommand(BotService botService)
    {
        _botService = botService;
    }

    [Command("info"), RequireGuild]
    public async Task InfoAsync(SlashCommandContext context)
    {
        DiscordClient client = context.Client;
        DiscordMember member = (await client.CurrentUser.GetAsMemberOfAsync(context.Guild))!;
        string botVersion = _botService.Version;

        var embed = new DiscordEmbedBuilder();
        embed.WithAuthor(member);
        embed.WithColor(member.Color);
        embed.WithThumbnail(member.AvatarUrl);
        embed.WithTitle($"HomeGameBot v{botVersion}");
        embed.AddField("Uptime", (DateTimeOffset.UtcNow - _botService.StartedAt).Humanize(), true);

        var builder = new StringBuilder();
        builder.AppendLine($"HomeGameBot: {botVersion}");
        builder.AppendLine($"D#+: {client.VersionString}");
        builder.AppendLine($"Gateway: {client.VersionString}");
        builder.AppendLine($"CLR: {Environment.Version.ToString(3)}");
        builder.AppendLine($"Host: {Environment.OSVersion}");

        embed.AddField("Version", Formatter.BlockCode(builder.ToString()));

        await context.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate, new DiscordInteractionResponseBuilder().AddEmbed(embed));
    }
}