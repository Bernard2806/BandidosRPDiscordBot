using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BandidosRPDiscordBot.Services
{
    public interface IMtaServerService
    {
        Task<List<MTAServerResponsePlayer>> GetPlayersAsync(string ip, int port, int timeout = 3000);
    }

    public class MTAServerResponsePlayer
    {
        public string Name { get; set; } = string.Empty;
        public int Ping { get; set; }
        public int Score { get; set; }
    }

    public class MtaServerService : IMtaServerService
    {
        // Máscara de bits válidos para prefijos de jugadores
        private const byte VALID_PLAYER_PREFIX_MASK = 0x00 | 0x01 | 0x02 | 0x04 | 0x08 | 0x16 | 0x32; // = 0x3F

        public async Task<List<MTAServerResponsePlayer>> GetPlayersAsync(string ip, int port, int timeout = 3000)
        {
            var players = new List<MTAServerResponsePlayer>();

            try
            {
                using var udpClient = new UdpClient();

                // Configurar timeout
                udpClient.Client.ReceiveTimeout = timeout;
                udpClient.Client.SendTimeout = timeout;

                var endpoint = new IPEndPoint(IPAddress.Parse(ip), port + 123);
                byte[] socketRequestTag = Encoding.ASCII.GetBytes("s");

                // Conectar y enviar
                await udpClient.Client.ConnectAsync(endpoint);
                await udpClient.SendAsync(socketRequestTag, socketRequestTag.Length);

                using var cts = new CancellationTokenSource(timeout);
                var result = await udpClient.ReceiveAsync(cts.Token);
                var data = result.Buffer;

                if (data == null || data.Length < 10)
                {
                    Console.WriteLine("⚠️ Respuesta vacía o muy corta.");
                    return players;
                }

                Console.WriteLine($"📦 Tamaño de respuesta UDP: {data.Length} bytes");
                Console.WriteLine("🔍 Primeros 20 bytes (HEX): " + BitConverter.ToString(data, 0, Math.Min(20, data.Length)));

                int index = 0;

                // Validar encabezado "EYE1"
                if (index + 4 > data.Length || Encoding.ASCII.GetString(data, index, 4) != "EYE1")
                {
                    Console.WriteLine("❌ Encabezado inválido.");
                    return players;
                }
                index += 4;

                // Validar juego "mta"
                if (index >= data.Length) return players;
                int length = data[index++];
                if (index + length - 1 > data.Length) return players;

                string gameTag = Encoding.ASCII.GetString(data, index, length - 1);
                if (gameTag != "mta")
                {
                    Console.WriteLine($"❌ Juego inválido: '{gameTag}', esperado 'mta'");
                    return players;
                }
                index += length - 1;

                Console.WriteLine("✅ Encabezado y juego válidos");

                // Saltar info general del servidor (8 campos)
                for (int i = 0; i < 8; i++)
                {
                    if (index >= data.Length)
                    {
                        Console.WriteLine($"❌ Fin prematuro en campo {i}");
                        return players;
                    }

                    length = data[index++];
                    if (index + length - 1 > data.Length)
                    {
                        Console.WriteLine($"❌ Longitud inválida en campo {i}: {length}");
                        return players;
                    }

                    // Leer el campo para debug
                    string fieldValue = Encoding.UTF8.GetString(data, index, length - 1);
                    Console.WriteLine($"🔍 Campo {i}: '{fieldValue}' (longitud: {length - 1})");

                    index += length - 1;
                }

                Console.WriteLine("✅ Info del servidor procesada");

                // Saltar reglas del servidor
                int rulesSkipped = 0;
                while (index < data.Length && data[index] != 0x01)
                {
                    // Nombre de la regla
                    if (index >= data.Length) break;
                    length = data[index++];
                    if (index + length - 1 > data.Length) break;
                    string ruleName = Encoding.UTF8.GetString(data, index, length - 1);
                    index += length - 1;

                    // Valor de la regla
                    if (index >= data.Length) break;
                    length = data[index++];
                    if (index + length - 1 > data.Length) break;
                    string ruleValue = Encoding.UTF8.GetString(data, index, length - 1);
                    index += length - 1;

                    Console.WriteLine($"🔍 Regla: '{ruleName}' = '{ruleValue}'");
                    rulesSkipped++;
                }

                Console.WriteLine($"✅ {rulesSkipped} reglas procesadas");

                // Verificar que encontramos el separador 0x01
                if (index >= data.Length || data[index] != 0x01)
                {
                    Console.WriteLine("❌ No se encontró el separador de jugadores (0x01)");
                    return players;
                }

                index++; // Saltar el separador 0x01
                Console.WriteLine("✅ Separador de jugadores encontrado");

                // Leer jugadores
                int playersProcessed = 0;
                while (index < data.Length)
                {
                    if (index >= data.Length) break;

                    byte prefix = data[index];
                    Console.WriteLine($"🔍 Posición {index}: Prefijo del jugador: 0x{prefix:X2}");

                    // Verificar si el prefijo es válido usando la máscara
                    // En el protocolo MTA, el prefijo debe tener solo bits válidos
                    if ((prefix & VALID_PLAYER_PREFIX_MASK) != prefix)
                    {
                        Console.WriteLine($"❌ Prefijo inválido: 0x{prefix:X2} (máscara válida: 0x{VALID_PLAYER_PREFIX_MASK:X2})");

                        // Mostrar los siguientes bytes para debug
                        int remainingBytes = Math.Min(10, data.Length - index);
                        if (remainingBytes > 0)
                        {
                            string nextBytes = BitConverter.ToString(data, index, remainingBytes);
                            Console.WriteLine($"🔍 Siguientes {remainingBytes} bytes: {nextBytes}");
                        }

                        Console.WriteLine("❌ Terminando lectura de jugadores");
                        break;
                    }

                    index++; // Avanzar después del prefijo

                    try
                    {
                        // Leer nombre del jugador
                        if (index >= data.Length) break;
                        length = data[index++];
                        if (index + length - 1 > data.Length) break;
                        string name = Encoding.UTF8.GetString(data, index, length - 1);
                        index += length - 1;

                        // Saltar team (1 byte) y skin (1 byte)
                        if (index + 2 > data.Length) break;
                        index += 2;

                        // Leer score
                        if (index >= data.Length) break;
                        length = data[index++];
                        if (index + length - 1 > data.Length) break;
                        string scoreStr = Encoding.UTF8.GetString(data, index, length - 1);
                        index += length - 1;

                        // Leer ping
                        if (index >= data.Length) break;
                        length = data[index++];
                        if (index + length - 1 > data.Length) break;
                        string pingStr = Encoding.UTF8.GetString(data, index, length - 1);
                        index += length - 1;

                        // Saltar time (1 byte)
                        if (index + 1 > data.Length) break;
                        index += 1;

                        var player = new MTAServerResponsePlayer
                        {
                            Name = name.TrimEnd('\0'),
                            Score = int.TryParse(scoreStr, out var score) ? score : 0,
                            Ping = int.TryParse(pingStr, out var ping) ? ping : 0
                        };

                        players.Add(player);
                        playersProcessed++;

                        Console.WriteLine($"✅ Jugador {playersProcessed}: '{player.Name}' (Score: {player.Score}, Ping: {player.Ping}) - Siguiente posición: {index}");

                        // Debug: mostrar los siguientes bytes para el próximo jugador
                        if (index < data.Length)
                        {
                            int remainingBytes = Math.Min(5, data.Length - index);
                            if (remainingBytes > 0)
                            {
                                string nextBytes = BitConverter.ToString(data, index, remainingBytes);
                                Console.WriteLine($"🔍 Próximos {remainingBytes} bytes: {nextBytes}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error procesando jugador {playersProcessed + 1}: {ex.Message}");
                        break;
                    }
                }

                Console.WriteLine($"✅ Total de jugadores procesados: {players.Count}");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("⏱️ Timeout de respuesta del servidor.");
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"📡 Error de socket: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔥 Error inesperado: {ex.Message}");
                Console.WriteLine($"🔥 Stack trace: {ex.StackTrace}");
            }

            return players;
        }
    }
}