using System;
using System.Drawing;
using System.Windows.Forms;
using SeaBattle.Enums;
using SeaBattle.Views;

namespace SeaBattle
{
    public partial class MainForm : Form
    {
        private GameManager gameManager;
        private BoardControl playerBoard;
        private BoardControl enemyBoard;
        private Button btnHost;
        private Button btnJoin;
        private Button btnAutoPlace;
        private TextBox txtIp;
        private TextBox txtPort;
        private Label lblStatus;
        private Label lblMessage;

        public MainForm()
        {
            InitializeComponent();
            InitializeGame();
        }

        private void InitializeComponent()
        {
            this.Text = "Морской бой";
            this.Size = new Size(850, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(850, 600);

            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            var controlPanel = new Panel
            {
                Height = 120,
                Dock = DockStyle.Top,
                BackColor = Color.LightGray,
                Padding = new Padding(5)
            };

            txtIp = new TextBox
            {
                Location = new Point(10, 15),
                Size = new Size(120, 23),
                Text = "127.0.0.1"
            };

            txtPort = new TextBox
            {
                Location = new Point(140, 15),
                Size = new Size(60, 23),
                Text = "8888"
            };

            btnHost = new Button
            {
                Location = new Point(210, 15),
                Size = new Size(100, 23),
                Text = "Создать игру",
                BackColor = Color.LightGreen
            };
            btnHost.Click += BtnHost_Click;

            btnJoin = new Button
            {
                Location = new Point(320, 15),
                Size = new Size(100, 23),
                Text = "Подключиться",
                BackColor = Color.LightSkyBlue
            };
            btnJoin.Click += BtnJoin_Click;

            btnAutoPlace = new Button
            {
                Location = new Point(430, 15),
                Size = new Size(130, 23),
                Text = "Авторасстановка",
                BackColor = Color.LightYellow,
                Enabled = false
            };
            btnAutoPlace.Click += BtnAutoPlace_Click;

            lblStatus = new Label
            {
                Location = new Point(10, 50),
                Size = new Size(400, 25),
                Text = "Статус: Расстановка кораблей",
                Font = new Font("Arial", 9, FontStyle.Bold)
            };

            lblMessage = new Label
            {
                Location = new Point(10, 80),
                Size = new Size(600, 25),
                Text = "",
                Font = new Font("Arial", 8)
            };

            controlPanel.Controls.AddRange(new Control[]
            {
                txtIp, txtPort, btnHost, btnJoin,
                btnAutoPlace, lblStatus, lblMessage
            });

            var boardsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(5)
            };

            var playerPanel = new Panel
            {
                Location = new Point(20, 40),
                Size = new Size(320, 350),
                BorderStyle = BorderStyle.FixedSingle
            };

            var enemyPanel = new Panel
            {
                Location = new Point(380, 40),
                Size = new Size(320, 350),
                BorderStyle = BorderStyle.FixedSingle
            };

            playerBoard = new BoardControl
            {
                Location = new Point(10, 30),
                IsInteractive = false
            };

            enemyBoard = new BoardControl
            {
                Location = new Point(10, 30),
                IsInteractive = false
            };

            var lblPlayer = new Label
            {
                Text = "ВАШЕ ПОЛЕ",
                Location = new Point(10, 5),
                Size = new Size(150, 20),
                Font = new Font("Arial", 10, FontStyle.Bold),
                ForeColor = Color.DarkBlue
            };

            var lblEnemy = new Label
            {
                Text = "ПОЛЕ ПРОТИВНИКА",
                Location = new Point(10, 5),
                Size = new Size(150, 20),
                Font = new Font("Arial", 10, FontStyle.Bold),
                ForeColor = Color.DarkRed
            };

            var legendPanel = new Panel
            {
                Location = new Point(20, 400),
                Size = new Size(680, 80),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.WhiteSmoke
            };

            var legendTitle = new Label
            {
                Text = "Легенда:",
                Location = new Point(10, 10),
                Size = new Size(70, 20),
                Font = new Font("Arial", 9, FontStyle.Bold)
            };

            var shipLegend = new Label
            {
                Text = "Корабль - Серый квадрат",
                Location = new Point(90, 10),
                Size = new Size(150, 20)
            };

            var missLegend = new Label
            {
                Text = "Промах - Синий круг",
                Location = new Point(250, 10),
                Size = new Size(150, 20)
            };

            var hitLegend = new Label
            {
                Text = "Попадание - Красный круг",
                Location = new Point(410, 10),
                Size = new Size(150, 20)
            };

            var sunkLegend = new Label
            {
                Text = "Потоплен - Красный квадрат",
                Location = new Point(570, 10),
                Size = new Size(150, 20)
            };

            var instruction1 = new Label
            {
                Text = "1. Создайте игру или подключитесь",
                Location = new Point(90, 35),
                Size = new Size(300, 20)
            };

            var instruction2 = new Label
            {
                Text = "2. Нажмите 'Авторасстановка' когда подключитесь",
                Location = new Point(90, 55),
                Size = new Size(300, 20)
            };

            legendPanel.Controls.AddRange(new Control[]
            {
                legendTitle, shipLegend, missLegend, hitLegend, sunkLegend,
                instruction1, instruction2
            });

            playerPanel.Controls.Add(lblPlayer);
            playerPanel.Controls.Add(playerBoard);

            enemyPanel.Controls.Add(lblEnemy);
            enemyPanel.Controls.Add(enemyBoard);

            boardsPanel.Controls.Add(playerPanel);
            boardsPanel.Controls.Add(enemyPanel);
            boardsPanel.Controls.Add(legendPanel);

            mainPanel.Controls.Add(boardsPanel);
            mainPanel.Controls.Add(controlPanel);

            this.Controls.Add(mainPanel);
        }

