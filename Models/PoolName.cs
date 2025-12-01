using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Kinis.Models
{
    public class PoolName
    {
        public string Text { get; set; } = "Pool";
        public Font Font { get; set; } = new Font("Segoe UI", 10);
        public Color Color { get; set; } = Color.Black;
        public RectangleF Bounds { get; set; }

        public void Draw(Graphics g)
        {
            using (var brush = new SolidBrush(Color))
            using (var format = new StringFormat())
            {
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                g.DrawString(Text, Font, brush, Bounds, format);
            }
        }
    }
}