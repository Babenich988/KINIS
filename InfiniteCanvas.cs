using Kinis.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Linq;

namespace Kinis
{
    public class InfiniteCanvas : Panel
    {
        private Point lastMousePos;
        private bool isDragging = false;
        private bool isDraggingBlock = false;
        private PointF canvasOffset = PointF.Empty;
        private float zoom = 1.0f;
        private List<BpmnBlock> blocks = new List<BpmnBlock>();
        private BpmnBlock selectedBlock = null;
        private PointF blockDragStart;

        public void SetBlocks(List<BpmnBlock> newBlocks)
        {
            blocks = newBlocks ?? new List<BpmnBlock>();
            this.Invalidate();
        }

        public List<BpmnBlock> GetBlocks()
        {
            return blocks;
        }
        public InfiniteCanvas()
        {
            this.DoubleBuffered = true;
            this.AutoScroll = false;
            this.BackColor = Color.White;
            this.BorderStyle = BorderStyle.FixedSingle;

            this.MouseDown += InfiniteCanvas_MouseDown;
            this.MouseMove += InfiniteCanvas_MouseMove;
            this.MouseUp += InfiniteCanvas_MouseUp;
            this.Paint += InfiniteCanvas_Paint;
            this.MouseClick += InfiniteCanvas_MouseClick;

            //Создаем фокус
            this.SetStyle(ControlStyles.Selectable, true);
            this.TabStop = true;
        }
        private void InfiniteCanvas_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && !IsCtrlPressed())
            {
                PointF virtualPos = ScreenToVirtual(e.Location);
                selectedBlock = GetBlockAtPoint(virtualPos);
                this.Invalidate();
            }
        }

        private void InfiniteCanvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                PointF virtualPos = ScreenToVirtual(e.Location);
                if (IsCtrlPressed())
                {
                    //Перемещение поля
                    isDragging = true;
                    lastMousePos = e.Location;
                    this.Cursor = Cursors.SizeAll;
                    this.Focus();
                }
                else
                {
                    //Перемещение блока
                    selectedBlock = GetBlockAtPoint(virtualPos);
                    if (selectedBlock != null)
                    {
                        isDraggingBlock = true;
                        blockDragStart = virtualPos;
                        this.Cursor = Cursors.SizeAll;
                    }
                }
            }
        }
        private void InfiniteCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            PointF virtualPos = ScreenToVirtual(e.Location);
            if (isDragging && IsCtrlPressed())
            {
                //Перемещение поле с учетом зума
                float deltaX = (e.X - lastMousePos.X) / zoom;
                float deltaY = (e.Y - lastMousePos.Y) / zoom;

                canvasOffset.X += deltaX;
                canvasOffset.Y += deltaY;

                lastMousePos = e.Location;
                this.Invalidate();
            }
            else if (isDraggingBlock && selectedBlock != null)
            {
                //Перемещение блока
                float deltaX = virtualPos.X - blockDragStart.X;
                float deltaY = virtualPos.Y - blockDragStart.Y;

                selectedBlock.Bounds = new RectangleF
                    (
                    selectedBlock.Bounds.X + deltaX,
                    selectedBlock.Bounds.Y + deltaY,
                    selectedBlock.Bounds.Width,
                    selectedBlock.Bounds.Height
                    );

                blockDragStart = virtualPos;
                this.Invalidate();
            }
        }
        private void InfiniteCanvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = false;
                isDraggingBlock = false;
                this.Cursor = Cursors.Default;
            }
        }
        private void InfiniteCanvas_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Используем смещение (панорамирование)
            g.TranslateTransform(canvasOffset.X * zoom, canvasOffset.Y * zoom);
            g.ScaleTransform(zoom, zoom);

            DrawGrid(g);

            // ------------------------
            // Рисуем блоки поверх сетки
            // ------------------------
            if (blocks != null)
            {
                foreach (var block in blocks)
                {
                    // блок.Draw должен рисовать в своих координатах.
                    bool isSelected = (block == selectedBlock);
                    block.Draw(g, isSelected);
                }
            }
        }
        private PointF ScreenToVirtual(Point screenPoint)
        {
            return new PointF
                (
                (screenPoint.X - canvasOffset.X * zoom) / zoom,
                (screenPoint.Y - canvasOffset.Y * zoom) / zoom
                );
        }
        private BpmnBlock GetBlockAtPoint(PointF point)
        {
            //Выбор верхнего блока при перекрытии
            foreach (var block in blocks.AsEnumerable().Reverse())
            {
                if (block.Bounds.Contains(point))
                    return block;
            }
            return null;
        }
        //Методы для зума
        public void ZoomIn()
        {
            zoom *= 1.2f;
            this.Invalidate();
        }
        public void ZoomOut()
        {
            zoom /= 1.2f;
            this.Invalidate();
        }
        public void ResetZoom()
        {
            zoom = 1.0f;
            this.Invalidate();
        }
        private bool IsCtrlPressed()
        {
            return (Control.ModifierKeys & Keys.Control) == Keys.Control;
        }
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
                //Вертикальные линии
                for (int x = startX; x <= endX; x += gridSize)
                {
                    g.DrawLine(gridPen, x, startY, x, endY);
                }
                //Горизонтальные линии
                for (int y = startY; y <= endY; y += gridSize)
                {
                    g.DrawLine(gridPen, startX, y, endX, y);
                }
            }
        }

        private RectangleF GetVisibleBounds()
        {
            return new RectangleF(-canvasOffset.X, -canvasOffset.Y,
                this.Width / zoom, this.Height / zoom);
        }

        //Публичные методы управления
        public void ResetView()
        {
            canvasOffset = PointF.Empty;
            zoom = 1.0f;
            this.Invalidate();
        }

        public PointF CanvasOffset => canvasOffset;
        public float Zoom => zoom;
    }
}