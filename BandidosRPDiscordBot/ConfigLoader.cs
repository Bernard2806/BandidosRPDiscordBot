using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace BandidosRPDiscordBot
{
    public static class ConfigLoader
    {
        public static IConfiguration Load()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory);

            // Detección: ¿estamos en desarrollo local o producción VPS?
            if (IsRunningLocally())
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[ConfigLoader] 🌿 Modo local detectado: usando UserSecrets");
                Console.ResetColor();

                builder.AddUserSecrets<Program>();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[ConfigLoader] 🛡️ Modo producción detectado: usando appsettings.secrets.json");
                Console.ResetColor();

                builder.AddJsonFile("appsettings.secrets.json", optional: false, reloadOnChange: true);
            }

            return builder.Build();
        }

        private static bool IsRunningLocally()
        {
            var env = Environment.GetEnvironmentVariable("APP_ENVIRONMENT");

            if (string.IsNullOrWhiteSpace(env))
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("[ConfigLoader] ⚠️ Variable 'APP_ENVIRONMENT' no encontrada. Usando modo LOCAL por defecto.");
                Console.ResetColor();
                return true; // fallback seguro
            }

            if (env.Trim().Equals("Production", StringComparison.OrdinalIgnoreCase))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[ConfigLoader] 🛡️ Entorno 'Production' detectado.");
                Console.ResetColor();
                return false;
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[ConfigLoader] 🌱 Entorno personalizado detectado: '{env}'. Asumiendo modo LOCAL.");
            Console.ResetColor();
            return true;
        }

    }

}
