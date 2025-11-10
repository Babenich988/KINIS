using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Kinis.Models;

namespace Kinis.Services
{
    public class BlockCreationService
    {
        private readonly InfiniteCanvas _canvas;
        private readonly List<BpmnBlock> _blocks;

        public BlockCreationService(InfiniteCanvas canvas, List<BpmnBlock> blocks)
        {
            _canvas = canvas;
            _blocks = blocks;
        }
        private SizeF GetDefaultBlockSize(string type)
        {
            return type switch
            {
                "Комментарий" => new SizeF(120, 80),
                "Задача" => new SizeF(120, 80),
                "Развилка" => new SizeF(80, 80),
                "Начальное событие" => new SizeF(60, 60),
                "Промежуточное событие" => new SizeF(60, 60),
                "Конечное событие" => new SizeF(60, 60),
                "Объект данных" => new SizeF(100, 60),
                "Хранилище данных" => new SizeF(120, 80),
                "Arrow" => new SizeF(100, 60),
                _ => new SizeF(120, 80)
            };
        }

        public BpmnBlock CreateBlockAtPosition(string type, string text, PointF position)
        {
            var defaultSize = GetDefaultBlockSize(type);

            var block = new BpmnBlock(
                position.X - defaultSize.Width / 2,
                position.Y - defaultSize.Height / 2,
                defaultSize.Width,
                defaultSize.Height
            )
            {
                Text = text,
                Type = type,
                FillColor = Color.White,
                BorderColor = Color.Black,
                Id = Guid.NewGuid().ToString()
            };

            return block;
        }

        public void AddBlockToCanvas(BpmnBlock block)
        {
            _blocks.Add(block);
            _canvas.SetBlocks(_blocks);
            _canvas.Invalidate();
        }

        public Dictionary<Keys, (string type, string text)> GetBlockKeyMappings()
        {
            return new Dictionary<Keys, (string, string)>
            {
                { Keys.D1, ("Комментарий", "Комментарий") },
                { Keys.D2, ("Задача", "Задача") },
                { Keys.D3, ("Развилка", "Развилка") },
                { Keys.D4, ("Начальное событие", "Начальное событие") },
                { Keys.D5, ("Промежуточное событие", "Промежуточное событие") },
                { Keys.D6, ("Конечное событие", "Конечное событие") },
                { Keys.D7, ("Объект данных", "Объект данных") },
                { Keys.D8, ("Хранилище данных", "Хранилище данных") },
                { Keys.D9, ("Arrow", "→") },
                { Keys.D0, ("Task", "Новая задача") }
            };
        }
    }
}