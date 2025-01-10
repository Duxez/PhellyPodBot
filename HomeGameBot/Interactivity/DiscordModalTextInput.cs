﻿using DSharpPlus.Entities;

namespace HomeGameBot.Interactivity;

internal sealed class DiscordModalNumberInput
{
    public DiscordModalNumberInput(TextInputComponent component)
    {
        InputComponent = component;

        CustomId = component.CustomId;
        int value;
        if (int.TryParse(component.Value, out value))
        {
            Value = value;
        }
    }
    
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
    public int? Value { get; internal set; }

    internal string CustomId { get; }

    internal TextInputComponent InputComponent { get; }

    internal DiscordModal? Modal { get; set; }
}