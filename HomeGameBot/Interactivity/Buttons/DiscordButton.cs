using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using HomeGameBot.Data;

namespace HomeGameBot.Interactivity.Buttons;

internal class DiscordButton
{
    protected readonly DiscordClient DiscordClient;
    protected readonly HomeGameContext DbContext;
    protected string CustomId { get; set; } = Guid.NewGuid().ToString("N");
    public DiscordButtonComponent ButtonComponent { get; protected set; }
    
    internal DiscordButton(DiscordClient discordClient, HomeGameContext dbContext, string label)
    {
        DiscordClient = discordClient;
        DbContext = dbContext;
        DiscordClient.ComponentInteractionCreated += OnButtonClicked;
        
        ButtonComponent = new DiscordButtonComponent(ButtonStyle.Primary, CustomId, label);
    }
    
    private async Task OnButtonClicked(DiscordClient sender, ComponentInteractionCreateEventArgs e)
    {
        if (e.Id != CustomId) return;
        
        var messageId = e.Message.Id;
        await ButtonClicked(messageId, e);
    }

    protected virtual Task ButtonClicked(ulong messageId, ComponentInteractionCreateEventArgs e)
    {
        // Implement this in an override from a derived class
        return Task.CompletedTask;
    }
}