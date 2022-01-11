using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using EconomyBot.DataStorage;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace EconomyBot
{
    class Program
    {
        private DiscordSocketClient _client;
        private CommandService _commands;
        private IServiceProvider _services;

        private TokenCredentials _credentials;
        private ProgramData _data;

        /// <summary>
        /// Launch Async environment
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static Task Main(string[] args) => new Program().MainAsync();

        /// <summary>
        /// Initial Async method
        /// </summary>
        /// <returns></returns>
        public async Task MainAsync()
        {
            //Load our credentials data
            LoadProgramData();

            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info,
                MessageCacheSize = 50
            });
            _client.Log += Log;

            _commands = new CommandService(new CommandServiceConfig
            {
                LogLevel = LogSeverity.Info,
                CaseSensitiveCommands = false
            });
            _commands.Log += Log;

            _services = ConfigureServices();
            await InitCommands();

            await _client.LoginAsync(TokenType.Bot, _credentials.Bot_Token);
            await _client.StartAsync();

            await Task.Delay(-1);
        }

        /// <summary>
        /// Configure services required for the Command context
        /// </summary>
        /// <returns></returns>
        private static IServiceProvider ConfigureServices()
        {
            var map = new ServiceCollection()
                .AddSingleton(new AndoraService());

            return map.BuildServiceProvider();
        }

        /// <summary>
        /// Load the credentials and data required for the bot to run.
        /// </summary>
        private void LoadProgramData()
        {
            if(!Directory.Exists(Directory.GetCurrentDirectory() + "\\Data"))
            {
                Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\Data");
            }

            if (File.Exists("Data\\TokenCredentials.json"))
            {
                _credentials = JsonConvert.DeserializeObject<TokenCredentials>(File.ReadAllText("Data\\TokenCredentials.json"));
            }
            else
            {
                _credentials = new TokenCredentials()
                {
                    Bot_Token = ""
                };
                File.WriteAllText("Data\\TokenCredentials.json", JsonConvert.SerializeObject(_credentials));
            }

            if(File.Exists("Data\\ProgramData.json"))
            {
                _data = JsonConvert.DeserializeObject<ProgramData>(File.ReadAllText("Data\\ProgramData.json"));
            }
            else
            {
                _data = new ProgramData();
                File.WriteAllText("Data\\ProgramData.json", JsonConvert.SerializeObject(_data));
            }
        }

        /// <summary>
        /// Initialize commands list
        /// </summary>
        /// <returns></returns>
        private async Task InitCommands()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            _client.MessageReceived += HandleCommandAsync;
        }

        /// <summary>
        /// Handle a command called by a user.
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private async Task HandleCommandAsync(SocketMessage arg)
        {
            // Bail out if it's a System Message.
            var msg = arg as SocketUserMessage;
            if (msg == null) return;

            // We don't want the bot to respond to itself or other bots.
            if (msg.Author.Id == _client.CurrentUser.Id || msg.Author.IsBot) return;

            // Create a number to track where the prefix ends and the command begins
            int pos = 0;
            // Replace the '!' with whatever character
            // you want to prefix your commands with.
            // Uncomment the second half if you also want
            // commands to be invoked by mentioning the bot instead.
            if (msg.HasCharPrefix(_data.PrefixChar, ref pos) || msg.HasMentionPrefix(_client.CurrentUser, ref pos))
            {
                // Create a Command Context.
                var context = new SocketCommandContext(_client, msg);

                // Execute the command. (result does not indicate a return value, 
                // rather an object stating if the command executed successfully).
                var result = await _commands.ExecuteAsync(context, pos, _services);

                // Uncomment the following lines if you want the bot
                // to send a message if it failed.
                // This does not catch errors from commands with 'RunMode.Async',
                // subscribe a handler for '_commands.CommandExecuted' to see those.
                //if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                //    await msg.Channel.SendMessageAsync(result.ErrorReason);
            }
        }

        /// <summary>
        /// Write a message to the console
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        private Task Log(LogMessage msg)
        {
            switch (msg.Severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogSeverity.Verbose:
                case LogSeverity.Debug:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
            }

            Console.WriteLine($"[{DateTime.Now.ToShortDateString()}-{DateTime.Now.ToShortTimeString()}] <{msg.Severity}> {msg.Source}: {msg.Message} {msg.Exception}");
            Console.ResetColor();

            return Task.CompletedTask;
        }
    }
}
