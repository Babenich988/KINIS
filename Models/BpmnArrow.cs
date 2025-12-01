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
        public BpmnBlock StartBlock { get; set; }
        public PointF StartPoint { get; set; }
        public BpmnBlock EndBlock { get; set; }
        public PointF EndPoint { get; set; }

        // Визуальные свойства (убраны дубликаты)
        public Color Color { get; set; } = Color.Black;
        public float Width { get; set; } = 2f;

        public List<PointF> ConnectionPoints { get; set; } = new List<PointF>();

        public bool IsStartAttached => StartBlock != null;
        public bool IsEndAttached => EndBlock != null;
        public bool IsFullyAttached => IsStartAttached && IsEndAttached;

        // ДОБАВЛЯЕМ свойство IsFloating
        public bool IsFloating { get; set; }

        // Индексы точек привязки на блоках
        public int StartConnectionPointIndex { get; set; } = -1;
        public int EndConnectionPointIndex { get; set; } = -1;

        // СОБЫТИЕ ДЛЯ ОТСЛЕЖИВАНИЯ ИЗМЕНЕНИЙ СТРЕЛКИ
        [field: NonSerialized]
        public event EventHandler ArrowModified;

        protected virtual void OnArrowModified()
        {
            ArrowModified?.Invoke(this, EventArgs.Empty);
        }

        // КОНСТРУКТОР ПО УМОЛЧАНИЮ ДЛЯ СЕРИАЛИЗАЦИИ
        public BpmnArrow()
        {
        }

        public BpmnArrow(BpmnBlock startBlock, PointF startPoint, BpmnBlock endBlock, PointF endPoint)
        {
            StartBlock = startBlock;
            StartPoint = startPoint;
            EndBlock = endBlock;
            EndPoint = endPoint;
        }

        public bool HitTest(PointF point, float tolerance = 5f)
        {
            return DistanceToLine(point, StartPoint, EndPoint) <= tolerance;
        }

        public bool HitTestEndpoint(PointF point, bool startEndpoint, float tolerance = 6f)
        {
            PointF endpoint = startEndpoint ? StartPoint : EndPoint;
            float dx = point.X - endpoint.X;
            float dy = point.Y - endpoint.Y;
            return Math.Sqrt(dx * dx + dy * dy) <= tolerance;
        }

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

        public void CalculateOrthogonalPath()
        {
            ConnectionPoints.Clear();

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
            float midX = (StartPoint.X + EndPoint.X) / 2;

            ConnectionPoints.Add(StartPoint);
            ConnectionPoints.Add(new PointF(midX, StartPoint.Y));
            ConnectionPoints.Add(new PointF(midX, EndPoint.Y));
            ConnectionPoints.Add(EndPoint);
        }

        private void CalculateAttachedPath()
        {
            ConnectionPoints.Add(StartPoint);

            bool startOnLeft = StartPoint.X <= StartBlock.Bounds.Left;
            bool startOnRight = StartPoint.X >= StartBlock.Bounds.Right;
            bool startOnTop = StartPoint.Y <= StartBlock.Bounds.Top;
            bool startOnBottom = StartPoint.Y >= StartBlock.Bounds.Bottom;

            bool endOnLeft = EndPoint.X <= EndBlock.Bounds.Left;
            bool endOnRight = EndPoint.X >= EndBlock.Bounds.Right;
            bool endOnTop = EndPoint.Y <= EndBlock.Bounds.Top;
            bool endOnBottom = EndPoint.Y >= EndBlock.Bounds.Bottom;

            if (startOnRight && endOnLeft)
            {
                float midY = (StartPoint.Y + EndPoint.Y) / 2;
                ConnectionPoints.Add(new PointF(StartPoint.X + 20, StartPoint.Y));
                ConnectionPoints.Add(new PointF(StartPoint.X + 20, midY));
                ConnectionPoints.Add(new PointF(EndPoint.X - 20, midY));
                ConnectionPoints.Add(new PointF(EndPoint.X - 20, EndPoint.Y));
            }
            else if (startOnBottom && endOnTop)
            {
                float midX = (StartPoint.X + EndPoint.X) / 2;
                ConnectionPoints.Add(new PointF(StartPoint.X, StartPoint.Y + 20));
                ConnectionPoints.Add(new PointF(midX, StartPoint.Y + 20));
                ConnectionPoints.Add(new PointF(midX, EndPoint.Y - 20));
                ConnectionPoints.Add(new PointF(EndPoint.X, EndPoint.Y - 20));
            }
            else
            {
                CalculateSimplePath();
            }

            ConnectionPoints.Add(EndPoint);
        }

        public void Draw(Graphics g, bool isSelected = false)
        {
            CalculateOrthogonalPath();

            using (var pen = new Pen(isSelected ? Color.Blue : Color, isSelected ? Width + 1 : Width))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;

                if (ConnectionPoints.Count >= 2)
                {
                    for (int i = 0; i < ConnectionPoints.Count - 1; i++)
                    {
                        g.DrawLine(pen, ConnectionPoints[i], ConnectionPoints[i + 1]);
                    }
                }
                else
                {
                    g.DrawLine(pen, StartPoint, EndPoint);
                }

                DrawArrowhead(g, isSelected);
            }

            if (isSelected)
            {
                DrawEndpointMarkers(g);
            }
        }

        private void DrawEndpointMarkers(Graphics g)
        {
            using (var brush = new SolidBrush(IsStartAttached ? Color.Green : Color.Red))
            {
                g.FillEllipse(brush, StartPoint.X - 4, StartPoint.Y - 4, 8, 8);
                g.DrawEllipse(Pens.White, StartPoint.X - 4, StartPoint.Y - 4, 8, 8);
            }

            using (var brush = new SolidBrush(IsEndAttached ? Color.Green : Color.Red))
            {
                g.FillEllipse(brush, EndPoint.X - 4, EndPoint.Y - 4, 8, 8);
                g.DrawEllipse(Pens.White, EndPoint.X - 4, EndPoint.Y - 4, 8, 8);
            }
        }

        private void DrawArrowhead(Graphics g, bool isSelected)
        {
            if (ConnectionPoints.Count < 2) return;

            PointF lineEnd = ConnectionPoints[ConnectionPoints.Count - 1];
            PointF lineStart = ConnectionPoints[ConnectionPoints.Count - 2];

            float dx = lineEnd.X - lineStart.X;
            float dy = lineEnd.Y - lineStart.Y;
            float length = (float)Math.Sqrt(dx * dx + dy * dy);

            if (length == 0) return;

            dx /= length;
            dy /= length;

            float arrowSize = 10f;
            float offset = -Width;
            PointF arrowTip = new PointF(
                lineEnd.X - dx * offset,
                lineEnd.Y - dy * offset
            );

            float arrowAngle = (float)(30 * Math.PI / 180);

            PointF leftPoint = new PointF(
                (float)(arrowTip.X - arrowSize * Math.Cos(arrowAngle) * dx + arrowSize * Math.Sin(arrowAngle) * dy),
                (float)(arrowTip.Y - arrowSize * Math.Cos(arrowAngle) * dy - arrowSize * Math.Sin(arrowAngle) * dx)
            );

            PointF rightPoint = new PointF(
                (float)(arrowTip.X - arrowSize * Math.Cos(arrowAngle) * dx - arrowSize * Math.Sin(arrowAngle) * dy),
                (float)(arrowTip.Y - arrowSize * Math.Cos(arrowAngle) * dy + arrowSize * Math.Sin(arrowAngle) * dx)
            );

            using (var arrowPath = new GraphicsPath())
            {
                arrowPath.AddLine(arrowTip, leftPoint);
                arrowPath.AddLine(leftPoint, rightPoint);
                arrowPath.AddLine(rightPoint, arrowTip);
                arrowPath.CloseFigure();

                using (var brush = new SolidBrush(isSelected ? Color.Blue : Color))
                {
                    g.FillPath(brush, arrowPath);
                }

                using (var outlinePen = new Pen(isSelected ? Color.DarkBlue : Color.DarkGray, 1))
                {
                    g.DrawPath(outlinePen, arrowPath);
                }
            }
        }

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

            OnArrowModified(); // ВЫЗЫВАЕМ СОБЫТИЕ ИЗМЕНЕНИЯ
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

            OnArrowModified(); // ВЫЗЫВАЕМ СОБЫТИЕ ИЗМЕНЕНИЯ
        }

        public void Move(float deltaX, float deltaY)
        {
            // ПЕРЕМЕЩАЕМ ВСЕГДА, без проверок
            StartPoint = new PointF(StartPoint.X + deltaX, StartPoint.Y + deltaY);
            EndPoint = new PointF(EndPoint.X + deltaX, EndPoint.Y + deltaY);

            // Также перемещаем промежуточные точки если они есть
            if (ConnectionPoints != null && ConnectionPoints.Count > 0)
            {
                for (int i = 0; i < ConnectionPoints.Count; i++)
                {
                    ConnectionPoints[i] = new PointF(
                        ConnectionPoints[i].X + deltaX,
                        ConnectionPoints[i].Y + deltaY
                    );
                }
            }

            OnArrowModified(); // ВЫЗЫВАЕМ СОБЫТИЕ ИЗМЕНЕНИЯ
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

            // Добавляем запас для стрелки и наконечника
            float padding = Width + 15; // Увеличиваем запас для наконечника
            return new RectangleF(minX - padding, minY - padding,
                                 (maxX - minX) + 2 * padding,
                                 (maxY - minY) + 2 * padding);
        }
    }
}