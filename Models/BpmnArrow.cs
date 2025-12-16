using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Kinis.Models
{
    /// <summary>
    /// Модель прямой стрелки BPMN с ортогональными изломами
    /// </summary>
    [Serializable]
    public class BpmnArrow
    {
        /// <summary>
        /// Уникальный идентификатор стрелки
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Текстовая метка стрелки
        /// </summary>
        public string Text { get; set; } = "";

        /// <summary>
        /// Блок, к которому привязано начало стрелки
        /// </summary>
        public BpmnBlock StartBlock { get; set; }

        /// <summary>
        /// Начальная точка стрелки
        /// </summary>
        public PointF StartPoint { get; set; }

        /// <summary>
        /// Блок, к которому привязан конец стрелки
        /// </summary>
        public BpmnBlock EndBlock { get; set; }

        /// <summary>
        /// Конечная точка стрелки
        /// </summary>
        public PointF EndPoint { get; set; }

        // Визуальные свойства
        /// <summary>
        /// Цвет стрелки
        /// </summary>
        public Color Color { get; set; } = Color.Black;

        /// <summary>
        /// Толщина линии стрелки
        /// </summary>
        public float Width { get; set; } = 2f;

        /// <summary>
        /// Список точек соединения для ортогонального пути
        /// </summary>
        public List<PointF> ConnectionPoints { get; set; } = new List<PointF>();

        /// <summary>
        /// Получает значение, указывающее привязано ли начало стрелки к блоку
        /// </summary>
        public bool IsStartAttached => StartBlock != null;

        /// <summary>
        /// Получает значение, указывающее привязан ли конец стрелки к блоку
        /// </summary>
        public bool IsEndAttached => EndBlock != null;

        /// <summary>
        /// Получает значение, указывающее полностью ли привязана стрелка к блокам
        /// </summary>
        public bool IsFullyAttached => IsStartAttached && IsEndAttached;

        /// <summary>
        /// Получает или задает значение, указывающее является ли стрелка плавающей
        /// </summary>
        public bool IsFloating { get; set; }

        /// <summary>
        /// Индекс точки привязки начала стрелки на блоке
        /// </summary>
        public int StartConnectionPointIndex { get; set; } = -1;

        /// <summary>
        /// Индекс точки привязки конца стрелки на блоке
        /// </summary>
        public int EndConnectionPointIndex { get; set; } = -1;

        /// <summary>
        /// Событие, возникающее при изменении стрелки
        /// </summary>
        [field: NonSerialized]
        public event EventHandler ArrowModified;

        /// <summary>
        /// Вызывает событие ArrowModified
        /// </summary>
        protected virtual void OnArrowModified()
        {
            ArrowModified?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Инициализирует новый экземпляр стрелки для сериализации
        /// </summary>
        public BpmnArrow()
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр стрелки с указанными параметрами
        /// </summary>
        /// <param name="startBlock">Начальный блок</param>
        /// <param name="startPoint">Начальная точка</param>
        /// <param name="endBlock">Конечный блок</param>
        /// <param name="endPoint">Конечная точка</param>
        public BpmnArrow(BpmnBlock startBlock, PointF startPoint, BpmnBlock endBlock, PointF endPoint)
        {
            StartBlock = startBlock;
            StartPoint = startPoint;
            EndBlock = endBlock;
            EndPoint = endPoint;
        }

        /// <summary>
        /// Проверяет попадание точки на стрелку
        /// </summary>
        /// <param name="point">Точка для проверки</param>
        /// <param name="tolerance">Допустимое расстояние до линии</param>
        /// <returns>True если точка попадает на стрелку</returns>
        public bool HitTest(PointF point, float tolerance = 5f)
        {
            return DistanceToLine(point, StartPoint, EndPoint) <= tolerance;
        }

        /// <summary>
        /// Проверяет попадание точки на маркер конца стрелки
        /// </summary>
        /// <param name="point">Точка для проверки</param>
        /// <param name="startEndpoint">True если проверяется начало, False если конец</param>
        /// <param name="tolerance">Допустимое расстояние до точки</param>
        /// <returns>True если точка попала на маркер конца</returns>
        public bool HitTestEndpoint(PointF point, bool startEndpoint, float tolerance = 6f)
        {
            PointF endpoint = startEndpoint ? StartPoint : EndPoint;
            float dx = point.X - endpoint.X;
            float dy = point.Y - endpoint.Y;
            return Math.Sqrt(dx * dx + dy * dy) <= tolerance;
        }

        /// <summary>
        /// Вычисляет расстояние от точки до линии
        /// </summary>
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

        /// <summary>
        /// Вычисляет ортогональный путь стрелки с изломами
        /// </summary>
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

        /// <summary>
        /// Вычисляет простой путь для непривязанной стрелки
        /// </summary>
        private void CalculateSimplePath()
        {
            float midX = (StartPoint.X + EndPoint.X) / 2;

            ConnectionPoints.Add(StartPoint);
            ConnectionPoints.Add(new PointF(midX, StartPoint.Y));
            ConnectionPoints.Add(new PointF(midX, EndPoint.Y));
            ConnectionPoints.Add(EndPoint);
        }

        /// <summary>
        /// Вычисляет сложный путь для привязанной стрелки
        /// </summary>
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

        /// <summary>
        /// Отрисовывает стрелку на графическом контексте
        /// </summary>
        /// <param name="g">Графический контекст для рисования</param>
        /// <param name="isSelected">Указывает выделена ли стрелка</param>
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

        /// <summary>
        /// Отрисовывает маркеры концов стрелки
        /// </summary>
        private void DrawEndpointMarkers(Graphics g)
        {
            using (var brush = new SolidBrush(IsStartAttached ? Color.Green : Color.Red))
            {
                g.FillEllipse(brush, StartPoint.X - 4, StartPoint.Y - 4, 8, 8);
                g.DrawEllipse(Pens.Transparent, StartPoint.X - 4, StartPoint.Y - 4, 8, 8);
            }

            using (var brush = new SolidBrush(IsEndAttached ? Color.Green : Color.Red))
            {
                g.FillEllipse(brush, EndPoint.X - 4, EndPoint.Y - 4, 8, 8);
                g.DrawEllipse(Pens.Transparent, EndPoint.X - 4, EndPoint.Y - 4, 8, 8);
            }
        }

        /// <summary>
        /// Отрисовывает наконечник стрелки
        /// </summary>
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

        /// <summary>
        /// Отвязывает конец стрелки от блока
        /// </summary>
        /// <param name="startEndpoint">True если отвязывается начало, False если конец</param>
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
        /// <param name="startEndpoint">True если привязывается начало, False если конец</param>
        /// <param name="block">Блок для привязки</param>
        /// <param name="point">Точка привязки</param>
        /// <param name="connectionPointIndex">Индекс точки привязки на блоке</param>
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

        /// <summary>
        /// Перемещает стрелку на указанное смещение
        /// </summary>
        /// <param name="deltaX">Смещение по оси X</param>
        /// <param name="deltaY">Смещение по оси Y</param>
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
        /// Возвращает минимальный прямоугольник, охватывающий всю стрелку
        /// </summary>
        /// <returns>Прямоугольник, содержащий стрелку</returns>
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