using DSharpPlus.Entities;

namespace HomeGameBot.Interactivity.Modals;

public sealed class DiscordModalTextInput(DiscordTextInputComponent component)
{
    /// <summary>
    ///     Gets the label of the input.
    /// </summary>
    /// <value>The label.</value>
    public string Label => InputComponent.Label;

    /// <summary>
    ///     Gets the placeholder of the input.
    /// </summary>
    /// <value>The placeholder.</value>
    public string? Placeholder => InputComponent.Placeholder;

    /// <summary>
    ///     Gets the value of the input.
    /// </summary>
    /// <value>The value.</value>
    public string? Value { get; internal set; } = component.Value;

    internal string CustomId { get; } = component.CustomId;

    internal DiscordTextInputComponent InputComponent { get; } = component;

    internal DiscordModal? Modal { get; set; }
}