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
                pen.EndCap = LineCap.Round; // Временный конец без наконечника

                // Рисуем основную линию БЕЗ наконечника
                g.DrawLine(pen, StartPoint, EndPoint);

                // ОТДЕЛЬНО РИСУЕМ НАКОНЕЧНИК С ЗАЛИВКОЙ
                DrawArrowhead(g, isSelected);
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
    }
}