using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Kinis.Models
{
    public class Pool
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public PoolName Name { get; set; } = new PoolName();
        public List<PoolLine> Lines { get; set; } = new List<PoolLine>();
        public RectangleF Bounds { get; set; }
        public Color FillColor { get; set; } = Color.White;
        public Color BorderColor { get; set; } = Color.Black;

        public Pool()
        {
            // Добавляем первую линию по умолчанию
            Lines.Add(new PoolLine());
        }
    }
}
