using System.Collections.Generic;
using System.Drawing;
using Kinis.Models;

namespace Kinis
{
    /// <summary>
    /// Модель листа холста для поддержки нескольких рабочих областей
    /// </summary>
    public class CanvasSheet
    {
        /// <summary>
        /// Название листа
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Список блоков на листе
        /// </summary>
        public List<BpmnBlock> Blocks { get; set; } = new List<BpmnBlock>();

        /// <summary>
        /// Список стрелок на листе
        /// </summary>
        public List<BpmnArrow> Arrows { get; set; } = new List<BpmnArrow>();

        /// <summary>
        /// Масштаб отображения листа
        /// </summary>
        public float Zoom { get; set; } = 1.0f;

        /// <summary>
        /// Смещение холста листа
        /// </summary>
        public PointF CanvasOffset { get; set; } = new PointF(0, 0);

        /// <summary>
        /// Инициализирует новый пустой лист
        /// </summary>
        public CanvasSheet() { }

        /// <summary>
        /// Инициализирует новый лист с указанным именем
        /// </summary>
        /// <param name="name">Название листа</param>
        public CanvasSheet(string name) { Name = name; }
    }
}