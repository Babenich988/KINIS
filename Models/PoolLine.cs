using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Kinis.Models
{
    /// <summary>
    /// Модель дорожки (lane) внутри пула BPMN
    /// </summary>
    public class PoolLine
    {
        /// <summary>
        /// Уникальный идентификатор дорожки
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Текст названия дорожки
        /// </summary>
        public string Text { get; set; } = "Lane";

        /// <summary>
        /// Границы дорожки
        /// </summary>
        public RectangleF Bounds { get; set; }

        /// <summary>
        /// Цвет заливки дорожки
        /// </summary>
        public Color FillColor { get; set; } = Color.White;

        /// <summary>
        /// Цвет границы дорожки
        /// </summary>
        public Color BorderColor { get; set; } = Color.Black;

        /// <summary>
        /// Список дочерних (вложенных) дорожек
        /// </summary>
        public List<PoolLine> ChildLines { get; set; } = new List<PoolLine>();

        /// <summary>
        /// Уровень вложенности дорожки (0 - верхний уровень)
        /// </summary>
        public int NestingLevel { get; set; } = 0;

        /// <summary>
        /// Ширина полосы названия дорожки
        /// </summary>
        public float NameStripWidth { get; set; } = 40f;

        /// <summary>
        /// Цвет полосы названия
        /// </summary>
        public Color NameStripColor { get; set; } = Color.White;

        /// <summary>
        /// Указывает, является ли фон дорожки прозрачным
        /// </summary>
        public bool IsTransparent { get; set; } = true;

        /// <summary>
        /// Цвет фона тела дорожки
        /// </summary>
        public Color BackgroundColor { get; set; } = Color.Transparent;

        /// <summary>
        /// Цвет фона полосы названия
        /// </summary>
        public Color NameStripBackgroundColor { get; set; } = Color.White;

        /// <summary>
        /// Толщина границы дорожки
        /// </summary>
        public float BorderWidth { get; set; } = 1f;

        /// <summary>
        /// Высота полосы названия (равна высоте дорожки)
        /// </summary>
        public float NameStripHeight => Bounds.Height;

        /// <summary>
        /// Относительная координата X относительно контейнера
        /// </summary>
        public float RelativeX { get; set; }

        /// <summary>
        /// Относительная координата Y относительно контейнера
        /// </summary>
        public float RelativeY { get; set; }

        /// <summary>
        /// Указывает, установлена ли относительная позиция
        /// </summary>
        public bool HasRelativePosition { get; set; }

        [NonSerialized]
        private PoolLine _parentLine;

        /// <summary>
        /// Родительская дорожка (для вложенных дорожек)
        /// </summary>
        public PoolLine ParentLine
        {
            get => _parentLine;
            set => _parentLine = value;
        }

        /// <summary>
        /// Устанавливает родительскую дорожку с обновлением вложенности
        /// </summary>
        /// <param name="parent">Родительская дорожка</param>
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

        /// <summary>
        /// Проверяет, является ли дорожка предком указанной дорожки
        /// </summary>
        /// <param name="lane">Дорожка для проверки</param>
        /// <returns>True если текущая дорожка является предком</returns>
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

        /// <summary>
        /// Получает всех потомков дорожки
        /// </summary>
        /// <returns>Список всех потомков</returns>
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

        /// <summary>
        /// Обновляет относительную позицию дорожки относительно контейнера
        /// </summary>
        /// <param name="containerBounds">Границы контейнера (пула или родительской дорожки)</param>
        public void UpdateRelativePosition(RectangleF containerBounds)
        {
            RelativeX = Bounds.X - containerBounds.X;
            RelativeY = Bounds.Y - containerBounds.Y;
            HasRelativePosition = true;
        }

        /// <summary>
        /// Применяет сохраненную относительную позицию к дорожке
        /// </summary>
        /// <param name="containerBounds">Границы контейнера</param>
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

        /// <summary>
        /// Получает границы полосы названия дорожки
        /// </summary>
        /// <returns>Прямоугольник границ полосы названия</returns>
        public RectangleF GetNameStripBounds()
        {
            return new RectangleF(
                Bounds.X,
                Bounds.Y,
                NameStripWidth,
                Bounds.Height  // Полоса названия занимает всю высоту дорожки
            );
        }

        /// <summary>
        /// Получает границы тела дорожки (без полосы названия)
        /// </summary>
        /// <returns>Прямоугольник границ тела дорожки</returns>
        public RectangleF GetBodyBounds()
        {
            return new RectangleF(
                Bounds.X + NameStripWidth,
                Bounds.Y,
                Bounds.Width - NameStripWidth,
                Bounds.Height  // Тело дорожки также занимает всю высоту
            );
        }

        /// <summary>
        /// Получает границы контура дорожки
        /// </summary>
        /// <returns>Прямоугольник границ контура</returns>
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

        /// <summary>
        /// Проверяет возможность добавления дочерней дорожки
        /// </summary>
        /// <returns>True если можно добавить дочернюю дорожку</returns>
        public bool CanAddChildLine()
        {
            return NestingLevel < 2; // Максимум 3 уровня вложенности (0,1,2)
        }

        /// <summary>
        /// Пытается добавить дочернюю дорожку
        /// </summary>
        /// <param name="childLine">Дочерняя дорожка для добавления</param>
        /// <returns>True если дорожка успешно добавлена</returns>
        public bool TryAddChildLine(PoolLine childLine)
        {
            if (!CanAddChildLine())
                return false;

            childLine.NestingLevel = this.NestingLevel + 1;
            ChildLines.Add(childLine);
            return true;
        }

        /// <summary>
        /// Проверяет возможность добавления вложенной дорожки
        /// </summary>
        /// <returns>True если можно добавить вложенную дорожку</returns>
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

        /// <summary>
        /// Обновляет позицию дорожки и всех ее потомков
        /// </summary>
        /// <param name="deltaX">Смещение по оси X</param>
        /// <param name="deltaY">Смещение по оси Y</param>
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