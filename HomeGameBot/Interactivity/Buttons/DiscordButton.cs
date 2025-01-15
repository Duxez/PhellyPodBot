using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using HomeGameBot.Data;

namespace HomeGameBot.Interactivity.Buttons;

internal class DiscordButton : IEventHandler<ComponentInteractionCreatedEventArgs>
{
    protected readonly DiscordClient DiscordClient;
    protected readonly HomeGameContext DbContext;
    protected string CustomId { get; set; } = Guid.NewGuid().ToString("N");
    public DiscordButtonComponent ButtonComponent { get; protected set; }
    
    internal DiscordButton(DiscordClient discordClient, HomeGameContext dbContext, string label)
    {
        DiscordClient = discordClient;
        DbContext = dbContext;
        
        ButtonComponent = new DiscordButtonComponent(DiscordButtonStyle.Primary, CustomId, label);
    }

    protected virtual Task ButtonClicked(ulong messageId, ComponentInteractionCreatedEventArgs e)
    {
        // Implement this in an override from a derived class
        return Task.CompletedTask;
    }

    public async Task HandleEventAsync(DiscordClient sender, ComponentInteractionCreatedEventArgs e)
    {
        if (e.Id != CustomId) return;
        
        var messageId = e.Message.Id;

        await ButtonClicked(messageId, e);
    }
}