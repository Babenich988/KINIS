using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Drawing.Drawing2D;

namespace Kinis.Models
{
    [Serializable]
    public class BpmnBlock
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Type { get; set; } = "Task";
        public string Text { get; set; } = "New Block";
        public RectangleF Bounds { get; set; }
        public Color FillColor { get; set; } = Color.White;
        public Color BorderColor { get; set; } = Color.Black;

        // КОНСТРУКТОР ПО УМОЛЧАНИЮ ДЛЯ СЕРИАЛИЗАЦИИ
        public BpmnBlock()
        {
            Bounds = new RectangleF(0, 0, 100, 60);
        }

        public BpmnBlock(float x, float y, float width = 100, float height = 60)
        {
            Bounds = new RectangleF(x, y, width, height);
        }

        private GraphicsPath RoundedRect(RectangleF bounds, int radius)
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

        public PointF[] GetConnectionPoints()
        {
            var points = new List<PointF>();
            points.Add(new PointF(Bounds.Left, Bounds.Top));
            points.Add(new PointF(Bounds.Left, Bounds.Top + Bounds.Height / 3));
            points.Add(new PointF(Bounds.Left, Bounds.Top + 2 * Bounds.Height / 3));
            points.Add(new PointF(Bounds.Left, Bounds.Bottom));
            points.Add(new PointF(Bounds.Right, Bounds.Top));
            points.Add(new PointF(Bounds.Right, Bounds.Top + Bounds.Height / 3));
            points.Add(new PointF(Bounds.Right, Bounds.Top + 2 * Bounds.Height / 3));
            points.Add(new PointF(Bounds.Right, Bounds.Bottom));
            points.Add(new PointF(Bounds.Left, Bounds.Top));
            points.Add(new PointF(Bounds.Left + Bounds.Width / 3, Bounds.Top));
            points.Add(new PointF(Bounds.Left + 2 * Bounds.Width / 3, Bounds.Top));
            points.Add(new PointF(Bounds.Right, Bounds.Top));
            points.Add(new PointF(Bounds.Left, Bounds.Bottom));
            points.Add(new PointF(Bounds.Left + Bounds.Width / 3, Bounds.Bottom));
            points.Add(new PointF(Bounds.Left + 2 * Bounds.Width / 3, Bounds.Bottom));
            points.Add(new PointF(Bounds.Right, Bounds.Bottom));

            return points.Distinct().ToArray();
        }

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

        public void Draw(Graphics g, bool isSelected)
        {
            using (var brush = new SolidBrush(Color.White))
            using (var pen = new Pen(BorderColor, 2))
            {
                switch (Type)
                {
                    case "Комментарий":
                        g.FillRectangle(brush, Bounds);
                        g.DrawRectangle(pen, Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height);
                        break;

                    case "Задача":
                        GraphicsPath taskPath = RoundedRect(Bounds, 12);
                        g.FillPath(brush, taskPath);
                        g.DrawPath(pen, taskPath);
                        break;

                    case "Развилка":
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
                        g.FillEllipse(brush, Bounds);
                        using (var thinPen = new Pen(BorderColor, 2))
                            g.DrawEllipse(thinPen, Bounds);
                        break;

                    case "Промежуточное событие":
                        g.FillEllipse(brush, Bounds);
                        g.DrawEllipse(pen, Bounds);
                        RectangleF innerCircle = RectangleF.Inflate(Bounds, -4, -4);
                        g.DrawEllipse(pen, innerCircle);
                        break;

                    case "Конечное событие":
                        g.FillEllipse(brush, Bounds);
                        using (var thickPen = new Pen(BorderColor, 4))
                            g.DrawEllipse(thickPen, Bounds);
                        break;

                    case "Объект данных":
                        GraphicsPath dataPath = new GraphicsPath();
                        float fold = 10f;
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
                        g.DrawLine(pen, Bounds.Right - fold, Bounds.Top, Bounds.Right, Bounds.Top + fold);
                        break;

                    case "Хранилище данных":
                        RectangleF ellipseRect = Bounds;
                        float curve = Bounds.Height / 3;
                        g.FillRectangle(brush, ellipseRect);
                        g.DrawEllipse(pen, ellipseRect.X, ellipseRect.Y, ellipseRect.Width, curve);
                        g.DrawEllipse(pen, ellipseRect.X, ellipseRect.Bottom - curve, ellipseRect.Width, curve);
                        g.DrawLine(pen, ellipseRect.X, ellipseRect.Y + curve / 2, ellipseRect.X, ellipseRect.Bottom - curve / 2);
                        g.DrawLine(pen, ellipseRect.Right, ellipseRect.Y + curve / 2, ellipseRect.Right, ellipseRect.Bottom - curve / 2);
                        break;

                    default:
                        g.FillRectangle(brush, Bounds);
                        g.DrawRectangle(pen, Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height);
                        break;
                }
            }

            using (var font = new Font("Segoe UI", 9, FontStyle.Regular))
            using (var textBrush = new SolidBrush(Color.Black))
            {
                var textSize = g.MeasureString(Text, font);
                float textX = Bounds.X + (Bounds.Width - textSize.Width) / 2f;
                float textY = Bounds.Y + (Bounds.Height - textSize.Height) / 2f;
                g.DrawString(Text, font, textBrush, textX, textY);
            }

            if (isSelected)
            {
                DrawHandles(g);
                DrawConnectionPoints(g);
                using (var highlight = new Pen(Color.DeepSkyBlue, 3))
                {
                    g.DrawRectangle(highlight, Bounds.X - 2, Bounds.Y - 2, Bounds.Width + 4, Bounds.Height + 4);
                }
            }
        }

        public RectangleF[] GetResizeHandles()
        {
            const int handleSize = 8;
            return new RectangleF[]
            {
        new RectangleF(Bounds.Left - handleSize/2, Bounds.Top - handleSize/2, handleSize, handleSize),
        new RectangleF(Bounds.Right - handleSize/2, Bounds.Top - handleSize/2, handleSize, handleSize),
        new RectangleF(Bounds.Left - handleSize/2, Bounds.Bottom - handleSize/2, handleSize, handleSize),
        new RectangleF(Bounds.Right - handleSize/2, Bounds.Bottom - handleSize/2, handleSize, handleSize)
            };
        }

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