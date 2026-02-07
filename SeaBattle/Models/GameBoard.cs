using System;
using System.Collections.Generic;
using System.Drawing;
using SeaBattle.Enums;

namespace SeaBattle.Models
{
    public class GameBoard
    {
        public const int BoardSize = 10;
        public Cell[,] Cells { get; private set; }
        public List<Ship> Ships { get; private set; }

        public GameBoard()
        {
            Cells = new Cell[BoardSize, BoardSize];
            Ships = new List<Ship>();
            InitializeBoard();
        }

        private void InitializeBoard()
        {
            for (int x = 0; x < BoardSize; x++)
            {
                for (int y = 0; y < BoardSize; y++)
                {
                    Cells[x, y] = new Cell(x, y);
                }
            }
        }

        public bool PlaceShip(Point start, int size, bool isHorizontal)
        {
            if (isHorizontal)
            {
                if (start.X + size > BoardSize) return false;
            }
            else
            {
                if (start.Y + size > BoardSize) return false;
            }

            for (int i = 0; i < size; i++)
            {
                int x = isHorizontal ? start.X + i : start.X;
                int y = isHorizontal ? start.Y : start.Y + i;

                if (!CheckCellAndNeighbors(x, y))
                    return false;
            }

            var shipDecks = new List<Point>();
            for (int i = 0; i < size; i++)
            {
                int x = isHorizontal ? start.X + i : start.X;
                int y = isHorizontal ? start.Y : start.Y + i;

                Cells[x, y].State = CellState.Ship;
                shipDecks.Add(new Point(x, y));
            }

            Ships.Add(new Ship(shipDecks, isHorizontal));
            return true;
        }

        private bool CheckCellAndNeighbors(int x, int y)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int nx = x + dx;
                    int ny = y + dy;

                    if (nx >= 0 && nx < BoardSize && ny >= 0 && ny < BoardSize)
                    {
                        if (Cells[nx, ny].State == CellState.Ship)
                            return false;
                    }
                }
            }
            return true;
        }

        public CellState Shoot(int x, int y)
        {
            if (x < 0 || x >= BoardSize || y < 0 || y >= BoardSize)
                throw new ArgumentOutOfRangeException();

            var cell = Cells[x, y];

            switch (cell.State)
            {
                case CellState.Empty:
                    cell.State = CellState.Miss;
                    return CellState.Miss;

                case CellState.Ship:
                    cell.State = CellState.Hit;
                    CheckShipDestroyed(x, y);
                    return CellState.Hit;

                default:
                    return cell.State;
            }
        }

        private void CheckShipDestroyed(int x, int y)
        {
            foreach (var ship in Ships)
            {
                if (ship.Decks.Contains(new Point(x, y)))
                {
                    bool allHit = true;
                    foreach (var deck in ship.Decks)
                    {
                        if (Cells[deck.X, deck.Y].State != CellState.Hit)
                        {
                            allHit = false;
                            break;
                        }
                    }

                    if (allHit)
                    {
                        foreach (var deck in ship.Decks)
                        {
                            Cells[deck.X, deck.Y].State = CellState.Sunk;
                        }
                        MarkAroundShip(ship);
                    }
                    break;
                }
            }
        }

        private void MarkAroundShip(Ship ship)
        {
            foreach (var deck in ship.Decks)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int x = deck.X + dx;
                        int y = deck.Y + dy;

                        if (x >= 0 && x < BoardSize && y >= 0 && y < BoardSize)
                        {
                            if (Cells[x, y].State == CellState.Empty)
                            {
                                Cells[x, y].State = CellState.Miss;
                            }
                        }
                    }
                }
            }
        }

        public bool AllShipsSunk()
        {
         
            if (Ships == null || Ships.Count == 0)
                return false;

            foreach (var ship in Ships)
            {
                bool shipDestroyed = true;
                foreach (var deck in ship.Decks)
                {
                    if (Cells[deck.X, deck.Y].State != CellState.Sunk)
                    {
                        shipDestroyed = false;
                        break;
                    }
                }
                if (!shipDestroyed)
                    return false;
            }
            return true;
        }

        public void AutoPlaceShips()
        {
            ClearBoard();
            int[] shipSizes = { 4, 3, 3, 2, 2, 2, 1, 1, 1, 1 };
            Random rand = new Random();

            foreach (int size in shipSizes)
            {
                bool placed = false;
                int attempts = 0;

                while (!placed && attempts < 100)
                {
                    int x = rand.Next(0, BoardSize);
                    int y = rand.Next(0, BoardSize);
                    bool isHorizontal = rand.Next(0, 2) == 0;

                    placed = PlaceShip(new Point(x, y), size, isHorizontal);
                    attempts++;
                }
            }
        }

        public void ClearBoard()
        {
            InitializeBoard();
            Ships.Clear();
        }
    }
}