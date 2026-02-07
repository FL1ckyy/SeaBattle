using System;
using System.Drawing;
using System.Windows.Forms;
using SeaBattle.Enums;
using SeaBattle.Models;

namespace SeaBattle.Views
{
    public class BoardControl : Control
    {
        private const int CellSize = 25;
        private GameBoard board;
        private bool isInteractive;

        public GameBoard Board
        {
            get => board;
            set
            {
                board = value;
                Invalidate();
            }
        }

        public bool IsInteractive
        {
            get => isInteractive;
            set
            {
                isInteractive = value;
                Cursor = value ? Cursors.Hand : Cursors.Default;
            }
        }

        public event EventHandler<CellClickEventArgs> CellClicked;

        public BoardControl()
        {
            DoubleBuffered = true;
            Size = new Size(CellSize * 10 + 2, CellSize * 10 + 2);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (board == null) return;

            var g = e.Graphics;
            g.Clear(Color.White);

            for (int i = 0; i <= 10; i++)
            {
                g.DrawLine(Pens.Black, i * CellSize, 0, i * CellSize, Height);
                g.DrawLine(Pens.Black, 0, i * CellSize, Width, i * CellSize);
            }

            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    var cell = board.Cells[x, y];
                    var rect = new Rectangle(x * CellSize + 1, y * CellSize + 1, CellSize - 2, CellSize - 2);

                    switch (cell.State)
                    {
                        case CellState.Ship:
                            g.FillRectangle(Brushes.Gray, rect);
                            break;

                        case CellState.Miss:
                            g.FillEllipse(Brushes.Blue, rect);
                            break;

                        case CellState.Hit:
                            g.FillEllipse(Brushes.Red, rect);
                            break;

                        case CellState.Sunk:
                            g.FillRectangle(Brushes.DarkRed, rect);
                            break;
                    }
                }
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            if (!IsInteractive || board == null) return;

            int x = e.X / CellSize;
            int y = e.Y / CellSize;

            if (x >= 0 && x < 10 && y >= 0 && y < 10)
            {
                CellClicked?.Invoke(this, new CellClickEventArgs(x, y));
            }
        }
    }

    public class CellClickEventArgs : EventArgs
    {
        public int X { get; }
        public int Y { get; }

        public CellClickEventArgs(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}