using GameServer.Data;
using GameServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace GameServer.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly AppDbContext _db;

        public RegisterModel(AppDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        public int PlayerCount { get; set; } = 1;

        [BindProperty]
        public List<PlayerInput> Players { get; set; } = new();

        public List<SelectListItem> CountryOptions { get; set; } = new();
        public bool RegistrationSuccess { get; set; } = false;

        public async Task OnGetAsync(int playerCount = 1)
        {
            PlayerCount = playerCount;
            await LoadCountries();
            Players = Enumerable.Range(0, PlayerCount)
                .Select(_ => new PlayerInput()).ToList();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadCountries();

            if (!ModelState.IsValid)
                return Page();

            foreach (var p in Players)
            {
                bool exists = await _db.Players
                    .AnyAsync(x => x.IdentityNumber == p.IdentityNumber);

                if (exists)
                {
                    ModelState.AddModelError("",
                        $"Identity number {p.IdentityNumber} already exists in the system.");
                    return Page();
                }

                _db.Players.Add(new Player
                {
                    FirstName = p.FirstName,
                    IdentityNumber = p.IdentityNumber,
                    Phone = p.Phone,
                    CountryId = p.CountryId
                });
            }

            await _db.SaveChangesAsync();
            RegistrationSuccess = true;
            return Page();
        }

        private async Task LoadCountries()
        {
            CountryOptions = await _db.Countries
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                }).ToListAsync();
        }
    }

    public class PlayerInput
    {
        [Required(ErrorMessage = "First name is required")]
        [MinLength(2, ErrorMessage = "First name must contain at least 2 characters")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Identity number is required")]
        [Range(1, 1000, ErrorMessage = "Identity number must be between 1 and 1000")]
        public int IdentityNumber { get; set; }

        [Required(ErrorMessage = "Phone is required")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone must contain exactly 10 digits")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a country")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a country")]
        public int CountryId { get; set; }
    }
}