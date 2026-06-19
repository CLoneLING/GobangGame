using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

namespace GobangGame
{
    public partial class Form1 : Form
    {
        // 常量
        private const int BoardSize = 15;
        private const int BoardPadding = 20;

        // 游戏状态
        private int[,] board;
        private int currentPlayer = 1;
        private bool gameOver = false;
        private int lastRow = -1, lastCol = -1, lastPlayer = -1;

        // 游戏模式
        private enum GameMode { None, Local, AISimple, AIHard, OnlineHost, OnlineClient }
        private GameMode currentMode = GameMode.None;
        private bool isAITurn = false;

        // 联机相关
        private NetworkManager networkManager;
        private bool isOnlineGame = false;
        private bool isMyTurn = false;
        private string myPlayerId;
        private string myPlayerName;
        private List<PlayerInfo> currentPlayers = new List<PlayerInfo>();
        private bool isMyPlayer => currentPlayers.FirstOrDefault(p => p.Id == myPlayerId)?.IsPlayer ?? false;

        // 计分
        private int blackScore = 0, whiteScore = 0;
        private bool showScore = false;

        // 控件
        private DoubleBufferedPanel chessPanel;
        private Label statusLabel;
        private Button newGameButton;
        private Button undoButton;
        private Panel menuPanel;
        private Panel startMenuPanel;
        private Panel scorePanel;
        private Label blackScoreLabel, whiteScoreLabel;
        private CheckBox showScoreCheckBox;

        // 联机专用按钮
        private Button requestUndoButton;
        private Button requestStartButton;
        private Button requestResetButton;
        private ComboBox playerListCombo;
        private Button kickButton;
        private Button swapButton;

        // 聊天控件
        private TextBox chatHistory;
        private TextBox chatInput;
        private Button sendChatBtn;

        // AI评分表
        private readonly Dictionary<string, int> patternScore = new Dictionary<string, int>
        {
            {"11111", 10000000}, {"011110", 100000}, {"011112", 10000}, {"211110", 10000},
            {"01110", 10000}, {"01112", 1000}, {"21110", 1000}, {"001100", 500}, {"00110", 500}, {"01100", 500}
        };

        public Form1()
        {
            InitializeComponent();
            CreateControls();
            ShowStartMenu();
            this.Resize += (s, e) => RecalculateChessPanelBounds();
        }

        private void InitializeComponent() { }

        // --------------------- 界面构建 ---------------------
        private void CreateControls()
        {
            this.Text = "五子棋 - 全能对战";
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.ClientSize = new Size(800, 850);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            menuPanel = new Panel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };

            var buttonPanel = new Panel { Dock = DockStyle.Top, Height = 70, Padding = new Padding(20, 10, 20, 10) };
            newGameButton = new Button { Text = "新游戏", Dock = DockStyle.Left, Width = 100, Height = 40, Margin = new Padding(5) };
            undoButton = new Button { Text = "悔棋", Dock = DockStyle.Left, Width = 100, Height = 40, Margin = new Padding(5) };
            showScoreCheckBox = new CheckBox { Text = "显示得分", Dock = DockStyle.Right, Width = 100, Height = 40, TextAlign = ContentAlignment.MiddleCenter };
            showScoreCheckBox.CheckedChanged += (s, e) => { showScore = showScoreCheckBox.Checked; scorePanel.Visible = showScore; RecalculateChessPanelBounds(); };

            requestUndoButton = new Button { Text = "申请悔棋", Dock = DockStyle.Right, Width = 100, Height = 40, Margin = new Padding(5) };
            requestStartButton = new Button { Text = "开始游戏", Dock = DockStyle.Right, Width = 100, Height = 40, Margin = new Padding(5) };
            requestResetButton = new Button { Text = "重置游戏", Dock = DockStyle.Right, Width = 100, Height = 40, Margin = new Padding(5) };
            requestUndoButton.Click += RequestUndo_Click;
            requestStartButton.Click += RequestStart_Click;
            requestResetButton.Click += RequestReset_Click;

            playerListCombo = new ComboBox { Dock = DockStyle.Right, Width = 120, Height = 30, DropDownStyle = ComboBoxStyle.DropDownList, Visible = false };
            kickButton = new Button { Text = "踢出", Dock = DockStyle.Right, Width = 60, Height = 40, Margin = new Padding(5), Visible = false };
            swapButton = new Button { Text = "互换", Dock = DockStyle.Right, Width = 60, Height = 40, Margin = new Padding(5), Visible = false };
            kickButton.Click += KickButton_Click;
            swapButton.Click += SwapButton_Click;

