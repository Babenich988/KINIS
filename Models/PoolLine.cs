using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Kinis.Models
{
    public class PoolLine
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Text { get; set; } = "Lane";
        public RectangleF Bounds { get; set; }
        public Color FillColor { get; set; } = Color.White;
        public Color BorderColor { get; set; } = Color.Black;
        public List<PoolLine> ChildLines { get; set; } = new List<PoolLine>();
        public int NestingLevel { get; set; } = 0;
        public float NameStripWidth { get; set; } = 40f; // Ширина полосы названия как у пула
        public Color NameStripColor { get; set; } = Color.White; // Цвет полосы названия

        // Метод для получения границ полосы названия
        public RectangleF GetNameStripBounds()
        {
            return new RectangleF(
                Bounds.X,
                Bounds.Y,
                NameStripWidth,
                Bounds.Height
            );
        }

        // Метод для получения границ тела дорожки (без полосы названия)
        public RectangleF GetBodyBounds()
        {
            return new RectangleF(
                Bounds.X + NameStripWidth,
                Bounds.Y,
                Bounds.Width - NameStripWidth,
                Bounds.Height
            );
        }

        public bool CanAddChildLine()
        {
            return NestingLevel < 2; // Максимум 3 уровня вложенности (0,1,2)
        }
        //Метод управления вложенностью
        public bool TryAddChildLine(PoolLine childLine)
        {
            if (!CanAddChildLine())
                return false;

            childLine.NestingLevel = this.NestingLevel + 1;
            ChildLines.Add(childLine);
            return true;
        }

        public bool CanAddNestedLine()
        {
            if (NestingLevel >= 2)
                return false;

            // Проверяем вложенные линии
            foreach (var child in ChildLines)
            {
                if (!child.CanAddNestedLine())
                    return false;
            }
            return true;
        }
        public void UpdatePosition(float deltaX, float deltaY)
        {
            Bounds = new RectangleF(
                Bounds.X + deltaX,
                Bounds.Y + deltaY,
                Bounds.Width,
                Bounds.Height
            );

            // Рекурсивно обновляем дочерние линии
            foreach (var child in ChildLines)
            {
                child.UpdatePosition(deltaX, deltaY);
            }
        }
    }
}