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
        public Color FillColor { get; set; } = Color.White; // по умолчанию белый фон
        public Color BorderColor { get; set; } = Color.Black; // контур — чёрный

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

        // === Основной метод рисования ===
        public void Draw(Graphics g, bool isSelected)
        {
            // используем FillColor — теперь фон у всех фигур одинаковый (по умолчанию белый)
            using (var brush = new SolidBrush(FillColor))
            using (var pen = new Pen(BorderColor, Math.Max(1f, Math.Min(Bounds.Width, Bounds.Height) / 30f)))
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
                            RectangleF ellipseRect = Bounds;
                            float curve = Bounds.Height / 3f;
                            g.FillRectangle(brush, ellipseRect);
                            g.DrawEllipse(pen, ellipseRect.X, ellipseRect.Y, ellipseRect.Width, curve);
                            g.DrawEllipse(pen, ellipseRect.X, ellipseRect.Bottom - curve, ellipseRect.Width, curve);
                            g.DrawLine(pen, ellipseRect.X, ellipseRect.Y + curve / 2f, ellipseRect.X, ellipseRect.Bottom - curve / 2f);
                            g.DrawLine(pen, ellipseRect.Right, ellipseRect.Y + curve / 2f, ellipseRect.Right, ellipseRect.Bottom - curve / 2f);
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

            // Текст внизу (если есть) — рисуем поверх, но обычно для событий текст не нужен
            using (var font = new Font("Segoe UI", Math.Max(8f, Math.Min(Bounds.Height / 6f, 12f)), FontStyle.Regular))
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
                using (var highlight = new Pen(Color.DeepSkyBlue, Math.Max(2f, Math.Min(Bounds.Width, Bounds.Height) / 50f)))
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
