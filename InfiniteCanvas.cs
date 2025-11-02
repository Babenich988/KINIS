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
        private TextBox editTextBox = null; // Добавляем TextBox для редактирования текста
        private bool autoAdjustCanvasOffset = true; // Флаг для автоматической корректировки смещения
        private ContextMenuStrip contextMenu;//Меню, вызываемые ПКМ.
        private ToolStripMenuItem deleteMenuItem;//Пункт "Удалить".
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
            this.MouseDoubleClick += InfiniteCanvas_MouseDoubleClick; // Добавляем обработчик двойного клика
            this.SetStyle(ControlStyles.Selectable, true);
            this.TabStop = true;

            contextMenu = new ContextMenuStrip();
            deleteMenuItem = new ToolStripMenuItem("Удалить")
            {
                ForeColor = Color.Red,
            };
            deleteMenuItem.Click += DeleteMenuItem_Click;
            contextMenu.Items.Add(deleteMenuItem);  
        }

        private void DeleteMenuItem_Click(object sender, EventArgs e)
        {
            if (selectedBlock != null)
            {
                blocks.Remove(selectedBlock);
                selectedBlock = null;
                Invalidate();
            }
        }

        protected override bool IsInputKey(Keys keyData)//обработчик клавиш на прямую
        {
            return true;
        }
        protected override void OnKeyDown(KeyEventArgs e)//удаление через кнопку delete
        {
            base.OnKeyDown(e);

            if (e.KeyCode == Keys.Delete)
            {
                if (selectedBlock != null) 
                {
                    blocks.Remove(selectedBlock);
                    selectedBlock = null;
                    Invalidate();
                }
            }
        }
        private void InfiniteCanvas_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            // Если уже редактируется какой-либо блок, завершаем редактирование текущего
            if (editTextBox != null)
            {
                UpdateBlockText(false); // Сохраняем изменения текущего блока
            }

            PointF virtualPos = ScreenToVirtual(e.Location);
            selectedBlock = GetBlockAtPoint(virtualPos);

            if (selectedBlock != null)
            {
                CreateEditTextBox();
            }
        }

        private void CreateEditTextBox()
        {
            if (selectedBlock == null) return;

            editTextBox = new TextBox();
            editTextBox.Text = selectedBlock.Text;

            // Transform the block's location for the textbox
            Point transformedLocation = Point.Round(VirtualToScreen(new PointF(selectedBlock.Bounds.X, selectedBlock.Bounds.Y)));

            editTextBox.Location = transformedLocation;
            editTextBox.Width = (int)(selectedBlock.Bounds.Width * zoom);
            editTextBox.Height = (int)(selectedBlock.Bounds.Height * zoom);

            editTextBox.Multiline = true;
            editTextBox.Font = Font; // Ensure the TextBox uses the same font

            editTextBox.LostFocus += EditTextBox_LostFocus;
            editTextBox.KeyDown += EditTextBox_KeyDown;

            Controls.Add(editTextBox);
            editTextBox.BringToFront();
            editTextBox.Focus();
        }

        private void EditTextBox_LostFocus(object sender, EventArgs e)
        {
            UpdateBlockText(false); // передаем false, чтобы указать, что фокус потерян, а не Enter
        }

        private void EditTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift)
                {
                    // Если нажат Shift + Enter, добавляем перенос строки
                    editTextBox.Text += Environment.NewLine;
                    editTextBox.SelectionStart = editTextBox.Text.Length;
                    editTextBox.SelectionLength = 0;
                    e.SuppressKeyPress = true; // Предотвращаем звук "ding"
                }
                else
                {
                    UpdateBlockText(true);  // передаем true, чтобы указать, что был нажат Enter
                    e.SuppressKeyPress = true; // Предотвращаем звук "ding"
                }

            }
            else if (e.KeyCode == Keys.Escape)
            {
                CancelEdit();
            }
        }

        private void UpdateBlockText(bool enterPressed)
        {
            if (selectedBlock != null && editTextBox != null)
            {
                // Сначала сохраняем текст в локальной переменной
                string newText = editTextBox.Text;
                bool textChanged = newText != selectedBlock.Text;

                // Затем убираем editTextBox, чтобы небыло сайд эффектов.
                RemoveEditTextBox();

                // Если был нажат Enter, или текст был изменен, обновляем текст.
                if (enterPressed || textChanged)
                {
                    selectedBlock.Text = newText;
                }

                Invalidate();
            }
        }

        private void CancelEdit()
        {
            // Отменяем редактирование без сохранения изменений
            RemoveEditTextBox();
            Invalidate();
        }


        private void RemoveEditTextBox()
        {
            if (editTextBox != null)
            {
                editTextBox.LostFocus -= EditTextBox_LostFocus;
                editTextBox.KeyDown -= EditTextBox_KeyDown;
                Controls.Remove(editTextBox);
                editTextBox.Dispose();
                editTextBox = null;
            }
        }

        //Метод для вычисления расстояния между двумя точками привязки
        private float Distance(PointF a, PointF b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        private void InfiniteCanvas_MouseWheel(object sender, MouseEventArgs e)
        {
            float zoomFactor = e.Delta > 0 ? 1.1f : 0.9f;
            PointF mousePosBeforeZoom = ScreenToVirtual(e.Location);
            zoom *= zoomFactor;
            PointF mousePosAfterZoom = ScreenToVirtual(e.Location);
            canvasOffset.X += (mousePosAfterZoom.X - mousePosBeforeZoom.X) * zoom;
            canvasOffset.Y += (mousePosAfterZoom.Y - mousePosBeforeZoom.Y) * zoom;

            // Обновляем положение и размер TextBox при изменении масштаба
            UpdateEditTextBoxLocation();

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
            if (e.Button == MouseButtons.Right)
            {
                PointF virtualPos = ScreenToVirtual(e.Location);

                // Сначала проверяем клик на ручки изменения размера
                if (selectedBlock != null)
                {
                    Invalidate();//Подсвечивать выбранный блок
                    contextMenu.Show(this, e.Location);//Показываем меню в позиции
                }
                else
                {
                    contextMenu.Hide();//если кликнули по пустому месту-прячем меню
                }
                return;
            }
            this.Focus();
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

                RectangleF newBounds = new RectangleF(
                    selectedBlock.Bounds.X + deltaX,
                    selectedBlock.Bounds.Y + deltaY,
                    selectedBlock.Bounds.Width,
                    selectedBlock.Bounds.Height
                );

                // Проверяем, чтобы блок не выходил за границы канвы
                if (autoAdjustCanvasOffset)
                {
                    AdjustCanvasOffsetForBlock(newBounds);
                }

                selectedBlock.Bounds = newBounds;

                blockDragStart = virtualPos;

                // Обновляем положение TextBox при перемещении блока
                UpdateEditTextBoxLocation();
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
                    // Проверяем, чтобы блок не выходил за границы канвы
                    if (autoAdjustCanvasOffset)
                    {
                        AdjustCanvasOffsetForBlock(newBounds);
                    }
                    selectedBlock.Bounds = newBounds;

                    // Обновляем положение и размер TextBox при изменении размера блока
                    UpdateEditTextBoxLocation();
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

            g.ResetTransform();

            // Обновляем положение и размер TextBox в Paint
            UpdateEditTextBoxLocation();
        }
        public PointF ScreenToWorld(Point screenPt)//получать реальные координаты холста с учётом прокрутки/масштаба
        {
            float worldX = (screenPt.X - canvasOffset.X) / zoom;
            float worldY = (screenPt.Y - canvasOffset.Y) / zoom;
            return new PointF(worldX, worldY);
        }
        private PointF ScreenToVirtual(Point screenPoint)
        {
            return new PointF(
                (screenPoint.X - canvasOffset.X * zoom) / zoom,
                (screenPoint.Y - canvasOffset.Y * zoom) / zoom
            );
        }
        private PointF VirtualToScreen(PointF virtualPoint)
        {
            return new PointF(
                virtualPoint.X * zoom + canvasOffset.X * zoom,
                virtualPoint.Y * zoom + canvasOffset.Y * zoom
            );
        }

        private BpmnBlock GetBlockAtPoint(PointF point)
        {
            //Чтобы не было сайд эффектов.
            if (editTextBox != null)
            {
                return selectedBlock;
            }

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
            UpdateEditTextBoxLocation();
            this.Invalidate();
        }

        public void ZoomOut()
        {
            zoom /= 1.2f;
            UpdateEditTextBoxLocation();
            this.Invalidate();
        }

        public void ResetZoom()
        {
            zoom = 1.0f;
            UpdateEditTextBoxLocation();
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
            UpdateEditTextBoxLocation();
            this.Invalidate();
        }

        public PointF CanvasOffset => canvasOffset;
        public float Zoom => zoom;

        // Вспомогательный метод для обновления положения и размера TextBox
        private void UpdateEditTextBoxLocation()
        {
            if (editTextBox != null && selectedBlock != null)
            {
                // Transform the block's location for the textbox
                Point transformedLocation = Point.Round(VirtualToScreen(new PointF(selectedBlock.Bounds.X, selectedBlock.Bounds.Y)));

                editTextBox.Location = transformedLocation;
                editTextBox.Width = (int)(selectedBlock.Bounds.Width * zoom);
                editTextBox.Height = (int)(selectedBlock.Bounds.Height * zoom);
            }
        }

        // Метод для корректировки смещения канвы
        private void AdjustCanvasOffsetForBlock(RectangleF blockBounds)
        {
            float virtualWidth = this.Width / zoom;
            float virtualHeight = this.Height / zoom;

            if (blockBounds.X < -canvasOffset.X)
            {
                canvasOffset.X = -blockBounds.X;
            }
            if (blockBounds.Y < -canvasOffset.Y)
            {
                canvasOffset.Y = -blockBounds.Y;
            }

            if (blockBounds.Right > -canvasOffset.X + virtualWidth)
            {
                canvasOffset.X = -(blockBounds.Right - virtualWidth);
            }

            if (blockBounds.Bottom > -canvasOffset.Y + virtualHeight)
            {
                canvasOffset.Y = -(blockBounds.Bottom - virtualHeight);
            }
        }
    }
}
