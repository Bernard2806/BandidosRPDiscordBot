using BandidosRPDiscordBot.DTOs;
using BandidosRPDiscordBot.Services;
using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BandidosRPDiscordBot
{
    public class MtaModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly IMtaServerService _mtaService;

        public MtaModule(IMtaServerService mtaService)
        {
            _mtaService = mtaService;
        }

        [SlashCommand("usuarios", "Muestra la cantidad de jugadores conectados al servidor")]
        public async Task Usuarios()
        {
            Console.WriteLine("⏳ Ejecutando comando /usuarios...");
            try
            {
                var jugadores = await _mtaService.GetPlayersAsync("144.217.174.214", 42531);
                int cantidad = jugadores.Count;

                string listaJugadores = jugadores.Any()
                    ? string.Join("\n", jugadores.Select(p => $"• {p.Name}"))
                    : "⚠️ No hay jugadores conectados";

                var embed = new EmbedBuilder()
                    .WithTitle("👥 Jugadores conectados a Bandidos RP")
                    .WithColor(cantidad > 0 ? Color.Green : Color.DarkRed)
                    .WithThumbnailUrl("https://i.imgur.com/Hf4hXNN.png")
                    .AddField("Cantidad", cantidad.ToString(), true)
                    .AddField("Lista", listaJugadores, false)
                    .WithFooter("Actualizado al momento")
                    .Build();

                await RespondAsync(embed: embed);
            }
            catch (Exception ex)
            {
                var errorEmbed = new EmbedBuilder()
                    .WithTitle("❌ Error al consultar el servidor MTA")
                    .WithColor(Color.Red)
                    .WithDescription("No se pudo obtener la lista de jugadores. El servidor podría estar caído o no accesible.")
                    .AddField("Detalles técnicos", ex.Message, false)
                    .Build();

                await RespondAsync(embed: errorEmbed);
            }
        }
    }
}
