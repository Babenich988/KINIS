using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Kinis.Models
{
    /// <summary>
    /// Композитный класс для представления пула BPMN с дорожками
    /// </summary>
    public class PoolComposite
    {
        /// <summary>
        /// Уникальный идентификатор композитного пула
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Блок названия пула (вертикальная полоса)
        /// </summary>
        public BpmnBlock NameBlock { get; set; }

        /// <summary>
        /// Основной блок тела пула
        /// </summary>
        public BpmnBlock BodyBlock { get; set; }

        /// <summary>
        /// Список дорожек внутри пула
        /// </summary>
        public List<PoolLine> Lanes { get; set; } = new List<PoolLine>();

        /// <summary>
        /// Инициализирует новый экземпляр композитного пула
        /// </summary>
        /// <param name="x">Координата X левого верхнего угла</param>
        /// <param name="y">Координата Y левого верхнего угла</param>
        /// <param name="width">Ширина пула</param>
        /// <param name="height">Высота пула</param>
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

        /// <summary>
        /// Получает общие границы пула (включая блок названия и тело)
        /// </summary>
        public RectangleF Bounds
        {
            get => new RectangleF(
                NameBlock.Bounds.X,
                NameBlock.Bounds.Y,
                NameBlock.Bounds.Width + BodyBlock.Bounds.Width,
                Math.Max(NameBlock.Bounds.Height, BodyBlock.Bounds.Height)
            );
        }

        /// <summary>
        /// Перемещает весь пул вместе с дорожками
        /// </summary>
        /// <param name="deltaX">Смещение по оси X</param>
        /// <param name="deltaY">Смещение по оси Y</param>
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

        /// <summary>
        /// Изменяет размер пула и обновляет дорожки
        /// </summary>
        /// <param name="newWidth">Новая ширина пула</param>
        /// <param name="newHeight">Новая высота пула</param>
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

        /// <summary>
        /// Добавляет новую дорожку в пул
        /// </summary>
        /// <param name="laneName">Название дорожки</param>
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

        /// <summary>
        /// Удаляет дорожку из пула
        /// </summary>
        /// <param name="lane">Дорожка для удаления</param>
        public void RemoveLane(PoolLine lane)
        {
            Lanes.Remove(lane);
            // TODO: Пересчитать позиции оставшихся дорожек
        }

        /// <summary>
        /// Получает дорожку по указанной точке
        /// </summary>
        /// <param name="point">Точка для проверки</param>
        /// <returns>Дорожка, содержащая точку, или null</returns>
        public PoolLine GetLaneAtPoint(PointF point)
        {
            foreach (var lane in Lanes.AsEnumerable().Reverse())
            {
                if (lane.Bounds.Contains(point))
                    return lane;
            }
            return null;
        }

        /// <summary>
        /// Отрисовывает пул и все его компоненты
        /// </summary>
        /// <param name="g">Графический контекст для рисования</param>
        /// <param name="isSelected">Указывает выделен ли пул</param>
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