        private void InitializeGame()
        {
            gameManager = new GameManager();
            gameManager.GameStateChanged += GameManager_GameStateChanged;
            gameManager.GameMessageReceived += GameManager_GameMessageReceived;
            gameManager.BoardUpdated += GameManager_BoardUpdated;
            gameManager.NetworkManager.Connected += NetworkManager_Connected;

            playerBoard.Board = gameManager.PlayerBoard;
            enemyBoard.Board = gameManager.EnemyBoard;

            playerBoard.CellClicked += PlayerBoard_CellClicked;
            enemyBoard.CellClicked += EnemyBoard_CellClicked;
        }

        private void NetworkManager_Connected()
        {
            Invoke(new Action(() =>
            {
                btnAutoPlace.Enabled = true;
                lblMessage.Text = "Соединение установлено! Нажмите 'Авторасстановка'.";
            }));
        }

        private void GameManager_GameStateChanged(GameState state)
        {
            Invoke(new Action(() =>
            {
                lblStatus.Text = $"Статус: {GetStateText(state)}";

                enemyBoard.IsInteractive = (state == GameState.MyTurn);

                if (state == GameState.GameOver)
                {
                    btnAutoPlace.Enabled = false;
                    enemyBoard.IsInteractive = false;
                }
            }));
        }

        private void GameManager_GameMessageReceived(string message)
        {
            Invoke(new Action(() =>
            {
                lblMessage.Text = message;
            }));
        }

        private void GameManager_BoardUpdated()
        {
            Invoke(new Action(() =>
            {
                playerBoard.Invalidate();
                enemyBoard.Invalidate();
            }));
        }

        private string GetStateText(GameState state)
        {
            switch (state)
            {
                case GameState.Placement: return "Расстановка кораблей";
                case GameState.WaitingConnection: return "Ожидание подключения";
                case GameState.MyTurn: return "ВАШ ХОД";
                case GameState.EnemyTurn: return "Ход противника";
                case GameState.GameOver: return "Игра окончена";
                default: return state.ToString();
            }
        }

        private void BtnHost_Click(object sender, EventArgs e)
        {
            if (int.TryParse(txtPort.Text, out int port))
            {
                btnHost.Enabled = false;
                btnJoin.Enabled = false;
                gameManager.CreateGame(port);
            }
            else
            {
                lblMessage.Text = "Ошибка: неверный порт!";
            }
        }

        private void BtnJoin_Click(object sender, EventArgs e)
        {
            if (int.TryParse(txtPort.Text, out int port))
            {
                btnHost.Enabled = false;
                btnJoin.Enabled = false;
                gameManager.ConnectToGame(txtIp.Text, port);
            }
            else
            {
                lblMessage.Text = "Ошибка: неверный порт!";
            }
        }

        private void BtnAutoPlace_Click(object sender, EventArgs e)
        {
            gameManager.AutoPlaceShips();
            playerBoard.Invalidate();
            btnAutoPlace.Enabled = false;
        }

        private void PlayerBoard_CellClicked(object sender, CellClickEventArgs e)
        {
        }

        private void EnemyBoard_CellClicked(object sender, CellClickEventArgs e)
        {
            gameManager.ProcessShot(e.X, e.Y);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            gameManager.NetworkManager.Disconnect();
            base.OnFormClosing(e);
        }
    }
}