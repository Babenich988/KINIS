using Kinis.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Kinis
{
    public class InfiniteCanvas : Panel
    {
        // Координаты для отслеживания движения мыши
        private Point lastMousePos;
        private bool isDragging = false;
        private PointF canvasOffset = PointF.Empty;

        // Коллекция блоков и выделенный блок
        private List<BpmnBlock> blocks = new List<BpmnBlock>();
        private BpmnBlock selectedBlock = null;

        // Индекс выбранной ручки и флаг растягивания
        private int selectedHandleIndex = -1;
        private bool isResizing = false;

        // Получение и установка списка блоков
        public void SetBlocks(List<BpmnBlock> b)
        {
            blocks = b;
            Invalidate();
        }

        public List<BpmnBlock> GetBlocks() => blocks;

        // Конструктор: настройка панели
        public InfiniteCanvas()
        {
            this.DoubleBuffered = true;
            this.AutoScroll = false;
            this.BackColor = Color.White;
            this.BorderStyle = BorderStyle.FixedSingle;

            // Подписка на события
            this.MouseDown += InfiniteCanvas_MouseDown;
            this.MouseMove += InfiniteCanvas_MouseMove;
            this.MouseUp += InfiniteCanvas_MouseUp;
            this.Paint += InfiniteCanvas_Paint;

            // Разрешаем фокус
            this.SetStyle(ControlStyles.Selectable, true);
            this.TabStop = true;
        }

        // Обработка клика мыши
        private void InfiniteCanvas_MouseDown(object sender, MouseEventArgs e)
        {
            lastMousePos = e.Location;
            selectedBlock = null;
            selectedHandleIndex = -1;

            // Проверяем, попал ли пользователь в блок или ручку
            foreach (var block in blocks)
            {
                if (block.Bounds.Contains(e.Location))
                {
                    selectedBlock = block;
                    break;
                }

                var handles = block.GetResizeHandles();
                for (int i = 0; i < handles.Length; i++)
                {
                    if (handles[i].Contains(e.Location))
                    {
                        selectedBlock = block;
                        selectedHandleIndex = i;
                        isResizing = true;
                        break;
                    }
                }
            }

            Invalidate();
        }

        // Перемещение мыши (растягивание блока)
        private void InfiniteCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isResizing && selectedBlock != null)
            {
                float dx = e.X - lastMousePos.X;
                float dy = e.Y - lastMousePos.Y;

                var rect = selectedBlock.Bounds;

                // Изменяем размеры в зависимости от выбранной ручки
                switch (selectedHandleIndex)
                {
                    case 0: rect.X += dx; rect.Y += dy; rect.Width -= dx; rect.Height -= dy; break; // ЛВ
                    case 1: rect.Y += dy; rect.Width += dx; rect.Height -= dy; break;             // ПВ
                    case 2: rect.X += dx; rect.Width -= dx; rect.Height += dy; break;             // ЛН
                    case 3: rect.Width += dx; rect.Height += dy; break;                           // ПН
                }

                selectedBlock.Bounds = rect;
                lastMousePos = e.Location;
                Invalidate();
            }
        }

        // Отпускание кнопки мыши
        private void InfiniteCanvas_MouseUp(object sender, MouseEventArgs e)
        {
            isResizing = false;
            selectedHandleIndex = -1;
        }

        // Основной метод отрисовки
        private void InfiniteCanvas_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TranslateTransform(canvasOffset.X, canvasOffset.Y);

            DrawGrid(g); // Сетка
            foreach (var block in blocks)
                block.Draw(g, block == selectedBlock);
        }

        // Рисуем сетку на фоне
        private void DrawGrid(Graphics g)
        {
            int gridSize = 20;
            Color gridColor = Color.Gray;

            RectangleF visibleBounds = GetVisibleBounds();
            int startX = (int)(visibleBounds.Left / gridSize) * gridSize - gridSize;
            int startY = (int)(visibleBounds.Top / gridSize) * gridSize - gridSize;
            int endX = (int)(visibleBounds.Right / gridSize) * gridSize + gridSize;
            int endY = (int)(visibleBounds.Bottom / gridSize) * gridSize + gridSize;

            using (Pen gridPen = new Pen(gridColor, 1))
            {
                for (int x = startX; x <= endX; x += gridSize)
                    g.DrawLine(gridPen, x, startY, x, endY); // Вертикальные линии

                for (int y = startY; y <= endY; y += gridSize)
                    g.DrawLine(gridPen, startX, y, endX, y); // Горизонтальные линии
            }
        }

        // Возвращает видимую часть холста
        private RectangleF GetVisibleBounds()
        {
            return new RectangleF(-canvasOffset.X, -canvasOffset.Y, this.Width, this.Height);
        }

        // Сброс положения камеры
        public void ResetView()
        {
            canvasOffset = PointF.Empty;
            this.Invalidate();
        }

        public PointF CanvasOffset => canvasOffset;
    }
}
