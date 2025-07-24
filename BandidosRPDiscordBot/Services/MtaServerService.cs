using BandidosRPDiscordBot.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BandidosRPDiscordBot.Services
{
    public interface IMtaServerService
    {
        Task<List<MTAServerResponsePlayer>> GetPlayersAsync(string ip, int port, int timeout = 3000);
    }

    public class MtaServerService : IMtaServerService
    {
        public async Task<List<MTAServerResponsePlayer>> GetPlayersAsync(string ip, int port, int timeout = 3000)
        {
            var players = new List<MTAServerResponsePlayer>();

            try
            {
                using var udpClient = new UdpClient();
                udpClient.Client.ReceiveTimeout = timeout;

                var endpoint = new IPEndPoint(IPAddress.Parse(ip), port + 123); // ASE usa puerto base + 123
                byte[] socketRequestTag = new byte[] { 0xFF, 0xFF, 0xFF, 0x01 };

                // Enviar consulta
                await udpClient.SendAsync(socketRequestTag, socketRequestTag.Length, endpoint);

                using var cts = new CancellationTokenSource(timeout);
                var result = await udpClient.ReceiveAsync(cts.Token);
                var data = result.Buffer;

                // Validación básica
                if (data == null || data.Length < 50)
                {
                    Console.WriteLine("⚠️ Respuesta del servidor demasiado corta o vacía.");
                    return players;
                }

                int index = 0;
                index += 4; // socketResponseTag
                int length = data[index++];
                index += length - 1; // socketResponseGame

                for (int i = 0; i < 8; i++) // saltar datos del servidor
                {
                    length = data[index++];
                    index += length - 1;
                }

                while (data[index] != 0x00) // hasta endServerInfoSuffix
                {
                    length = data[index++];
                    index += length - 1;

                    length = data[index++];
                    index += length - 1;
                }

                index++; // saltar endServerInfoSuffix

                // Leer jugadores conectados
                while (index < data.Length && data[index] == 0x00)
                {
                    index++; // startPlayerInfoPrefix

                    length = data[index++];
                    string playerName = Encoding.UTF8.GetString(data, index, length - 1);
                    index += length - 1;

                    index += 2; // skip team y skin

                    length = data[index++];
                    string scoreStr = Encoding.UTF8.GetString(data, index, length - 1);
                    index += length - 1;

                    length = data[index++];
                    string pingStr = Encoding.UTF8.GetString(data, index, length - 1);
                    index += length - 1;

                    index++; // skip time

                    players.Add(new MTAServerResponsePlayer
                    {
                        Name = playerName,
                        Score = int.TryParse(scoreStr, out int score) ? score : 0,
                        Ping = int.TryParse(pingStr, out int ping) ? ping : 0
                    });
                }

                Console.WriteLine($"✅ Jugadores recibidos: {players.Count}");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("⏱️ Timeout de lectura UDP. El servidor MTA no respondió.");
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"📡 Error de red al contactar el servidor MTA: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔥 Error inesperado al parsear respuesta MTA: {ex.Message}");
            }

            return players;
        }
    }
}
