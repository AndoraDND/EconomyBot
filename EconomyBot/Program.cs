using System;
using System.IO;
using System.Linq;
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

        internal static MessageHandler _messageHandler;
        private NPCPingService _npcPingService;

        private delegate void WeeklyEvent();
        private static event WeeklyEvent OnWeeklyRefresh;
        private DateTime _lastWeeklyUpdate;

        /// <summary>
        /// Time to handle occasional polling updates. Unit is Milliseconds
        /// </summary>
        private int _updateTimer = 60 * 1000;

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
                MessageCacheSize = 50,
                GatewayIntents = GatewayIntents.AllUnprivileged | 
                    GatewayIntents.GuildMembers | 
                    GatewayIntents.DirectMessages | 
                    GatewayIntents.DirectMessageReactions | 
                    GatewayIntents.GuildMessageReactions
            });
            _client.Log += Log;
            _client.Ready += Client_Ready;
            _client.SlashCommandExecuted += SlashCommandHandler;

            _messageHandler = new MessageHandler();
            //_messageHandler.AddMessage("DTD has been reset for this week!", 934921635914481734, 934929339743625276, DateTime.Parse("02/06/2022 12:00:00"), TimeSpan.FromDays(7));
            //_messageHandler.AddMessage("<@&929483390934216784>", 929453375257444362, 929453375257444365, DateTime.Now + TimeSpan.FromMinutes(1));

            _commands = new CommandService(new CommandServiceConfig
            {
                LogLevel = LogSeverity.Info,
                CaseSensitiveCommands = false
            });
            _commands.Log += Log;

            _services = ConfigureServices(_client, _credentials);

            //Set up event management for NPC pings
            _npcPingService = ((AndoraService)_services.GetService(typeof(AndoraService))).NPCPingService;
            _client.MessageReceived += HandleMessageAsync;

            await InitCommands();

            await _client.LoginAsync(TokenType.Bot, _credentials.Bot_Token);
            await _client.StartAsync();

            _client.ThreadCreated += _client_ThreadCreated;

            DateTime timeNow;
            try
            {
                timeNow = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById("America/Chicago"));
            }
            catch
            {
                timeNow = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time"));
            }
            Console.WriteLine($"Current Time: {timeNow.ToString()}");
            var nextSunday = timeNow.Date;
            nextSunday = nextSunday.AddDays(7 - (int)nextSunday.DayOfWeek);
            nextSunday = nextSunday.AddHours(12);
            _lastWeeklyUpdate = DateTime.Parse(nextSunday.ToString());
            Console.WriteLine($"Next Weekly Refresh Time: {nextSunday.ToString()}");

            Console.WriteLine($"Downloading Guild Users...");
            await Task.Run(async () => await DownloadGuildUsers());

            while (true)
            {
                //Update
                await UpdateTask();
                await Task.Delay(_updateTimer);
            }
        }

        /// <summary>
        /// Download guild users prior to runtime of this bot.
        /// </summary>
        /// <returns></returns>
        private async Task DownloadGuildUsers()
        {
            foreach (var guild in _client.Guilds)
            {
                await guild.DownloadUsersAsync();
            }

            Console.WriteLine("Done downloading guild users.");
        }

        private async Task Client_Ready()
        {
            var verifyCommand = new SlashCommandBuilder();
            verifyCommand.WithName("verify");
            verifyCommand.WithDescription("Verify a DTD request for a user.");
            verifyCommand.AddOption("original-message", ApplicationCommandOptionType.String, "Link to the original message to verify.", true);
            verifyCommand.AddOption("item-list", ApplicationCommandOptionType.String, "Verify the user owns a series of items.", false);

            var charSheetCommand = new SlashCommandBuilder();
            charSheetCommand.WithName("get-sheet");
            charSheetCommand.WithDescription("Get a User's Character Sheet.");
            charSheetCommand.AddOption("user", ApplicationCommandOptionType.User, "A Discord User.", true);
            charSheetCommand.AddOption("force", ApplicationCommandOptionType.Boolean, "Force the cache to update", false);

            var appearanceCommand = new SlashCommandBuilder();
            appearanceCommand.WithName("appearance");
            appearanceCommand.WithDescription("Get the Listed Appearance of a User's Character.");
            appearanceCommand.AddOption("user", ApplicationCommandOptionType.User, "A Discord User.", true);

            var dtdCommand = new SlashCommandBuilder();
            dtdCommand.WithName("dtd");
            dtdCommand.WithDescription("Handle updates related to DTD.");
            dtdCommand.AddOption(new SlashCommandOptionBuilder()
                .WithName("get")
                .WithDescription("Get the remaining DTDs for a user.")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("user", ApplicationCommandOptionType.User, "A Discord User.", true)
                );
            dtdCommand.AddOption(new SlashCommandOptionBuilder()
               .WithName("set")
               .WithDescription("Spend DTDs for a user.")
               .WithType(ApplicationCommandOptionType.SubCommand)
               .AddOption("user", ApplicationCommandOptionType.User, "A Discord User.", true)
               .AddOption("dtd-amount", ApplicationCommandOptionType.Integer, "Amount of DTDs to spend", true)
               );

            var updateDBCommand = new SlashCommandBuilder();
            updateDBCommand.WithName("update-db");
            updateDBCommand.WithDescription("Update the backend with a character's current data.");
            updateDBCommand.AddOption("user", ApplicationCommandOptionType.User, "A Discord User.", true);

            var resetPriorityPbp = new SlashCommandBuilder();
            resetPriorityPbp.WithName("reset-priority-pbp");
            resetPriorityPbp.WithDescription("Update the backend with the final pbp date.");
            resetPriorityPbp.AddOption("end-date", ApplicationCommandOptionType.String, "A date. Use Month/Day/Year format.", true);
            resetPriorityPbp.AddOption("player-one", ApplicationCommandOptionType.User, "A Discord User.", false);
            resetPriorityPbp.AddOption("player-two", ApplicationCommandOptionType.User, "A Discord User.", false);
            resetPriorityPbp.AddOption("player-three", ApplicationCommandOptionType.User, "A Discord User.", false);
            resetPriorityPbp.AddOption("player-four", ApplicationCommandOptionType.User, "A Discord User.", false);
            resetPriorityPbp.AddOption("player-five", ApplicationCommandOptionType.User, "A Discord User.", false);
            resetPriorityPbp.AddOption("player-six", ApplicationCommandOptionType.User, "A Discord User.", false);

            try
            {
                foreach (var guild in _client.Guilds)
                {
                    await guild.CreateApplicationCommandAsync(verifyCommand.Build());
                    await guild.CreateApplicationCommandAsync(charSheetCommand.Build());
                    await guild.CreateApplicationCommandAsync(appearanceCommand.Build());
                    await guild.CreateApplicationCommandAsync(dtdCommand.Build());
                    await guild.CreateApplicationCommandAsync(updateDBCommand.Build());
                    await guild.CreateApplicationCommandAsync(resetPriorityPbp.Build());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            var andoraService = ((AndoraService)_services.GetService(typeof(AndoraService)));
            switch (command.Data.Name)
            {
                case "verify":
                {
                    await andoraService.HandleVerifyCommand(command);
                    break;
                }
                case "get-sheet":
                {
                    await andoraService.GetCharacterSheetCommand(command);
                    break;
                }
                case "appearance":
                {
                    await andoraService.GetAppearanceCommand(command);
                    break;
                }
                case "update-db":
                {
                    await andoraService.UpdateDBCharacterCommand(command);
                    break;
                }
                case "reset-priority-pbp":
                {
                    await andoraService.PriorityReset_PBP(command);
                    break;
                }
                case "dtd":
                {
                    switch (command.Data.Options.First().Name)
                    {
                        case "get":
                            await andoraService.GetCharacterDTDCommand(command);
                            break;
                        case "set":
                            await andoraService.UpdateCharacterDTDCommand(command);
                            break;
                    }
                    
                    break;
                }
            }
        }

        /// <summary>
        /// Callback when a thread is created
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private Task _client_ThreadCreated(SocketThreadChannel arg)
        {
            Task.Run(async () => await JoinThread(arg));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Have the bot join a thread
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        private async Task JoinThread(SocketThreadChannel channel)
        {
            if (!channel.HasJoined)
            {
                await channel.JoinAsync();
            }
        }

        /// <summary>
        /// Looping update check
        /// </summary>
        /// <returns></returns>
        private async Task UpdateTask()
        {
            await _messageHandler.Tick(_client);

            DateTime timeNow;
            try
            {
                timeNow = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById("America/Chicago"));
            }
            catch
            {
                timeNow = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time"));
            }
            if (timeNow >= _lastWeeklyUpdate)
            {
                //Update the refresh time.
                _lastWeeklyUpdate = _lastWeeklyUpdate.AddDays(7);
                OnWeeklyRefresh?.Invoke();
            }
        }

        /// <summary>
        /// Configure services required for the Command context
        /// </summary>
        /// <returns></returns>
        private static IServiceProvider ConfigureServices(DiscordSocketClient client, TokenCredentials credentials)
        {
            var reactionReplyService = new ReactionReplyService();
            client.ReactionAdded += reactionReplyService.OnReactionReceived;
            client.MessageReceived += reactionReplyService.OnMessageReceived;

            var andoraService = new AndoraService(client, credentials);
            OnWeeklyRefresh += andoraService.OnWeeklyRefresh;
            
            var map = new ServiceCollection()
                .AddSingleton(andoraService)
                .AddSingleton(reactionReplyService);

            return map.BuildServiceProvider();
        }

        /// <summary>
        /// Load the credentials and data required for the bot to run.
        /// </summary>
        private void LoadProgramData()
        {
            if(!Directory.Exists(Directory.GetCurrentDirectory() + "/Data"))
            {
                Directory.CreateDirectory(Directory.GetCurrentDirectory() + "/Data");
            }

            if (File.Exists("Data/TokenCredentials.json"))
            {
                _credentials = JsonConvert.DeserializeObject<TokenCredentials>(File.ReadAllText("Data/TokenCredentials.json"));
            }
            else
            {
                _credentials = new TokenCredentials()
                {
                    Bot_Token = ""
                };
                File.WriteAllText("Data/TokenCredentials.json", JsonConvert.SerializeObject(_credentials));
            }

            if(File.Exists("Data/ProgramData.json"))
            {
                _data = JsonConvert.DeserializeObject<ProgramData>(File.ReadAllText("Data/ProgramData.json"));
            }
            else
            {
                _data = new ProgramData();
                File.WriteAllText("Data/ProgramData.json", JsonConvert.SerializeObject(_data));
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

        private async Task HandleMessageAsync(SocketMessage arg)
        {
            await _npcPingService.HandleMessageReceived(arg);
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
                IResult result;
                try
                {
                    result = await _commands.ExecuteAsync(context, pos, _services);
                }
                catch(Exception e)
                {
                    var bot_owner = await _client.GetUserAsync(126538520193007616);

                    await bot_owner.SendMessageAsync($"{msg.Author.Username}#{msg.Author.Discriminator} encountered an error in <#{msg.Channel.Id}>");
                    await bot_owner.SendMessageAsync($"Economy Bot error: \n{e.Message}\n{e.StackTrace}");
                    Console.WriteLine(e.Message + "\n\n" + e.StackTrace);
                }

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
        internal static Task Log(LogMessage msg)
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

            DateTime timeNow;
            try
            {
                timeNow = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById("America/Chicago"));
            }
            catch
            {
                timeNow = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time"));
            }
            Console.WriteLine($"[{timeNow.ToShortDateString()}-{timeNow.ToShortTimeString()}] <{msg.Severity}> {msg.Source}: {msg.Message} {msg.Exception}");
            Console.ResetColor();

            return Task.CompletedTask;
        }
    }
}
