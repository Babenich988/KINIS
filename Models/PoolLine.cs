using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Kinis.Models
{
    public class PoolLine
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Text { get; set; } = "Lane";
        public RectangleF Bounds { get; set; }
        public Color FillColor { get; set; } = Color.White;
        public Color BorderColor { get; set; } = Color.Black;
        public List<PoolLine> ChildLines { get; set; } = new List<PoolLine>();
        public int NestingLevel { get; set; } = 0;

        public bool CanAddChildLine()
        {
            return NestingLevel < 2; // Максимум 3 уровня вложенности (0,1,2)
        }
    }
}