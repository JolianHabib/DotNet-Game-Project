using GameServer.Data;
using GameServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GameServer.Pages
{
    public class ManagePlayersModel : PageModel
    {
        private readonly AppDbContext _db;
        public ManagePlayersModel(AppDbContext db) { _db = db; }

        public List<Player> Players { get; set; } = new();
        public List<SelectListItem> CountryOptions { get; set; } = new();

        [BindProperty]
        public Player EditPlayer { get; set; } = new();

        public string? Message { get; set; }

        public async Task OnGetAsync()
        {
            await LoadData();
        }

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            await LoadData();

            if (string.IsNullOrWhiteSpace(EditPlayer.FirstName) || EditPlayer.FirstName.Length < 2)
            {
                Message = "Error: First name must be at least 2 characters!";
                return Page();
            }

            if (string.IsNullOrWhiteSpace(EditPlayer.Phone) || !System.Text.RegularExpressions.Regex.IsMatch(EditPlayer.Phone, @"^\d{10}$"))
            {
                Message = "Error: Phone must be exactly 10 digits!";
                return Page();
            }

            if (EditPlayer.CountryId <= 0)
            {
                Message = "Error: Please select a country!";
                return Page();
            }

            var player = await _db.Players.FindAsync(EditPlayer.Id);
            if (player == null)
            {
                Message = "Player not found!";
                return Page();
            }

            player.FirstName = EditPlayer.FirstName;
            player.Phone = EditPlayer.Phone;
            player.CountryId = EditPlayer.CountryId;

            await _db.SaveChangesAsync();
            Message = "Player updated successfully!";
            await LoadData();
            return Page();
        }
        public async Task<IActionResult> OnPostDeletePlayerAsync(int id)
        {
            var player = await _db.Players.FindAsync(id);
            if (player != null)
            {
                _db.Players.Remove(player);
                await _db.SaveChangesAsync();
                Message = "Player deleted successfully!";
            }
            await LoadData();
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteGameAsync(int id)
        {
            var game = await _db.Games.FindAsync(id);
            if (game != null)
            {
                _db.Games.Remove(game);
                await _db.SaveChangesAsync();
                Message = "Game deleted successfully!";
            }
            await LoadData();
            return Page();
        }

        private async Task LoadData()
        {
            Players = await _db.Players
                .Include(p => p.Country)
                .Include(p => p.Games)
                .ToListAsync();

            CountryOptions = await _db.Countries
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                }).ToListAsync();
        }
    }
}