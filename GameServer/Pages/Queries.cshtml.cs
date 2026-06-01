using GameServer.Data;
using GameServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GameServer.Pages
{
    public class QueriesModel : PageModel
    {
        private readonly AppDbContext _db;
        public QueriesModel(AppDbContext db) { _db = db; }

        public List<Player> Q22_Players { get; set; } = new();
        public List<object> Q23_Players { get; set; } = new();
        public List<Game> Q24_Games { get; set; } = new();
        public List<object> Q25_FirstPlayers { get; set; } = new();
        public List<Game> Q26_Games { get; set; } = new();
        public List<object> Q27_PlayerGameCount { get; set; } = new();
        public List<IGrouping<int, Player>> Q28_Groups { get; set; } = new();
        public List<IGrouping<string, Player>> Q29_ByCountry { get; set; } = new();
        public List<object> Q30_TopCountries { get; set; } = new();

        public List<SelectListItem> PlayerNames { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SelectedPlayer { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SortOrder { get; set; }

        public async Task OnGetAsync()
        {
            var q22Raw = await _db.Players
                .Include(p => p.Country)
                .Include(p => p.Games)
                .Where(p => p.Games.Any(g => g.Result != null))
                .ToListAsync();

            Q22_Players = SortOrder == "desc"
                ? q22Raw.OrderByDescending(p => p.FirstName.ToLower()).ToList()
                : q22Raw.OrderBy(p => p.FirstName.ToLower()).ToList();

            var q23Raw = await _db.Players
                .Include(p => p.Games)
                .Where(p => p.Games.Any(g => g.Result != null))
                .Select(p => new
                {
                    p.FirstName,
                    LastGame = p.Games.Where(g => g.Result != null).Max(g => g.StartTime)
                })
                .ToListAsync();

            Q23_Players = q23Raw
                .OrderByDescending(x => x.FirstName.ToLower())
                .Cast<object>()
                .ToList();

            Q24_Games = await _db.Games
                .Include(g => g.Player)
                .ToListAsync();

            var q25Raw = await _db.Players
                .Include(p => p.Country)
                .Include(p => p.Games)
                .Where(p => p.Games.Any())
                .ToListAsync();

            Q25_FirstPlayers = q25Raw
                .GroupBy(p => p.CountryId)
                .Select(g => (object)new
                {
                    Country = g.First().Country.Name,
                    Player = g.OrderBy(p => p.Games.Min(gm => gm.StartTime))
                              .First().FirstName
                })
                .ToList();

            var rawNames = await _db.Players
                .Select(p => p.FirstName)
                .ToListAsync();

            PlayerNames = rawNames
                .DistinctBy(n => n.ToLower())
                .OrderBy(n => n.ToLower())
                .Select(n => new SelectListItem
                {
                    Value = n,
                    Text = n
                })
                .ToList();

            if (!string.IsNullOrEmpty(SelectedPlayer))
            {
                Q26_Games = await _db.Games
                    .Include(g => g.Player)
                    .Where(g => g.Player.FirstName.ToLower() == SelectedPlayer.ToLower()).ToListAsync();
            }
            Q27_PlayerGameCount = await _db.Players
                .Select(p => (object)new
                {
                    p.FirstName,
                    GameCount = p.Games.Count()
                })
                .ToListAsync();

            Q28_Groups = _db.Players
                .Include(p => p.Games)
                .AsEnumerable()
                .GroupBy(p => p.Games.Count)
                .OrderByDescending(g => g.Key)
                .ToList();

            Q29_ByCountry = _db.Players
                .Include(p => p.Country)
                .AsEnumerable()
                .GroupBy(p => p.Country.Name)
                .OrderBy(g => g.Key)
                .ToList();

            var q30Raw = await _db.Games
                .Include(g => g.Player)
                .ThenInclude(p => p.Country)
                .GroupBy(g => g.Player.Country.Name)
                .Select(g => new
                {
                    Country = g.Key,
                    GameCount = g.Count()
                })
                .OrderByDescending(g => g.GameCount)
                .Take(2)
                .ToListAsync();

            Q30_TopCountries = q30Raw.Cast<object>().ToList();
        }
    }
}