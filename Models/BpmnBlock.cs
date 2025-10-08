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
    }
}
