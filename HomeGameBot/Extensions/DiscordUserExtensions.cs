using DSharpPlus.Entities;
using DSharpPlus.Exceptions;

namespace HomeGameBot.Extensions;

internal static class DiscordUserExntensions
{
    public static async Task<DiscordMember?> GetAsMemberOfAsync(this DiscordUser user, DiscordGuild guild)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(guild);

        if (user is DiscordMember member && member.Guild == guild)
        {
            return member;
        }

        if (guild.Members.TryGetValue(user.Id, out member!))
        {
            return member;
        }
        
        try
        {
            return await guild.GetMemberAsync(user.Id);
        }
        catch (NotFoundException)
        {
            return null;
        }
    }
    
    public static string GetUsernameWithDiscriminator(this DiscordUser user)
    {
        if (user is null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        if (user.Discriminator == "0")
        {
            // user has a new username. see: https://discord.com/blog/usernames
            return user.Username;
        }

        return $"{user.Username}#{user.Discriminator}";
    }
}