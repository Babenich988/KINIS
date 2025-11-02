using System;
using System.Collections.Generic;
using System.Drawing;

namespace Kinis.Models
{
    [Serializable] // Позволяет сохранять объекты этого класса (например, в XML)
    public class BpmnBlock
    {
        // Уникальный идентификатор блока
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // Тип блока (Task, Event, Gateway и т.п.)
        public string Type { get; set; } = "Task";

        // Текст внутри блока
        public string Text { get; set; } = "New Block";

        // Координаты и размеры (x, y, ширина, высота)
        public RectangleF Bounds { get; set; }

        // Цвет заливки
        public Color FillColor { get; set; } = Color.White;

        // Цвет рамки
        public Color BorderColor { get; set; } = Color.Black;

        // Конструктор для создания блока с координатами и размерами
        public BpmnBlock(float x, float y, float width = 100, float height = 60)
        {
            Bounds = new RectangleF(x, y, width, height);
        }

        //Метод создания точек привязки
        public PointF[] GetConnectionPoints()
        {
            var points = new List<PointF>();

        }

        //Метод отрисовки точек привязки
        public void DrawConnectionPoints(Graphics g)
        {
            var points = GetConnectionPoints();
            using (var brush = new SolidBrush(Color.Green))
            {
                foreach (var point in points)
                {
                    g.FillEllipse(brush, point.X - 3, point.Y - 3, 6, 6);
                }
            }
        }

        // Метод отрисовки блока
        public void Draw(Graphics g, bool isSelected = false)
        {
            // 1. Рисуем заливку
            using (var brush = new SolidBrush(FillColor))
                g.FillRectangle(brush, Bounds);

            // 2. Рисуем границу (если блок выбран — делаем рамку толще и синей)
            using (var pen = new Pen(isSelected ? Color.Blue : BorderColor, isSelected ? 2 : 1))
                g.DrawRectangle(pen, Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height);

            // 3. Рисуем текст по центру
            using (var font = new Font("Segoe UI", 9))
            using (var textBrush = new SolidBrush(Color.Black))
            {
                var textSize = g.MeasureString(Text, font);
                var textX = Bounds.X + (Bounds.Width - textSize.Width) / 2;
                var textY = Bounds.Y + (Bounds.Height - textSize.Height) / 2;
                g.DrawString(Text, font, textBrush, textX, textY);
            }

            // 4. Если блок выбран — рисуем ручки для растяжения
            if (isSelected)
            {
                DrawHandles(g);
                // 5. И точки привязки
                DrawConnectionPoints(g);
            }
        }

        // Получаем координаты четырёх "ручек" по углам для растяжения
        public RectangleF[] GetResizeHandles()
        {
            const int handleSize = 8;
            return new RectangleF[]
            {
        new RectangleF(Bounds.Left - handleSize/2, Bounds.Top - handleSize/2, handleSize, handleSize), // ЛВ
        new RectangleF(Bounds.Right - handleSize/2, Bounds.Top - handleSize/2, handleSize, handleSize), // ПВ
        new RectangleF(Bounds.Left - handleSize/2, Bounds.Bottom - handleSize/2, handleSize, handleSize), // ЛН
        new RectangleF(Bounds.Right - handleSize/2, Bounds.Bottom - handleSize/2, handleSize, handleSize) // ПН
            };
        }

        // Рисуем ручки при выделении
        public void DrawHandles(Graphics g)
        {
            using (var brush = new SolidBrush(Color.Blue))
            {
                foreach (var handle in GetResizeHandles())
                    g.FillRectangle(brush, handle);
            }
        }
    }
}
