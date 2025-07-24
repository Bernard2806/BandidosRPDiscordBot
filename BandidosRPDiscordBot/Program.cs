using BandidosRPDiscordBot.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("❌ Error: No se encontró el token de Discord en UserSecrets");
                return;
            }

            if (!ulong.TryParse(_config["GuildId"], out ulong guildId))
            {
                Console.WriteLine("❌ Error: GuildId inválido en UserSecrets");
                return;
            }

            // 🧰 Registrar servicios en el contenedor
            _services = new ServiceCollection()
                .AddLogging(builder => builder.AddConsole()) // ✅ Esto permite ver los logs en consola
                .AddSingleton<IMtaServerService, MtaServerService>()
                .AddSingleton<ITimeSyncService, TimeSyncService>()
                .AddSingleton(_config)
                .BuildServiceProvider();

            // ⚙️ Discord client
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged,
                LogLevel = LogSeverity.Info
            });

            _client.Log += Log;

            // ⚙️ InteractionService para slash commands
            _interactionService = new InteractionService(_client);
            _interactionService.Log += Log;

            // 📦 Cargar módulos desde ensamblado
            await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            // Agregar comando de test temporalmente
            Console.WriteLine("🧪 Agregando comando de test...");

            // ⚡ Ejecutar comandos al recibir interacciones - VERSIÓN SIMPLIFICADA
            _client.InteractionCreated += async interaction =>
            {
                Console.WriteLine($"🎯 Interacción recibida a las: {DateTime.Now:HH:mm:ss.fff}");

                var ctx = new SocketInteractionContext(_client, interaction);
                var result = await _interactionService.ExecuteCommandAsync(ctx, _services);

                if (!result.IsSuccess)
                {
                    Console.WriteLine($"❌ Error al ejecutar comando: {result.Error} - {result.ErrorReason}");
                }
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

                    foreach (var comando in comandos)
                    {
                        Console.WriteLine($"   - /{comando.Name}: {comando.Description}");
                    }

                    // 🔁 Iniciar verificación periódica de desfase horario
                    var timer = new System.Timers.Timer(TimeSpan.FromMinutes(15).TotalMilliseconds);
                    timer.Elapsed += async (_, _) =>
                    {
                        var syncService = _services.GetRequiredService<ITimeSyncService>();
                        await syncService.CheckAndLogTimeOffsetAsync();
                    };
                    timer.Start();
                    Console.WriteLine("⏱️ Timer de verificación NTP iniciado cada 15 minutos");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Error al registrar comandos: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
            };

            // 🚀 Arrancar bot
            try
            {
                await _client.LoginAsync(TokenType.Bot, token);
                await _client.StartAsync();

                Console.WriteLine("🔄 Bot corriendo, esperando comandos...");
                await Task.Delay(-1);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al iniciar el bot: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine($"[{msg.Severity}] {msg.Source}: {msg.Message}");
            if (msg.Exception != null)
            {
                Console.WriteLine($"Exception: {msg.Exception}");
            }
            return Task.CompletedTask;
        }
    }
}