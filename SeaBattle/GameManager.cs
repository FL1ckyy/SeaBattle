using System;
using System.Collections.Generic;
using SeaBattle.Enums;
using SeaBattle.Models;
using SeaBattle.Network;

namespace SeaBattle
{
    public class GameManager
    {
        public GameState CurrentState { get; private set; }
        public GameBoard PlayerBoard { get; private set; }
        public GameBoard EnemyBoard { get; private set; }
        public NetworkManager NetworkManager { get; private set; }
        public bool IsMyTurn { get; private set; }

        private int enemyShipCellsRemaining = 20; // Всего палуб кораблей: 1*4 + 2*3 + 3*2 + 4*1 = 20
        private int playerShipCellsRemaining = 20;

        public event Action<GameState> GameStateChanged;
        public event Action<string> GameMessageReceived;
        public event Action BoardUpdated;

        public GameManager()
        {
            PlayerBoard = new GameBoard();
            EnemyBoard = new GameBoard();
            CurrentState = GameState.Placement;
            NetworkManager = new NetworkManager(this);
            IsMyTurn = false;

            NetworkManager.MessageReceived += OnNetworkMessage;
            NetworkManager.StatusChanged += OnNetworkStatus;
        }

        private void OnNetworkMessage(string message)
        {
            GameMessageReceived?.Invoke(message);
        }

        private void OnNetworkStatus(string status)
        {
            GameMessageReceived?.Invoke(status);
        }

        public void ChangeState(GameState newState)
        {
            CurrentState = newState;
            GameStateChanged?.Invoke(newState);

            if (newState == GameState.MyTurn)
            {
                IsMyTurn = true;
            }
            else if (newState == GameState.EnemyTurn)
            {
                IsMyTurn = false;
            }
        }

        public void StartGameAsHost()
        {
            NetworkManager.SendMessage(MessageType.StartGame, null);
            ChangeState(GameState.MyTurn);
            GameMessageReceived?.Invoke("Игра началась! Ваш ход.");
        }

        public void StartGameAsClient()
        {
            ChangeState(GameState.EnemyTurn);
            GameMessageReceived?.Invoke("Игра началась! Ход противника.");
        }

        public void ProcessShot(int x, int y)
        {
            if (CurrentState != GameState.MyTurn)
            {
                GameMessageReceived?.Invoke("Сейчас не ваш ход!");
                return;
            }

            if (x < 0 || x >= 10 || y < 0 || y >= 10)
                return;

            var cell = EnemyBoard.Cells[x, y];
            if (cell.State == CellState.Miss || cell.State == CellState.Hit || cell.State == CellState.Sunk)
            {
                GameMessageReceived?.Invoke("Сюда уже стреляли!");
                return;
            }

            NetworkManager.SendShot(x, y);
            ChangeState(GameState.EnemyTurn);
            GameMessageReceived?.Invoke("Выстрел отправлен. Ожидайте ответа...");
        }

        public void ProcessIncomingShot(int x, int y)
        {
            var cell = PlayerBoard.Cells[x, y];
            var oldState = cell.State;

            var result = PlayerBoard.Shoot(x, y);
            string resultStr = result.ToString();

            NetworkManager.SendShotResult(x, y, resultStr);

            if (oldState == CellState.Ship)
            {
                playerShipCellsRemaining--;

                if (playerShipCellsRemaining <= 0)
                {
                    ChangeState(GameState.GameOver);
                    GameMessageReceived?.Invoke("Вы проиграли!");
                    NetworkManager.SendMessage(MessageType.GameOver, null);
                    return;
                }
            }

            if (result == CellState.Miss)
            {
                ChangeState(GameState.MyTurn);
                GameMessageReceived?.Invoke("Противник промахнулся! Ваш ход.");
            }
            else
            {
                ChangeState(GameState.EnemyTurn);
                GameMessageReceived?.Invoke("Противник попал! Его следующий ход.");
            }

            BoardUpdated?.Invoke();
        }

        public void ProcessShotResult(int x, int y, string result)
        {
            if (result == "Hit")
            {
                EnemyBoard.Cells[x, y].State = CellState.Hit;
                enemyShipCellsRemaining--;
            }
            else if (result == "Miss")
            {
                EnemyBoard.Cells[x, y].State = CellState.Miss;
            }
            else if (result == "Sunk")
            {
                EnemyBoard.Cells[x, y].State = CellState.Sunk;
                enemyShipCellsRemaining--;

                MarkAroundDestroyedShip(x, y);
            }

            if (result == "Hit" || result == "Sunk")
            {
                ChangeState(GameState.MyTurn);
                GameMessageReceived?.Invoke("Попадание! Стреляйте еще.");
            }
            else
            {
                ChangeState(GameState.EnemyTurn);
                GameMessageReceived?.Invoke("Промах! Ход противника.");
            }

            if (enemyShipCellsRemaining <= 0)
            {
                ChangeState(GameState.GameOver);
                GameMessageReceived?.Invoke("Вы победили! Все корабли противника уничтожены!");
            }

            BoardUpdated?.Invoke();
        }

        private void MarkAroundDestroyedShip(int x, int y)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int nx = x + dx;
                    int ny = y + dy;

                    if (nx >= 0 && nx < 10 && ny >= 0 && ny < 10)
                    {
                        if (EnemyBoard.Cells[nx, ny].State == CellState.Empty)
                        {
                            EnemyBoard.Cells[nx, ny].State = CellState.Miss;
                        }
                    }
                }
            }
        }

        public void AutoPlaceShips()
        {
            PlayerBoard.AutoPlaceShips();
            enemyShipCellsRemaining = 20;
            playerShipCellsRemaining = 20;
            GameMessageReceived?.Invoke("Корабли расставлены автоматически.");

            if (NetworkManager.IsConnected && NetworkManager.IsHost)
            {
                StartGameAsHost();
            }
            else if (NetworkManager.IsConnected && !NetworkManager.IsHost)
            {
                GameMessageReceived?.Invoke("Корабли расставлены. Ожидайте начала игры...");
            }
        }

        public async void CreateGame(int port)
        {
            await NetworkManager.StartServer(port);
            GameMessageReceived?.Invoke($"Сервер создан. Ожидание подключения на порту {port}...");
            ChangeState(GameState.WaitingConnection);
        }

        public async void ConnectToGame(string ip, int port)
        {
            await NetworkManager.ConnectToServer(ip, port);
            ChangeState(GameState.WaitingConnection);
        }
    }
}