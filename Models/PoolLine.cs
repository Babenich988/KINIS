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
        public bool IsTransparent { get; set; } = true; // По умолчанию прозрачный фон
        public Color BackgroundColor { get; set; } = Color.Transparent; // Цвет фона тела
        public Color NameStripBackgroundColor { get; set; } = Color.White; // Цвет фона полосы названия
        public float BorderWidth { get; set; } = 1f; // Толщина границы
        public float NameStripHeight => Bounds.Height; // Полоса названия занимает всю высоту дорожки

        // Добавим поля для хранения относительной позиции относительно пула
        public float RelativeX { get; set; }
        public float RelativeY { get; set; }
        public bool HasRelativePosition { get; set; }

        [NonSerialized]
        private PoolLine _parentLine;

        public PoolLine ParentLine
        {
            get => _parentLine;
            set => _parentLine = value;
        }

        // Метод для установки родителя с обновлением вложенности
        public void SetParent(PoolLine parent)
        {
            if (parent == this) return; // Нельзя быть родителем самому себе

            // Удаляем из старого родителя
            if (_parentLine != null)
            {
                _parentLine.ChildLines.Remove(this);
            }

            // Устанавливаем нового родителя
            _parentLine = parent;

            if (parent != null)
            {
                // Обновляем уровень вложенности
                NestingLevel = parent.NestingLevel + 1;

                // Добавляем к новому родителю
                if (!parent.ChildLines.Contains(this))
                {
                    parent.ChildLines.Add(this);
                }
            }
            else
            {
                NestingLevel = 0; // Верхний уровень
            }
        }

        // Метод для проверки, является ли дорожка предком
        public bool IsAncestorOf(PoolLine lane)
        {
            if (ChildLines == null) return false;

            foreach (var child in ChildLines)
            {
                if (child == lane) return true;
                if (child.IsAncestorOf(lane)) return true;
            }

            return false;
        }

        // Метод для получения всех потомков
        public List<PoolLine> GetAllDescendants()
        {
            var descendants = new List<PoolLine>();

            if (ChildLines != null)
            {
                foreach (var child in ChildLines)
                {
                    descendants.Add(child);
                    descendants.AddRange(child.GetAllDescendants());
                }
            }

            return descendants;
        }

        // Метод для обновления относительной позиции относительно родителя (пула или родительской дорожки)
        public void UpdateRelativePosition(RectangleF containerBounds)
        {
            RelativeX = Bounds.X - containerBounds.X;
            RelativeY = Bounds.Y - containerBounds.Y;
            HasRelativePosition = true;
        }

        // Метод для применения относительной позиции
        public void ApplyRelativePosition(RectangleF containerBounds)
        {
            if (HasRelativePosition)
            {
                Bounds = new RectangleF(
                    containerBounds.X + RelativeX,
                    containerBounds.Y + RelativeY,
                    Bounds.Width,
                    Bounds.Height
                );
            }
        }

        // Метод для получения границ полосы названия
        public RectangleF GetNameStripBounds()
        {
            return new RectangleF(
                Bounds.X,
                Bounds.Y,
                NameStripWidth,
                Bounds.Height  // Полоса названия занимает всю высоту дорожки
            );
        }

        // Метод для получения границ тела дорожки (без полосы названия)
        public RectangleF GetBodyBounds()
        {
            return new RectangleF(
                Bounds.X + NameStripWidth,
                Bounds.Y,
                Bounds.Width - NameStripWidth,
                Bounds.Height  // Тело дорожки также занимает всю высоту
            );
        }

        // Добавим метод для получения границ только контура (без полосы названия):
        public RectangleF GetOutlineBounds()
        {
            if (IsTransparent)
            {
                // Для прозрачного стиля - только внешний контур
                return new RectangleF(
                    Bounds.X + NameStripWidth,
                    Bounds.Y,
                    Bounds.Width - NameStripWidth,
                    Bounds.Height
                );
            }
            else
            {
                // Для обычного стиля - тело дорожки
                return GetBodyBounds();
            }
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