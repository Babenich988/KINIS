//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Drawing;

//namespace Kinis.Models
//{
//    public class Pool
//    {
//        public string Id { get; set; } = Guid.NewGuid().ToString();
//        public PoolName Name { get; set; } = new PoolName();
//        public List<PoolLine> Lines { get; set; } = new List<PoolLine>();
//        public RectangleF Bounds { get; set; }
//        public Color FillColor { get; set; } = Color.White;
//        public Color BorderColor { get; set; } = Color.Black;

//        public Pool()
//        {
//            // Добавляем первую линию по умолчанию
//            Lines.Add(new PoolLine());
//        }
//        public void Draw(Graphics g, bool isSelected = false)
//        {
//            // Отрисовка основного прямоугольника пула
//            using (var brush = new SolidBrush(FillColor))
//            using (var pen = new Pen(BorderColor, 2))
//            {
//                g.FillRectangle(brush, Bounds);
//                g.DrawRectangle(pen, Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height);
//            }

//            // Отрисовка линий
//            foreach (var line in Lines)
//            {
//                DrawLine(g, line);
//            }

//            // Отрисовка названия
//            Name.Draw(g);
//        }

//        private void DrawLine(Graphics g, PoolLine line)
//        {
//            // Отрисовка линии и её дочерних линий
//            using (var brush = new SolidBrush(line.FillColor))
//            using (var pen = new Pen(line.BorderColor, 1))
//            {
//                g.FillRectangle(brush, line.Bounds);
//                g.DrawRectangle(pen, line.Bounds.X, line.Bounds.Y,
//                               line.Bounds.Width, line.Bounds.Height);
//            }

//            // Рекурсивная отрисовка дочерних линий
//            foreach (var child in line.ChildLines)
//            {
//                DrawLine(g, child);
//            }
//        }
//    }
//}
