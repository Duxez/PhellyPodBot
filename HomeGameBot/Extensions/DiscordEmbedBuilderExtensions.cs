using DSharpPlus.Entities;

namespace HomeGameBot.Extensions;

internal static class DiscordEmbedBuilderExtensions
{
    public static DiscordEmbedBuilder AddField<T>(
        this DiscordEmbedBuilder builder,
        string name,
        T? value,
        bool inline = false)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.AddField(name, value?.ToString(), inline);
    }

    public static DiscordEmbedBuilder AddFieldIf<T>(
        this DiscordEmbedBuilder builder,
        bool condition,
        string name,
        T? value,
        bool inline = false)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (condition)
        {
            builder.AddField(name, value?.ToString(), inline);
        }

        return builder;
    }

    public static DiscordEmbedBuilder AddFieldIf<T>(
        this DiscordEmbedBuilder builder,
        Func<bool> predicate,
        string name,
        T? value,
        bool inline = false)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (predicate is null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        if (predicate.Invoke())
        {
            builder.AddField(name, value?.ToString(), inline);
        }

        return builder;
    }

    public static DiscordEmbedBuilder AddFieldIf<T>(
        this DiscordEmbedBuilder builder,
        Func<bool> predicate,
        string name,
        Func<T?> valueFactory,
        bool inline = false)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (predicate is null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        if (valueFactory is null)
        {
            throw new ArgumentNullException(nameof(valueFactory));
        }

        if (predicate.Invoke())
        {
            builder.AddField(name, valueFactory.Invoke()?.ToString(), inline);
        }

        return builder;
    }

    public static DiscordEmbedBuilder AddFieldIf<T>(
        this DiscordEmbedBuilder builder,
        bool condition,
        string name,
        Func<T?> valueFactory,
        bool inline = false)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (valueFactory is null)
        {
            throw new ArgumentNullException(nameof(valueFactory));
        }

        if (condition)
        {
            builder.AddField(name, valueFactory.Invoke()?.ToString(), inline);
        }

        return builder;
    }

    public static DiscordEmbedBuilder WithAuthor(this DiscordEmbedBuilder builder, DiscordUser user)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (user is null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        return builder.WithAuthor(user.GetUsernameWithDiscriminator(), iconUrl: user.AvatarUrl);
    }
}