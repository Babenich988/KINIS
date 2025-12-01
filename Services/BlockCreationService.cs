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
            switch (type)
            {
                case "Пул":
                    return new SizeF(400, 200); // Увеличиваем размер пула
                case "Комментарий":
                    return new SizeF(120, 80);
                case "Задача":
                    return new SizeF(120, 80);
                case "Развилка":
                    return new SizeF(80, 80);
                case "Начальное событие":
                    return new SizeF(60, 60);
                case "Промежуточное событие":
                    return new SizeF(60, 60);
                case "Конечное событие":
                    return new SizeF(60, 60);
                case "Объект данных":
                    return new SizeF(100, 60);
                case "Хранилище данных":
                    return new SizeF(120, 80);
                case "Arrow":
                    return new SizeF(100, 60);
                case "Событие-получение сообщения":
                case "Событие-отправка сообщения":
                case "Событие-ошибка обработчик":
                case "Событие-ошибка инициатор":
                case "Событие-отмена обработчик":
                case "Событие-отмена инициатор":
                case "Событие-остановка":
                    return new SizeF(60, 60);
                //case "Пул":
                //    return new SizeF(400, 200);
                default:
                    return new SizeF(120, 80);
            }
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

        public Dictionary<Keys, BlockMapping> GetBlockKeyMappings()
        {
            return new Dictionary<Keys, BlockMapping>
            {
                { Keys.D1, new BlockMapping { Type = "Комментарий", Text = "Комментарий" } },
                { Keys.D2, new BlockMapping { Type = "Задача", Text = "Задача" } },
                { Keys.D3, new BlockMapping { Type = "Развилка", Text = "Развилка" } },
                { Keys.D4, new BlockMapping { Type = "Начальное событие", Text = "Начальное событие" } },
                { Keys.D5, new BlockMapping { Type = "Промежуточное событие", Text = "Промежуточное событие" } },
                { Keys.D6, new BlockMapping { Type = "Конечное событие", Text = "Конечное событие" } },
                { Keys.D7, new BlockMapping { Type = "Объект данных", Text = "Объект данных" } },
                { Keys.D8, new BlockMapping { Type = "Хранилище данных", Text = "Хранилище данных" } },
                { Keys.D9, new BlockMapping { Type = "Arrow", Text = "→" } },
                { Keys.D0, new BlockMapping { Type = "CurvedArrow", Text = "↷" } } // ИЗМЕНЯЕМ: CurvedArrow вместо Task
            };
        }
        public class BlockMapping
        {
            public string Type { get; set; }
            public string Text { get; set; }
        }
    }
}