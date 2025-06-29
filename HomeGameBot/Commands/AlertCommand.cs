using DSharpPlus.SlashCommands;
using HomeGameBot.Data;
using HomeGameBot.Extensions;
using Microsoft.EntityFrameworkCore;

namespace HomeGameBot.Commands;

internal sealed class AlertCommand(HomeGameContext dbContext) : ApplicationCommandModule
{
    private readonly HomeGameContext _dbContext = dbContext;

    [SlashCommand("alert", "Opt in or out of alerts for new pods.")]
    public async Task ToggleAlertAsync(InteractionContext context)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.UserId == context.User.Id);

        if (user == null)
        {
            var displayName = context.User.GetAsMemberOfAsync(context.Guild).Result?.DisplayName ?? context.User.Username;
            user = new User
            {
                UserId = context.User.Id,
                DisplayName = displayName,
                AlertEnabled = true
            };
            await dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();
            await context.CreateResponseAsync("You have opted into receiving alerts for new pods!", null, true);
            return;
        }
        
        user.AlertEnabled = !user.AlertEnabled;
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();
        
        if (user.AlertEnabled)
        {
            await context.CreateResponseAsync("You have opted into receiving alerts for new pods!", null, true);
        }
        else
        {
            await context.CreateResponseAsync("You have opted out of receiving alerts for new pods.", null, true);
        }
    }
}