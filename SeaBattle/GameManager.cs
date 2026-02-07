using System;
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

        public event Action<GameState> GameStateChanged;
        public event Action<string> GameMessageReceived;

        public GameManager()
        {
            PlayerBoard = new GameBoard();
            EnemyBoard = new GameBoard();
            CurrentState = GameState.Placement;
            NetworkManager = new NetworkManager(this);
        }

        public void ChangeState(GameState newState)
        {
            CurrentState = newState;
            GameStateChanged?.Invoke(newState);
        }

        public void StartGame()
        {
            ChangeState(GameState.MyTurn);
            GameMessageReceived?.Invoke("Игра началась! Ваш ход.");
        }

        public void ProcessShot(int x, int y, bool isPlayerShooting = true)
        {
            if (CurrentState == GameState.GameOver)
                return;

            if (isPlayerShooting && CurrentState != GameState.MyTurn)
            {
                GameMessageReceived?.Invoke("Сейчас не ваш ход!");
                return;
            }

            var targetBoard = isPlayerShooting ? EnemyBoard : PlayerBoard;
            var result = targetBoard.Shoot(x, y);

            // Отправка результата по сети
            if (NetworkManager.IsConnected)
            {
                NetworkManager.SendShotResult(x, y, result);
            }

            // Проверка конца игры
            if (targetBoard.AllShipsSunk())
            {
                ChangeState(GameState.GameOver);
                string winner = isPlayerShooting ? "Вы победили!" : "Вы проиграли!";
                GameMessageReceived?.Invoke(winner);
                return;
            }

            // Смена хода
            if (result == CellState.Miss)
            {
                ChangeState(isPlayerShooting ? GameState.EnemyTurn : GameState.MyTurn);
                GameMessageReceived?.Invoke(isPlayerShooting ? "Промах! Ход противника." : "Ваш ход.");
            }
            else
            {
                GameMessageReceived?.Invoke("Попадание! Стреляйте еще.");
            }
        }

        public void AutoPlaceShips()
        {
            PlayerBoard.AutoPlaceShips();
            GameMessageReceived?.Invoke("Корабли расставлены автоматически.");
        }
    }
}