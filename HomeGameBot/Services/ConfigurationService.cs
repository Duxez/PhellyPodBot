using System.Diagnostics.CodeAnalysis;
using DSharpPlus.Entities;
using HomeGameBot.HomeGameBot;
using Microsoft.Extensions.Configuration;

namespace HomeGameBot.Services;

internal sealed class ConfigurationService
{
    private readonly IConfiguration _configuration;
    
    public ConfigurationService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public GuildConfiguration? GetGuildConfiguration(DiscordGuild guild)
    {
        ArgumentNullException.ThrowIfNull(guild);
        return _configuration.GetSection(guild.Id.ToString())?.Get<GuildConfiguration>();
    }
    
    public bool TryGetGuildConfiguration(DiscordGuild guild, [NotNullWhen(true)] out GuildConfiguration? configuration)
    {
        configuration = null;

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContracts
        if (guild is null)
        {
            return false;
        }

        configuration = GetGuildConfiguration(guild);
        return configuration is not null;
    }
}