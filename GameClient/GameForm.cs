using GameClient.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GameClient
{
    public partial class GameForm : Form
    {
        const int ROWS = 8;
        const int COLS = 4;
        const int CELL_SIZE = 70;
        const int BOARD_X = 20;
        const int BOARD_Y = 60;

        int animStep = 0;
        int serverGameId = 0;

        int[,] board = new int[ROWS, COLS];

        int selectedRow = -1, selectedCol = -1;
        Bitmap drawingLayer;
        bool isReplayMode = false;

        Timer animTimer = new Timer();
        Timer gameTimer = new Timer();
        int timeLeft = 10;
        int timeLimit = 10;
        bool isPlayerTurn = true;
        bool isDrawing = false;
        Point lastDrawPoint;

        Timer winTimer = new Timer();
        bool winFlash = false;
        int winFlashCount = 0;
        bool gameOver = false;
        int winner = 0; 

        Dictionary<string, int> soldierIds = new Dictionary<string, int>();
        HashSet<int> usedBackMove = new HashSet<int>();
        int nextSoldierId = 0;
        LocalDbContext localDb;
        LocalGame currentGame = null;
        int moveCounter = 0;
        string currentPlayerName = "Player";
        static readonly System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient(
            new System.Net.Http.HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true
            });
        const string SERVER_URL = "https://localhost:7266";
        public GameForm()
        {
            InitializeComponent();
            this.Text = "Checkers Game";
            this.Size = new Size(800, 700);
            this.DoubleBuffered = true;

            InitBoard();
            InitTimers();
            SetupControls();
        }

        void InitBoard()
        {
            board = new int[ROWS, COLS];
            board[0, 1] = 2;
            board[0, 3] = 2;
            board[1, 0] = 2;
            board[1, 2] = 2;
            board[6, 1] = 1;
            board[6, 3] = 1;
            board[7, 0] = 1;
            board[7, 2] = 1;

            drawingLayer = new Bitmap(COLS * CELL_SIZE + 10, ROWS * CELL_SIZE + 10);

            soldierIds.Clear();
            usedBackMove.Clear();
            nextSoldierId = 0;
            for (int r = 0; r < ROWS; r++)
                for (int c = 0; c < COLS; c++)
                    if (board[r, c] == 1)
                        soldierIds[$"{r},{c}"] = nextSoldierId++;
        }

        void InitTimers()
        {
            gameTimer.Interval = 1000;
            gameTimer.Tick += GameTimer_Tick;

            animTimer.Interval = 100;
            animTimer.Tick += (s, e) => this.Invalidate();
            animTimer.Start();

            winTimer.Interval = 300;
            winTimer.Tick += WinTimer_Tick;
        }

        void SetupControls()
        {
            var lblTimeTitle = new Label
            {
                Text = "Time limit:",
                Location = new Point(BOARD_X + COLS * CELL_SIZE + 20, BOARD_Y),
                AutoSize = true
            };

            var cmbTime = new ComboBox
            {
                Location = new Point(BOARD_X + COLS * CELL_SIZE + 20, BOARD_Y + 25),
                Width = 80,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbTime.Items.AddRange(new object[] { 2, 5, 15 });
            cmbTime.SelectedIndex = -1;
            cmbTime.SelectedIndexChanged += (s, e) =>
            {
                timeLimit = (int)cmbTime.SelectedItem;
                timeLeft = timeLimit;
                UpdateTimerLabel();
            };

            var lblTimer = new Label
            {
                Name = "lblTimer",
                Text = $"Time: {timeLeft}s",
                Location = new Point(BOARD_X + COLS * CELL_SIZE + 20, BOARD_Y + 60),
                AutoSize = true,
                Font = new Font("Arial", 14, FontStyle.Bold),
                ForeColor = Color.Red
            };

            var btnClear = new Button
            {
                Text = "Clear Drawing",
                Location = new Point(BOARD_X + COLS * CELL_SIZE + 20, BOARD_Y + 100),
                Width = 120
            };
            btnClear.Click += (s, e) =>
            {
                drawingLayer = new Bitmap(
                    COLS * CELL_SIZE + 10,
                    ROWS * CELL_SIZE + 10);
                this.Invalidate();
            };

            var btnStart = new Button
            {
                Text = "Start Game",
                Location = new Point(BOARD_X + COLS * CELL_SIZE + 20, BOARD_Y + 140),
                Width = 120,
                BackColor = Color.Green,
                ForeColor = Color.White,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            btnStart.Click += BtnStart_Click;

            var btnReplay = new Button
            {
                Text = "Replay Game",
                Location = new Point(BOARD_X + COLS * CELL_SIZE + 20, BOARD_Y + 180),
                Width = 120,
                BackColor = Color.DarkBlue,
                ForeColor = Color.White
            };
            btnReplay.Click += BtnReplay_Click;

            var lblHelp = new Label
            {
                Text = "Left click: select/move\nRight click: free draw",
                Location = new Point(BOARD_X + COLS * CELL_SIZE + 20, BOARD_Y + 230),
                AutoSize = true,
                ForeColor = Color.DarkBlue
            };

            var lblPlayerInfo = new Label
            {
                Name = "lblPlayerInfo",
                Text = "",
                Location = new Point(BOARD_X + COLS * CELL_SIZE + 20, BOARD_Y + 270),
                AutoSize = true,
                ForeColor = Color.DarkGreen,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };

            this.Controls.AddRange(new Control[]
            {
    lblTimeTitle, cmbTime, lblTimer, btnClear, btnStart, btnReplay, lblHelp, lblPlayerInfo
            });

            this.MouseDown += GameForm_MouseDown;
            this.MouseMove += GameForm_MouseMove;
            this.MouseUp += (s, e) => isDrawing = false;
        }

        async void BtnStart_Click(object sender, EventArgs e)
        {
            isReplayMode = false;
            string input = "";
            using (var loginForm = new Form { Text = "Player Login", Size = new Size(300, 150) })
            {
                var lbl = new Label { Text = "Enter your Player ID:", Location = new Point(10, 10), AutoSize = true };
                var txt = new TextBox { Location = new Point(10, 35), Width = 260 };
                var btn = new Button { Text = "OK", Location = new Point(100, 70), Width = 80 };
                btn.Click += (s, ev) => { input = txt.Text; loginForm.Close(); };
                loginForm.Controls.AddRange(new Control[] { lbl, txt, btn });
                loginForm.ShowDialog();
            }

            if (string.IsNullOrWhiteSpace(input))
            {
                MessageBox.Show("You must enter your Player ID!", "Error");
                return;
            }

            if (!int.TryParse(input, out int playerId))
            {
                MessageBox.Show("Invalid ID. Please enter a number.", "Error");
                return;
            }

            try
            {
                var response = await httpClient.GetAsync($"{SERVER_URL}/api/players/{playerId}");

                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Player not found! Please register on the website first.", "Error");
                    return;
                }

                var json = await response.Content.ReadAsStringAsync();
                var root = JObject.Parse(json);
                currentPlayerName = root["firstName"]?.Value<string>() ?? "Player";

                MessageBox.Show($"Welcome, {currentPlayerName}!", "Login Success");
                var lbl = this.Controls["lblPlayerInfo"] as Label;
                if (lbl != null)
                {
                    var country = root["country"]?.Value<string>() ?? "";
                    var phone = root["phone"]?.Value<string>() ?? "";
                    lbl.Text = $"Player: {currentPlayerName}\nID: {playerId}\nCountry: {country}\nPhone: {phone}";
                }
                var startResponse = await httpClient.PostAsync(
                    $"{SERVER_URL}/api/games/start",
                    new System.Net.Http.StringContent(
                        playerId.ToString(),
                        System.Text.Encoding.UTF8,
                        "application/json"));
                if (startResponse.IsSuccessStatusCode)
                {
                    var startJson = await startResponse.Content.ReadAsStringAsync();
                    int idx2 = startJson.IndexOf("\"id\":");
                    if (idx2 >= 0)
                    {
                        int start2 = idx2 + 5;
                        int end2 = startJson.IndexOf(",", start2);
                        if (end2 < 0) end2 = startJson.IndexOf("}", start2);
                        serverGameId = int.Parse(startJson.Substring(start2, end2 - start2).Trim());
                    }
                }
            }
            catch
            {
                MessageBox.Show("Cannot connect to server. Make sure the server is running.", "Error");
                return;
            }
            localDb = new LocalDbContext(currentPlayerName);
            gameTimer.Stop();
            winTimer.Stop();
            InitBoard();
            gameOver = false;
            isPlayerTurn = true;
            timeLeft = timeLimit;
            winFlashCount = 0;
            winFlash = false;
            selectedRow = selectedCol = -1;
            UpdateTimerLabel();

            moveCounter = 0;
            currentGame = new LocalGame
            {
                PlayerName = currentPlayerName,
                StartTime = DateTime.Now,
                Result = "Unknown"
            };
            localDb.Games.Add(currentGame);
            localDb.SaveChanges();

            gameTimer.Start();
            this.Invalidate();
        }

        void BtnReplay_Click(object sender, EventArgs e)
        {
            if (localDb == null)
            {
                string input = "";
                using (var nameForm = new Form { Text = "Player Login", Size = new Size(300, 150) })
                {
                    var lbl = new Label { Text = "Enter your player name:", Location = new Point(10, 10), AutoSize = true };
                    var txt = new TextBox { Location = new Point(10, 35), Width = 260 };
                    var btn = new Button { Text = "OK", Location = new Point(100, 70), Width = 80 };
                    btn.Click += (s, ev) => { input = txt.Text; nameForm.Close(); };
                    nameForm.Controls.AddRange(new Control[] { lbl, txt, btn });
                    nameForm.ShowDialog();
                }

                if (string.IsNullOrWhiteSpace(input)) return;
                localDb = new LocalDbContext(input);
            }

            var games = localDb.Games.ToList();
            if (games.Count == 0)
            {
                MessageBox.Show("No recorded games found!", "Replay");
                return;
            }

            var form = new Form
            {
                Text = "Select Game to Replay",
                Size = new Size(450, 300)
            };

            var listBox = new ListBox { Dock = DockStyle.Fill };

            foreach (var g in games)
                listBox.Items.Add(
                    $"#{g.Id} - {g.PlayerName} - {g.StartTime:yyyy-MM-dd HH:mm} - {g.Result}");

            var btnOk = new Button
            {
                Text = "Replay Selected",
                Dock = DockStyle.Bottom,
                BackColor = Color.DarkBlue,
                ForeColor = Color.White,
                Height = 35
            };

            btnOk.Click += (s, ev) =>
            {
                if (listBox.SelectedIndex < 0) return;
                var selectedGame = games[listBox.SelectedIndex];
                form.Close();
                StartReplay(selectedGame);
            };

            form.Controls.Add(listBox);
            form.Controls.Add(btnOk);
            form.ShowDialog();
        }

        void StartReplay(LocalGame game)
        {
            isReplayMode = true;
            gameTimer.Stop();
            winTimer.Stop();
            InitBoard();
            gameOver = false;
            isPlayerTurn = false;
            selectedRow = selectedCol = -1;
            this.Invalidate();

            var moves = localDb.Moves
                .Where(m => m.GameId == game.Id)
                .OrderBy(m => m.MoveNumber)
                .ToList();

            int index = 0;
            var replayTimer = new Timer { Interval = 800 };

            replayTimer.Tick += (s, e) =>
            {
                if (index >= moves.Count)
                {
                    replayTimer.Stop();

                    if (game.Result == "Win")
                    {
                        winner = 1;
                        gameOver = true;
                        StartWinAnimation();
                    }
                    else if (game.Result == "Lose")
                    {
                        winner = 2;
                        gameOver = true;
                        StartWinAnimation();
                    }
                    else
                    {
                        MessageBox.Show(
                            $"Replay finished!\nResult: {game.Result}",
                            "Replay Done");
                    }
                    return;
                }

                var move = moves[index++];

                if (Math.Abs(move.ToRow - move.FromRow) == 2)
                {
                    int midR = (move.FromRow + move.ToRow) / 2;
                    int midC = (move.FromCol + move.ToCol) / 2;
                    board[midR, midC] = 0;
                }

                board[move.FromRow, move.FromCol] = 0;
                board[move.ToRow, move.ToCol] = move.IsServerMove ? 2 : 1;
                this.Invalidate();
            };

            replayTimer.Start();
        }
        void GameTimer_Tick(object sender, EventArgs e)
        {
            if (!isPlayerTurn || gameOver) return;

            timeLeft--;
            UpdateTimerLabel();

            if (timeLeft <= 0)
            {
                gameTimer.Stop();
                gameOver = true;
                winner = 2;
                SaveGameResult("Timeout");
                StartWinAnimation();
                MessageBox.Show("Time's up! Server wins!", "Game Over");
            }
        }

        void UpdateTimerLabel()
        {
            var lbl = this.Controls["lblTimer"] as Label;
            if (lbl != null)
                lbl.Text = $"Time: {timeLeft}s";
        }

        void GameForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (gameOver && e.Button != MouseButtons.Right) 
                return;
            int col = (e.X - BOARD_X) / CELL_SIZE;
            int row = (e.Y - BOARD_Y) / CELL_SIZE;

            bool onBoard = col >= 0 && col < COLS &&
                           row >= 0 && row < ROWS;

            if (e.Button == MouseButtons.Left && onBoard && isPlayerTurn)
                HandleBoardClick(row, col);
            else if (e.Button == MouseButtons.Right)
            {
                isDrawing = true;
                lastDrawPoint = new Point(e.X - BOARD_X, e.Y - BOARD_Y);
            }
        }

        void GameForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isDrawing) return;

            using (Graphics g = Graphics.FromImage(drawingLayer))
            {
                g.DrawLine(new Pen(Color.Red, 3),
                    lastDrawPoint,
                    new Point(e.X - BOARD_X, e.Y - BOARD_Y));
            }
            lastDrawPoint = new Point(e.X - BOARD_X, e.Y - BOARD_Y);
            this.Invalidate();
        }

        void HandleBoardClick(int row, int col)
        {
            if (currentGame == null)
            {
                MessageBox.Show("Please press Start Game first!", "Error");
                return;
            }

            if (board[row, col] == 1 && selectedRow == -1)
            {
                selectedRow = row;
                selectedCol = col;
            }
            else if (selectedRow != -1)
            {
                if (IsValidMove(selectedRow, selectedCol, row, col))
                {
                    MovePlayer(selectedRow, selectedCol, row, col);
                    selectedRow = selectedCol = -1;
                    isPlayerTurn = false;
                    gameTimer.Stop();
                    timeLeft = timeLimit;
                    UpdateTimerLabel();
                    if (!CheckWin())
                    {
                        var serverDelay = new Timer();
                        serverDelay.Interval = 800;
                        serverDelay.Tick += async (s, e) =>
                        {
                            serverDelay.Stop();
                            await ServerMove();
                            if (!gameOver)
                            {
                                CheckWin();
                                if (!gameOver)
                                {
                                    isPlayerTurn = true;
                                    timeLeft = timeLimit;
                                    gameTimer.Start();
                                }
                            }
                        };
                        serverDelay.Start();
                    }
                }
                else
                {
                    selectedRow = selectedCol = -1;
                }
            }
            this.Invalidate();
        }

        bool IsValidMove(int fromR, int fromC, int toR, int toC)
        {
            if (toR < 0 || toR >= ROWS || toC < 0 || toC >= COLS)
                return false;
            if (board[toR, toC] != 0) return false;

            int dr = toR - fromR;
            int dc = Math.Abs(toC - fromC);

            if (dr == -1 && dc == 1) return true;

            if (dr == 1 && dc == 1)
            {
                string key = $"{fromR},{fromC}";
                if (soldierIds.ContainsKey(key) && !usedBackMove.Contains(soldierIds[key]))
                    return true;
            }

            if (dr == -2 && dc == 2)
            {
                int midR = (fromR + toR) / 2;
                int midC = (fromC + toC) / 2;
                if (board[midR, midC] == 2) return true;
            }

            return false;
        }
        void MovePlayer(int fromR, int fromC, int toR, int toC)
        {
            if (Math.Abs(toR - fromR) == 2)
            {
                int midR = (fromR + toR) / 2;
                int midC = (fromC + toC) / 2;
                board[midR, midC] = 0;
                soldierIds.Remove($"{midR},{midC}");
            }

            board[toR, toC] = 1;
            board[fromR, fromC] = 0;

            string oldKey = $"{fromR},{fromC}";
            string newKey = $"{toR},{toC}";
            if (soldierIds.ContainsKey(oldKey))
            {
                int id = soldierIds[oldKey];
                soldierIds.Remove(oldKey);
                soldierIds[newKey] = id;

                if (toR > fromR)
                    usedBackMove.Add(id);
            }

            if (currentGame != null)
            {
                localDb.Moves.Add(new LocalMove
                {
                    GameId = currentGame.Id,
                    MoveNumber = ++moveCounter,
                    FromRow = fromR,
                    FromCol = fromC,
                    ToRow = toR,
                    ToCol = toC,
                    IsServerMove = false
                });
                localDb.SaveChanges();
            }
        }
        async Task ServerMove()
        {
            try
            {
                var boardArray = new int[ROWS][];
                for (int r = 0; r < ROWS; r++)
                {
                    boardArray[r] = new int[COLS];
                    for (int c = 0; c < COLS; c++)
                        boardArray[r][c] = board[r, c];
                }

                var request = new
                {
                    gameId = serverGameId,
                    moveNumber = moveCounter,
                    fromRow = 0,
                    fromCol = 0,
                    toRow = 0,
                    toCol = 0,
                    board = boardArray
                };

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(request);
                var content = new System.Net.Http.StringContent(
                    json, System.Text.Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync($"{SERVER_URL}/api/games/move", content);

                if (!response.IsSuccessStatusCode)
                {
                    gameOver = true;
                    winner = 1;
                    gameTimer.Stop();
                    SaveGameResult("Win");
                    StartWinAnimation();
                    return;
                }

                var responseJson = await response.Content.ReadAsStringAsync();

                if (responseJson == "null" || string.IsNullOrEmpty(responseJson))
                {
                    gameOver = true;
                    winner = 1;
                    gameTimer.Stop();
                    SaveGameResult("Win");
                    StartWinAnimation();
                    return;
                }

                var root = Newtonsoft.Json.Linq.JObject.Parse(responseJson);

                int fromR = root["fromRow"].Value<int>();
                int fromC = root["fromCol"].Value<int>();
                int toR = root["toRow"].Value<int>();
                int toC = root["toCol"].Value<int>();

                if (Math.Abs(toR - fromR) == 2)
                {
                    int midR = (fromR + toR) / 2;
                    int midC = (fromC + toC) / 2;
                    board[midR, midC] = 0;
                    soldierIds.Remove($"{midR},{midC}");

                }

                board[toR, toC] = 2;
                board[fromR, fromC] = 0;

                if (currentGame != null)
                {
                    localDb.Moves.Add(new LocalMove
                    {
                        GameId = currentGame.Id,
                        MoveNumber = ++moveCounter,
                        FromRow = fromR,
                        FromCol = fromC,
                        ToRow = toR,
                        ToCol = toC,
                        IsServerMove = true
                    });
                    localDb.SaveChanges();
                }
            }
            catch 
            {
                var allMoves = GetServerMoves();
                if (allMoves.Count == 0)
                {
                    gameOver = true;
                    winner = 1;
                    gameTimer.Stop();
                    SaveGameResult("Win");
                    StartWinAnimation();
                    return;
                }

                var rnd = new Random();
                var move = allMoves[rnd.Next(allMoves.Count)];

                if (Math.Abs(move.Item3 - move.Item1) == 2)
                {
                    int midR = (move.Item1 + move.Item3) / 2;
                    int midC = (move.Item2 + move.Item4) / 2;
                    board[midR, midC] = 0;
                    soldierIds.Remove($"{midR},{midC}");

                }

                board[move.Item3, move.Item4] = 2;
                board[move.Item1, move.Item2] = 0;

                if (currentGame != null)
                {
                    localDb.Moves.Add(new LocalMove
                    {
                        GameId = currentGame.Id,
                        MoveNumber = ++moveCounter,
                        FromRow = move.Item1,
                        FromCol = move.Item2,
                        ToRow = move.Item3,
                        ToCol = move.Item4,
                        IsServerMove = true
                    });
                    localDb.SaveChanges();
                }
            }

            if (GetServerMoves().Count == 0 && !gameOver)
            {
                gameOver = true;
                winner = 1;
                gameTimer.Stop();
                SaveGameResult("Win");
                StartWinAnimation();
                return;
            }

            this.Invalidate();
        }

        List<(int, int, int, int)> GetServerMoves()
        {
            var moves = new List<(int, int, int, int)>();
            for (int r = 0; r < ROWS; r++)
                for (int c = 0; c < COLS; c++)
                    if (board[r, c] == 2)
                    {
                        if (r + 1 < ROWS)
                        {
                            if (c + 1 < COLS && board[r + 1, c + 1] == 0)
                                moves.Add((r, c, r + 1, c + 1));
                            if (c - 1 >= 0 && board[r + 1, c - 1] == 0)
                                moves.Add((r, c, r + 1, c - 1));
                        }

                        if (r + 2 < ROWS)
                        {
                            if (c + 2 < COLS &&
                                board[r + 1, c + 1] == 1 &&
                                board[r + 2, c + 2] == 0)
                                moves.Add((r, c, r + 2, c + 2));

                            if (c - 2 >= 0 &&
                                board[r + 1, c - 1] == 1 &&
                                board[r + 2, c - 2] == 0)
                                moves.Add((r, c, r + 2, c - 2));
                        }
                    }
            return moves;
        }
        List<(int, int, int, int)> GetPlayerMoves()
        {
            var moves = new List<(int, int, int, int)>();
            for (int r = 0; r < ROWS; r++)
                for (int c = 0; c < COLS; c++)
                    if (board[r, c] == 1)
                    {
                        if (r - 1 >= 0)
                        {
                            if (c + 1 < COLS && board[r - 1, c + 1] == 0)
                                moves.Add((r, c, r - 1, c + 1));
                            if (c - 1 >= 0 && board[r - 1, c - 1] == 0)
                                moves.Add((r, c, r - 1, c - 1));
                        }

                        string key = $"{r},{c}";
                        if (r + 1 < ROWS && soldierIds.ContainsKey(key) && !usedBackMove.Contains(soldierIds[key]))
                        {
                            if (c + 1 < COLS && board[r + 1, c + 1] == 0)
                                moves.Add((r, c, r + 1, c + 1));
                            if (c - 1 >= 0 && board[r + 1, c - 1] == 0)
                                moves.Add((r, c, r + 1, c - 1));
                        }

                        if (r - 2 >= 0)
                        {
                            if (c + 2 < COLS && board[r - 1, c + 1] == 2 && board[r - 2, c + 2] == 0)
                                moves.Add((r, c, r - 2, c + 2));
                            if (c - 2 >= 0 && board[r - 1, c - 1] == 2 && board[r - 2, c - 2] == 0)
                                moves.Add((r, c, r - 2, c - 2));
                        }
                    }
            return moves;
        }
        bool CheckWin()
        {
            for (int c = 0; c < COLS; c++)
                if (board[0, c] == 1)
                {
                    gameOver = true; winner = 1;
                    gameTimer.Stop();
                    SaveGameResult("Win");
                    StartWinAnimation();
                    return true;
                }

            for (int c = 0; c < COLS; c++)
                if (board[ROWS - 1, c] == 2)
                {
                    gameOver = true; winner = 2;
                    gameTimer.Stop();
                    SaveGameResult("Lose");
                    StartWinAnimation();
                    return true;
                }

            if (GetPlayerMoves().Count == 0)
            {
                gameOver = true; winner = 2;
                gameTimer.Stop();
                SaveGameResult("Lose");
                StartWinAnimation();
                return true;
            }

            return false;
        }


        async void SaveGameResult(string result)
        {
            if (currentGame != null)
            {
                currentGame.Result = result;
                currentGame.DurationSeconds =
                    (int)(DateTime.Now - currentGame.StartTime).TotalSeconds;
                localDb.SaveChanges();

                try
                {
                    var request = new
                    {
                        gameId = serverGameId,
                        result = result,
                        durationSeconds = currentGame.DurationSeconds
                    };

                    var json = Newtonsoft.Json.JsonConvert.SerializeObject(request);
                    var content = new System.Net.Http.StringContent(
                        json, System.Text.Encoding.UTF8, "application/json");

                    await httpClient.PostAsync($"{SERVER_URL}/api/games/end", content);
                }
                catch { }
            }
        }

        void StartWinAnimation()
        {
            winFlashCount = 0;
            winTimer.Start();
        }

        void WinTimer_Tick(object sender, EventArgs e)
        {
            winFlash = !winFlash;
            winFlashCount++;
            this.Invalidate();

            if (winFlashCount >= 16)
            {
                winTimer.Stop();

                if (isReplayMode)
                {
                    gameOver = false;
                    winner = 0;
                    winFlash = false;
                    winFlashCount = 0;
                    isReplayMode = false;
                    InitBoard();
                    this.Invalidate();
                }
                else
                {
                    string msg = winner == 1 ? "You Win! " : "Server Wins!";
                    MessageBox.Show(msg, "Game Over");
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;

            DrawBoard(g);
            DrawPieces(g);
            DrawDrawingLayer(g);
            DrawTimerBar(g);
        }


        void DrawBoard(Graphics g)
        {
            animStep = (animStep + 1) % 20;
            int pulse = Math.Abs(animStep - 10); 

            for (int r = 0; r < ROWS; r++)
                for (int c = 0; c < COLS; c++)
                {
                    int x = BOARD_X + c * CELL_SIZE;
                    int y = BOARD_Y + r * CELL_SIZE;

                    Color cellColor = (r + c) % 2 == 0
                        ? Color.Wheat : Color.SaddleBrown;

                    if (r == selectedRow && c == selectedCol)
                    {
                        int green = 200 + pulse * 5;
                        cellColor = Color.FromArgb(255, green, 0);
                    }

                    using (var brush = new SolidBrush(cellColor))
                        g.FillRectangle(brush, x, y, CELL_SIZE, CELL_SIZE);
                    g.DrawRectangle(Pens.Black,
                        x, y, CELL_SIZE, CELL_SIZE);
                }
        }

        void DrawPieces(Graphics g)
        {
            for (int r = 0; r < ROWS; r++)
                for (int c = 0; c < COLS; c++)
                {
                    if (board[r, c] == 0) continue;

                    int pulse = Math.Abs(animStep - 10);
                    int offset = pulse / 3;

                    int x = BOARD_X + c * CELL_SIZE + 8 + offset;
                    int y = BOARD_Y + r * CELL_SIZE + 8 + offset;
                    int size = CELL_SIZE - 16 - offset * 2;

                    Color pieceColor;

                    if (board[r, c] == 1)
                        pieceColor = (gameOver && winner == 1 && winFlash && winTimer.Enabled)
                            ? Color.LimeGreen : Color.RoyalBlue;
                    else
                        pieceColor = (gameOver && winner == 2 && winFlash && winTimer.Enabled)
                            ? Color.LimeGreen : Color.Crimson;

                    using (var brush = new SolidBrush(pieceColor))
                        g.FillEllipse(brush, x, y, size, size);
                    g.DrawEllipse(Pens.Black, x, y, size, size);
                }
        }

        void DrawDrawingLayer(Graphics g)
        {
            g.DrawImage(drawingLayer, BOARD_X, BOARD_Y);
        }

        private void GameForm_Load(object sender, EventArgs e)
        {

        }

        void DrawTimerBar(Graphics g)
        {
            if (!isPlayerTurn || gameOver) return;

            int barWidth = timeLimit > 0
                ? (int)((float)timeLeft / timeLimit * COLS * CELL_SIZE)
                : 0;
            Color barColor = timeLeft > 5 ? Color.Green : Color.Red;

            using (var brush = new SolidBrush(barColor))
                g.FillRectangle(brush, BOARD_X, BOARD_Y - 20, barWidth, 12);
            g.DrawRectangle(Pens.Black,
                BOARD_X, BOARD_Y - 20, COLS * CELL_SIZE, 12);

            g.DrawString($"Your Turn - {timeLeft}s",
                new Font("Arial", 10, FontStyle.Bold),
                Brushes.Black, BOARD_X, BOARD_Y - 40);
        }
    }
}