namespace GameServer.Models
{
    public class Player
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public int IdentityNumber { get; set; }
        public string Phone { get; set; } = string.Empty;
        public int CountryId { get; set; }

        public Country Country { get; set; } = null!;
        public ICollection<Game> Games { get; set; } = new List<Game>();
    }
}