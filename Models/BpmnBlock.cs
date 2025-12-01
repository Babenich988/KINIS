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
            if (Type == "Пул")
            {
                var poolPoints = new List<PointF>();

                // Точки соединения только на правой границе тела пула (не на полосе названия)
                float bodyLeft = Bounds.X + 40f; // Полоса названия шириной 40px
                float bodyRight = Bounds.Right;

                // Левая сторона тела (внутренняя граница между полосой названия и телом)
                poolPoints.Add(new PointF(bodyLeft, Bounds.Top));
                poolPoints.Add(new PointF(bodyLeft, Bounds.Top + Bounds.Height / 3));
                poolPoints.Add(new PointF(bodyLeft, Bounds.Top + 2 * Bounds.Height / 3));
                poolPoints.Add(new PointF(bodyLeft, Bounds.Bottom));

                // Правая сторона тела
                poolPoints.Add(new PointF(bodyRight, Bounds.Top));
                poolPoints.Add(new PointF(bodyRight, Bounds.Top + Bounds.Height / 3));
                poolPoints.Add(new PointF(bodyRight, Bounds.Top + 2 * Bounds.Height / 3));
                poolPoints.Add(new PointF(bodyRight, Bounds.Bottom));

                // Верхняя и нижняя стороны тела
                poolPoints.Add(new PointF(bodyLeft, Bounds.Top));
                poolPoints.Add(new PointF(bodyLeft + (bodyRight - bodyLeft) / 3, Bounds.Top));
                poolPoints.Add(new PointF(bodyLeft + 2 * (bodyRight - bodyLeft) / 3, Bounds.Top));
                poolPoints.Add(new PointF(bodyRight, Bounds.Top));

                poolPoints.Add(new PointF(bodyLeft, Bounds.Bottom));
                poolPoints.Add(new PointF(bodyLeft + (bodyRight - bodyLeft) / 3, Bounds.Bottom));
                poolPoints.Add(new PointF(bodyLeft + 2 * (bodyRight - bodyLeft) / 3, Bounds.Bottom));
                poolPoints.Add(new PointF(bodyRight, Bounds.Bottom));

                return poolPoints.Distinct().ToArray();
            }
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

            // Для пула не рисуем точки соединения (или рисуем только на границе тела)
            if (Type == "Пул")
            {
                // Рисуем точки только на правой границе тела пула
                using (var brush = new SolidBrush(Color.Green))
                {
                    // Точки на правой стороне тела пула
                    float rightX = Bounds.X + Bounds.Width;
                    float topY = Bounds.Y;
                    float bottomY = Bounds.Bottom;

                    // Верхняя точка правой стороны
                    g.FillEllipse(brush, rightX - 3, topY - 3, 6, 6);
                    // Нижняя точка правой стороны
                    g.FillEllipse(brush, rightX - 3, bottomY - 3, 6, 6);
                    // Центральная точка правой стороны
                    g.FillEllipse(brush, rightX - 3, topY + Bounds.Height / 2 - 3, 6, 6);
                }
                return;
            }

            // Стандартные точки соединения для других блоков
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

            // Для пула используем угловые ручки
            if (Type == "Пул")
            {
                return new RectangleF[]
                {
            // Верхний левый угол
            new RectangleF(Bounds.Left - handleSize/2, Bounds.Top - handleSize/2, handleSize, handleSize),
            // Верхний правый угол
            new RectangleF(Bounds.Right - handleSize/2, Bounds.Top - handleSize/2, handleSize, handleSize),
            // Нижний левый угол
            new RectangleF(Bounds.Left - handleSize/2, Bounds.Bottom - handleSize/2, handleSize, handleSize),
            // Нижний правый угол
            new RectangleF(Bounds.Right - handleSize/2, Bounds.Bottom - handleSize/2, handleSize, handleSize)
                };
            }
            else
            {
                // Оригинальные ручки для других блоков
                return new RectangleF[]
                {
            new RectangleF(Bounds.Left - handleSize/2, Bounds.Top - handleSize/2, handleSize, handleSize),
            new RectangleF(Bounds.Right - handleSize/2, Bounds.Top - handleSize/2, handleSize, handleSize),
            new RectangleF(Bounds.Left - handleSize/2, Bounds.Bottom - handleSize/2, handleSize, handleSize),
            new RectangleF(Bounds.Right - handleSize/2, Bounds.Bottom - handleSize/2, handleSize, handleSize)
                };
            }
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
            // Отрисовка основного контура пула
            using (var poolBrush = new SolidBrush(FillColor))
            using (var poolPen = new Pen(BorderColor, 2))
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
                using (var nameBrush = new SolidBrush(Color.Black))
                using (var format = new StringFormat())
                {
                    format.Alignment = StringAlignment.Center;
                    format.LineAlignment = StringAlignment.Center;

                    // Сохраняем текущее состояние графики
                    var state = g.Save();

                    g.TranslateTransform(
                        nameStripRect.X + nameStripRect.Width / 2,
                        nameStripRect.Y + nameStripRect.Height / 2
                    );
                    g.RotateTransform(-90);
                    g.DrawString(Text, nameFont, nameBrush, 0, 0, format);

                    // Восстанавливаем состояние
                    g.Restore(state);
                }
            }

            // Отрисовка дорожек пула
            DrawPoolLanes(g);

            // Специальная обработка выделения для пула
            if (isSelected)
            {
                // Рисуем рамку выделения вокруг всего пула
                // Толщина пена должна учитывать масштаб, поэтому используем фиксированную толщину
                using (var highlightPen = new Pen(Color.DeepSkyBlue, 3f)) // 3 пикселя в мировых координатах
                {
                    // Координаты рамки выделения
                    float padding = 2f;
                    RectangleF highlightRect = new RectangleF(
                        Bounds.X - padding,
                        Bounds.Y - padding,
                        Bounds.Width + 2 * padding,
                        Bounds.Height + 2 * padding
                    );

                    g.DrawRectangle(highlightPen,
                        highlightRect.X,
                        highlightRect.Y,
                        highlightRect.Width,
                        highlightRect.Height);
                }

                // Рисуем ручки изменения размера
                foreach (var handle in GetResizeHandles())
                {
                    using (var handleBrush = new SolidBrush(Color.Blue))
                    {
                        g.FillRectangle(handleBrush, handle);
                    }
                }

                // Рисуем точки соединения
                DrawConnectionPoints(g);
            }
        }

        private void DrawPoolLanes(Graphics g)
        {
            if (PoolLanes == null || PoolLanes.Count == 0)
                return;

            // Сохраняем текущее состояние графики
            var state = g.Save();

            try
            {
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
            finally
            {
                // Восстанавливаем состояние
                g.Restore(state);
            }
        }

        private void DrawNestedLanes(Graphics g, PoolLine parentLane)
        {
            if (parentLane.ChildLines == null || parentLane.ChildLines.Count == 0)
                return;

            // Сохраняем текущее состояние графики
            var state = g.Save();

            try
            {
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
            finally
            {
                // Восстанавливаем состояние
                g.Restore(state);
            }
        }

        public void InitializePoolLanes()
        {
            if (Type != "Пул") return;

            if (PoolLanes == null)
                PoolLanes = new List<PoolLine>();

            // Создаем первую дорожку по умолчанию
            var defaultLane = new PoolLine
            {
                Text = "Дорожка 1",
                Bounds = new RectangleF(
                    Bounds.X + 40f, // Отступ от левого края (ширина полосы названия)
                    Bounds.Y + 40f, // Отступ сверху (под названием пула)
                    Bounds.Width - 40f, // Ширина пула минус полоса названия
                    60f // Высота дорожки
                ),
                FillColor = Color.White,
                BorderColor = Color.Black
            };

            PoolLanes.Add(defaultLane);
        }

        public void ValidateLanePositions()
        {
            if (Type != "Пул" || PoolLanes == null) return;

            float minX = Bounds.X + 40f; // Левая граница тела пула
            float maxX = Bounds.Right;   // Правая граница пула
            float minY = Bounds.Y + 40f; // Ниже названия пула
            float maxY = Bounds.Bottom;  // Нижняя граница пула

            foreach (var lane in PoolLanes)
            {
                // Ограничиваем позицию дорожки по X
                if (lane.Bounds.X < minX)
                    lane.Bounds = new RectangleF(minX, lane.Bounds.Y, lane.Bounds.Width, lane.Bounds.Height);

                if (lane.Bounds.Right > maxX)
                    lane.Bounds = new RectangleF(maxX - lane.Bounds.Width, lane.Bounds.Y, lane.Bounds.Width, lane.Bounds.Height);

                // Ограничиваем позицию дорожки по Y
                if (lane.Bounds.Y < minY)
                    lane.Bounds = new RectangleF(lane.Bounds.X, minY, lane.Bounds.Width, lane.Bounds.Height);

                if (lane.Bounds.Bottom > maxY)
                    lane.Bounds = new RectangleF(lane.Bounds.X, maxY - lane.Bounds.Height, lane.Bounds.Width, lane.Bounds.Height);

                // Рекурсивно проверяем дочерние дорожки
                ValidateNestedLanePositions(lane, minX, maxX, minY, maxY);
            }
        }
        //Метод для проверки и ограничения позиций дорожек внутри пула
        private void ValidateNestedLanePositions(PoolLine parentLane, float minX, float maxX, float minY, float maxY)
        {
            if (parentLane.ChildLines == null) return;

            foreach (var childLane in parentLane.ChildLines)
            {
                // Для вложенных дорожек дополнительный отступ слева
                float nestedMinX = parentLane.Bounds.X + 20f;

                if (childLane.Bounds.X < nestedMinX)
                    childLane.Bounds = new RectangleF(nestedMinX, childLane.Bounds.Y, childLane.Bounds.Width, childLane.Bounds.Height);

                if (childLane.Bounds.Right > maxX)
                    childLane.Bounds = new RectangleF(maxX - childLane.Bounds.Width, childLane.Bounds.Y, childLane.Bounds.Width, childLane.Bounds.Height);

                // Вложенные дорожки должны быть внутри родительской по вертикали
                if (childLane.Bounds.Y < parentLane.Bounds.Y)
                    childLane.Bounds = new RectangleF(childLane.Bounds.X, parentLane.Bounds.Y, childLane.Bounds.Width, childLane.Bounds.Height);

                if (childLane.Bounds.Bottom > parentLane.Bounds.Bottom)
                    childLane.Bounds = new RectangleF(childLane.Bounds.X, parentLane.Bounds.Bottom - childLane.Bounds.Height, childLane.Bounds.Width, childLane.Bounds.Height);

                // Рекурсивная проверка
                ValidateNestedLanePositions(childLane, nestedMinX, maxX, minY, maxY);
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