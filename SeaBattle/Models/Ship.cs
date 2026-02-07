using System.Collections.Generic;
using System.Drawing;

namespace SeaBattle.Models
{
    public class Ship
    {
        public List<Point> Decks { get; set; }
        public bool IsHorizontal { get; set; }

        public Ship()
        {
            Decks = new List<Point>();
        }

        public Ship(List<Point> decks, bool isHorizontal)
        {
            Decks = decks;
            IsHorizontal = isHorizontal;
        }
    }
}