            buttonPanel.Controls.Add(newGameButton);
            buttonPanel.Controls.Add(undoButton);
            buttonPanel.Controls.Add(showScoreCheckBox);
            buttonPanel.Controls.Add(requestUndoButton);
            buttonPanel.Controls.Add(requestStartButton);
            buttonPanel.Controls.Add(requestResetButton);
            buttonPanel.Controls.Add(kickButton);
            buttonPanel.Controls.Add(swapButton);
            buttonPanel.Controls.Add(playerListCombo);

            statusLabel = new Label { Dock = DockStyle.Top, Height = 45, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("微软雅黑", 12F, FontStyle.Bold), Text = "" };

            scorePanel = new Panel { Dock = DockStyle.Top, Height = 40, Visible = false };
            blackScoreLabel = new Label { Text = "黑方胜场: 0", Dock = DockStyle.Left, Width = 150, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("微软雅黑", 10F), Padding = new Padding(20, 0, 0, 0) };
            whiteScoreLabel = new Label { Text = "白方胜场: 0", Dock = DockStyle.Right, Width = 150, TextAlign = ContentAlignment.MiddleRight, Font = new Font("微软雅黑", 10F), Padding = new Padding(0, 0, 20, 0) };
            scorePanel.Controls.Add(blackScoreLabel);
            scorePanel.Controls.Add(whiteScoreLabel);

            menuPanel.Controls.Add(scorePanel);
            menuPanel.Controls.Add(statusLabel);
            menuPanel.Controls.Add(buttonPanel);

            chessPanel = new DoubleBufferedPanel();
            chessPanel.BackColor = Color.Beige;
            chessPanel.Paint += ChessPanel_Paint;
            chessPanel.MouseClick += ChessPanel_MouseClick;
            chessPanel.Visible = false;

            chatHistory = new TextBox { Multiline = true, ReadOnly = true, Dock = DockStyle.Bottom, Height = 100, BackColor = Color.WhiteSmoke };
            chatInput = new TextBox { Dock = DockStyle.Bottom, Height = 30 };
            sendChatBtn = new Button { Text = "发送", Dock = DockStyle.Bottom, Height = 30 };
            sendChatBtn.Click += SendChat_Click;

            startMenuPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 240, 240), Visible = true };
            Label titleLabel = new Label { Text = "五子棋", Font = new Font("微软雅黑", 24F, FontStyle.Bold), ForeColor = Color.DarkSlateGray, AutoSize = false, Size = new Size(300, 60), TextAlign = ContentAlignment.MiddleCenter };
            Button localBtn = new Button { Text = "本地对战 (双人)", Size = new Size(200, 50), Font = new Font("微软雅黑", 12F) };
            Button aiBtn = new Button { Text = "人机对战", Size = new Size(200, 50), Font = new Font("微软雅黑", 12F) };
            Button hostBtn = new Button { Text = "创建房间 (房主)", Size = new Size(200, 50), Font = new Font("微软雅黑", 12F) };
            Button joinBtn = new Button { Text = "加入房间 (成员)", Size = new Size(200, 50), Font = new Font("微软雅黑", 12F) };

            localBtn.Click += (s, e) => StartGame(GameMode.Local);
            aiBtn.Click += (s, e) => ShowDifficultyMenu();
            hostBtn.Click += (s, e) => StartOnlineGame(true);
            joinBtn.Click += (s, e) => StartOnlineGame(false);

            titleLabel.Location = new Point((startMenuPanel.Width - 300) / 2, 80);
            localBtn.Location = new Point((startMenuPanel.Width - 200) / 2, 200);
            aiBtn.Location = new Point((startMenuPanel.Width - 200) / 2, 270);
            hostBtn.Location = new Point((startMenuPanel.Width - 200) / 2, 340);
            joinBtn.Location = new Point((startMenuPanel.Width - 200) / 2, 410);
            startMenuPanel.Controls.Add(titleLabel);
            startMenuPanel.Controls.Add(localBtn);
            startMenuPanel.Controls.Add(aiBtn);
            startMenuPanel.Controls.Add(hostBtn);
            startMenuPanel.Controls.Add(joinBtn);
            startMenuPanel.Resize += (s, e) =>
            {
                titleLabel.Location = new Point((startMenuPanel.Width - 300) / 2, 80);
                localBtn.Location = new Point((startMenuPanel.Width - 200) / 2, 200);
                aiBtn.Location = new Point((startMenuPanel.Width - 200) / 2, 270);
                hostBtn.Location = new Point((startMenuPanel.Width - 200) / 2, 340);
                joinBtn.Location = new Point((startMenuPanel.Width - 200) / 2, 410);
            };

            this.Controls.Add(chatHistory);
            this.Controls.Add(chatInput);
            this.Controls.Add(sendChatBtn);
            this.Controls.Add(chessPanel);
            this.Controls.Add(startMenuPanel);
            this.Controls.Add(menuPanel);
        }

        // --------------------- 计分逻辑 ---------------------
        private void UpdateScoreDisplay()
        {
            blackScoreLabel.Text = $"黑方胜场: {blackScore}";
            whiteScoreLabel.Text = $"白方胜场: {whiteScore}";
        }
        private void AddWinToPlayer(int winner)
        {
            if (winner == 1) blackScore++;
            else if (winner == 2) whiteScore++;
            UpdateScoreDisplay();
        }

        // --------------------- 游戏模式切换 ---------------------
        private void ShowStartMenu()
        {
            startMenuPanel.Visible = true;
            chessPanel.Visible = false;
            statusLabel.Text = "";
            currentMode = GameMode.None;
            networkManager?.Disconnect();
            isOnlineGame = false;
            playerListCombo.Visible = false;
            kickButton.Visible = false;
            swapButton.Visible = false;
        }

        private void ShowDifficultyMenu()
        {
            var dialog = new Form { Text = "选择难度", Size = new Size(260, 150), FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent, MaximizeBox = false, MinimizeBox = false };
            Button simpleBtn = new Button { Text = "简单模式", Location = new Point(20, 20), Size = new Size(100, 40) };
            Button hardBtn = new Button { Text = "困难模式", Location = new Point(130, 20), Size = new Size(100, 40) };
            simpleBtn.Click += (s, e) => { dialog.Close(); StartGame(GameMode.AISimple); };
            hardBtn.Click += (s, e) => { dialog.Close(); StartGame(GameMode.AIHard); };
            dialog.Controls.Add(simpleBtn);
            dialog.Controls.Add(hardBtn);
            dialog.ShowDialog(this);
        }

        private void StartGame(GameMode mode)
        {
            currentMode = mode;
            startMenuPanel.Visible = false;
            chessPanel.Visible = true;
            InitializeGame();
            if (mode == GameMode.Local)
                statusLabel.Text = "当前玩家：黑方 ●";
            else
                statusLabel.Text = "当前玩家：黑方 (您) ●";
            isAITurn = false;
            isOnlineGame = false;
        }

        private void StartOnlineGame(bool asHost)
        {
            string playerName = Microsoft.VisualBasic.Interaction.InputBox("请输入您的昵称", "玩家名称", "玩家" + new Random().Next(1000));
            if (string.IsNullOrWhiteSpace(playerName)) playerName = "玩家" + new Random().Next(1000);
            myPlayerName = playerName;
            if (asHost)
            {
                string portStr = Microsoft.VisualBasic.Interaction.InputBox("请输入端口号 (默认 8888)", "创建房间", "8888");
                if (!int.TryParse(portStr, out int port)) port = 8888;
                networkManager = new NetworkManager();
                networkManager.OnMessageReceived += OnNetworkMessage;
                networkManager.OnStatusChanged += (msg) => statusLabel.Text = msg;
                if (networkManager.StartHost(port, myPlayerName))
                {
                    currentMode = GameMode.OnlineHost;
                    isOnlineGame = true;
                    startMenuPanel.Visible = false;
                    chessPanel.Visible = true;
                    InitializeGame();
                    statusLabel.Text = $"房主模式，等待玩家连接... 您的IP: {NetworkManager.GetLocalIP()}:{port}";
                    isMyTurn = false;
                    gameOver = true;
                    myPlayerId = networkManager.LocalPlayerId;
                }
            }
            else
            {
                string address = Microsoft.VisualBasic.Interaction.InputBox("请输入 IP:端口 (例如 192.168.1.100:8888)", "加入房间", "");
                string[] parts = address.Split(':');
                if (parts.Length != 2 || !int.TryParse(parts[1], out int port))
                {
                    MessageBox.Show("格式错误，应为 IP:端口");
                    return;
                }
                networkManager = new NetworkManager();
                networkManager.OnMessageReceived += OnNetworkMessage;
                networkManager.OnStatusChanged += (msg) => statusLabel.Text = msg;
                if (networkManager.ConnectToHost(parts[0], port, myPlayerName))
                {
                    currentMode = GameMode.OnlineClient;
                    isOnlineGame = true;
                    startMenuPanel.Visible = false;
                    chessPanel.Visible = true;
                    InitializeGame();
                    statusLabel.Text = "已连接，等待房主开始游戏...";
                    isMyTurn = false;
                    gameOver = true;
                    myPlayerId = networkManager.LocalPlayerId;
                }
            }
        }

        // --------------------- 游戏基础逻辑 ---------------------
        private void InitializeGame()
        {
            board = new int[BoardSize, BoardSize];
            currentPlayer = 1;
            gameOver = false;
            lastRow = -1; lastCol = -1; lastPlayer = -1;
            chessPanel.Invalidate();
        }

        private bool CheckWin(int row, int col, int player)
        {
            int[,] dirs = { { 0, 1 }, { 1, 0 }, { 1, 1 }, { 1, -1 } };
            for (int d = 0; d < 4; d++)
            {
                int count = 1;
                int dx = dirs[d, 0], dy = dirs[d, 1];
                for (int step = 1; step <= 4; step++)
                {
                    int nr = row + step * dx, nc = col + step * dy;
                    if (nr < 0 || nr >= BoardSize || nc < 0 || nc >= BoardSize) break;
                    if (board[nr, nc] == player) count++; else break;
                }
                for (int step = 1; step <= 4; step++)
                {
                    int nr = row - step * dx, nc = col - step * dy;
                    if (nr < 0 || nr >= BoardSize || nc < 0 || nc >= BoardSize) break;
                    if (board[nr, nc] == player) count++; else break;
                }
                if (count >= 5) return true;
            }
            return false;
        }

        private bool IsBoardFull()
        {
            for (int i = 0; i < BoardSize; i++)
                for (int j = 0; j < BoardSize; j++)
                    if (board[i, j] == 0) return false;
            return true;
        }

        private void SwitchPlayer()
        {
            currentPlayer = (currentPlayer == 1) ? 2 : 1;
            if (!isOnlineGame)
            {
                statusLabel.Text = (currentPlayer == 1) ? "当前玩家：黑方 ●" : "当前玩家：白方 ○";
                if (currentMode != GameMode.Local && currentPlayer == 2 && !gameOver)
                {
                    isAITurn = true;
                    Timer timer = new Timer { Interval = 100 };
                    timer.Tick += (s, e) => { timer.Stop(); AITakeTurn(); };
                    timer.Start();
                }
            }
            else
            {
                isMyTurn = !isMyTurn;
                statusLabel.Text = isMyTurn ? "你的回合" : "等待对方落子";
            }
        }

        // --------------------- AI 逻辑 ---------------------
        private void AITakeTurn()
        {
            if (gameOver || currentPlayer != 2) return;
            if (currentMode == GameMode.AISimple)
                AISimpleMove();
            else if (currentMode == GameMode.AIHard)
                AIHardMove();
            chessPanel.Invalidate();
            if (CheckWin(lastRow, lastCol, 2))
            {
                gameOver = true;
                AddWinToPlayer(2);
                MessageBox.Show("AI胜利！", "游戏结束");
                statusLabel.Text = "AI胜利！";
                return;
            }
            if (IsBoardFull())
            {
                gameOver = true;
                MessageBox.Show("平局！", "游戏结束");
                statusLabel.Text = "平局";
                return;
            }
            currentPlayer = 1;
            statusLabel.Text = "当前玩家：黑方 (您) ●";
            isAITurn = false;
        }

        private void AISimpleMove()
        {
            int bestScore = -1, bestRow = -1, bestCol = -1;
            for (int i = 0; i < BoardSize; i++)
                for (int j = 0; j < BoardSize; j++)
                    if (board[i, j] == 0)
                    {
                        int score = EvaluatePosition(i, j, 2);
                        if (score > bestScore) { bestScore = score; bestRow = i; bestCol = j; }
                    }
            if (bestRow != -1) { board[bestRow, bestCol] = 2; lastRow = bestRow; lastCol = bestCol; lastPlayer = 2; }
        }

        private void AIHardMove()
        {
            int bestScore = int.MinValue, bestRow = -1, bestCol = -1;
            for (int i = 0; i < BoardSize; i++)
                for (int j = 0; j < BoardSize; j++)
                    if (board[i, j] == 0)
                    {
                        board[i, j] = 2;
                        int score = Minimax(2, int.MinValue, int.MaxValue, false);
                        board[i, j] = 0;
                        if (score > bestScore) { bestScore = score; bestRow = i; bestCol = j; }
                    }
            if (bestRow != -1) { board[bestRow, bestCol] = 2; lastRow = bestRow; lastCol = bestCol; lastPlayer = 2; }
        }

        private int Minimax(int depth, int alpha, int beta, bool isMax)
        {
            if (depth == 0) return EvaluateBoardForPlayer(2) - EvaluateBoardForPlayer(1);
            if (isMax)
            {
                int maxEval = int.MinValue;
                for (int i = 0; i < BoardSize; i++)
                    for (int j = 0; j < BoardSize; j++)
                        if (board[i, j] == 0)
                        {
                            board[i, j] = 2;
                            int eval = Minimax(depth - 1, alpha, beta, false);
                            board[i, j] = 0;
                            maxEval = Math.Max(maxEval, eval);
                            alpha = Math.Max(alpha, eval);
                            if (beta <= alpha) break;
                        }
                return maxEval;
            }
            else
            {
                int minEval = int.MaxValue;
                for (int i = 0; i < BoardSize; i++)
                    for (int j = 0; j < BoardSize; j++)
                        if (board[i, j] == 0)
                        {
                            board[i, j] = 1;
                            int eval = Minimax(depth - 1, alpha, beta, true);
                            board[i, j] = 0;
                            minEval = Math.Min(minEval, eval);
                            beta = Math.Min(beta, eval);
                            if (beta <= alpha) break;
                        }
                return minEval;
            }
        }

        private int EvaluateBoardForPlayer(int player)
        {
            int total = 0;
            for (int i = 0; i < BoardSize; i++)
                for (int j = 0; j < BoardSize; j++)
                    if (board[i, j] == player)
                        total += EvaluatePosition(i, j, player);
            return total;
        }

        private int EvaluatePosition(int row, int col, int player)
        {
            int score = 0;
            int[,] dirs = { { 1, 0 }, { 0, 1 }, { 1, 1 }, { 1, -1 } };
            foreach (var dir in dirs)
            {
                int dx = dir[0], dy = dir[1];
                string pattern = "";
                for (int step = -4; step <= 4; step++)
                {
                    int nr = row + step * dx, nc = col + step * dy;
                    if (nr < 0 || nr >= BoardSize || nc < 0 || nc >= BoardSize)
                        pattern += "x";
                    else if (nr == row && nc == col)
                        pattern += player.ToString();
                    else
                        pattern += board[nr, nc] == 0 ? "0" : board[nr, nc].ToString();
                }
                foreach (var kv in patternScore)
                    if (pattern.Contains(kv.Key))
                        score += kv.Value;
            }
            return score;
        }

        // --------------------- 网络消息处理 ---------------------
        private void OnNetworkMessage(NetworkMessage msg)
        {
            this.Invoke((Action)(() =>
            {
                switch (msg.Type)
                {
                    case MessageType.PlayerListUpdate:
                        currentPlayers = JsonSerializer.Deserialize<List<PlayerInfo>>(msg.Data.ToString());
                        UpdatePlayerListUI();
                        break;
                    case MessageType.Move:
                        var move = JsonSerializer.Deserialize<MoveData>(msg.Data.ToString());
                        if (board[move.Row, move.Col] == 0)
                        {
                            board[move.Row, move.Col] = move.Player;
                            lastRow = move.Row; lastCol = move.Col; lastPlayer = move.Player;
                            chessPanel.Invalidate();
                            if (CheckWin(move.Row, move.Col, move.Player))
                            {
                                gameOver = true;
                                string winnerName = move.Player == 1 ? "黑方胜利！" : "白方胜利！";
                                AddWinToPlayer(move.Player);
                                MessageBox.Show(winnerName, "游戏结束");
                                statusLabel.Text = winnerName;
                                networkManager.SendMessage(new NetworkMessage { Type = MessageType.GameOver, Data = winnerName });
                                return;
                            }
                            isMyTurn = (move.Player == 2 && currentMode == GameMode.OnlineClient) || (move.Player == 1 && currentMode == GameMode.OnlineHost);
                            currentPlayer = (move.Player == 1) ? 2 : 1;
                            statusLabel.Text = isMyTurn ? "你的回合" : "等待对方落子";
                        }
                        break;
                    case MessageType.GameOver:
                        MessageBox.Show(msg.Data.ToString(), "游戏结束");
                        gameOver = true;
                        statusLabel.Text = msg.Data.ToString();
                        break;
                    case MessageType.StartApprove:
                        gameOver = false;
                        InitializeGame();
                        isMyTurn = (currentMode == GameMode.OnlineHost && isMyPlayer);
                        statusLabel.Text = "游戏开始！" + (isMyTurn ? "你的回合" : "等待对方落子");
                        break;
                    case MessageType.UndoRequest:
                        var requester = msg.Data.ToString();
                        DialogResult res = MessageBox.Show($"{requester} 请求悔棋，是否同意？", "悔棋申请", MessageBoxButtons.YesNo);
                        networkManager.SendMessage(new NetworkMessage { Type = res == DialogResult.Yes ? MessageType.UndoApprove : MessageType.UndoReject });
                        break;
                    case MessageType.UndoApprove:
                        if (lastRow != -1)
                        {
                            board[lastRow, lastCol] = 0;
                            currentPlayer = lastPlayer;
                            lastRow = -1; lastCol = -1;
                            chessPanel.Invalidate();
                            isMyTurn = (currentPlayer == 1 && currentMode == GameMode.OnlineHost) || (currentPlayer == 2 && currentMode == GameMode.OnlineClient);
                            statusLabel.Text = "悔棋成功，" + (isMyTurn ? "你的回合" : "等待对方");
                        }
                        break;
                    case MessageType.UndoReject:
                        MessageBox.Show("对方拒绝了悔棋请求");
                        break;
                    case MessageType.ResetGame:
                        InitializeGame();
                        isMyTurn = (currentMode == GameMode.OnlineHost);
                        statusLabel.Text = "游戏已重置，" + (isMyTurn ? "你的回合" : "等待对方");
                        break;
                    case MessageType.Chat:
                        var chat = JsonSerializer.Deserialize<ChatData>(msg.Data.ToString());
                        AppendChatMessage($"{chat.From}: {chat.Content}");
                        break;
                    case MessageType.JoinSuccess:
                        myPlayerId = msg.Data.ToString();
                        statusLabel.Text = "加入成功，等待房主开始游戏";
                        break;
                    case MessageType.SwapRequest:
                        var swapReq = JsonSerializer.Deserialize<SwapRequestData>(msg.Data.ToString());
                        DialogResult swapRes = MessageBox.Show($"{swapReq.RequesterName} 请求与您互换位置（参与位<->观战位），是否同意？", "互换申请", MessageBoxButtons.YesNo);
                        networkManager.SendMessage(new NetworkMessage
                        {
                            Type = swapRes == DialogResult.Yes ? MessageType.SwapApprove : MessageType.SwapReject,
                            Data = swapReq
                        });
                        break;
                    case MessageType.SwapApprove:
                        var approveData = JsonSerializer.Deserialize<SwapRequestData>(msg.Data.ToString());
                        networkManager.SwapRoles(approveData.RequesterId, approveData.TargetId);
                        break;
                    case MessageType.SwapReject:
                        MessageBox.Show("对方拒绝了互换请求");
                        break;
                    case MessageType.KickPlayer:
                        MessageBox.Show("您被房主踢出房间");
                        ShowStartMenu();
                        break;
                }
            }));
        }

        private void UpdatePlayerListUI()
        {
            playerListCombo.Items.Clear();
            foreach (var p in currentPlayers)
            {
                if (p.Id == myPlayerId) continue;
                playerListCombo.Items.Add($"{p.Name} ({(p.IsPlayer ? "参与" : "观战")})");
            }
            bool isHost = currentMode == GameMode.OnlineHost;
            playerListCombo.Visible = isHost && playerListCombo.Items.Count > 0;
            kickButton.Visible = isHost;
            swapButton.Visible = isOnlineGame && !isHost;
        }

        private void KickButton_Click(object sender, EventArgs e)
        {
            if (playerListCombo.SelectedItem == null) return;
            string selected = playerListCombo.SelectedItem.ToString();
            var target = currentPlayers.FirstOrDefault(p => $"{p.Name} ({(p.IsPlayer ? "参与" : "观战")})" == selected);
            if (target != null && networkManager.IsHost)
            {
                networkManager.KickPlayer(target.Id);
            }
        }

        private void SwapButton_Click(object sender, EventArgs e)
        {
            if (playerListCombo.SelectedItem == null) return;
            string selected = playerListCombo.SelectedItem.ToString();
            var target = currentPlayers.FirstOrDefault(p => $"{p.Name} ({(p.IsPlayer ? "参与" : "观战")})" == selected);
            if (target != null)
            {
                networkManager.SendMessage(new NetworkMessage
                {
                    Type = MessageType.SwapRequest,
                    Data = new SwapRequestData
                    {
                        RequesterId = myPlayerId,
                        RequesterName = myPlayerName,
                        TargetId = target.Id,
                        TargetName = target.Name
                    }
                });
            }
        }

        private void RequestUndo_Click(object sender, EventArgs e)
        {
            if (isOnlineGame && !gameOver && isMyTurn)
            {
                networkManager.SendMessage(new NetworkMessage { Type = MessageType.UndoRequest, Data = myPlayerName });
                statusLabel.Text = "已发送悔棋申请，等待房主同意...";
            }
        }

        private void RequestStart_Click(object sender, EventArgs e)
        {
            if (isOnlineGame && gameOver)
            {
                int playersCount = currentPlayers.Count(p => p.IsPlayer);
                if (playersCount != 2)
                {
                    MessageBox.Show("需要恰好2名参与位玩家才能开始游戏", "提示");
                    return;
                }
                networkManager.SendMessage(new NetworkMessage { Type = MessageType.StartRequest, Data = myPlayerName });
                statusLabel.Text = "已发送开始申请，等待房主同意...";
            }
        }

        private void RequestReset_Click(object sender, EventArgs e)
        {
            if (isOnlineGame && !gameOver)
            {
                if (MessageBox.Show("重置游戏会清空当前对局，是否继续？", "确认", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    networkManager.SendMessage(new NetworkMessage { Type = MessageType.ResetGame });
            }
        }

        private void SendChat_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(chatInput.Text) && networkManager != null)
            {
                var chatMsg = new ChatData { From = myPlayerName, Content = chatInput.Text };
                networkManager.SendMessage(new NetworkMessage { Type = MessageType.Chat, Data = chatMsg });
                AppendChatMessage($"我: {chatInput.Text}");
                chatInput.Clear();
            }
        }

        private void AppendChatMessage(string msg)
        {
            if (chatHistory.InvokeRequired)
                chatHistory.Invoke((Action)(() => chatHistory.AppendText(msg + Environment.NewLine)));
            else
                chatHistory.AppendText(msg + Environment.NewLine);
        }

        // --------------------- 绘制与落子 ---------------------
        private void RecalculateChessPanelBounds()
        {
            if (chessPanel == null || menuPanel == null) return;
            int menuHeight = menuPanel.Height;
            int clientWidth = this.ClientSize.Width;
            int clientHeight = this.ClientSize.Height;
            int availableHeight = clientHeight - menuHeight - chatHistory.Height - chatInput.Height - sendChatBtn.Height - 10;
            int side = Math.Min(clientWidth, availableHeight);
            int left = (clientWidth - side) / 2;
            int top = menuHeight + (availableHeight - side) / 2;
            chessPanel.SetBounds(left, top, side, side);
        }

        private double GetCellSizeDouble()
        {
            int side = chessPanel.Width;
            int available = side - 2 * BoardPadding;
            return (double)available / (BoardSize - 1);
        }
        private int GetCellSize() => (int)Math.Floor(GetCellSizeDouble());
        private int GetBoardStart()
        {
            double cell = GetCellSizeDouble();
            double totalWidth = cell * (BoardSize - 1);
            double side = chessPanel.Width;
            double start = (side - totalWidth) / 2;
            return (int)Math.Round(start);
        }

        private class DoubleBufferedPanel : Panel
        {
            public DoubleBufferedPanel()
            {
                DoubleBuffered = true;
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
                ResizeRedraw = true;
            }
        }

        private void ChessPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            double cellDouble = GetCellSizeDouble();
            int cellInt = (int)Math.Floor(cellDouble);
            if (cellInt <= 0) return;
            int start = GetBoardStart();
            double endDouble = start + cellDouble * (BoardSize - 1);
            int end = (int)Math.Round(endDouble);
            Pen linePen = new Pen(Color.Black, 1.5f);
            for (int i = 0; i < BoardSize; i++)
            {
                double posDouble = start + i * cellDouble;
                int pos = (int)Math.Round(posDouble);
                g.DrawLine(linePen, start, pos, end, pos);
                g.DrawLine(linePen, pos, start, pos, end);
            }
            Brush starBrush = Brushes.Black;
            int starRadius = Math.Max(3, cellInt / 6);
            // 修正：使用 int[][] 并遍历
            int[][] starPoints = new int[][]
            {
                new int[] { 3, 3 }, new int[] { 3, 11 },
                new int[] { 11, 3 }, new int[] { 11, 11 },
                new int[] { 7, 7 }
            };
            foreach (int[] p in starPoints)
            {
                double xDouble = start + p[1] * cellDouble;
                double yDouble = start + p[0] * cellDouble;
                int x = (int)Math.Round(xDouble);
                int y = (int)Math.Round(yDouble);
                g.FillEllipse(starBrush, x - starRadius, y - starRadius, starRadius * 2, starRadius * 2);
            }
            for (int row = 0; row < BoardSize; row++)
                for (int col = 0; col < BoardSize; col++)
                    if (board[row, col] != 0)
                    {
                        Brush brush = (board[row, col] == 1) ? Brushes.Black : Brushes.White;
                        double xDouble = start + col * cellDouble;
                        double yDouble = start + row * cellDouble;
                        int x = (int)Math.Round(xDouble);
                        int y = (int)Math.Round(yDouble);
                        int radius = cellInt / 2 - 2;
                        g.FillEllipse(brush, x - radius, y - radius, radius * 2, radius * 2);
                        if (board[row, col] == 2)
                            g.DrawEllipse(Pens.Black, x - radius, y - radius, radius * 2, radius * 2);
                    }
        }

        private void ChessPanel_MouseClick(object sender, MouseEventArgs e)
        {
            if (gameOver) return;
            if (isOnlineGame)
            {
                if (!isMyTurn || !isMyPlayer)
                {
                    if (!isMyTurn) MessageBox.Show("等待对方落子");
                    else if (!isMyPlayer) MessageBox.Show("您是观战位，不能落子");
                    return;
                }
                double cell = GetCellSizeDouble();
                if (cell <= 0) return;
                int start = GetBoardStart();
                int col = (int)Math.Round((e.X - start) / cell);
                int row = (int)Math.Round((e.Y - start) / cell);
                if (row < 0 || row >= BoardSize || col < 0 || col >= BoardSize) return;
                if (board[row, col] != 0) return;
                networkManager.SendMessage(new NetworkMessage
                {
                    Type = MessageType.Move,
                    Data = new MoveData { Row = row, Col = col, Player = currentPlayer }
                });
                board[row, col] = currentPlayer;
                lastRow = row; lastCol = col; lastPlayer = currentPlayer;
                chessPanel.Invalidate();
                if (CheckWin(row, col, currentPlayer))
                {
                    gameOver = true;
                    string winnerName = (currentPlayer == 1) ? "黑方胜利！" : "白方胜利！";
                    AddWinToPlayer(currentPlayer);
                    MessageBox.Show(winnerName, "游戏结束");
                    statusLabel.Text = winnerName;
                    networkManager.SendMessage(new NetworkMessage { Type = MessageType.GameOver, Data = winnerName });
                    return;
                }
                isMyTurn = false;
                currentPlayer = (currentPlayer == 1) ? 2 : 1;
                statusLabel.Text = "等待对方落子";
            }
            else
            {
                if ((currentMode == GameMode.AISimple || currentMode == GameMode.AIHard) && currentPlayer != 1) return;
                double cell = GetCellSizeDouble();
                if (cell <= 0) return;
                int start = GetBoardStart();
                int col = (int)Math.Round((e.X - start) / cell);
                int row = (int)Math.Round((e.Y - start) / cell);
                if (row < 0 || row >= BoardSize || col < 0 || col >= BoardSize) return;
                if (board[row, col] != 0) return;
                board[row, col] = currentPlayer;
                lastRow = row; lastCol = col; lastPlayer = currentPlayer;
                chessPanel.Invalidate();
                if (CheckWin(row, col, currentPlayer))
                {
                    gameOver = true;
                    AddWinToPlayer(currentPlayer);
                    MessageBox.Show((currentPlayer == 1 ? "黑方胜利！" : "白方胜利！"), "游戏结束");
                    statusLabel.Text = (currentPlayer == 1 ? "黑方胜利！" : "白方胜利！");
                    return;
                }
                if (IsBoardFull())
                {
                    gameOver = true;
                    MessageBox.Show("平局！", "游戏结束");
                    statusLabel.Text = "平局";
                    return;
                }
                SwitchPlayer();
            }
        }

        private void NewGameButton_Click(object sender, EventArgs e)
        {
            if (currentMode == GameMode.None)
                ShowStartMenu();
            else
            {
                InitializeGame();
                if (currentMode != GameMode.Local)
                {
                    statusLabel.Text = "当前玩家：黑方 (您) ●";
                    isAITurn = false;
                }
                else
                    statusLabel.Text = "当前玩家：黑方 ●";
                if (isOnlineGame)
                {
                    isMyTurn = (currentMode == GameMode.OnlineHost && isMyPlayer);
                    statusLabel.Text = isMyTurn ? "你的回合" : "等待对方落子";
                }
            }
        }

        private void UndoButton_Click(object sender, EventArgs e)
        {
            if (gameOver || currentMode == GameMode.None) return;
            if (isOnlineGame)
            {
                if (!isMyTurn) { MessageBox.Show("轮到对方，不能悔棋"); return; }
                RequestUndo_Click(sender, e);
                return;
            }
            if ((currentMode == GameMode.AISimple || currentMode == GameMode.AIHard) && currentPlayer != 1)
            {
                MessageBox.Show("AI思考中，请稍后悔棋", "提示");
                return;
            }
            if (lastRow == -1) return;
            board[lastRow, lastCol] = 0;
            currentPlayer = lastPlayer;
            lastRow = -1; lastCol = -1; lastPlayer = -1;
            gameOver = false;
            if (currentMode != GameMode.Local)
                statusLabel.Text = "当前玩家：黑方 (您) ●";
            else
                statusLabel.Text = "当前玩家：黑方 ●";
            chessPanel.Invalidate();
            isAITurn = false;
        }
    }

    public class MoveData { public int Row { get; set; } public int Col { get; set; } public int Player { get; set; } }
    public class ChatData { public string From { get; set; } public string Content { get; set; } }
}