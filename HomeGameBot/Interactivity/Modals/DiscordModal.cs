using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;

namespace HomeGameBot.Interactivity.Modals;

/// <summary>
///     Represents a modal that can be displayed to the user.
/// </summary>
public sealed class DiscordModal : IEventHandler<ModalSubmittedEventArgs>
{
    private DiscordClient? _discordClient;
    private readonly string _customId = Guid.NewGuid().ToString("N");
    private readonly Dictionary<string, DiscordModalTextInput> _inputs = new();
    private TaskCompletionSource _taskCompletionSource = new();

    public DiscordModal()
    {
    }

    /// <summary>
    ///     Gets the title of this modal.
    /// </summary>
    /// <value>The title.</value>
    public required string Title { get; set; }
    
    public void SetInputs(IEnumerable<DiscordModalTextInput> inputs)
    {
        foreach (DiscordModalTextInput input in inputs)
            _inputs.Add(input.CustomId, input);
    }
    
    public void SetClient(DiscordClient? discordClient)
    {
        _discordClient = discordClient;
    }

    /// <summary>
    ///     Responds with this modal to the specified interaction.
    /// </summary>
    /// <param name="interaction">The interaction to which the modal will respond.</param>
    /// <param name="timeout">How long to wait </param>
    /// <exception cref="ArgumentNullException"><paramref name="interaction" /> is <see langword="null" />.</exception>
    public async Task<DiscordModalResponse> RespondToAsync(DiscordInteraction interaction, TimeSpan timeout)
    {
        if (interaction is null) throw new ArgumentNullException(nameof(interaction));

        var builder = new DiscordInteractionResponseBuilder();
        builder.WithTitle(Title);
        builder.WithCustomId(_customId);

        foreach ((_, DiscordModalTextInput input) in _inputs)
            builder.AddComponents(input.InputComponent);

        _taskCompletionSource = new TaskCompletionSource();
        await interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal, builder).ConfigureAwait(false);

        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Token.Register(() => _taskCompletionSource.TrySetCanceled());
        if (timeout != Timeout.InfiniteTimeSpan)
            cancellationTokenSource.CancelAfter(timeout);

        try
        {
            await _taskCompletionSource.Task.ConfigureAwait(false);
            return DiscordModalResponse.Success;
        }
        catch (TaskCanceledException)
        {
            return DiscordModalResponse.Timeout;
        }
    }

    private async Task OnModalSubmitted(DiscordClient sender, ModalSubmittedEventArgs e)
    {
        if (e.Interaction.Data.CustomId != _customId)
            return;
        
        var inputComponents = new List<DiscordTextInputComponent>();

        if (e.Interaction.Data.Components != null)
            foreach (var component in e.Interaction.Data.Components)
            {
                switch (component)
                {
                    case DiscordActionRowComponent rowComponent:
                        inputComponents.AddRange(rowComponent.Components.OfType<DiscordTextInputComponent>());
                        break;
                    case DiscordTextInputComponent textInputComponent:
                        inputComponents.Add(textInputComponent);
                        break;
                }
            }

        foreach (DiscordTextInputComponent inputComponent in inputComponents)
        {
            if (_inputs.TryGetValue(inputComponent.CustomId, out DiscordModalTextInput? input))
                input.Value = inputComponent.Value;
        }

        _taskCompletionSource.TrySetResult();
        await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);
    }

    public async Task HandleEventAsync(DiscordClient sender, ModalSubmittedEventArgs eventArgs)
    {
        await OnModalSubmitted(sender, eventArgs);
    }
}