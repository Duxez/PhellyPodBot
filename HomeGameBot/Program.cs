﻿using System;
using System.IO;
using System.Net.Http;
using DSharpPlus;
using HomeGameBot.Data;
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

builder.Services.AddSingleton(new DiscordClient(new DiscordConfiguration
{
    Token = Environment.GetEnvironmentVariable("DISCORD_TOKEN"),
    LoggerFactory = new SerilogLoggerFactory(),
    Intents = DiscordIntents.AllUnprivileged | DiscordIntents.GuildMembers | DiscordIntents.MessageContents
}));

builder.Services.AddDbContextFactory<HomeGameContext>();
builder.Services.AddHostedSingleton<DatabaseService>();

builder.Services.AddSingleton<HttpClient>();
builder.Services.AddSingleton<ConfigurationService>();
builder.Services.AddSingleton<HomeGameService>();

builder.Services.AddHostedSingleton<BotService>();

IHost app = builder.Build();
await app.RunAsync();