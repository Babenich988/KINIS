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
    /// <summary>
    /// Сервис создания блоков BPMN
    /// </summary>
    public class BlockCreationService
    {
        private readonly InfiniteCanvas _canvas;
        private readonly List<BpmnBlock> _blocks;

        /// <summary>
        /// Инициализирует сервис создания блоков
        /// </summary>
        /// <param name="canvas">Холст для отображения блоков</param>
        /// <param name="blocks">Список блоков на холсте</param>
        public BlockCreationService(InfiniteCanvas canvas, List<BpmnBlock> blocks)
        {
            _canvas = canvas;
            _blocks = blocks;
        }

        /// <summary>
        /// Получает размер по умолчанию для указанного типа блока
        /// </summary>
        /// <param name="type">Тип блока</param>
        /// <returns>Размер блока по умолчанию</returns>
        private SizeF GetDefaultBlockSize(string type)
        {
            switch (type)
            {
                case "Пул":
                    return new SizeF(600, 400); // Увеличиваем размер пула
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
                default:
                    return new SizeF(120, 80);
            }
        }

        /// <summary>
        /// Создает блок в указанной позиции
        /// </summary>
        /// <param name="type">Тип создаваемого блока</param>
        /// <param name="text">Текст блока</param>
        /// <param name="position">Позиция центра блока</param>
        /// <returns>Созданный блок BPMN</returns>
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

            // Для пула создаем 3 дорожки по умолчанию
            if (type == "Пул")
            {
                block.InitializePoolLanes(); // Теперь создаст 3 дорожки
            }

            return block;
        }

        /// <summary>
        /// Добавляет блок на холст
        /// </summary>
        /// <param name="block">Блок для добавления</param>
        public void AddBlockToCanvas(BpmnBlock block)
        {
            _blocks.Add(block);
            _canvas.SetBlocks(_blocks);
            _canvas.Invalidate();
        }

        /// <summary>
        /// Получает сопоставление горячих клавиш с типами блоков
        /// </summary>
        /// <returns>Словарь сопоставления клавиш с типами блоков</returns>
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

        /// <summary>
        /// Класс для сопоставления горячих клавиш с типами блоков
        /// </summary>
        public class BlockMapping
        {
            /// <summary>
            /// Тип блока
            /// </summary>
            public string Type { get; set; }

            /// <summary>
            /// Текст по умолчанию для блока
            /// </summary>
            public string Text { get; set; }
        }
    }
}