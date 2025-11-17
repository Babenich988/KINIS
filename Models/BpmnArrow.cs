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

        // Основа
        public BpmnBlock StartBlock { get; set; }
        public PointF StartPoint { get; set; }

        public BpmnBlock EndBlock { get; set; }
        public PointF EndPoint { get; set; }

        // Визуальные параметры
        public Color Color { get; set; } = Color.Black;
        public float Width { get; set; } = 2f;

        // Точки маршрута (рисуются как ортогональная ломаная)
        public List<PointF> ConnectionPoints { get; set; } = new List<PointF>();

        // Привязка
        public int StartConnectionPointIndex { get; set; } = -1;
        public int EndConnectionPointIndex { get; set; } = -1;

        public bool IsStartAttached => StartBlock != null;
        public bool IsEndAttached => EndBlock != null;
        public bool IsFullyAttached => IsStartAttached && IsEndAttached;

        /// <summary>
        /// Стрелка считается плавающей, если не привязана к обоим концам.
        /// </summary>
        public bool IsFloating
        {
            get
            {
                // Плавающая, если:
                // 1) оба конца не привязаны,
                // 2) или один привязан, другой нет
                return !(IsStartAttached && IsEndAttached);
            }
        }

        public BpmnArrow() { }

        public BpmnArrow(BpmnBlock startBlock, PointF startPoint, BpmnBlock endBlock, PointF endPoint)
        {
            StartBlock = startBlock;
            EndBlock = endBlock;
            StartPoint = startPoint;
            EndPoint = endPoint;
        }

        // ================================
        //          HIT TEST
        // ================================
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

        private float DistanceToLine(PointF p, PointF a, PointF b)
        {
            float A = p.X - a.X;
            float B = p.Y - a.Y;
            float C = b.X - a.X;
            float D = b.Y - a.Y;

            float dot = A * C + B * D;
            float lenSq = C * C + D * D;
            float param = (lenSq != 0) ? dot / lenSq : -1;

            float xx, yy;

            if (param < 0)
            {
                xx = a.X;
                yy = a.Y;
            }
            else if (param > 1)
            {
                xx = b.X;
                yy = b.Y;
            }
            else
            {
                xx = a.X + param * C;
                yy = a.Y + param * D;
            }

            float dx = p.X - xx;
            float dy = p.Y - yy;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        // ================================
        //           PATH
        // ================================
        public void CalculateOrthogonalPath()
        {
            ConnectionPoints.Clear();

            if (IsStartAttached && IsEndAttached)
                CalculateAttachedPath();
            else
                CalculateSimplePath();
        }

        private void CalculateSimplePath()
        {
            float midX = (StartPoint.X + EndPoint.X) / 2f;

            ConnectionPoints.Add(StartPoint);
            ConnectionPoints.Add(new PointF(midX, StartPoint.Y));
            ConnectionPoints.Add(new PointF(midX, EndPoint.Y));
            ConnectionPoints.Add(EndPoint);
        }

        private void CalculateAttachedPath()
        {
            ConnectionPoints.Add(StartPoint);

            bool startLeft = StartPoint.X <= StartBlock.Bounds.Left;
            bool startRight = StartPoint.X >= StartBlock.Bounds.Right;
            bool startTop = StartPoint.Y <= StartBlock.Bounds.Top;
            bool startBottom = StartPoint.Y >= StartBlock.Bounds.Bottom;

            bool endLeft = EndPoint.X <= EndBlock.Bounds.Left;
            bool endRight = EndPoint.X >= EndBlock.Bounds.Right;
            bool endTop = EndPoint.Y <= EndBlock.Bounds.Top;
            bool endBottom = EndPoint.Y >= EndBlock.Bounds.Bottom;

            // Право → лево
            if (startRight && endLeft)
            {
                float midY = (StartPoint.Y + EndPoint.Y) / 2;
                ConnectionPoints.Add(new PointF(StartPoint.X + 20, StartPoint.Y));
                ConnectionPoints.Add(new PointF(StartPoint.X + 20, midY));
                ConnectionPoints.Add(new PointF(EndPoint.X - 20, midY));
                ConnectionPoints.Add(new PointF(EndPoint.X - 20, EndPoint.Y));
            }
            // Вниз → вверх
            else if (startBottom && endTop)
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

        // ================================
        //           DRAWING
        // ================================
        public void Draw(Graphics g, bool isSelected = false)
        {
            CalculateOrthogonalPath();

            using (var pen = new Pen(isSelected ? Color.Blue : Color, Width))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;

                for (int i = 0; i < ConnectionPoints.Count - 1; i++)
                    g.DrawLine(pen, ConnectionPoints[i], ConnectionPoints[i + 1]);

                DrawArrowhead(g, pen.Color);
            }

            if (isSelected)
                DrawEndpointMarkers(g);
        }

        private void DrawEndpointMarkers(Graphics g)
        {
            using (var b = new SolidBrush(IsStartAttached ? Color.Green : Color.Red))
                g.FillEllipse(b, StartPoint.X - 4, StartPoint.Y - 4, 8, 8);

            using (var b = new SolidBrush(IsEndAttached ? Color.Green : Color.Red))
                g.FillEllipse(b, EndPoint.X - 4, EndPoint.Y - 4, 8, 8);
        }

        private void DrawArrowhead(Graphics g, Color c)
        {
            if (ConnectionPoints.Count < 2) return;

            PointF end = ConnectionPoints[^1];
            PointF prev = ConnectionPoints[^2];

            float dx = end.X - prev.X;
            float dy = end.Y - prev.Y;
            float len = (float)Math.Sqrt(dx * dx + dy * dy);
            if (len == 0) return;

            dx /= len;
            dy /= len;

            float size = 10f;

            PointF tip = new PointF(end.X, end.Y);
            PointF left = new PointF(
                end.X - dx * size - dy * size * 0.5f,
                end.Y - dy * size + dx * size * 0.5f);

            PointF right = new PointF(
                end.X - dx * size + dy * size * 0.5f,
                end.Y - dy * size - dx * size * 0.5f);

            using (var p = new GraphicsPath())
            {
                p.AddPolygon(new[] { tip, left, right });
                using (var brush = new SolidBrush(c))
                    g.FillPath(brush, p);
            }
        }

        // ================================
        //           MODIFY
        // ================================
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

        /// <summary>Перемещение стрелки (только координаты).</summary>
        public void Move(float dx, float dy)
        {
            StartPoint = new PointF(StartPoint.X + dx, StartPoint.Y + dy);
            EndPoint = new PointF(EndPoint.X + dx, EndPoint.Y + dy);

            if (ConnectionPoints != null)
            {
                for (int i = 0; i < ConnectionPoints.Count; i++)
                    ConnectionPoints[i] = new PointF(ConnectionPoints[i].X + dx, ConnectionPoints[i].Y + dy);
            }
        }

        public RectangleF GetBounds()
        {
            if (ConnectionPoints.Count == 0)
                return new RectangleF(StartPoint.X, StartPoint.Y, 0, 0);

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            foreach (var p in ConnectionPoints)
            {
                if (p.X < minX) minX = p.X;
                if (p.X > maxX) maxX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.Y > maxY) maxY = p.Y;
            }

            float pad = Width + 4;
            return new RectangleF(minX - pad, minY - pad, (maxX - minX) + pad * 2, (maxY - minY) + pad * 2);
        }
    }
}
