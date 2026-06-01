namespace GameServer.Models
{
    public class Move
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public int MoveNumber { get; set; }
        public int FromRow { get; set; }
        public int FromCol { get; set; }
        public int ToRow { get; set; }
        public int ToCol { get; set; }
        public bool IsServerMove { get; set; } = false;

        public Game Game { get; set; } = null!;
    }
}