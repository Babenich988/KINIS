using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace Kinis
{
    public class InfiniteCanvas : Panel
    {
        private Point lastMousePos;
        private bool isDragging = false;
        private PointF canvasOffset = PointF.Empty;

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

            //Создаем фокус
            this.SetStyle(ControlStyles.Selectable, true);
            this.TabStop = true;
        }

        private void InfiniteCanvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && IsCtrlPressed())
            {
                isDragging = true;
                lastMousePos = e.Location;
                this.Cursor = Cursors.SizeAll;
                this.Focus();
            }
        }

        private void InfiniteCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                if (IsCtrlPressed())
                {
                    float deltaX = e.X - lastMousePos.X;
                    float deltaY = e.Y - lastMousePos.Y;

                    canvasOffset.X += deltaX;
                    canvasOffset.Y += deltaY;

                    lastMousePos = e.Location;
                    this.Invalidate();
                }
            }
            else
            {
                isDragging = false;
                this.Cursor = Cursors.Default;
            }
        }

        private void InfiniteCanvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && isDragging)
            {
                isDragging = false;
                this.Cursor = Cursors.Default;
            }
        }

        private bool IsCtrlPressed()
        {
            return (Control.ModifierKeys & Keys.Control) == Keys.Control;
        }

        private void InfiniteCanvas_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            //Используем смещение
            g.TranslateTransform(canvasOffset.X, canvasOffset.Y);
            DrawGrid(g);

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
            return new RectangleF(-canvasOffset.X, -canvasOffset.Y, this.Width, this.Height);
        }

        //Публичные методы управления
        public void ResetView()
        {
            canvasOffset = PointF.Empty;
            this.Invalidate();
        }

        public PointF CanvasOffset => canvasOffset;
    }
}
