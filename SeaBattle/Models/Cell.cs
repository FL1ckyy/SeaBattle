using SeaBattle.Enums;

namespace SeaBattle.Models
{
    public class Cell
    {
        public int X { get; set; }
        public int Y { get; set; }
        public CellState State { get; set; }

        public Cell() { }

        public Cell(int x, int y)
        {
            X = x;
            Y = y;
            State = CellState.Empty;
        }
    }
}