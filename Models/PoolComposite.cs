using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Kinis.Models
{
    public class PoolComposite
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public BpmnBlock NameBlock { get; set; }
        public BpmnBlock BodyBlock { get; set; }
        public List<PoolLine> Lanes { get; set; } = new List<PoolLine>();

        public PoolComposite(float x, float y, float width = 400, float height = 200)
        {
            // Блок названия (левая полоса)
            NameBlock = new BpmnBlock(x, y, 40, height)
            {
                Type = "PoolName",
                Text = "Pool",
                FillColor = Color.White,
                BorderColor = Color.Black
            };

            // Основное тело пула
            BodyBlock = new BpmnBlock(x + 40, y, width - 40, height)
            {
                Type = "PoolBody",
                Text = "",
                FillColor = Color.White,
                BorderColor = Color.Black
            };
        }

        public RectangleF Bounds
        {
            get => new RectangleF(
                NameBlock.Bounds.X,
                NameBlock.Bounds.Y,
                NameBlock.Bounds.Width + BodyBlock.Bounds.Width,
                Math.Max(NameBlock.Bounds.Height, BodyBlock.Bounds.Height)
            );
        }
    }
}