using System;
using System.IO;
using System.Net.Http;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Extensions;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using HomeGameBot.Commands;
using HomeGameBot.Data;
using HomeGameBot.Interactivity;
using HomeGameBot.Interactivity.Buttons;
using HomeGameBot.Interactivity.Modals;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HomeGameBot.Services;
using Serilog;
using Serilog.Extensions.Logging;
using X10D.Hosting.DependencyInjection;

Directory.CreateDirectory("data");

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/latest.log", rollingInterval: RollingInterval.Day)
#if DEBUG
    .MinimumLevel.Debug()
#endif
    .CreateLogger();

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile("data/config.json", true, true);

builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

builder.Services.AddDiscordClient(Environment.GetEnvironmentVariable("DISCORD_TOKEN"),
    DiscordIntents.AllUnprivileged | DiscordIntents.GuildMembers | DiscordIntents.MessageContents);

builder.Services.ConfigureEventHandlers(b =>
{
    b.AddEventHandlers([typeof(DiscordModal), typeof(BotService), typeof(DiscordButton)]);
});
builder.Services.AddInteractivityExtension(new InteractivityConfiguration());
builder.Services.AddCommandsExtension((provider, extension) =>
{
    extension.AddCommands([typeof(InfoCommand), typeof(PodCommand)]);
    extension.AddProcessor(new SlashCommandProcessor());
}, new CommandsConfiguration()
{
    RegisterDefaultCommandProcessors = false
});

builder.Services.AddDbContextFactory<HomeGameContext>();
builder.Services.AddHostedSingleton<DatabaseService>();

builder.Services.AddSingleton<HttpClient>();
builder.Services.AddSingleton<ConfigurationService>();
builder.Services.AddSingleton<HomeGameService>();

builder.Services.AddHostedSingleton<BotService>();

IHost app = builder.Build();
await app.RunAsync();