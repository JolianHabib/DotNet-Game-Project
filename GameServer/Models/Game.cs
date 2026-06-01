namespace GameServer.Models
{
    public class Game
    {
        public int Id { get; set; }
        public int PlayerId { get; set; }
        public DateTime StartTime { get; set; } = DateTime.Now;
        public int? DurationSeconds { get; set; }
        public string? Result { get; set; } 

        public Player Player { get; set; } = null!;
        public ICollection<Move> Moves { get; set; } = new List<Move>();
    }
}