using Kinis.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;

namespace Kinis
{
    public class InfiniteCanvas : Panel
    {
        private Point lastMousePos;
        private bool isDragging = false;
        private bool isDraggingBlock = false;
        private bool isResizing = false; // ДОБАВЛЕНО: флаг изменения размера
        private PointF canvasOffset = PointF.Empty;
        private float zoom = 1.0f;
        private List<BpmnBlock> blocks = new List<BpmnBlock>();
        private BpmnBlock selectedBlock = null;
        private PointF blockDragStart;
        private int selectedHandleIndex = -1; // ДОБАВЛЕНО: индекс выбранной ручки
        private PointF resizeStartPoint; // ДОБАВЛЕНО: начальная точка изменения размера
        private RectangleF originalBounds; // ДОБАВЛЕНО: оригинальные размеры блока

        public void SetBlocks(List<BpmnBlock> b)
        {
            blocks = b;
            Invalidate();
        }

        public List<BpmnBlock> GetBlocks() => blocks;

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
            this.MouseWheel += InfiniteCanvas_MouseWheel;

            this.SetStyle(ControlStyles.Selectable, true);
            this.TabStop = true;
        }

        private void InfiniteCanvas_MouseWheel(object sender, MouseEventArgs e)
        {
            float zoomFactor = e.Delta > 0 ? 1.1f : 0.9f;
            PointF mousePosBeforeZoom = ScreenToVirtual(e.Location);
            zoom *= zoomFactor;
            PointF mousePosAfterZoom = ScreenToVirtual(e.Location);
            canvasOffset.X += (mousePosAfterZoom.X - mousePosBeforeZoom.X) * zoom;
            canvasOffset.Y += (mousePosAfterZoom.Y - mousePosBeforeZoom.Y) * zoom;
            this.Invalidate();
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

                // Сначала проверяем клик на ручки изменения размера
                if (selectedBlock != null)
                {
                    var handles = selectedBlock.GetResizeHandles();
                    for (int i = 0; i < handles.Length; i++)
                    {
                        if (handles[i].Contains(virtualPos))
                        {
                            isResizing = true;
                            selectedHandleIndex = i;
                            resizeStartPoint = virtualPos;
                            originalBounds = selectedBlock.Bounds;
                            return;
                        }
                    }
                }

                if (IsCtrlPressed())
                {
                    // Перемещение поля
                    isDragging = true;
                    lastMousePos = e.Location;
                    this.Cursor = Cursors.SizeAll;
                    this.Focus();
                }
                else
                {
                    // Перемещение блока
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

            // Изменение курсора при наведении на ручки
            if (selectedBlock != null && !isDragging && !isDraggingBlock && !isResizing)
            {
                var handles = selectedBlock.GetResizeHandles();
                bool onHandle = false;
                for (int i = 0; i < handles.Length; i++)
                {
                    if (handles[i].Contains(virtualPos))
                    {
                        // Устанавливаем соответствующий курсор для каждой ручки
                        if (i == 0 || i == 3) this.Cursor = Cursors.SizeNWSE;
                        else if (i == 1 || i == 2) this.Cursor = Cursors.SizeNESW;
                        onHandle = true;
                        break;
                    }
                }
                if (!onHandle) this.Cursor = Cursors.Default;
            }

            if (isDragging && IsCtrlPressed())
            {
                // Перемещение поля с учетом зума
                float deltaX = (e.X - lastMousePos.X) / zoom;
                float deltaY = (e.Y - lastMousePos.Y) / zoom;

                canvasOffset.X += deltaX;
                canvasOffset.Y += deltaY;

                lastMousePos = e.Location;
                this.Invalidate();
            }
            else if (isDraggingBlock && selectedBlock != null)
            {
                // Перемещение блока
                float deltaX = virtualPos.X - blockDragStart.X;
                float deltaY = virtualPos.Y - blockDragStart.Y;

                selectedBlock.Bounds = new RectangleF(
                    selectedBlock.Bounds.X + deltaX,
                    selectedBlock.Bounds.Y + deltaY,
                    selectedBlock.Bounds.Width,
                    selectedBlock.Bounds.Height
                );

                blockDragStart = virtualPos;
                this.Invalidate();
            }
            else if (isResizing && selectedBlock != null)
            {
                // Изменение размера блока
                float deltaX = virtualPos.X - resizeStartPoint.X;
                float deltaY = virtualPos.Y - resizeStartPoint.Y;

                RectangleF newBounds = originalBounds;

                switch (selectedHandleIndex)
                {
                    case 0: // Левый верхний
                        newBounds.X += deltaX;
                        newBounds.Y += deltaY;
                        newBounds.Width -= deltaX;
                        newBounds.Height -= deltaY;
                        break;
                    case 1: // Правый верхний
                        newBounds.Y += deltaY;
                        newBounds.Width += deltaX;
                        newBounds.Height -= deltaY;
                        break;
                    case 2: // Левый нижний
                        newBounds.X += deltaX;
                        newBounds.Width -= deltaX;
                        newBounds.Height += deltaY;
                        break;
                    case 3: // Правый нижний
                        newBounds.Width += deltaX;
                        newBounds.Height += deltaY;
                        break;
                }

                // Минимальный размер
                if (newBounds.Width > 20 && newBounds.Height > 20)
                {
                    selectedBlock.Bounds = newBounds;
                    this.Invalidate();
                }
            }
        }

        private void InfiniteCanvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = false;
                isDraggingBlock = false;
                isResizing = false;
                selectedHandleIndex = -1;
                this.Cursor = Cursors.Default;
            }
        }

        private void InfiniteCanvas_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            g.TranslateTransform(canvasOffset.X * zoom, canvasOffset.Y * zoom);
            g.ScaleTransform(zoom, zoom);

            DrawGrid(g);

            if (blocks != null)
            {
                foreach (var block in blocks)
                {
                    bool isSelected = (block == selectedBlock);
                    block.Draw(g, isSelected);
                }
            }
        }

        private PointF ScreenToVirtual(Point screenPoint)
        {
            return new PointF(
                (screenPoint.X - canvasOffset.X * zoom) / zoom,
                (screenPoint.Y - canvasOffset.Y * zoom) / zoom
            );
        }

        private BpmnBlock GetBlockAtPoint(PointF point)
        {
            foreach (var block in blocks.AsEnumerable().Reverse())
            {
                if (block.Bounds.Contains(point))
                    return block;
            }
            return null;
        }

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
                for (int x = startX; x <= endX; x += gridSize)
                    g.DrawLine(gridPen, x, startY, x, endY);

                for (int y = startY; y <= endY; y += gridSize)
                    g.DrawLine(gridPen, startX, y, endX, y);
            }
        }

        private RectangleF GetVisibleBounds()
        {
            return new RectangleF(-canvasOffset.X, -canvasOffset.Y,
                this.Width / zoom, this.Height / zoom);
        }

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