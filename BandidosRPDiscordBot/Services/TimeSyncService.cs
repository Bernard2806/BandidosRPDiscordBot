using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Threading.Tasks;

namespace BandidosRPDiscordBot.Services
{
    public interface ITimeSyncService
    {
        Task CheckAndLogTimeOffsetAsync();
    }

    public class TimeSyncService : ITimeSyncService
    {
        private readonly ILogger<TimeSyncService> _logger;
        private const string NtpServer = "time.windows.com";
        private const double ThresholdSeconds = 2.5;

        public TimeSyncService(ILogger<TimeSyncService> logger)
        {
            _logger = logger;
        }

        public async Task CheckAndLogTimeOffsetAsync()
        {
            try
            {
                var ntpTime = await GetNetworkTimeAsync(NtpServer);
                var localTime = DateTime.UtcNow;
                var offset = Math.Abs((ntpTime - localTime).TotalSeconds);

                if (offset > ThresholdSeconds)
                {
                    _logger.LogWarning($"⚠️ Desfase detectado: {offset:F2}s respecto a {NtpServer}");
                }
                else
                {
                    _logger.LogInformation($"✅ Sincronización OK: {offset:F2}s de diferencia con {NtpServer}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error al verificar hora NTP: {ex.Message}");
            }
        }

        private async Task<DateTime> GetNetworkTimeAsync(string ntpServer)
        {
            const int NtpDataLength = 48;
            var ntpData = new byte[NtpDataLength];
            ntpData[0] = 0x1B;

            using var client = new UdpClient();
            await client.SendAsync(ntpData, ntpData.Length, ntpServer, 123);
            var response = await client.ReceiveAsync();

            ulong intPart = BitConverter.ToUInt32(response.Buffer.Skip(40).Take(4).Reverse().ToArray());
            ulong fracPart = BitConverter.ToUInt32(response.Buffer.Skip(44).Take(4).Reverse().ToArray());

            var milliseconds = (intPart * 1000) + ((fracPart * 1000) / 0x100000000L);
            var ntpEpoch = new DateTime(1900, 1, 1);
            return ntpEpoch.AddMilliseconds((long)milliseconds);
        }
    }
}
