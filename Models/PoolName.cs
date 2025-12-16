using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Kinis.Models
{
    /// <summary>
    /// Модель названия пула с поддержкой вертикального текста
    /// </summary>
    /// <remarks>
    /// Устаревший класс - функциональность интегрирована в BpmnBlock
    /// Сохранен для обратной совместимости
    /// </remarks>
    public class PoolName
    {
        /// <summary>
        /// Текст названия пула
        /// </summary>
        public string Text { get; set; } = "Pool";

        /// <summary>
        /// Шрифт для отображения текста
        /// </summary>
        public Font Font { get; set; } = new Font("Segoe UI", 10f);

        /// <summary>
        /// Типы элементов, для которых текст не отображается
        /// </summary>
        public static readonly HashSet<string> NoTextTypes = new HashSet<string>
        {
            "Развилка И",
            "Событие-получение сообщения",
            "Событие-получение сообщения (промежуточное)",
            "Событие-отправка сообщения (промежуточное)",
            "Событие-отправка сообщения",
            "Событие-ошибка обработчик",
            "Событие-ошибка инициатор",
            "Событие-отмена обработчик",
            "Событие-отмена инициатор",
            "Событие-остановка"
        };

        /// <summary>
        /// Цвет текста
        /// </summary>
        public Color Color { get; set; } = Color.Black;

        /// <summary>
        /// Границы области отрисовки текста
        /// </summary>
        public RectangleF Bounds { get; set; }

        /// <summary>
        /// Отрисовывает вертикальный текст названия пула
        /// </summary>
        /// <param name="g">Графический контекст для рисования</param>
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