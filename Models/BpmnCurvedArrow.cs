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

            // РИСУЕМ МАРКЕРЫ КОНЦОВ ЕСЛИ СТРЕЛКА ВЫДЕЛЕНА
            if (isSelected)
            {
                DrawEndpointMarkers(g);
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
    }
}