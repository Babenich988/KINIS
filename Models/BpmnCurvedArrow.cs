using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Kinis.Model
{
    [Serializable]
    public class BpmnCurvedArrow
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

        // Контрольные точки для кривой Безье
        public PointF ControlPoint1 { get; set; }
        public PointF ControlPoint2 { get; set; }

        public BpmnCurvedArrow() { }        

        public BpmnCurvedArrow(BpmnBlock startBlock, PointF startPoint, BpmnBlock endBlock, PointF endPoint)
        {
            StartBlock = startBlock;
            StartPoint = startPoint;
            EndBlock = endBlock;
            EndPoint = endPoint;
        }

        private float Distance(PointF a, PointF b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        // Индексы точек привязки
        public int StartConnectionPointIndex { get; set; } = -1;
        public int EndConnectionPointIndex { get; set; } = -1;
        
        /// <summary>
        /// Отвязывает конец стрелки от блока
        /// </summary>
        public void Detach(bool startEndpoint)
        {
            if (startEndpoint)
            {
                StartBlock = null;
                StartConnectionPointIndex = -1;
            }
            else
            {
                EndBlock = null;
                EndConnectionPointIndex = -1;
            }
        }

        /// <summary>
        /// Привязывает конец стрелки к блоку и точке
        /// </summary>
        public void Attach(bool startEndpoint, BpmnBlock block, PointF point, int connectionPointIndex = -1)
        {
            if (startEndpoint)
            {
                StartBlock = block;
                StartPoint = point;
                StartConnectionPointIndex = connectionPointIndex;
            }
            else
            {
                EndBlock = block;
                EndPoint = point;
                EndConnectionPointIndex = connectionPointIndex;
            }
        }
        //Метод отрисовки кривой
        public void Draw(Graphics g, bool isSelected = false)
        {
            // РИСУЕМ КРИВУЮ БЕЗЬЕ
            using (var pen = new Pen(isSelected ? Color.Blue : Color, isSelected ? Width + 1 : Width))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;

                using (var path = CreateCurvedPath())
                {
                    g.DrawPath(pen, path);
                }
            }

            // РИСУЕМ НАКОНЕЧНИК НА КОНЕЧНОЙ ТОЧКЕ
            DrawArrowhead(g, isSelected);

            // РИСУЕМ МАРКЕРЫ КОНЦОВ ЕСЛИ СТРЕЛКА ВЫДЕЛЕНА
            if (isSelected)
            {
                DrawEndpointMarkers(g);
            }
        }

        // Метод для отрисовки маркеров концов
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

        // Создает путь для кривой Безье
        private GraphicsPath CreateCurvedPath()
        {
            var path = new GraphicsPath();
            path.AddBezier(StartPoint, ControlPoint1, ControlPoint2, EndPoint);
            return path;
        }

        // Вычисляем контрольные точки для плавной кривой
        public void CalculateControlPoints()
        {
            if (IsStartAttached && IsEndAttached)
            {
                CalculateAttachedCurve();
            }
            else
            {
                CalculateSimpleCurve();
            }
        }

        private void CalculateSimpleCurve()
        {
            // Простая кривая для непривязанных стрелок
            float dx = EndPoint.X - StartPoint.X;
            float dy = EndPoint.Y - StartPoint.Y;

            // Контрольные точки создают плавную S-образную кривую
            float offset = Math.Max(50, Math.Abs(dx) * 0.3f);

            ControlPoint1 = new PointF(StartPoint.X + offset, StartPoint.Y);
            ControlPoint2 = new PointF(EndPoint.X - offset, EndPoint.Y);
        }
        
        private void CalculateAttachedCurve()
        {
            // Базовая логика для привязанных стрелок
            float curveStrength = 80f;
            ControlPoint1 = new PointF(StartPoint.X + curveStrength, StartPoint.Y);
            ControlPoint2 = new PointF(EndPoint.X - curveStrength, EndPoint.Y);

            // Определяем относительное положение блоков
            bool startOnLeft = StartPoint.X <= startBounds.Left;
            bool startOnRight = StartPoint.X >= startBounds.Right;
            bool startOnTop = StartPoint.Y <= startBounds.Top;
            bool startOnBottom = StartPoint.Y >= startBounds.Bottom;

            bool endOnLeft = EndPoint.X <= endBounds.Left;
            bool endOnRight = EndPoint.X >= endBounds.Right;
            bool endOnTop = EndPoint.Y <= endBounds.Top;
            bool endOnBottom = EndPoint.Y >= endBounds.Bottom;

            if ((startOnRight && endOnLeft) || (startOnLeft && endOnRight))
            {
                // Блоки рядом по горизонтали
                ControlPoint1 = new PointF(StartPoint.X + curveStrength, StartPoint.Y);
                ControlPoint2 = new PointF(EndPoint.X - curveStrength, EndPoint.Y);
            }

            else if ((startOnBottom && endOnTop) || (startOnTop && endOnBottom))
            {
                // Блоки рядом по вертикали
                ControlPoint1 = new PointF(StartPoint.X, StartPoint.Y + curveStrength);
                ControlPoint2 = new PointF(EndPoint.X, EndPoint.Y - curveStrength);
            }
        }

        // Проверяем попадает ли точка на кривую стрелку
        public bool HitTest(PointF point, float tolerance = 5f)
        {
            // Аппроксимируем кривую отрезками и проверяем расстояние
            var path = CreateCurvedPath();
            var points = FlattenPath(path, 20);

            for (int i = 0; i < points.Length - 1; i++)
            {
                if (DistanceToLine(point, points[i], points[i + 1]) <= tolerance)
                    return true;
            }
            return false;
        }

        // Аппроксимирует путь точками для проверки попадания
        private PointF[] FlattenPath(GraphicsPath path, int pointsCount)
        {
            path.Flatten();
            var pathPoints = path.PathPoints;

            if (pathPoints.Length > pointsCount)
            {
                var result = new List<PointF>();
                int step = pathPoints.Length / pointsCount;
                for (int i = 0; i < pathPoints.Length; i += step)
                {
                    result.Add(pathPoints[i]);
                }
                return result.ToArray();
            }

            return pathPoints;
        }

        // Метод нахождения расстояния до линии
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

        // Проверка попадания на маркеры концов
        public bool HitTestEndpoint(PointF point, bool startEndpoint, float tolerance = 6f)
        {
            PointF endpoint = startEndpoint ? StartPoint : EndPoint;
            float dx = point.X - endpoint.X;
            float dy = point.Y - endpoint.Y;
            return Math.Sqrt(dx * dx + dy * dy) <= tolerance;
        }

        /// <summary>
        /// Возвращает минимальный прямоугольник, охватывающий всю стрелку
        /// </summary>
        public RectangleF GetBounds()
        {
            float minX = Math.Min(Math.Min(StartPoint.X, EndPoint.X), Math.Min(ControlPoint1.X, ControlPoint2.X));
            float maxX = Math.Max(Math.Max(StartPoint.X, EndPoint.X), Math.Max(ControlPoint1.X, ControlPoint2.X));
            float minY = Math.Min(Math.Min(StartPoint.Y, EndPoint.Y), Math.Min(ControlPoint1.Y, ControlPoint2.Y));
            float maxY = Math.Max(Math.Max(StartPoint.Y, EndPoint.Y), Math.Max(ControlPoint1.Y, ControlPoint2.Y));

            float padding = Width + 5;
            return new RectangleF(minX - padding, minY - padding,
                                (maxX - minX) + 2 * padding, (maxY - minY) + 2 * padding);
        }

        /// <summary>
        /// Перемещает всю стрелку
        /// </summary>
        public void Move(float deltaX, float deltaY)
        {
            StartPoint = new PointF(StartPoint.X + deltaX, StartPoint.Y + deltaY);
            EndPoint = new PointF(EndPoint.X + deltaX, EndPoint.Y + deltaY);
            ControlPoint1 = new PointF(ControlPoint1.X + deltaX, ControlPoint1.Y + deltaY);
            ControlPoint2 = new PointF(ControlPoint2.X + deltaX, ControlPoint2.Y + deltaY);
        }

        // Вычисляет направление кривой в конечной точке
        private PointF CalculateCurveEndDirection()
        {
            // Производная кривой Безье в конечной точке
            float dx = 3 * (EndPoint.X - ControlPoint2.X);
            float dy = 3 * (EndPoint.Y - ControlPoint2.Y);

            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            if (length == 0) return new PointF(1, 0);

            return new PointF(dx / length, dy / length);
        }

        private void DrawArrowhead(Graphics g, bool isSelected)
        {
            // Вычисляем направление в конечной точке кривой
            PointF direction = CalculateCurveEndDirection();

            // Размер наконечника
            float arrowSize = 10f;

            // Сдвигаем наконечник немного назад от конечной точки
            PointF arrowTip = new PointF(
                EndPoint.X - direction.X * arrowSize * 0.3f,
                EndPoint.Y - direction.Y * arrowSize * 0.3f
            );

            // Угол наконечника (в радианах)
            float arrowAngle = (float)(30 * Math.PI / 180);

            // Левая точка треугольника
            PointF leftPoint = new PointF(
                (float)(arrowTip.X - arrowSize * Math.Cos(arrowAngle) * direction.X + arrowSize * Math.Sin(arrowAngle) * direction.Y),
                (float)(arrowTip.Y - arrowSize * Math.Cos(arrowAngle) * direction.Y - arrowSize * Math.Sin(arrowAngle) * direction.X)
            );

            // Правая точка треугольника  
            PointF rightPoint = new PointF(
                (float)(arrowTip.X - arrowSize * Math.Cos(arrowAngle) * direction.X - arrowSize * Math.Sin(arrowAngle) * direction.Y),
                (float)(arrowTip.Y - arrowSize * Math.Cos(arrowAngle) * direction.Y + arrowSize * Math.Sin(arrowAngle) * direction.X)
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
            }

            // Обводим контур
            using (var outlinePen = new Pen(isSelected ? Color.DarkBlue : Color.DarkGray, 1))
            {
                g.DrawPath(outlinePen, arrowPath);
            }
        }
    }
}