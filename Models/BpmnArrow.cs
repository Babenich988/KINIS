using System;
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
        public bool IsFloating => !IsStartAttached && !IsEndAttached;

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
        public void Draw(Graphics g, bool isSelected = false)
        {
            using (var pen = new Pen(isSelected ? Color.Blue : Color, isSelected ? Width + 1 : Width))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;

                // Рисуем основную линию
                g.DrawLine(pen, StartPoint, EndPoint);

                // Рисуем наконечник
                DrawArrowhead(g, isSelected);
            }

            // ДОБАВЛЯЕМ: Рисуем маркеры концов если стрелка выделена
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
            // Вычисляем направление стрелки
            float dx = EndPoint.X - StartPoint.X;
            float dy = EndPoint.Y - StartPoint.Y;
            float length = (float)Math.Sqrt(dx * dx + dy * dy);

            if (length == 0) return;

            // Нормализуем направление
            dx /= length;
            dy /= length;

            // Размер наконечника
            float arrowSize = 10f;

            // СДВИГАЕМ НАКОНЕЧНИК ЧУТЬ ДАЛЬШЕ ОТ КОНЦА СТРЕЛКИ
            float offset = -Width; // Сдвигаем на толщину линии

            // Вершина наконечника (сдвинута от конца стрелки)
            PointF arrowTip = new PointF(
                EndPoint.X - dx * offset,
                EndPoint.Y - dy * offset
            );

            // Угол наконечника (в радианах)
            float arrowAngle = (float)(30 * Math.PI / 180); // 30 градусов

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
    }
}