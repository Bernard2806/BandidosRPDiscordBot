using BandidosRPDiscordBot.DTOs;
using BandidosRPDiscordBot.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace BandidosRPDiscordBot
{
    public class MtaModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly IMtaServerService _mtaService;

        public MtaModule(IMtaServerService mtaService)
        {
            _mtaService = mtaService;
            Console.WriteLine("🔧 MtaModule constructor ejecutado");
        }

        [SlashCommand("usuarios", "Muestra la cantidad de jugadores conectados al servidor")]
        public async Task Usuarios()
        {
            var stopwatch = Stopwatch.StartNew();
            Console.WriteLine($"⏳ [{DateTime.Now:HH:mm:ss.fff}] Comando /usuarios recibido");

            try
            {
                // ✅ SOLUCIÓN: Responder en un hilo separado inmediatamente
                var respondTask = Task.Run(async () =>
                {
                    try
                    {
                        await RespondAsync("🔍 Consultando servidor MTA...", ephemeral: true);
                        Console.WriteLine($"✅ Respuesta enviada en: {stopwatch.ElapsedMilliseconds}ms");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error al responder: {ex.Message} - {stopwatch.ElapsedMilliseconds}ms");
                        return false;
                    }
                });

                // Esperar máximo 2 segundos para la respuesta
                var timeoutTask = Task.Delay(2000);
                var completedTask = await Task.WhenAny(respondTask, timeoutTask);

                if (completedTask == timeoutTask || !(await respondTask))
                {
                    Console.WriteLine($"❌ Timeout o fallo en respuesta inicial");
                    return;
                }

                // Ahora hacer la consulta al servidor MTA
                Console.WriteLine($"🌐 Iniciando consulta MTA - Tiempo: {stopwatch.ElapsedMilliseconds}ms");

                var jugadoresTask = _mtaService.GetPlayersAsync("144.217.174.214", 42531);
                var mtaTimeoutTask = Task.Delay(TimeSpan.FromSeconds(8));

                var mtaCompletedTask = await Task.WhenAny(jugadoresTask, mtaTimeoutTask);

                if (mtaCompletedTask == mtaTimeoutTask)
                {
                    var timeoutEmbed = new EmbedBuilder()
                        .WithTitle("⏳ Servidor sin respuesta")
                        .WithColor(Color.Orange)
                        .WithDescription("El servidor MTA tardó demasiado en responder.\nIntentá de nuevo más tarde.")
                        .WithFooter($"Timeout en: {stopwatch.ElapsedMilliseconds}ms")
                        .Build();

                    await FollowupAsync(embed: timeoutEmbed, ephemeral: true);
                    return;
                }

                var jugadores = await jugadoresTask;
                int cantidad = jugadores?.Count ?? 0;

                string listaJugadores = jugadores?.Any() == true
                    ? string.Join("\n", jugadores.Select(p => $"• **{p.Name}** (Ping: {p.Ping}ms)"))
                    : "⚠️ No hay jugadores conectados en este momento";

                Console.WriteLine($"📊 Jugadores: {cantidad} - Tiempo: {stopwatch.ElapsedMilliseconds}ms");

                var embed = new EmbedBuilder()
                    .WithTitle("👥 Jugadores en Bandidos RP")
                    .WithColor(cantidad > 0 ? Color.Green : Color.DarkRed)
                    .WithThumbnailUrl("https://i.imgur.com/Hf4hXNN.png")
                    .AddField("Cantidad", $"{cantidad} jugador{(cantidad != 1 ? "es" : "")}", true)
                    .AddField("Lista", listaJugadores, false)
                    .WithFooter($"Completado en: {stopwatch.ElapsedMilliseconds}ms")
                    .Build();

                await FollowupAsync(embed: embed);
                Console.WriteLine($"✅ Comando completado - {stopwatch.ElapsedMilliseconds}ms total");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error general: {ex.Message} - {stopwatch.ElapsedMilliseconds}ms");

                try
                {
                    if (!Context.Interaction.HasResponded)
                    {
                        await RespondAsync("❌ Error interno del bot", ephemeral: true);
                    }
                    else
                    {
                        await FollowupAsync("❌ Error durante la consulta", ephemeral: true);
                    }
                }
                catch
                {
                    Console.WriteLine("❌ No se pudo enviar mensaje de error");
                }
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        // Comando de test simple para verificar conectividad
        [SlashCommand("test", "Comando de prueba simple")]
        public async Task Test()
        {
            Console.WriteLine($"🧪 Test recibido: {DateTime.Now:HH:mm:ss.fff}");

            try
            {
                await RespondAsync("✅ Bot funcionando correctamente!", ephemeral: true);
                Console.WriteLine("✅ Test completado");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test falló: {ex.Message}");
            }
        }
    }
}