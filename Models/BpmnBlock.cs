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
        public List<PoolLine> PoolLanes { get; set; } = new List<PoolLine>();
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
            // ДИАГНОСТИКА: Проверяем, вызывается ли отрисовка для пула
            if (Type == "Пул")
            {
                System.Diagnostics.Debug.WriteLine($"BpmnBlock.Draw для пула: Text={Text}, Bounds={Bounds}");
            }

            // Для типа "Пул" обрабатываем отдельно, чтобы избежать конфликтов с using
            if (Type == "Пул")
            {
                DrawPool(g, isSelected);
                return;
            }

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

                    case "Событие-получение сообщения":
                        {
                            g.FillEllipse(brush, Bounds);
                            g.DrawEllipse(pen, Bounds);

                            // Внутренний конверт
                            using (var p = new Pen(Color.Black, 2))
                            {
                                float mx = Bounds.X + Bounds.Width / 2f;
                                float my = Bounds.Y + Bounds.Height / 2f;

                                g.DrawRectangle(p,
                                    mx - 10, my - 7,
                                    20, 14);

                                g.DrawLine(p, mx - 10, my - 7, mx, my);
                                g.DrawLine(p, mx + 10, my - 7, mx, my);
                            }
                        }
                        break;

                    case "Событие-отправка сообщения":
                        {
                            using (var thick = new Pen(BorderColor, 3))
                                g.DrawEllipse(thick, Bounds);

                            // Иконка — конверт открытый
                            using (var p = new Pen(Color.Black, 2))
                            {
                                float mx = Bounds.X + Bounds.Width / 2f;
                                float my = Bounds.Y + Bounds.Height / 2f;

                                g.DrawPolygon(p, new[]
                                {
                                    new PointF(mx - 10, my - 5),
                                    new PointF(mx, my + 8),
                                    new PointF(mx + 10, my - 5)
                                });
                            }
                        }
                        break;

                    case "Событие-ошибка обработчик":
                    case "Событие-ошибка инициатор":
                        {
                            g.FillEllipse(brush, Bounds);
                            using (var thick = new Pen(BorderColor, 3)) g.DrawEllipse(thick, Bounds);

                            using (var p = new Pen(Color.Black, 2))
                            {
                                float x = Bounds.X + 15;
                                float y = Bounds.Y + Bounds.Height / 2f;

                                g.DrawLines(p, new[]
                                {
                                    new PointF(x, y),
                                    new PointF(x + 10, y - 8),
                                    new PointF(x + 20, y + 8),
                                    new PointF(x + 30, y - 8)
                                });
                            }
                        }
                        break;

                    case "Событие-отмена обработчик":
                    case "Событие-отмена инициатор":
                        {
                            g.FillEllipse(brush, Bounds);
                            using (var thick = new Pen(BorderColor, 3)) g.DrawEllipse(thick, Bounds);

                            using (var p = new Pen(Color.Black, 3))
                            {
                                float mx = Bounds.X + Bounds.Width / 2;
                                float my = Bounds.Y + Bounds.Height / 2;
                                float s = 12;

                                g.DrawLine(p, mx - s, my - s, mx + s, my + s);
                                g.DrawLine(p, mx - s, my + s, mx + s, my - s);
                            }
                        }
                        break;

                    case "Событие-остановка":
                        {
                            using (var thick = new Pen(BorderColor, 4)) g.DrawEllipse(thick, Bounds);

                            using (var p = new Pen(Color.Black, 4))
                            {
                                float mx = Bounds.X + Bounds.Width / 2;
                                float my = Bounds.Y + Bounds.Height / 2;
                                float s = 14;

                                g.DrawLine(p, mx - s, my - s, mx + s, my + s);
                                g.DrawLine(p, mx - s, my + s, mx + s, my - s);
                            }
                        }
                        return; // Выходим из метода Draw
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

        private void DrawPool(Graphics g, bool isSelected)
        {
            // ДОБАВЛЯЕМ ДИАГНОСТИКУ
            System.Diagnostics.Debug.WriteLine($"DrawPool вызван: Bounds={Bounds}, Text={Text}, FillColor={FillColor}");

            // Временный яркий цвет для отладки
            Color tempFillColor = Color.LightBlue;
            Color tempBorderColor = Color.DarkBlue;

            // Отрисовка основного контура пула
            using (var poolBrush = new SolidBrush(tempFillColor))
            using (var poolPen = new Pen(tempBorderColor, 3))
            {
                // Полоса названия (левая часть)
                float nameStripWidth = 40f;
                RectangleF nameStripRect = new RectangleF(
                    Bounds.X,
                    Bounds.Y,
                    nameStripWidth,
                    Bounds.Height
                );

                // Тело пула (правая часть)
                RectangleF bodyRect = new RectangleF(
                    Bounds.X + nameStripWidth,
                    Bounds.Y,
                    Bounds.Width - nameStripWidth,
                    Bounds.Height
                );

                // ДИАГНОСТИКА: Размеры прямоугольников
                System.Diagnostics.Debug.WriteLine($"Полоса: {nameStripRect}, Тело: {bodyRect}");

                // Заливка полосы названия и тела
                g.FillRectangle(poolBrush, nameStripRect);
                g.FillRectangle(poolBrush, bodyRect);

                // Контур полосы названия и тела
                g.DrawRectangle(poolPen, nameStripRect.X, nameStripRect.Y,
                               nameStripRect.Width, nameStripRect.Height);
                g.DrawRectangle(poolPen, bodyRect.X, bodyRect.Y,
                               bodyRect.Width, bodyRect.Height);

                // Вертикальный текст названия пула
                using (var nameFont = new Font("Segoe UI", 10, FontStyle.Bold))
                using (var nameBrush = new SolidBrush(Color.Red)) // Красный для видимости
                using (var format = new StringFormat())
                {
                    format.Alignment = StringAlignment.Center;
                    format.LineAlignment = StringAlignment.Center;

                    g.TranslateTransform(
                        nameStripRect.X + nameStripRect.Width / 2,
                        nameStripRect.Y + nameStripRect.Height / 2
                    );
                    g.RotateTransform(-90);
                    g.DrawString(Text + " TEST", nameFont, nameBrush, 0, 0, format);
                    g.ResetTransform();
                }

                // ДИАГНОСТИКА: Рисуем красный крест в центре пула для отладки
                g.DrawLine(Pens.Red, Bounds.Left, Bounds.Top, Bounds.Right, Bounds.Bottom);
                g.DrawLine(Pens.Red, Bounds.Right, Bounds.Top, Bounds.Left, Bounds.Bottom);
            }

            // Отрисовка дорожек пула
            DrawPoolLanes(g);

            // Специальная обработка выделения для пула
            if (isSelected)
            {
                // Рисуем рамку выделения вокруг всего пула
                using (var highlightPen = new Pen(Color.Red, 3))
                {
                    g.DrawRectangle(highlightPen,
                        Bounds.X - 2,
                        Bounds.Y - 2,
                        Bounds.Width + 4,
                        Bounds.Height + 4
                    );
                }

                DrawHandles(g);
                DrawConnectionPoints(g);
            }
        }

        private void DrawPoolLanes(Graphics g)
        {
            if (PoolLanes == null || PoolLanes.Count == 0)
                return;

            foreach (var lane in PoolLanes)
            {
                // Заливка дорожки
                using (var laneBrush = new SolidBrush(lane.FillColor))
                {
                    g.FillRectangle(laneBrush, lane.Bounds);
                }

                // Контур дорожки
                using (var lanePen = new Pen(lane.BorderColor, 1))
                {
                    g.DrawRectangle(lanePen, lane.Bounds.X, lane.Bounds.Y,
                                   lane.Bounds.Width, lane.Bounds.Height);
                }

                // Текст дорожки
                using (var laneFont = new Font("Segoe UI", 9))
                using (var textBrush = new SolidBrush(Color.Black))
                {
                    var textSize = g.MeasureString(lane.Text, laneFont);
                    float textX = lane.Bounds.X + 10f;
                    float textY = lane.Bounds.Y + (lane.Bounds.Height - textSize.Height) / 2f;
                    g.DrawString(lane.Text, laneFont, textBrush, textX, textY);
                }

                // Рекурсивная отрисовка вложенных дорожек
                DrawNestedLanes(g, lane);
            }
        }

        private void DrawNestedLanes(Graphics g, PoolLine parentLane)
        {
            if (parentLane.ChildLines == null || parentLane.ChildLines.Count == 0)
                return;

            foreach (var childLane in parentLane.ChildLines)
            {
                // Заливка вложенной дорожки
                using (var childBrush = new SolidBrush(childLane.FillColor))
                {
                    g.FillRectangle(childBrush, childLane.Bounds);
                }

                // Контур вложенной дорожки
                using (var childPen = new Pen(childLane.BorderColor, 1))
                {
                    g.DrawRectangle(childPen, childLane.Bounds.X, childLane.Bounds.Y,
                                   childLane.Bounds.Width, childLane.Bounds.Height);
                }

                // Текст вложенной дорожки
                using (var childFont = new Font("Segoe UI", 9))
                using (var textBrush = new SolidBrush(Color.Black))
                {
                    var textSize = g.MeasureString(childLane.Text, childFont);
                    float textX = childLane.Bounds.X + 20f; // Больший отступ для вложенности
                    float textY = childLane.Bounds.Y + (childLane.Bounds.Height - textSize.Height) / 2f;
                    g.DrawString(childLane.Text, childFont, textBrush, textX, textY);
                }

                // Рекурсивная отрисовка следующих уровней вложенности
                DrawNestedLanes(g, childLane);
            }
        }       

        public void UpdatePoolLanesPosition(float deltaX, float deltaY)
        {
            if (PoolLanes == null || PoolLanes.Count == 0) return;

            // Полностью пересчитываем все позиции дорожек относительно нового положения пула
            float currentY = Bounds.Y + 40f; // Стартовая позиция под названием

            for (int i = 0; i < PoolLanes.Count; i++)
            {
                var lane = PoolLanes[i];
                lane.Bounds = new RectangleF(
                    Bounds.X + 40f,        // Фиксированный отступ от левого края пула
                    currentY,              // Абсолютная позиция Y
                    Bounds.Width - 40f,    // Ширина по размеру пула
                    lane.Bounds.Height     // Сохраняем высоту
                );
                currentY += lane.Bounds.Height; // Следующая дорожка ниже
            }
        }
    }
}