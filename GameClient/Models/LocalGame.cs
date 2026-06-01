using System;
using System.Collections.Generic;

namespace GameClient.Models
{
    public class LocalGame
    {
        public int Id { get; set; }
        public string PlayerName { get; set; }
        public DateTime StartTime { get; set; }
        public int DurationSeconds { get; set; }
        public string Result { get; set; }

        public virtual ICollection<LocalMove> Moves { get; set; }
            = new List<LocalMove>();
    }

    public class LocalMove
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public int MoveNumber { get; set; }
        public int FromRow { get; set; }
        public int FromCol { get; set; }
        public int ToRow { get; set; }
        public int ToCol { get; set; }
        public bool IsServerMove { get; set; }

        public virtual LocalGame Game { get; set; }
    }
}