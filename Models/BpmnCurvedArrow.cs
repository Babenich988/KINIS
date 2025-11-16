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
    }
}