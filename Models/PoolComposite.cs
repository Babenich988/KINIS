using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Kinis.Models
{
    public class PoolComposite
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public BpmnBlock NameBlock { get; set; }
        public BpmnBlock BodyBlock { get; set; }
        public List<PoolLine> Lanes { get; set; } = new List<PoolLine>();

        public PoolComposite(float x, float y, float width = 400, float height = 200)
        {
            // Блок названия (левая полоса)
            NameBlock = new BpmnBlock(x, y, 40, height)
            {
                Type = "PoolName",
                Text = "Pool",
                FillColor = Color.White,
                BorderColor = Color.Black
            };

            // Основное тело пула
            BodyBlock = new BpmnBlock(x + 40, y, width - 40, height)
            {
                Type = "PoolBody",
                Text = "",
                FillColor = Color.White,
                BorderColor = Color.Black
            };
        }

        public RectangleF Bounds
        {
            get => new RectangleF(
                NameBlock.Bounds.X,
                NameBlock.Bounds.Y,
                NameBlock.Bounds.Width + BodyBlock.Bounds.Width,
                Math.Max(NameBlock.Bounds.Height, BodyBlock.Bounds.Height)
            );
        }

        public void Move(float deltaX, float deltaY)
        {
            NameBlock.Bounds = new RectangleF(
                NameBlock.Bounds.X + deltaX,
                NameBlock.Bounds.Y + deltaY,
                NameBlock.Bounds.Width,
                NameBlock.Bounds.Height
            );

            BodyBlock.Bounds = new RectangleF(
                BodyBlock.Bounds.X + deltaX,
                BodyBlock.Bounds.Y + deltaY,
                BodyBlock.Bounds.Width,
                BodyBlock.Bounds.Height
            );

            // Перемещаем все дорожки
            foreach (var lane in Lanes)
            {
                lane.Bounds = new RectangleF(
                    lane.Bounds.X + deltaX,
                    lane.Bounds.Y + deltaY,
                    lane.Bounds.Width,
                    lane.Bounds.Height
                );
            }
        }

        public void Resize(float newWidth, float newHeight)
        {
            BodyBlock.Bounds = new RectangleF(
                BodyBlock.Bounds.X,
                BodyBlock.Bounds.Y,
                newWidth - 40, // Минус ширина NameBlock
                newHeight
            );

            NameBlock.Bounds = new RectangleF(
                NameBlock.Bounds.X,
                NameBlock.Bounds.Y,
                40,
                newHeight
            );

            // Обновляем ширину дорожек
            foreach (var lane in Lanes)
            {
                lane.Bounds = new RectangleF(
                    lane.Bounds.X,
                    lane.Bounds.Y,
                    newWidth - 40,
                    lane.Bounds.Height
                );
            }
        }
        public void AddLane(string laneName)
        {
            float laneHeight = 60f;
            float startY = BodyBlock.Bounds.Y;

            if (Lanes.Count > 0)
            {
                var lastLane = Lanes[Lanes.Count - 1];
                startY = lastLane.Bounds.Bottom;
            }

            var newLane = new PoolLine
            {
                Text = laneName,
                Bounds = new RectangleF(
                    BodyBlock.Bounds.X,
                    startY,
                    BodyBlock.Bounds.Width,
                    laneHeight
                )
            };

            Lanes.Add(newLane);

            // Увеличиваем высоту пула при добавлении дорожки
            float newHeight = Math.Max(BodyBlock.Bounds.Height, (startY + laneHeight) - BodyBlock.Bounds.Y);
            Resize(Bounds.Width, newHeight + 40); // +40 для запаса
        }

        public void RemoveLane(PoolLine lane)
        {
            Lanes.Remove(lane);
            // TODO: Пересчитать позиции оставшихся дорожек
        }

        public PoolLine GetLaneAtPoint(PointF point)
        {
            foreach (var lane in Lanes.AsEnumerable().Reverse())
            {
                if (lane.Bounds.Contains(point))
                    return lane;
            }
            return null;
        }

        public void Draw(Graphics g, bool isSelected = false)
        {
            // Отрисовка блоков пула
            NameBlock.Draw(g, false);
            BodyBlock.Draw(g, false);

            // Отрисовка вертикального текста в NameBlock
            using (var font = new Font("Segoe UI", 10))
            using (var textBrush = new SolidBrush(Color.Black))
            using (var format = new StringFormat())
            {
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;

                g.TranslateTransform(
                    NameBlock.Bounds.X + NameBlock.Bounds.Width / 2,
                    NameBlock.Bounds.Y + NameBlock.Bounds.Height / 2
                );
                g.RotateTransform(-90);
                g.DrawString(NameBlock.Text, font, textBrush, 0, 0, format);
                g.ResetTransform();
            }

            // Отрисовка дорожек
            foreach (var lane in Lanes)
            {
                using (var brush = new SolidBrush(lane.FillColor))
                using (var pen = new Pen(lane.BorderColor, 1))
                {
                    g.FillRectangle(brush, lane.Bounds);
                    g.DrawRectangle(pen, lane.Bounds.X, lane.Bounds.Y,
                                  lane.Bounds.Width, lane.Bounds.Height);

                    // Текст дорожки
                    using (var laneFont = new Font("Segoe UI", 9))
                    using (var textBrush = new SolidBrush(Color.Black))
                    {
                        var textSize = g.MeasureString(lane.Text, laneFont);
                        float textX = lane.Bounds.X + 10f;
                        float textY = lane.Bounds.Y + (lane.Bounds.Height - textSize.Height) / 2f;
                        g.DrawString(lane.Text, laneFont, textBrush, textX, textY);
                    }
                }
            }
        }
    }
}