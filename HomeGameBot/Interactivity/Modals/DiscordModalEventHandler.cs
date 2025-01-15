using DSharpPlus;
using DSharpPlus.EventArgs;

namespace HomeGameBot.Interactivity.Modals;

public class DiscordModalEventHandler : IEventHandler<ModalSubmittedEventArgs>
{
    public Task HandleEventAsync(DiscordClient sender, ModalSubmittedEventArgs eventArgs)
    {
        throw new NotImplementedException();
    }
}