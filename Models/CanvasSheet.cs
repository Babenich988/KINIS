using System.Collections.Generic;
using System.Drawing;
using Kinis.Models;

namespace Kinis
{
    public class CanvasSheet
    {
        public string Name { get; set; }
        public List<BpmnBlock> Blocks { get; set; } = new List<BpmnBlock>();
        public List<BpmnArrow> Arrows { get; set; } = new List<BpmnArrow>();
        public float Zoom { get; set; } = 1.0f;
        public PointF CanvasOffset { get; set; } = new PointF(0, 0);

        public CanvasSheet() { }
        public CanvasSheet(string name) { Name = name; }
    }
}