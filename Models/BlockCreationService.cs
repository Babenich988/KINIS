using Kinis.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

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
    }
}