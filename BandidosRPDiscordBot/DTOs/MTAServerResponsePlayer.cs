using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BandidosRPDiscordBot.DTOs
{
    /// <summary>
    /// Clase que representa la respuesta del servidor MTA para un jugador.
    /// </summary>
    public class MTAServerResponsePlayer
    {
        /// <summary>
        /// Nombre del jugador.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Ping del jugador en milisegundos.
        /// </summary>
        public int Ping { get; set; }

        /// <summary>
        /// Puntuación del jugador (en algunos modos de juego puede ser kills, en otros puede ser puntos).
        /// </summary>
        public int Score { get; set; }
    }
}
