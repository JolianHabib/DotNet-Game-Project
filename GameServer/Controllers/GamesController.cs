using GameServer.Data;
using GameServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GamesController : ControllerBase
    {
        private readonly AppDbContext _db;
        private static readonly Random _rng = new Random();

        public GamesController(AppDbContext db)
        {
            _db = db;
        }

        [HttpPost("start")]
        
        public async Task<IActionResult> StartGame([FromBody] int identityNumber)
        {
            var player = await _db.Players
                .FirstOrDefaultAsync(p => p.IdentityNumber == identityNumber);

            if (player == null)
                return NotFound("Player not found");

            var game = new Game
            {
                PlayerId = player.Id,
                StartTime = DateTime.Now
            };

            _db.Games.Add(game);
            await _db.SaveChangesAsync();

            return Ok(new { id = game.Id, playerId = game.PlayerId }); 
        }

        [HttpPost("move")]
        public async Task<IActionResult> MakeMove([FromBody] MoveRequest request)
        {
            var playerMove = new Move
            {
                GameId = request.GameId,
                MoveNumber = request.MoveNumber,
                FromRow = request.FromRow,
                FromCol = request.FromCol,
                ToRow = request.ToRow,
                ToCol = request.ToCol,
                IsServerMove = false
            };
            _db.Moves.Add(playerMove);

            var serverMove = GenerateServerMove(request.Board, request.MoveNumber);

            if (serverMove != null)
            {
                serverMove.GameId = request.GameId;
                serverMove.IsServerMove = true;
                _db.Moves.Add(serverMove);
            }

            await _db.SaveChangesAsync();
            return Ok(serverMove);
        }

        [HttpPost("end")]
        public async Task<IActionResult> EndGame([FromBody] EndGameRequest request)
        {
            var game = await _db.Games.FindAsync(request.GameId);
            if (game == null) return NotFound();

            game.Result = request.Result;
            game.DurationSeconds = request.DurationSeconds > 0
                ? request.DurationSeconds
                : (int)(DateTime.Now - game.StartTime).TotalSeconds;
            await _db.SaveChangesAsync();
            return Ok(game);
        }

        private Move? GenerateServerMove(int[][] board, int moveNumber)
        {
            var validMoves = new List<Move>();
            var eatMoves = new List<Move>();

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    if (board[row][col] != 2) continue;

                    if (row + 1 < 8)
                    {
                        if (col + 1 < 4 && board[row + 1][col + 1] == 0)
                            validMoves.Add(new Move { FromRow = row, FromCol = col, ToRow = row + 1, ToCol = col + 1, MoveNumber = moveNumber + 1 });

                        if (col - 1 >= 0 && board[row + 1][col - 1] == 0)
                            validMoves.Add(new Move { FromRow = row, FromCol = col, ToRow = row + 1, ToCol = col - 1, MoveNumber = moveNumber + 1 });
                    }

                    if (row + 2 < 8)
                    {
                        if (col + 2 < 4 && board[row + 1][col + 1] == 1 && board[row + 2][col + 2] == 0)
                            eatMoves.Add(new Move { FromRow = row, FromCol = col, ToRow = row + 2, ToCol = col + 2, MoveNumber = moveNumber + 1 });

                        if (col - 2 >= 0 && board[row + 1][col - 1] == 1 && board[row + 2][col - 2] == 0)
                            eatMoves.Add(new Move { FromRow = row, FromCol = col, ToRow = row + 2, ToCol = col - 2, MoveNumber = moveNumber + 1 });
                    }
                }
            }

            var allMoves = eatMoves.Count > 0 ? eatMoves : validMoves;
            if (allMoves.Count == 0) return null;

            return allMoves[_rng.Next(allMoves.Count)];
        }
    }

    public class MoveRequest
    {
        public int GameId { get; set; }
        public int MoveNumber { get; set; }
        public int FromRow { get; set; }
        public int FromCol { get; set; }
        public int ToRow { get; set; }
        public int ToCol { get; set; }
        public int[][] Board { get; set; } = new int[8][];
    }

    public class EndGameRequest
    {
        public int GameId { get; set; }
        public string Result { get; set; } = string.Empty;
        public int DurationSeconds { get; set; }
    }
}