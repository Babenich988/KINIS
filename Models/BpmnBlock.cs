using System;
using System.Drawing;
using System.Drawing.Drawing2D;

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
        private GraphicsPath RoundedRect(RectangleF bounds, int radius)//функция для скругления фигур
        {
            float d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        // Метод отрисовки блока
        public void Draw(Graphics g, bool isSelected)
        {
            using (var brush = new SolidBrush(Color.White)) // все белые фигуры
            using (var pen = new Pen(BorderColor, 2))
            {
                switch (Type)
                {
                    case "Комментарий":
                        // Прямоугольник
                        g.FillRectangle(brush, Bounds);
                        g.DrawRectangle(pen, Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height);
                        break;

                    case "Задача":
                        // Прямоугольник с округлёнными углами
                        GraphicsPath taskPath = RoundedRect(Bounds, 12);
                        g.FillPath(brush, taskPath);
                        g.DrawPath(pen, taskPath);
                        break;

                    case "Развилка":
                        // Ромб
                        PointF c = new PointF(Bounds.X + Bounds.Width / 2, Bounds.Y + Bounds.Height / 2);
                        PointF[] diamondPoints =
                        {
                    new PointF(c.X, Bounds.Y),
                    new PointF(Bounds.Right, c.Y),
                    new PointF(c.X, Bounds.Bottom),
                    new PointF(Bounds.Left, c.Y)
                };
                        g.FillPolygon(brush, diamondPoints);
                        g.DrawPolygon(pen, diamondPoints);
                        break;

                    case "Начальное событие":
                        // Круг с тонкой обводкой
                        g.FillEllipse(brush, Bounds);
                        using (var thinPen = new Pen(BorderColor, 2))
                            g.DrawEllipse(thinPen, Bounds);
                        break;

                    case "Промежуточное событие":
                        //  Круг с двойной обводкой
                        g.FillEllipse(brush, Bounds);
                        g.DrawEllipse(pen, Bounds);
                        RectangleF innerCircle = RectangleF.Inflate(Bounds, -4, -4);
                        g.DrawEllipse(pen, innerCircle);
                        break;

                    case "Конечное событие":
                        // Круг с толстой обводкой
                        g.FillEllipse(brush, Bounds);
                        using (var thickPen = new Pen(BorderColor, 4))
                            g.DrawEllipse(thickPen, Bounds);
                        break;

                    case "Объект данных":
                        // Прямоугольник с "загнутым" правым верхним углом
                        GraphicsPath dataPath = new GraphicsPath();
                        float fold = 10f; // размер загиба
                        dataPath.AddPolygon(new PointF[]
                        {
                    new PointF(Bounds.Left, Bounds.Top),
                    new PointF(Bounds.Right - fold, Bounds.Top),
                    new PointF(Bounds.Right, Bounds.Top + fold),
                    new PointF(Bounds.Right, Bounds.Bottom),
                    new PointF(Bounds.Left, Bounds.Bottom)
                        });
                        g.FillPath(brush, dataPath);
                        g.DrawPath(pen, dataPath);
                        // Рисуем линию загиба
                        g.DrawLine(pen, Bounds.Right - fold, Bounds.Top, Bounds.Right, Bounds.Top + fold);
                        break;

                    case "Хранилище данных":
                        // Овальная фигура (цилиндр)
                        RectangleF ellipseRect = Bounds;
                        float curve = Bounds.Height / 3;
                        g.FillRectangle(brush, ellipseRect);
                        g.DrawEllipse(pen, ellipseRect.X, ellipseRect.Y, ellipseRect.Width, curve); // верх
                        g.DrawEllipse(pen, ellipseRect.X, ellipseRect.Bottom - curve, ellipseRect.Width, curve); // низ
                        g.DrawLine(pen, ellipseRect.X, ellipseRect.Y + curve / 2, ellipseRect.X, ellipseRect.Bottom - curve / 2);
                        g.DrawLine(pen, ellipseRect.Right, ellipseRect.Y + curve / 2, ellipseRect.Right, ellipseRect.Bottom - curve / 2);
                        break;

                    default:
                        g.FillRectangle(brush, Bounds);
                        g.DrawRectangle(pen, Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height);
                        break;
                }
            }

            // 🖋️ Подпись блока
            using (var font = new Font("Segoe UI", 9, FontStyle.Regular))
            using (var textBrush = new SolidBrush(Color.Black))
            {
                var textSize = g.MeasureString(Text, font);
                float textX = Bounds.X + (Bounds.Width - textSize.Width) / 2f;
                float textY = Bounds.Y + (Bounds.Height - textSize.Height) / 2f;
                g.DrawString(Text, font, textBrush, textX, textY);
            }

            // 🔷 Если выбран — выделяем голубой рамкой
            if (isSelected)
            {
                using (var highlight = new Pen(Color.DeepSkyBlue, 3))
                {
                    g.DrawRectangle(highlight, Bounds.X - 2, Bounds.Y - 2, Bounds.Width + 4, Bounds.Height + 4);
                }
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
