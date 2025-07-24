using BandidosRPDiscordBot.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace BandidosRPDiscordBot
{
    public class Program
    {
        private DiscordSocketClient _client;
        private IConfiguration _config;
        private InteractionService _interactionService;
        private IServiceProvider _services;

        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            // 🔐 Leer token y GuildId desde UserSecrets
            _config = new ConfigurationBuilder()
                .AddUserSecrets<Program>()
                .Build();

            string token = _config["DiscordToken"];
            ulong guildId = ulong.Parse(_config["GuildId"]);

            // 🧰 Registrar servicios en el contenedor
            _services = new ServiceCollection()
                .AddSingleton<IMtaServerService, MtaServerService>()
                .AddSingleton(_config)
                .BuildServiceProvider();

            // ⚙️ Discord client
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged
            });
            _client.Log += Log;

            // ⚙️ InteractionService para slash commands
            _interactionService = new InteractionService(_client.Rest);

            // 📦 Cargar módulos desde ensamblado
            await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            // 🧠 Ejecutar comandos
            _client.InteractionCreated += async interaction =>
            {
                var ctx = new SocketInteractionContext(_client, interaction);
                await _interactionService.ExecuteCommandAsync(ctx, _services);
            };

            // 📌 Registrar comandos en el evento Ready
            _client.Ready += async () =>
            {
                Console.WriteLine($"✅ Bot conectado como {_client.CurrentUser}");

                try
                {
                    await _interactionService.RegisterCommandsToGuildAsync(guildId);
                    Console.WriteLine($"✅ Comandos registrados en el servidor: {guildId}");

                    var comandos = _interactionService.SlashCommands;
                    Console.WriteLine($"📦 Total de comandos activos: {comandos.Count}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Error al registrar comandos: {ex.Message}");
                }
            };

            // 🚀 Arrancar bot
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
            Console.WriteLine("🔄 Bot corriendo, esperando comandos...");
            await Task.Delay(-1);
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine($"[Discord] {msg}");
            return Task.CompletedTask;
        }
    }
}