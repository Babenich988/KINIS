using System;
using System.Drawing;

namespace Kinis.Models
{
    [Serializable] // Нужно для сохранения объекта в XML
    public class BpmnBlock
    {
        // Уникальный ID блока (чтобы потом отличать один от другого)
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // Тип блока (например, Task, Event, Gateway)
        public string Type { get; set; } = "Task";

        // Текст, который будет внутри блока
        public string Text { get; set; } = "New Block";

        // Координаты и размеры блока на поле (x, y, width, height)
        public RectangleF Bounds { get; set; }

        // Цвет заливки блока
        public Color FillColor { get; set; } = Color.White;

        // Цвет границы блока
        public Color BorderColor { get; set; } = Color.Black;

        // Конструктор (создание блока)
        public BpmnBlock(float x, float y, float width = 100, float height = 60)
        {
            Bounds = new RectangleF(x, y, width, height);
        }
        // Метод для рисования блока на экране
        public void Draw(Graphics g, bool isSelected = false)
        {
            // 1. Заливка фона блока
            using (var brush = new SolidBrush(FillColor))
                g.FillRectangle(brush, Bounds);

            // 2. Рисуем границу (если выбран — синим и толще)
            using (var pen = new Pen(isSelected ? Color.Blue : BorderColor, isSelected ? 2 : 1))
                g.DrawRectangle(pen, Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height);

            // 3. Рисуем текст в центре блока
            using (var font = new Font("Segoe UI", 9))
            using (var textBrush = new SolidBrush(Color.Black))
            {
                var textSize = g.MeasureString(Text, font);
                var textX = Bounds.X + (Bounds.Width - textSize.Width) / 2;
                var textY = Bounds.Y + (Bounds.Height - textSize.Height) / 2;
                g.DrawString(Text, font, textBrush, textX, textY);
            }
        }
    }
}
