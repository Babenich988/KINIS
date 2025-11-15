using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Kinis.Models
{
    [Serializable]
    public class BpmnArrow
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Text { get; set; } = "";

        // Ссылки на блоки и точки привязки
        public BpmnBlock StartBlock { get; set; }
        public PointF StartPoint { get; set; }
        public BpmnBlock EndBlock { get; set; }
        public PointF EndPoint { get; set; }

        // Флаги привязки
        public bool IsStartAttached => StartBlock != null;
        public bool IsEndAttached => EndBlock != null;
        public bool IsFullyAttached => IsStartAttached && IsEndAttached;
        public bool IsFloating { get; set; }

        // Визуальные свойства
        public Color Color { get; set; } = Color.Black;
        public float Width { get; set; } = 2f;

        public BpmnArrow() { }

        public BpmnArrow(BpmnBlock startBlock, PointF startPoint, BpmnBlock endBlock, PointF endPoint)
        {
            StartBlock = startBlock;
            StartPoint = startPoint;
            EndBlock = endBlock;
            EndPoint = endPoint;
        }

        //Проверяем попадает ли точка на стрелку
        public bool HitTest(PointF point, float tolerance = 5f)
        {
            return DistanceToLine(point, StartPoint, EndPoint) <= tolerance;
        }

        //Проверка попадания на маркеры концов
        public bool HitTestEndpoint(PointF point, bool startEndpoint, float tolerance = 6f)
        {
            PointF endpoint = startEndpoint ? StartPoint : EndPoint;
            float dx = point.X - endpoint.X;
            float dy = point.Y - endpoint.Y;
            return Math.Sqrt(dx * dx + dy * dy) <= tolerance;
        }

        //Метод нахождения расстояния до стрелки
        private float DistanceToLine(PointF point, PointF lineStart, PointF lineEnd)
        {
            float A = point.X - lineStart.X;
            float B = point.Y - lineStart.Y;
            float C = lineEnd.X - lineStart.X;
            float D = lineEnd.Y - lineStart.Y;

            float dot = A * C + B * D;
            float lenSq = C * C + D * D;
            float param = (lenSq != 0) ? dot / lenSq : -1;

            float xx, yy;

            if (param < 0)
            {
                xx = lineStart.X;
                yy = lineStart.Y;
            }
            else if (param > 1)
            {
                xx = lineEnd.X;
                yy = lineEnd.Y;
            }
            else
            {
                xx = lineStart.X + param * C;
                yy = lineStart.Y + param * D;
            }

            float dx = point.X - xx;
            float dy = point.Y - yy;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        // ДОБАВЛЯЕМ: Промежуточные точки для ломаной линии
        public List<PointF> ConnectionPoints { get; set; } = new List<PointF>();

        /// <summary>
        /// Вычисляет ортогональный путь для стрелки
        /// </summary>
        public void CalculateOrthogonalPath()
        {
            ConnectionPoints.Clear();

            // Базовая логика: горизонтально-вертикально-горизонтально
            if (IsStartAttached && IsEndAttached)
            {
                CalculateAttachedPath();
            }
            else
            {
                CalculateSimplePath();
            }
        }

        private void CalculateSimplePath()
        {
            // Простой путь для непривязанных стрелок
            float midX = (StartPoint.X + EndPoint.X) / 2;

            ConnectionPoints.Add(StartPoint);
            ConnectionPoints.Add(new PointF(midX, StartPoint.Y)); // горизонтальный сегмент
            ConnectionPoints.Add(new PointF(midX, EndPoint.Y));   // вертикальный сегмент  
            ConnectionPoints.Add(EndPoint);
        }

        private void CalculateAttachedPath()
        {
            // Умный путь для привязанных стрелок
            ConnectionPoints.Add(StartPoint);

            // Определяем направление относительно блоков
            bool startOnLeft = StartPoint.X <= StartBlock.Bounds.Left;
            bool startOnRight = StartPoint.X >= StartBlock.Bounds.Right;
            bool startOnTop = StartPoint.Y <= StartBlock.Bounds.Top;
            bool startOnBottom = StartPoint.Y >= StartBlock.Bounds.Bottom;

            bool endOnLeft = EndPoint.X <= EndBlock.Bounds.Left;
            bool endOnRight = EndPoint.X >= EndBlock.Bounds.Right;
            bool endOnTop = EndPoint.Y <= EndBlock.Bounds.Top;
            bool endOnBottom = EndPoint.Y >= EndBlock.Bounds.Bottom;

            // Базовая логика маршрутизации
            if (startOnRight && endOnLeft)
            {
                // Блоки рядом по горизонтали
                float midY = (StartPoint.Y + EndPoint.Y) / 2;
                ConnectionPoints.Add(new PointF(StartPoint.X + 20, StartPoint.Y));
                ConnectionPoints.Add(new PointF(StartPoint.X + 20, midY));
                ConnectionPoints.Add(new PointF(EndPoint.X - 20, midY));
                ConnectionPoints.Add(new PointF(EndPoint.X - 20, EndPoint.Y));
            }
            else if (startOnBottom && endOnTop)
            {
                // Блоки рядом по вертикали
                float midX = (StartPoint.X + EndPoint.X) / 2;
                ConnectionPoints.Add(new PointF(StartPoint.X, StartPoint.Y + 20));
                ConnectionPoints.Add(new PointF(midX, StartPoint.Y + 20));
                ConnectionPoints.Add(new PointF(midX, EndPoint.Y - 20));
                ConnectionPoints.Add(new PointF(EndPoint.X, EndPoint.Y - 20));
            }
            else
            {
                // Сложный случай - используем простой путь
                CalculateSimplePath();
            }

            ConnectionPoints.Add(EndPoint);
        }

        public void Draw(Graphics g, bool isSelected = false)
        {
            // ВЫЧИСЛЯЕМ ПУТЬ ПЕРЕД ОТРИСОВКОЙ
            CalculateOrthogonalPath();

            using (var pen = new Pen(isSelected ? Color.Blue : Color, isSelected ? Width + 1 : Width))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;

                // РИСУЕМ ЛОМАНУЮ ЛИНИЮ вместо прямой
                if (ConnectionPoints.Count >= 2)
                {
                    // Рисуем все сегменты пути
                    for (int i = 0; i < ConnectionPoints.Count - 1; i++)
                    {
                        g.DrawLine(pen, ConnectionPoints[i], ConnectionPoints[i + 1]);
                    }
                }
                else
                {
                    // Fallback: рисуем прямую линию
                    g.DrawLine(pen, StartPoint, EndPoint);
                }

                // РИСУЕМ НАКОНЕЧНИК НА КОНЕЧНОЙ ТОЧКЕ
                DrawArrowhead(g, isSelected);
            }

            // РИСУЕМ МАРКЕРЫ КОНЦОВ ЕСЛИ СТРЕЛКА ВЫДЕЛЕНА
            if (isSelected)
            {
                DrawEndpointMarkers(g);
            }
        }

        //Метод для отрисовки маркеров концов
        private void DrawEndpointMarkers(Graphics g)
        {
            // Маркер начальной точки (зеленый если привязан, красный если свободен)
            using (var brush = new SolidBrush(IsStartAttached ? Color.Green : Color.Red))
            {
                g.FillEllipse(brush, StartPoint.X - 4, StartPoint.Y - 4, 8, 8);
                g.DrawEllipse(Pens.White, StartPoint.X - 4, StartPoint.Y - 4, 8, 8);
            }

            // Маркер конечной точки (зеленый если привязан, красный если свободен)
            using (var brush = new SolidBrush(IsEndAttached ? Color.Green : Color.Red))
            {
                g.FillEllipse(brush, EndPoint.X - 4, EndPoint.Y - 4, 8, 8);
                g.DrawEllipse(Pens.White, EndPoint.X - 4, EndPoint.Y - 4, 8, 8);
            }
        }

        private void DrawArrowhead(Graphics g, bool isSelected)
        {
            if (ConnectionPoints.Count < 2) return;

            // БЕРЕМ ПОСЛЕДНИЕ ДВЕ ТОЧКИ ДЛЯ ОПРЕДЕЛЕНИЯ НАПРАВЛЕНИЯ
            PointF lineEnd = ConnectionPoints[ConnectionPoints.Count - 1];
            PointF lineStart = ConnectionPoints[ConnectionPoints.Count - 2];

            // Вычисляем направление из последнего сегмента
            float dx = lineEnd.X - lineStart.X;
            float dy = lineEnd.Y - lineStart.Y;
            float length = (float)Math.Sqrt(dx * dx + dy * dy);

            if (length == 0) return;

            // Нормализуем направление
            dx /= length;
            dy /= length;

            // Размер наконечника
            float arrowSize = 10f;

            // Сдвигаем наконечник назад от конечной точки
            float offset = -Width;
            PointF arrowTip = new PointF(
                lineEnd.X - dx * offset,
                lineEnd.Y - dy * offset
            );

            // Угол наконечника (в радианах)
            float arrowAngle = (float)(30 * Math.PI / 180);

            // Левая точка треугольника
            PointF leftPoint = new PointF(
                (float)(arrowTip.X - arrowSize * Math.Cos(arrowAngle) * dx + arrowSize * Math.Sin(arrowAngle) * dy),
                (float)(arrowTip.Y - arrowSize * Math.Cos(arrowAngle) * dy - arrowSize * Math.Sin(arrowAngle) * dx)
            );

            // Правая точка треугольника  
            PointF rightPoint = new PointF(
                (float)(arrowTip.X - arrowSize * Math.Cos(arrowAngle) * dx - arrowSize * Math.Sin(arrowAngle) * dy),
                (float)(arrowTip.Y - arrowSize * Math.Cos(arrowAngle) * dy + arrowSize * Math.Sin(arrowAngle) * dx)
            );

            // Создаем путь для наконечника
            using (var arrowPath = new GraphicsPath())
            {
                arrowPath.AddLine(arrowTip, leftPoint);
                arrowPath.AddLine(leftPoint, rightPoint);
                arrowPath.AddLine(rightPoint, arrowTip);
                arrowPath.CloseFigure();

                // ЗАЛИВАЕМ наконечник
                using (var brush = new SolidBrush(isSelected ? Color.Blue : Color))
                {
                    g.FillPath(brush, arrowPath);
                }

                // Обводим контур
                using (var outlinePen = new Pen(isSelected ? Color.DarkBlue : Color.DarkGray, 1))
                {
                    g.DrawPath(outlinePen, arrowPath);
                }
            }
        }

        /// <summary>
        /// Отвязывает конец стрелки от блока
        /// </summary>
        public void Detach(bool startEndpoint)
        {
            if (startEndpoint)
                StartBlock = null;
            else
                EndBlock = null;
        }

        /// <summary>
        /// Привязывает конец стрелки к блоку и точке
        /// </summary>
        public void Attach(bool startEndpoint, BpmnBlock block, PointF point)
        {
            if (startEndpoint)
            {
                StartBlock = block;
                StartPoint = point;
            }
            else
            {
                EndBlock = block;
                EndPoint = point;
            }
        }

        /// <summary>
        /// Перемещает всю стрелку (только если она не привязана)
        /// </summary>
        public void Move(float deltaX, float deltaY)
        {
            if (IsFloating)
            {
                StartPoint = new PointF(StartPoint.X + deltaX, StartPoint.Y + deltaY);
                EndPoint = new PointF(EndPoint.X + deltaX, EndPoint.Y + deltaY);
            }
        }

        // внутри класса BpmnArrow
        public void SetFloating(bool value)
        {
            // если есть доп. логика при переключении — добавьте её здесь
            IsFloating = value;
        }
        /// <summary>
        /// Возвращает минимальный прямоугольник, охватывающий всю стрелку.
        /// Используется для выделения рамкой.
        /// </summary>
        public RectangleF GetBounds()
        {
            if (ConnectionPoints == null || ConnectionPoints.Count == 0)
                return new RectangleF(StartPoint.X, StartPoint.Y, 0, 0);

            float minX = ConnectionPoints[0].X;
            float maxX = ConnectionPoints[0].X;
            float minY = ConnectionPoints[0].Y;
            float maxY = ConnectionPoints[0].Y;

            foreach (var pt in ConnectionPoints)
            {
                if (pt.X < minX) minX = pt.X;
                if (pt.X > maxX) maxX = pt.X;
                if (pt.Y < minY) minY = pt.Y;
                if (pt.Y > maxY) maxY = pt.Y;
            }

            // Добавим небольшой запас, равный ширине стрелки + толерантность для выделения
            float padding = Width + 5;
            return new RectangleF(minX - padding, minY - padding, (maxX - minX) + 2 * padding, (maxY - minY) + 2 * padding);
        }
    }
}