using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

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

        // Добавим константу для минимальной высоты дорожки в класс BpmnBlock:
        private const float LANE_MIN_HEIGHT = 40f;

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

        public void ConvertLanesToDrawIOStyle()
        {
            if (PoolLanes == null) return;

            foreach (var lane in PoolLanes)
            {
                lane.IsTransparent = true;
                lane.BackgroundColor = Color.Transparent;
                lane.NameStripBackgroundColor = Color.White;
                lane.BorderWidth = 1f;
                lane.NameStripWidth = 40f;

                // Рекурсивно обновляем вложенные дорожки
                ConvertNestedLanesToDrawIOStyle(lane);
            }
        }

        private void ConvertNestedLanesToDrawIOStyle(PoolLine lane)
        {
            if (lane.ChildLines == null) return;

            foreach (var childLane in lane.ChildLines)
            {
                childLane.IsTransparent = true;
                childLane.BackgroundColor = Color.Transparent;
                childLane.NameStripBackgroundColor = Color.White;
                childLane.BorderWidth = 1f;
                childLane.NameStripWidth = 30f;
                childLane.BorderColor = Color.DarkGray;

                ConvertNestedLanesToDrawIOStyle(childLane);
            }
        }

        public void DrawConnectionPoints(Graphics g)
        {
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
            var points = GetConnectionPoints();
            using (var brush = new SolidBrush(Color.Green))
            {
                foreach (var point in points)
                {
                    g.FillEllipse(brush, point.X - 3, point.Y - 3, 6, 6);
                }
            }
        }

        // === Вспомогательные методы для масштабируемых иконок ===
        private void DrawEnvelopeIcon(Graphics g, RectangleF bounds)
        {
            // центр и масштаб
            var center = new PointF(bounds.X + bounds.Width / 2f, bounds.Y + bounds.Height / 2f);
            float w = bounds.Width * 0.45f;
            float h = bounds.Height * 0.28f;
            RectangleF rect = new RectangleF(center.X - w / 2f, center.Y - h / 2f, w, h);

            using (var pen = new Pen(Color.Black, Math.Max(1f, bounds.Width / 60f)))
            {
                // прямоугольник конверта
                g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
                // "угол" — линии от углов к центру верхней стороны
                g.DrawLine(pen, rect.Left, rect.Top, rect.Left + rect.Width / 2f, rect.Top + rect.Height / 2f);
                g.DrawLine(pen, rect.Right, rect.Top, rect.Left + rect.Width / 2f, rect.Top + rect.Height / 2f);
            }
        }

        private void DrawDoubleCircle(Graphics g, RectangleF rect, Pen pen)
        {
            g.DrawEllipse(pen, rect);
            var inner = RectangleF.Inflate(rect, -6, -6);
            g.DrawEllipse(pen, inner);
        }

        private void DrawOpenEnvelopeIcon(Graphics g, RectangleF bounds)
        {
            // треугольник вниз (как "отправка") — масштабируем
            var center = new PointF(bounds.X + bounds.Width / 2f, bounds.Y + bounds.Height / 2f);
            float w = bounds.Width * 0.5f;
            float h = bounds.Height * 0.35f;

            PointF p1 = new PointF(center.X - w / 2f, center.Y - h / 2f);
            PointF p2 = new PointF(center.X, center.Y + h / 2f);
            PointF p3 = new PointF(center.X + w / 2f, center.Y - h / 2f);

            using (var pen = new Pen(Color.Black, Math.Max(1f, bounds.Width / 60f)))
            {
                g.DrawPolygon(pen, new[] { p1, p2, p3 });
            }
        }

        private void DrawErrorIcon(Graphics g, RectangleF bounds)
        {
            // рисуем "изломанную линию" или молнию, масштабируемо
            float s = Math.Min(bounds.Width, bounds.Height);
            float thickness = Math.Max(1f, s / 30f);
            var cx = bounds.X + bounds.Width * 0.2f;
            var cy = bounds.Y + bounds.Height * 0.2f;
            float w = bounds.Width * 0.6f;
            float h = bounds.Height * 0.5f;

            PointF a = new PointF(bounds.Left + w * 0.05f + cx - bounds.X, bounds.Top + h * 0.1f + cy - bounds.Y);
            // simpler zigzag based on bounds center
            var center = new PointF(bounds.X + bounds.Width / 2f, bounds.Y + bounds.Height / 2f);
            var p = new[]
            {
                new PointF(center.X - w*0.25f, center.Y - h*0.25f),
                new PointF(center.X - w*0.05f, center.Y - h*0.0f),
                new PointF(center.X + w*0.15f, center.Y - h*0.15f),
                new PointF(center.X - w*0.05f, center.Y + h*0.25f)
            };

            using (var pen = new Pen(Color.Black, thickness))
            {
                g.DrawLines(pen, p);
            }
        }

        private void DrawCrossIcon(Graphics g, RectangleF bounds, float thicknessFactor = 0.07f)
        {
            float s = Math.Min(bounds.Width, bounds.Height);
            float thickness = Math.Max(1f, s * thicknessFactor);
            var center = new PointF(bounds.X + bounds.Width / 2f, bounds.Y + bounds.Height / 2f);
            float r = Math.Min(bounds.Width, bounds.Height) * 0.35f;

            using (var pen = new Pen(Color.Black, thickness))
            {
                g.DrawLine(pen, center.X - r, center.Y - r, center.X + r, center.Y + r);
                g.DrawLine(pen, center.X - r, center.Y + r, center.X + r, center.Y - r);
            }
        }

        private void DrawPlusIcon(Graphics g, RectangleF bounds)
        {
            float s = Math.Min(bounds.Width, bounds.Height);
            float thickness = Math.Max(1f, s / 30f);
            var center = new PointF(bounds.X + bounds.Width / 2f, bounds.Y + bounds.Height / 2f);
            float len = Math.Min(bounds.Width, bounds.Height) * 0.35f;

            using (var pen = new Pen(Color.Black, thickness))
            {
                g.DrawLine(pen, center.X, center.Y - len, center.X, center.Y + len);
                g.DrawLine(pen, center.X - len, center.Y, center.X + len, center.Y);
            }
        }
        //Метод отрисовки иконки для боковой панели
        private void DrawSidebarIcon(Graphics g)
        {
            using (var pen = new Pen(Color.Red, 2))
            {
                g.DrawRectangle(pen, Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height);
            }

            if (!Kinis.UI.SidebarIconRegistry.Icons.TryGetValue(Type, out var icon))
                return;

            float pad = Math.Min(Bounds.Width, Bounds.Height) * 0.15f;

            RectangleF r = new RectangleF(
                Bounds.X + pad,
                Bounds.Y + pad,
                Bounds.Width - pad * 2,
                Bounds.Height - pad * 2
            );

            g.DrawImage(icon, r);
        }

        // === Основной метод рисования ===
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
            bool isSidebarBlock = Bounds.Width <= 120 && Bounds.Height <= 60;
            using (var brush = new SolidBrush(Color.White))
            using (var pen = new Pen(BorderColor, 2))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                switch (Type)
                {
                    case "Комментарий":
                        g.FillRectangle(brush, Bounds);
                        g.DrawRectangle(pen, Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height);
                        break;

                    case "Задача":
                        {
                            GraphicsPath taskPath = RoundedRect(Bounds, 12);
                            g.FillPath(brush, taskPath);
                            g.DrawPath(pen, taskPath);
                        }
                        break;

                    case "Развилка":
                        {
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
                        }
                        break;

                    case "Развилка И": // AND gateway — ромб с плюсом
                        {
                            PointF center = new PointF(Bounds.X + Bounds.Width / 2f, Bounds.Y + Bounds.Height / 2f);
                            PointF top = new PointF(center.X, Bounds.Y);
                            PointF right = new PointF(Bounds.Right, center.Y);
                            PointF bottom = new PointF(center.X, Bounds.Bottom);
                            PointF left = new PointF(Bounds.X, center.Y);
                            PointF[] diamond = new PointF[] { top, right, bottom, left };
                            g.FillPolygon(brush, diamond);
                            g.DrawPolygon(pen, diamond);
                            // плюс внутри
                            DrawPlusIcon(g, Bounds);
                        }
                        break;

                    case "Начальное событие":
                        {
                            g.FillEllipse(brush, Bounds);
                            using (var thinPen = new Pen(BorderColor, Math.Max(1f, Math.Min(Bounds.Width, Bounds.Height) / 30f)))
                            {
                                g.DrawEllipse(thinPen, Bounds);
                            }
                        }
                        break;

                    case "Промежуточное событие":
                        {
                            g.FillEllipse(brush, Bounds);
                            g.DrawEllipse(pen, Bounds);
                            RectangleF innerCircle = RectangleF.Inflate(Bounds, -Math.Max(3f, Math.Min(Bounds.Width, Bounds.Height) * 0.07f), -Math.Max(3f, Math.Min(Bounds.Width, Bounds.Height) * 0.07f));
                            g.DrawEllipse(pen, innerCircle);
                        }
                        break;

                    case "Конечное событие":
                        {
                            g.FillEllipse(brush, Bounds);
                            using (var thickPen = new Pen(BorderColor, Math.Max(2f, Math.Min(Bounds.Width, Bounds.Height) / 15f)))
                                g.DrawEllipse(thickPen, Bounds);
                        }
                        break;

                    case "Объект данных":
                        {
                            GraphicsPath dataPath = new GraphicsPath();
                            float fold = Math.Min(10f, Bounds.Width * 0.12f);
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
                        }
                        break;

                    case "Хранилище данных":
                        {
                            float curve = Bounds.Height / 3f;

                            using (GraphicsPath path = new GraphicsPath())
                            {
                                path.AddArc(Bounds.X, Bounds.Y, Bounds.Width, curve, 180, 180);

                                path.AddLine(
                                    Bounds.Right,
                                    Bounds.Y + curve / 2f,
                                    Bounds.Right,
                                    Bounds.Bottom - curve / 2f
                                );

                                path.AddArc(
                                    Bounds.X,
                                    Bounds.Bottom - curve,
                                    Bounds.Width,
                                    curve,
                                    0,
                                    180
                                );

                                path.AddLine(
                                    Bounds.X,
                                    Bounds.Bottom - curve / 2f,
                                    Bounds.X,
                                    Bounds.Y + curve / 2f
                                );

                                path.CloseFigure();

                                g.FillPath(brush, path);
                                g.DrawPath(pen, path);

                                // ✅ Внешний верхний эллипс
                                g.DrawEllipse(
                                    pen,
                                    Bounds.X,
                                    Bounds.Y,
                                    Bounds.Width,
                                    curve
                                );

                                // ✅ Внутренний нижний эллипс
                                var innerBottom = new RectangleF(
                                    Bounds.X + 2,
                                    Bounds.Bottom - curve + 2,
                                    Bounds.Width - 4,
                                    curve - 4
                                );

                                using (var thinPen = new Pen(BorderColor, 1))
                                {
                                    g.DrawEllipse(thinPen, innerBottom);
                                }
                            }
                        }
                        break;

                    case "Событие-получение сообщения":
                        {
                            // круг + иконка конверта (в середине)
                            g.FillEllipse(brush, Bounds);
                            g.DrawEllipse(pen, Bounds);
                            DrawEnvelopeIcon(g, Bounds);
                        }
                        break;

                    case "Событие-получение сообщения (промежуточное)":
                        {
                            g.FillEllipse(brush, Bounds);
                            g.DrawEllipse(pen, Bounds);
                            RectangleF innerCircle = RectangleF.Inflate(Bounds, -Math.Max(3f, Math.Min(Bounds.Width, Bounds.Height) * 0.07f), -Math.Max(3f, Math.Min(Bounds.Width, Bounds.Height) * 0.07f));
                            g.DrawEllipse(pen, innerCircle);

                            using (var p = new Pen(Color.Black, 2))
                            {
                                float mx = Bounds.X + Bounds.Width / 2f;
                                float my = Bounds.Y + Bounds.Height / 2f;

                                float w = Bounds.Width * 0.5f;
                                float h = Bounds.Height * 0.35f;

                                RectangleF env = new RectangleF(
                                    mx - w / 2,
                                    my - h / 2,
                                    w,
                                    h
                                );

                                g.DrawRectangle(p, env.X, env.Y, env.Width, env.Height);
                                g.DrawLine(p, env.Left, env.Top, mx, env.Top + env.Height / 2);
                                g.DrawLine(p, env.Right, env.Top, mx, env.Top + env.Height / 2);
                            }
                        }
                        break;

                    case "Событие-отправка сообщения":
                        {
                            // круг с более толстой линией и иконкой "отправки" (треугольник)
                            using (var thick = new Pen(BorderColor, Math.Max(2f, Math.Min(Bounds.Width, Bounds.Height) / 15f)))
                                g.DrawEllipse(thick, Bounds);
                            DrawOpenEnvelopeIcon(g, Bounds);
                            g.FillEllipse(brush, Bounds);
                            g.DrawEllipse(pen, Bounds);
                            float mx = Bounds.X + Bounds.Width / 2f;
                            float my = Bounds.Y + Bounds.Height / 2f;

                            float w = Bounds.Width * 0.5f;
                            float h = Bounds.Height * 0.35f;

                            RectangleF env = new RectangleF(
                                mx - w / 2,
                                my - h / 2,
                                w,
                                h
                            );

                            using (var b = new SolidBrush(Color.Black))
                            using (var wp = new Pen(Color.White, 2))
                            {
                                g.FillRectangle(b, env);

                                g.DrawLine(wp, env.Left, env.Top, mx, env.Top + env.Height / 2);
                                g.DrawLine(wp, env.Right, env.Top, mx, env.Top + env.Height / 2);
                            }
                        }
                        break;

                    case "Событие-отправка сообщения (промежуточное)":
                        {
                            g.FillEllipse(brush, Bounds);
                            g.DrawEllipse(pen, Bounds);
                            RectangleF innerCircle = RectangleF.Inflate(Bounds, -Math.Max(3f, Math.Min(Bounds.Width, Bounds.Height) * 0.07f), -Math.Max(3f, Math.Min(Bounds.Width, Bounds.Height) * 0.07f));
                            g.DrawEllipse(pen, innerCircle);

                            float mx = Bounds.X + Bounds.Width / 2f;
                            float my = Bounds.Y + Bounds.Height / 2f;

                            float w = Bounds.Width * 0.5f;
                            float h = Bounds.Height * 0.35f;

                            RectangleF env = new RectangleF(
                                mx - w / 2,
                                my - h / 2,
                                w,
                                h
                            );

                            using (var b = new SolidBrush(Color.Black))
                            using (var wp = new Pen(Color.White, 2))
                            {
                                g.FillRectangle(b, env);

                                g.DrawLine(wp, env.Left, env.Top, mx, env.Top + env.Height / 2);
                                g.DrawLine(wp, env.Right, env.Top, mx, env.Top + env.Height / 2);
                            }
                        }
                        break;

                    case "Событие-ошибка обработчик":
                        {
                            g.FillEllipse(brush, Bounds);
                            g.DrawEllipse(pen, Bounds);
                            RectangleF innerCircle = RectangleF.Inflate(Bounds, -Math.Max(3f, Math.Min(Bounds.Width, Bounds.Height) * 0.07f), -Math.Max(3f, Math.Min(Bounds.Width, Bounds.Height) * 0.07f));
                            g.DrawEllipse(pen, innerCircle);
                            DrawErrorIcon(g, Bounds);
                        }
                        break;

                    case "Событие-ошибка инициатор":
                        {
                            g.FillEllipse(brush, Bounds);
                            using (var thick = new Pen(BorderColor, Math.Max(2f, Math.Min(Bounds.Width, Bounds.Height) / 15f)))
                                g.DrawEllipse(thick, Bounds);
                            DrawErrorIcon(g, Bounds);
                        }
                        break;

                    case "Событие-отмена обработчик":
                        {
                            g.FillEllipse(brush, Bounds);
                            g.DrawEllipse(pen, Bounds);
                            RectangleF innerCircle = RectangleF.Inflate(Bounds, -Math.Max(3f, Math.Min(Bounds.Width, Bounds.Height) * 0.07f), -Math.Max(3f, Math.Min(Bounds.Width, Bounds.Height) * 0.07f));
                            g.DrawEllipse(pen, innerCircle);
                            DrawCrossIcon(g, Bounds, 0.06f);
                        }
                        break;

                    case "Событие-отмена инициатор":
                        {
                            g.FillEllipse(brush, Bounds);
                            using (var thick = new Pen(BorderColor, Math.Max(2f, Math.Min(Bounds.Width, Bounds.Height) / 15f)))
                                g.DrawEllipse(thick, Bounds);
                            DrawCrossIcon(g, Bounds, 0.06f);
                        }
                        break;

                    case "Событие-остановка":
                        {
                            g.FillEllipse(brush, Bounds);

                            using (var thick = new Pen(BorderColor, 4))
                                g.DrawEllipse(thick, Bounds);

                            float size = Bounds.Width * 0.45f;

                            RectangleF inner = new RectangleF(
                                Bounds.X + Bounds.Width / 2 - size / 2,
                                Bounds.Y + Bounds.Height / 2 - size / 2,
                                size,
                                size
                            );

                            using (var b = new SolidBrush(Color.Black))
                            using (var p = new Pen(Color.Black, 2))
                            {
                                g.FillEllipse(b, inner);
                                g.DrawEllipse(p, inner);
                            }
                        }
                        break;

                    default:
                        g.FillRectangle(brush, Bounds);
                        g.DrawRectangle(pen, Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height);
                        break;
                }
            } // using brush/pen

            // ===================== ТЕКСТ ВНУТРИ БЛОКА =====================
            if (!NoTextTypes.Contains(Type) && !string.IsNullOrWhiteSpace(Text))
            {
                using (var font = new Font("Segoe UI", 8, FontStyle.Regular))
                using (var brush = new SolidBrush(Color.Black))
                {
                    var format = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center,
                        Trimming = StringTrimming.EllipsisWord,
                        FormatFlags = StringFormatFlags.LineLimit
                    };

                    RectangleF rect = new RectangleF(
                        Bounds.X + 4,
                        Bounds.Y + 4,
                        Bounds.Width - 8,
                        Bounds.Height - 8
                    );

                    // 🔴 КЛЮЧЕВОЕ УСЛОВИЕ
                    SizeF size = g.MeasureString(Text, font, (int)rect.Width);

                    if (size.Height <= rect.Height)
                    {
                        g.DrawString(Text, font, brush, rect, format);
                    }
                    // иначе — вообще НЕ рисуем
                }
            }

            if (isSelected)
            {
                DrawHandles(g);
                DrawConnectionPoints(g);

                using (var highlight = new Pen(Color.DeepSkyBlue, 2))
                {
                    g.DrawRectangle(
                        highlight,
                        Bounds.X - 2,
                        Bounds.Y - 2,
                        Bounds.Width + 4,
                        Bounds.Height + 4
                    );
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

        // В классе BpmnBlock добавим метод для изменения размера пула с обновлением дорожек:
        public void ResizePool(float newWidth, float newHeight)
        {
            // Сохраняем старые границы
            RectangleF oldBounds = Bounds;

            // Обновляем границы пула
            Bounds = new RectangleF(Bounds.X, Bounds.Y, newWidth, newHeight);

            // Если высота уменьшилась, проверяем и обновляем высоту дорожек
            if (newHeight < oldBounds.Height && PoolLanes != null)
            {
                ValidateLanePositions();
            }

            // Если ширина изменилась, обновляем ширину дорожек
            if (newWidth != oldBounds.Width && PoolLanes != null)
            {
                UpdateLanesWidth(newWidth);
            }
        }

        // Добавим метод для обновления ширины дорожек:
        private void UpdateLanesWidth(float poolWidth)
        {
            if (PoolLanes == null) return;

            float nameStripWidth = 40f; // Ширина полосы названия пула
            float bodyWidth = poolWidth - nameStripWidth; // Ширина тела пула

            foreach (var lane in PoolLanes)
            {
                UpdateLaneWidthRecursive(lane, bodyWidth);
            }
        }

        private void UpdateLaneWidthRecursive(PoolLine lane, float availableWidth)
        {
            // Рассчитываем ширину с учетом вложенности
            float laneBodyWidth = availableWidth - (lane.NestingLevel * 20f);

            lane.Bounds = new RectangleF(
                lane.Bounds.X,
                lane.Bounds.Y,
                lane.NameStripWidth + laneBodyWidth,
                lane.Bounds.Height
            );

            // Рекурсивно обновляем вложенные дорожки
            if (lane.ChildLines != null)
            {
                foreach (var child in lane.ChildLines)
                {
                    UpdateLaneWidthRecursive(child, availableWidth - 20f);
                }
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
                Bounds.Y, // Без отступа
                Bounds.Width - nameStripWidth,
                Bounds.Height
            );

            // 1. Заливка только полосы названия (белый)
            using (var nameStripBrush = new SolidBrush(Color.White))
            {
                g.FillRectangle(nameStripBrush, nameStripRect);
            }

            // 2. Контур пула
            using (var poolPen = new Pen(BorderColor, 1f))
            {
                g.DrawRectangle(poolPen,
                    nameStripRect.X,
                    nameStripRect.Y,
                    nameStripRect.Width,
                    nameStripRect.Height);

                g.DrawRectangle(poolPen,
                    bodyRect.X,
                    bodyRect.Y,
                    bodyRect.Width,
                    bodyRect.Height);

                g.DrawLine(poolPen,
                    nameStripRect.Right,
                    nameStripRect.Top,
                    nameStripRect.Right,
                    nameStripRect.Bottom);
            }

            // 3. Вертикальный текст названия пула (центрируем по всей высоте)
            using (var nameFont = new Font("Segoe UI", 10, FontStyle.Bold))
            using (var nameBrush = new SolidBrush(Color.Black))
            using (var format = new StringFormat())
            {
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;

                var state = g.Save();

                g.TranslateTransform(
                    nameStripRect.X + nameStripRect.Width / 2,
                    nameStripRect.Y + nameStripRect.Height / 2
                );
                g.RotateTransform(-90);
                g.DrawString(Text, nameFont, nameBrush, 0, 0, format);

                g.Restore(state);
            }

            // 4. Отрисовка дорожек пула
            DrawPoolLanes(g);

            // 5. Обработка выделения
            if (isSelected)
            {
                using (var highlightPen = new Pen(Color.DeepSkyBlue, 2f))
                {
                    highlightPen.DashStyle = DashStyle.Dash;
                    RectangleF highlightRect = new RectangleF(
                        Bounds.X - 2,
                        Bounds.Y - 2,
                        Bounds.Width + 4,
                        Bounds.Height + 4
                    );
                    g.DrawRectangle(highlightPen,
                        highlightRect.X,
                        highlightRect.Y,
                        highlightRect.Width,
                        highlightRect.Height);
                }

                foreach (var handle in GetResizeHandles())
                {
                    using (var handleBrush = new SolidBrush(Color.Blue))
                    {
                        g.FillRectangle(handleBrush, handle);
                    }
                }

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
                    DrawLaneWithDrawIOStyle(g, lane, false);
                }
            }
            finally
            {
                // Восстанавливаем состояние
                g.Restore(state);
            }
        }

        // Новый метод для отрисовки дорожки в стиле draw.io
        private void DrawLaneWithDrawIOStyle(Graphics g, PoolLine lane, bool isNested)
        {
            // Получаем границы полосы названия и тела
            RectangleF nameStripRect = lane.GetNameStripBounds();
            RectangleF bodyRect = lane.GetBodyBounds();
            RectangleF outlineRect = lane.GetOutlineBounds();

            // 1. Рисуем полосу названия (белый фон) - занимает всю высоту дорожки
            using (var nameBrush = new SolidBrush(lane.NameStripBackgroundColor))
            {
                g.FillRectangle(nameBrush, nameStripRect);
            }

            // 2. Рисуем контур полосы названия
            using (var namePen = new Pen(lane.BorderColor, lane.BorderWidth))
            {
                g.DrawRectangle(namePen,
                    nameStripRect.X,
                    nameStripRect.Y,
                    nameStripRect.Width,
                    nameStripRect.Height);
            }

            // 3. Если дорожка не прозрачная, рисуем фон тела
            if (!lane.IsTransparent)
            {
                using (var bodyBrush = new SolidBrush(lane.BackgroundColor))
                {
                    g.FillRectangle(bodyBrush, bodyRect);
                }
            }

            // 4. Рисуем контур тела дорожки (каркас)
            using (var outlinePen = new Pen(lane.BorderColor, lane.BorderWidth))
            {
                // Для каркасного стиля рисуем только внешний контур
                if (lane.IsTransparent)
                {
                    // Рисуем полный прямоугольник контура
                    g.DrawRectangle(outlinePen,
                        outlineRect.X,
                        outlineRect.Y,
                        outlineRect.Width,
                        outlineRect.Height);

                    // Рисуем вертикальную линию между полосой названия и телом
                    g.DrawLine(outlinePen,
                        nameStripRect.Right,
                        nameStripRect.Top,
                        nameStripRect.Right,
                        nameStripRect.Bottom);
                }
                else
                {
                    // Для обычного стиля рисуем только контур тела
                    g.DrawRectangle(outlinePen,
                        bodyRect.X,
                        bodyRect.Y,
                        bodyRect.Width,
                        bodyRect.Height);
                }
            }

            // 5. Рисуем вертикальный текст в полосе названия (с учетом всей высоты)
            DrawVerticalLaneText(g, lane, nameStripRect);

            // 6. Рисуем вложенные дорожки
            if (lane.ChildLines != null && lane.ChildLines.Count > 0)
            {
                // Для вложенных дорожек смещаемся вправо
                float nestedStartX = nameStripRect.Right + (isNested ? 10f : 20f);

                foreach (var childLane in lane.ChildLines)
                {
                    // Для вложенных дорожек уменьшаем ширину полосы названия
                    childLane.NameStripWidth = 30f;
                    childLane.IsTransparent = true; // Вложенные дорожки всегда прозрачные

                    DrawLaneWithDrawIOStyle(g, childLane, true);
                }
            }
        }

        // Отрисовка вертикального текста
        private void DrawVerticalLaneText(Graphics g, PoolLine lane, RectangleF nameStripRect)
        {
            // Сохраняем состояние графики
            var state = g.Save();

            try
            {
                // Рассчитываем область для текста (немного меньше полосы названия для отступов)
                float textPadding = 5f;
                RectangleF textArea = new RectangleF(
                    nameStripRect.X + textPadding,
                    nameStripRect.Y + textPadding,
                    nameStripRect.Width - 2 * textPadding,
                    nameStripRect.Height - 2 * textPadding
                );

                // Перемещаем начало координат в центр области текста
                g.TranslateTransform(
                    textArea.X + textArea.Width / 2,
                    textArea.Y + textArea.Height / 2
                );

                // Поворачиваем на 90 градусов против часовой стрелки
                g.RotateTransform(-90);

                // Рисуем текст с учетом высоты
                using (var laneFont = new Font("Segoe UI", 9, FontStyle.Bold))
                using (var textBrush = new SolidBrush(Color.Black))
                using (var format = new StringFormat())
                {
                    format.Alignment = StringAlignment.Center;
                    format.LineAlignment = StringAlignment.Center;
                    format.Trimming = StringTrimming.Word; // Обрезаем текст, если не помещается
                    format.FormatFlags = StringFormatFlags.LineLimit;

                    // Создаем прямоугольник для текста (после поворота оси поменялись местами)
                    // Ширина текстовой области = высота исходного прямоугольника (после поворота это станет высотой)
                    // Высота текстовой области = ширина исходного прямоугольника (после поворота это станет шириной)
                    RectangleF textRect = new RectangleF(
                        -textArea.Height / 2,  // Центрируем по новой оси X (бывшая Y)
                        -textArea.Width / 2,   // Центрируем по новой оси Y (бывшая X)
                        textArea.Height,       // Высота текстовой области = высоте исходного прямоугольника
                        textArea.Width         // Ширина текстовой области = ширине исходного прямоугольника
                    );

                    g.DrawString(lane.Text, laneFont, textBrush, textRect, format);
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

            // Очищаем существующие дорожки
            PoolLanes.Clear();

            // Создаем три дорожки по умолчанию
            for (int i = 1; i <= 3; i++)
            {
                AddPoolLane($"Дорожка {i}");
            }
        }

        public void AddPoolLane(string laneName)
        {
            if (PoolLanes == null)
                PoolLanes = new List<PoolLine>();

            float laneHeight = 60f;
            float startY = Bounds.Y; // УБИРАЕМ отступ в 40px - теперь с самого верха пула

            if (PoolLanes.Count > 0)
            {
                var lastLane = PoolLanes[PoolLanes.Count - 1];
                startY = lastLane.Bounds.Bottom;
            }

            var newLane = new PoolLine
            {
                Text = laneName,
                Bounds = new RectangleF(
                    Bounds.X + 40f, // Отступ от левого края (ширина полосы названия пула)
                    startY,
                    Bounds.Width - 40f, // Ширина пула минус полоса названия
                    laneHeight
                ),
                FillColor = Color.Transparent,
                BackgroundColor = Color.Transparent,
                NameStripBackgroundColor = Color.White,
                BorderColor = Color.Black,
                BorderWidth = 1f,
                IsTransparent = true,
                NameStripWidth = 40f,
                NestingLevel = 0,
                ParentLine = null
            };

            PoolLanes.Add(newLane);

            // Пересчитываем высоту пула
            UpdatePoolHeight();
        }
        private void UpdatePoolHeight()
        {
            if (PoolLanes == null || PoolLanes.Count == 0)
            {
                // Минимальная высота пула - 200px (вместо 120px)
                Bounds = new RectangleF(
                    Bounds.X,
                    Bounds.Y,
                    Bounds.Width,
                    200f
                );
                return;
            }

            // Находим самую нижнюю точку среди всех дорожек
            float maxBottom = Bounds.Y; // УБИРАЕМ отступ в 40px

            foreach (var lane in PoolLanes)
            {
                float laneBottom = GetLaneBottomRecursive(lane);
                if (laneBottom > maxBottom)
                    maxBottom = laneBottom;
            }

            // Обновляем высоту пула
            float newHeight = maxBottom - Bounds.Y + 20f; // Добавляем небольшой отступ снизу
            Bounds = new RectangleF(
                Bounds.X,
                Bounds.Y,
                Bounds.Width,
                Math.Max(200f, newHeight) // Минимум 200px
            );
        }

        private float GetLaneBottomRecursive(PoolLine lane)
        {
            float bottom = lane.Bounds.Bottom;

            if (lane.ChildLines != null && lane.ChildLines.Count > 0)
            {
                foreach (var child in lane.ChildLines)
                {
                    float childBottom = GetLaneBottomRecursive(child);
                    if (childBottom > bottom)
                        bottom = childBottom;
                }
            }

            return bottom;
        }

        public void ValidateLanePositions()
        {
            if (Type != "Пул" || PoolLanes == null) return;

            float poolNameStripWidth = 40f;
            float minX = Bounds.X + poolNameStripWidth;
            float maxX = Bounds.Right;
            float minY = Bounds.Y; // Было: Bounds.Y + 40f
            float maxY = Bounds.Bottom;

            foreach (var lane in PoolLanes)
            {
                ValidateLanePositionRecursive(lane, minX, maxX, minY, maxY);
            }
        }


        private void ValidateLanePositionRecursive(PoolLine lane, float minX, float maxX, float minY, float maxY)
        {
            // Для вложенных дорожек учитываем родительские границы
            if (lane.ParentLine != null)
            {
                minX = lane.ParentLine.Bounds.X + lane.ParentLine.NameStripWidth;
                maxX = lane.ParentLine.Bounds.Right;
                minY = lane.ParentLine.Bounds.Y;
                maxY = lane.ParentLine.Bounds.Bottom;
            }

            // Ограничиваем позицию дорожки
            if (lane.Bounds.X < minX)
                lane.Bounds = new RectangleF(minX, lane.Bounds.Y, lane.Bounds.Width, lane.Bounds.Height);

            if (lane.Bounds.Right > maxX)
            {
                float newWidth = Math.Max(lane.NameStripWidth + 20, maxX - lane.Bounds.X);
                lane.Bounds = new RectangleF(lane.Bounds.X, lane.Bounds.Y, newWidth, lane.Bounds.Height);
            }

            if (lane.Bounds.Y < minY)
                lane.Bounds = new RectangleF(lane.Bounds.X, minY, lane.Bounds.Width, lane.Bounds.Height);

            // ВЫСОТА: ограничиваем высоту дорожки, если она выходит за нижнюю границу
            if (lane.Bounds.Bottom > maxY)
            {
                float newHeight = Math.Max(LANE_MIN_HEIGHT, maxY - lane.Bounds.Y);
                lane.Bounds = new RectangleF(
                    lane.Bounds.X,
                    lane.Bounds.Y,
                    lane.Bounds.Width,
                    newHeight
                );
            }

            // Проверяем, чтобы дорожка не выходила за границы по ширине (с учетом полосы названия)
            if (lane.Bounds.Width < lane.NameStripWidth + 20)
            {
                lane.Bounds = new RectangleF(
                    lane.Bounds.X,
                    lane.Bounds.Y,
                    lane.NameStripWidth + 20,
                    lane.Bounds.Height
                );
            }

            // Рекурсивно проверяем дочерние дорожки
            if (lane.ChildLines != null)
            {
                foreach (var child in lane.ChildLines)
                {
                    ValidateLanePositionRecursive(child, minX, maxX, minY, maxY);
                }
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
            float currentY = Bounds.Y; 

            for (int i = 0; i < PoolLanes.Count; i++)
            {
                var lane = PoolLanes[i];

                // Если у дорожки есть сохраненная относительная позиция, используем ее
                if (lane.HasRelativePosition)
                {
                    // Применяем относительную позицию относительно нового положения пула
                    // Полоса названия пула (40px) + смещение дорожки
                    lane.Bounds = new RectangleF(
                        Bounds.X + 40f + lane.RelativeX,
                        Bounds.Y + lane.RelativeY,
                        lane.Bounds.Width,
                        lane.Bounds.Height
                    );
                }
                else
                {
                    // Стандартное позиционирование (дорожки не перемещались)
                    lane.Bounds = new RectangleF(
                        Bounds.X + 40f,        // Фиксированный отступ от левого края пула
                        currentY,              // Абсолютная позиция Y (без отступа в 40px)
                        Bounds.Width - 40f,    // Ширина по размеру пула
                        lane.Bounds.Height     // Сохраняем высоту
                    );
                    currentY += lane.Bounds.Height; // Следующая дорожка ниже
                }
            }
        }
    }
}