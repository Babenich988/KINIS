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
        private bool isResizing = false;
        private PointF canvasOffset = PointF.Empty;
        private float zoom = 1.0f;
        private const float MIN_ZOOM = 0.25f;
        private const float MAX_ZOOM = 5.0f;
        private const float ZOOM_STEP = 1.2f;
        private List<BpmnBlock> blocks = new List<BpmnBlock>();

        // НОВАЯ СИСТЕМА ВЫДЕЛЕНИЯ
        private List<object> selectedElements = new List<object>();
        private object primarySelectedElement = null;

        // Свойства для обратной совместимости
        private BpmnBlock selectedBlock
        {
            get { return primarySelectedElement as BpmnBlock; }
            set
            {
                primarySelectedElement = value;
                if (value != null && !selectedElements.Contains(value))
                {
                    selectedElements.Clear();
                    selectedElements.Add(value);
                }
            }
        }

        private BpmnArrow selectedArrow
        {
            get { return primarySelectedElement as BpmnArrow; }
            set
            {
                primarySelectedElement = value;
                if (value != null && !selectedElements.Contains(value))
                {
                    selectedElements.Clear();
                    selectedElements.Add(value);
                }
            }
        }

        private PointF blockDragStart;
        private int selectedHandleIndex = -1;
        private PointF resizeStartPoint;
        private RectangleF originalBounds;
        private TextBox editTextBox = null;
        private bool autoAdjustCanvasOffset = true;
        private bool isSelecting = false;
        private RectangleF selectionRectangle;
        private PointF selectionDragStartPoint;

        private ContextMenuStrip contextMenu;
        private ToolStripMenuItem deleteMenuItem;
        private List<BpmnArrow> arrows = new List<BpmnArrow>();
        private bool isCreatingArrow = false;
        private BpmnArrow tempArrow = null;
        private BpmnBlock arrowStartBlock = null;
        private PointF arrowStartPoint = PointF.Empty;
        private bool isDraggingArrow = false;
        private bool isDraggingArrowEnd = false;
        private bool isDraggingStartPoint = false;
        private PointF arrowDragStart = PointF.Empty;
        private object lastSelectedElement = null;
        public event Action<float> ZoomChanged;

        public void SetBlocks(List<BpmnBlock> b)
        {
            blocks = b;
            Invalidate();
        }

        public void SetArrows(List<BpmnArrow> a)
        {
            arrows = a ?? new List<BpmnArrow>();
            Invalidate();
        }

        public List<BpmnArrow> GetArrows() => arrows;
        public List<BpmnBlock> GetBlocks() => blocks;

        // НОВЫЕ МЕТОДЫ ДЛЯ РАБОТЫ С ВЫДЕЛЕНИЕМ
        public List<BpmnBlock> GetSelectedBlocks() => selectedElements.OfType<BpmnBlock>().ToList();
        public List<BpmnArrow> GetSelectedArrows() => selectedElements.OfType<BpmnArrow>().ToList();
        public bool IsElementSelected(object element) => selectedElements.Contains(element);
        public List<object> GetSelectedElements() => selectedElements.ToList();
        public void ClearSelection()
        {
            selectedElements.Clear();
            primarySelectedElement = null;
            Invalidate();
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
            this.MouseWheel += InfiniteCanvas_MouseWheel;
            this.MouseDoubleClick += InfiniteCanvas_MouseDoubleClick;
            this.SetStyle(ControlStyles.Selectable, true);
            this.TabStop = true;
            this.Focus();
            this.KeyDown += InfiniteCanvas_KeyDown;

            contextMenu = new ContextMenuStrip();
            deleteMenuItem = new ToolStripMenuItem("Удалить");
            deleteMenuItem.ForeColor = Color.Red;
            deleteMenuItem.Click += DeleteMenuItem_Click;
            contextMenu.Items.Add(deleteMenuItem);
        }

        private void DeleteMenuItem_Click(object sender, EventArgs e)
        {
            DeleteSelectedElements();
        }

        private void InfiniteCanvas_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                DeleteSelectedElements();
            }
        }

        private void DeleteSelectedElements()
        {
            bool changed = false;

            foreach (var element in selectedElements.ToList())
            {
                if (element is BpmnBlock block)
                {
                    blocks.Remove(block);
                    // Удаляем стрелки, привязанные к этому блоку
                    arrows.RemoveAll(a => a.StartBlock == block || a.EndBlock == block);
                    changed = true;
                }
                else if (element is BpmnArrow arrow)
                {
                    arrows.Remove(arrow);
                    changed = true;
                }
            }

            if (changed)
            {
                selectedElements.Clear();
                primarySelectedElement = null;
                Invalidate();
            }
        }

        protected override bool IsInputKey(Keys keyData)
        {
            return true;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.KeyCode == Keys.Delete)
            {
                DeleteSelectedElements();
            }
        }

        private void InfiniteCanvas_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (editTextBox != null)
            {
                UpdateBlockText(false);
            }

            PointF virtualPos = ScreenToVirtual(e.Location);
            var clickedBlock = GetBlockAtPoint(virtualPos);

            if (clickedBlock != null)
            {
                // Устанавливаем как основной выделенный элемент
                if (!selectedElements.Contains(clickedBlock))
                {
                    selectedElements.Clear();
                    selectedElements.Add(clickedBlock);
                }
                primarySelectedElement = clickedBlock;
                selectedBlock = clickedBlock;

                CreateEditTextBox();
            }
        }

        private void CreateEditTextBox()
        {
            if (selectedBlock == null) return;

            editTextBox = new TextBox();
            editTextBox.Text = selectedBlock.Text;

            Point transformedLocation = Point.Round(VirtualToScreen(new PointF(selectedBlock.Bounds.X, selectedBlock.Bounds.Y)));

            editTextBox.Location = transformedLocation;
            editTextBox.Width = (int)(selectedBlock.Bounds.Width * zoom);
            editTextBox.Height = (int)(selectedBlock.Bounds.Height * zoom);

            editTextBox.Multiline = true;
            editTextBox.Font = Font;

            editTextBox.LostFocus += EditTextBox_LostFocus;
            editTextBox.KeyDown += EditTextBox_KeyDown;

            Controls.Add(editTextBox);
            editTextBox.BringToFront();
            editTextBox.Focus();
        }

        private void EditTextBox_LostFocus(object sender, EventArgs e)
        {
            UpdateBlockText(false);
        }

        private void EditTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift)
                {
                    editTextBox.Text += Environment.NewLine;
                    editTextBox.SelectionStart = editTextBox.Text.Length;
                    editTextBox.SelectionLength = 0;
                    e.SuppressKeyPress = true;
                }
                else
                {
                    UpdateBlockText(true);
                    e.SuppressKeyPress = true;
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
                string newText = editTextBox.Text;
                bool textChanged = newText != selectedBlock.Text;

                RemoveEditTextBox();

                if (enterPressed || textChanged)
                {
                    selectedBlock.Text = newText;
                }

                Invalidate();
            }
        }

        private void CancelEdit()
        {
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

        private (BpmnBlock block, PointF point) FindNearestConnectionPoint(PointF virtualPos, float maxDistance = 15f)
        {
            BpmnBlock nearestBlock = null;
            PointF nearestPoint = PointF.Empty;
            float minDistance = float.MaxValue;

            foreach (var block in blocks)
            {
                var points = block.GetConnectionPoints();
                foreach (var point in points)
                {
                    float distance = Distance(point, virtualPos);
                    if (distance < minDistance && distance <= maxDistance)
                    {
                        minDistance = distance;
                        nearestBlock = block;
                        nearestPoint = point;
                    }
                }
            }

            return (nearestBlock, nearestPoint);
        }

        private BpmnArrow GetArrowAtPoint(PointF point)
        {
            foreach (var arrow in arrows.AsEnumerable().Reverse())
            {
                if (arrow.HitTest(point))
                    return arrow;
            }
            return null;
        }

        private float Distance(PointF a, PointF b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        private void UpdateAttachedArrows(BpmnBlock movedBlock, RectangleF previousBounds)
        {
            foreach (var arrow in arrows)
            {
                if (arrow.StartBlock == movedBlock)
                {
                    float deltaX = movedBlock.Bounds.X - previousBounds.X;
                    float deltaY = movedBlock.Bounds.Y - previousBounds.Y;

                    arrow.StartPoint = new PointF(
                        arrow.StartPoint.X + deltaX,
                        arrow.StartPoint.Y + deltaY
                    );
                }

                if (arrow.EndBlock == movedBlock)
                {
                    float deltaX = movedBlock.Bounds.X - previousBounds.X;
                    float deltaY = movedBlock.Bounds.Y - previousBounds.Y;

                    arrow.EndPoint = new PointF(
                        arrow.EndPoint.X + deltaX,
                        arrow.EndPoint.Y + deltaY
                    );
                }
            }
        }

        private void InfiniteCanvas_MouseWheel(object sender, MouseEventArgs e)
        {
            float zoomFactor = e.Delta > 0 ? ZOOM_STEP : 1.0f / ZOOM_STEP;
            float newZoom = zoom * zoomFactor;

            if (newZoom >= MIN_ZOOM && newZoom <= MAX_ZOOM)
            {
                PointF mousePosBeforeZoom = ScreenToVirtual(e.Location);
                zoom = newZoom;
                PointF mousePosAfterZoom = ScreenToVirtual(e.Location);
                canvasOffset.X += (mousePosAfterZoom.X - mousePosBeforeZoom.X) * zoom;
                canvasOffset.Y += (mousePosAfterZoom.Y - mousePosBeforeZoom.Y) * zoom;
                UpdateEditTextBoxLocation();
                this.Invalidate();
            }
        }

        private void InfiniteCanvas_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && !IsCtrlPressed())
            {
                PointF virtualPos = ScreenToVirtual(e.Location);

                // Снимаем выделение при клике в пустое место без Ctrl
                var clickedBlock = GetBlockAtPoint(virtualPos);
                var clickedArrow = GetArrowAtPoint(virtualPos);

                if (clickedBlock == null && clickedArrow == null)
                {
                    selectedElements.Clear();
                    primarySelectedElement = null;
                    Invalidate();
                }
            }
        }

        private void InfiniteCanvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                PointF virtualPos = ScreenToVirtual(e.Location);
                this.Focus();

                // Если не зажат Ctrl - очищаем выделение перед новым выделением
                if (!IsCtrlPressed())
                {
                    selectedElements.Clear();
                    primarySelectedElement = null;
                }

                // 1. ПРОВЕРЯЕМ КЛИК НА МАРКЕРЫ КОНЦОВ СТРЕЛКИ (только для основного выделенного элемента)
                if (primarySelectedElement is BpmnArrow selectedArrowForHandles)
                {
                    if (selectedArrowForHandles.HitTestEndpoint(virtualPos, true))
                    {
                        isDraggingArrowEnd = true;
                        isDraggingStartPoint = true;
                        arrowDragStart = virtualPos;
                        this.Cursor = Cursors.Cross;
                        return;
                    }
                    else if (selectedArrowForHandles.HitTestEndpoint(virtualPos, false))
                    {
                        isDraggingArrowEnd = true;
                        isDraggingStartPoint = false;
                        arrowDragStart = virtualPos;
                        this.Cursor = Cursors.Cross;
                        return;
                    }
                }

                // 2. ПРОВЕРЯЕМ КЛИК НА СТРЕЛКУ
                var clickedArrow = GetArrowAtPoint(virtualPos);
                if (clickedArrow != null)
                {
                    if (!selectedElements.Contains(clickedArrow))
                        selectedElements.Add(clickedArrow);
                    primarySelectedElement = clickedArrow;

                    if (clickedArrow.IsFloating)
                    {
                        isDraggingArrow = true;
                        arrowDragStart = virtualPos;
                        this.Cursor = Cursors.SizeAll;
                    }
                    return;
                }

                // 3. Проверяем клик на ручки изменения размера (только для блоков)
                if (primarySelectedElement is BpmnBlock selectedBlockForHandles)
                {
                    var handles = selectedBlockForHandles.GetResizeHandles();
                    for (int i = 0; i < handles.Length; i++)
                    {
                        if (handles[i].Contains(virtualPos))
                        {
                            isResizing = true;
                            selectedHandleIndex = i;
                            resizeStartPoint = virtualPos;
                            originalBounds = selectedBlockForHandles.Bounds;
                            return;
                        }
                    }
                }

                // 4. Проверяем клик на блок
                var clickedBlock = GetBlockAtPoint(virtualPos);
                if (clickedBlock != null)
                {
                    if (!selectedElements.Contains(clickedBlock))
                        selectedElements.Add(clickedBlock);
                    primarySelectedElement = clickedBlock;
                    isDraggingBlock = true;
                    blockDragStart = virtualPos;
                    this.Cursor = Cursors.SizeAll;
                }
                else
                {
                    // 5. Если кликнули в пустое место И зажат Ctrl - начинаем панорамирование
                    if (IsCtrlPressed())
                    {
                        isDragging = true;
                        lastMousePos = e.Location;
                        this.Cursor = Cursors.SizeAll;
                    }
                    else
                    {
                        // Кликнули в пустое место - начинаем выделение прямоугольником
                        isSelecting = true;
                        selectionDragStartPoint = virtualPos;
                        selectionRectangle = new RectangleF(virtualPos.X, virtualPos.Y, 0, 0);
                        selectedElements.Clear();
                        this.Invalidate();
                    }
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                PointF virtualPos = ScreenToVirtual(e.Location);

                // ПРОВЕРЯЕМ КЛИК НА СТРЕЛКУ ДЛЯ КОНТЕКСТНОГО МЕНЮ
                var clickedArrow = GetArrowAtPoint(virtualPos);
                if (clickedArrow != null)
                {
                    if (!selectedElements.Contains(clickedArrow))
                        selectedElements.Add(clickedArrow);
                    primarySelectedElement = clickedArrow;
                    Invalidate();
                    contextMenu.Show(this, e.Location);
                    return;
                }

                // ПРОВЕРЯЕМ КЛИК НА БЛОК ДЛЯ КОНТЕКСТНОГО МЕНЮ
                var clickedBlock = GetBlockAtPoint(virtualPos);
                if (clickedBlock != null)
                {
                    if (!selectedElements.Contains(clickedBlock))
                        selectedElements.Add(clickedBlock);
                    primarySelectedElement = clickedBlock;
                    Invalidate();
                    contextMenu.Show(this, e.Location);
                    return;
                }

                contextMenu.Hide();
            }
        }

        private void InfiniteCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            PointF virtualPos = ScreenToVirtual(e.Location);

            // 1. ПЕРЕТАСКИВАНИЕ КОНЦА СТРЕЛКИ
            if (isDraggingArrowEnd && primarySelectedElement is BpmnArrow selectedArrowForDrag)
            {
                if (IsCtrlPressed())
                {
                    selectedArrowForDrag.Detach(isDraggingStartPoint);
                    if (isDraggingStartPoint)
                        selectedArrowForDrag.StartPoint = virtualPos;
                    else
                        selectedArrowForDrag.EndPoint = virtualPos;
                }
                else
                {
                    var (block, point) = FindNearestConnectionPoint(virtualPos);
                    if (block != null)
                    {
                        selectedArrowForDrag.Attach(isDraggingStartPoint, block, point);
                    }
                    else
                    {
                        selectedArrowForDrag.Detach(isDraggingStartPoint);
                        if (isDraggingStartPoint)
                            selectedArrowForDrag.StartPoint = virtualPos;
                        else
                            selectedArrowForDrag.EndPoint = virtualPos;
                    }
                }

                this.Invalidate();
                return;
            }

            // 2. ПЕРЕМЕЩЕНИЕ ВСЕЙ СТРЕЛКИ (ЕСЛИ ОНА СВОБОДНАЯ)
            if (isDraggingArrow && primarySelectedElement is BpmnArrow selectedArrowForMove && selectedArrowForMove.IsFloating)
            {
                float deltaX = virtualPos.X - arrowDragStart.X;
                float deltaY = virtualPos.Y - arrowDragStart.Y;
                selectedArrowForMove.Move(deltaX, deltaY);
                arrowDragStart = virtualPos;
                this.Invalidate();
                return;
            }

            // 3. ПАНОРАМИРОВАНИЕ ХОЛСТА
            if (isDragging && IsCtrlPressed())
            {
                float deltaX = (e.X - lastMousePos.X) / zoom;
                float deltaY = (e.Y - lastMousePos.Y) / zoom;

                canvasOffset.X += deltaX;
                canvasOffset.Y += deltaY;

                lastMousePos = e.Location;
                this.Invalidate();
                return;
            }

            // 4. ПРОВЕРКА КУРСОРОВ ДЛЯ РУЧЕК РАЗМЕРА
            if (primarySelectedElement is BpmnBlock blockForCursor && !isDragging && !isDraggingBlock && !isResizing && !isSelecting)
            {
                var handles = blockForCursor.GetResizeHandles();
                bool onHandle = false;
                for (int i = 0; i < handles.Length; i++)
                {
                    if (handles[i].Contains(virtualPos))
                    {
                        if (i == 0 || i == 3) this.Cursor = Cursors.SizeNWSE;
                        else if (i == 1 || i == 2) this.Cursor = Cursors.SizeNESW;
                        onHandle = true;
                        break;
                    }
                }
                if (!onHandle) this.Cursor = Cursors.Default;
            }

            // 5. ПЕРЕМЕЩЕНИЕ ВЫДЕЛЕННЫХ ЭЛЕМЕНТОВ
            if (isDraggingBlock && selectedElements.Count > 0)
            {
                float deltaX = virtualPos.X - blockDragStart.X;
                float deltaY = virtualPos.Y - blockDragStart.Y;

                // Перемещаем все выделенные элементы
                foreach (var element in selectedElements)
                {
                    if (element is BpmnBlock blockToMove)
                    {
                        RectangleF previousBounds = blockToMove.Bounds;
                        RectangleF newBounds = new RectangleF(
                            blockToMove.Bounds.X + deltaX,
                            blockToMove.Bounds.Y + deltaY,
                            blockToMove.Bounds.Width,
                            blockToMove.Bounds.Height
                        );

                        if (autoAdjustCanvasOffset)
                        {
                            AdjustCanvasOffsetForBlock(newBounds);
                        }

                        blockToMove.Bounds = newBounds;
                        UpdateAttachedArrows(blockToMove, previousBounds);
                    }
                    else if (element is BpmnArrow arrowToMove && arrowToMove.IsFloating)
                    {
                        // Перемещаем только свободные стрелки
                        arrowToMove.Move(deltaX, deltaY);
                    }
                }

                blockDragStart = virtualPos;
                UpdateEditTextBoxLocation();
                this.Invalidate();
            }
            // 6. ИЗМЕНЕНИЕ РАЗМЕРА БЛОКА
            else if (isResizing && primarySelectedElement is BpmnBlock resizingBlock)
            {
                float deltaX = virtualPos.X - resizeStartPoint.X;
                float deltaY = virtualPos.Y - resizeStartPoint.Y;

                RectangleF newBounds = originalBounds;

                switch (selectedHandleIndex)
                {
                    case 0:
                        newBounds.X += deltaX;
                        newBounds.Y += deltaY;
                        newBounds.Width -= deltaX;
                        newBounds.Height -= deltaY;
                        break;
                    case 1:
                        newBounds.Y += deltaY;
                        newBounds.Width += deltaX;
                        newBounds.Height -= deltaY;
                        break;
                    case 2:
                        newBounds.X += deltaX;
                        newBounds.Width -= deltaX;
                        newBounds.Height += deltaY;
                        break;
                    case 3:
                        newBounds.Width += deltaX;
                        newBounds.Height += deltaY;
                        break;
                }

                if (newBounds.Width > 20 && newBounds.Height > 20)
                {
                    if (autoAdjustCanvasOffset)
                    {
                        AdjustCanvasOffsetForBlock(newBounds);
                    }
                    resizingBlock.Bounds = newBounds;
                    UpdateAttachedArrows(resizingBlock, originalBounds);
                    UpdateEditTextBoxLocation();
                    this.Invalidate();
                }
            }
            // 7. ВЫДЕЛЕНИЕ ГРУППЫ БЛОКОВ
            else if (isSelecting)
            {
                float x = Math.Min(selectionDragStartPoint.X, virtualPos.X);
                float y = Math.Min(selectionDragStartPoint.Y, virtualPos.Y);
                float width = Math.Abs(virtualPos.X - selectionDragStartPoint.X);
                float height = Math.Abs(virtualPos.Y - selectionDragStartPoint.Y);

                selectionRectangle = new RectangleF(x, y, width, height);
                Invalidate();
            }
            // 8. СБРОС КУРСОРА
            else if (!isDragging && !isDraggingBlock && !isResizing && !isSelecting)
            {
                if (this.Cursor != Cursors.Default)
                    this.Cursor = Cursors.Default;
            }
        }

        private void InfiniteCanvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (isDraggingArrowEnd && primarySelectedElement is BpmnArrow selectedArrowForAttach)
                {
                    PointF virtualPos = ScreenToVirtual(e.Location);
                    var (block, point) = FindNearestConnectionPoint(virtualPos);

                    if (block != null)
                    {
                        selectedArrowForAttach.Attach(isDraggingStartPoint, block, point);
                    }
                }

                if (isSelecting)
                {
                    isSelecting = false;

                    // Выделяем блоки, полностью попадающие в прямоугольник
                    foreach (var blockInSelection in blocks)
                    {
                        if (selectionRectangle.Contains(blockInSelection.Bounds))
                        {
                            if (!selectedElements.Contains(blockInSelection))
                                selectedElements.Add(blockInSelection);
                        }
                    }

                    // Выделяем стрелки, у которых оба конца попадают в прямоугольник
                    foreach (var arrowInSelection in arrows)
                    {
                        if (selectionRectangle.Contains(arrowInSelection.StartPoint) &&
                            selectionRectangle.Contains(arrowInSelection.EndPoint))
                        {
                            if (!selectedElements.Contains(arrowInSelection))
                                selectedElements.Add(arrowInSelection);
                        }
                    }

                    // Устанавливаем основной элемент (первый из выделенных)
                    if (selectedElements.Count > 0)
                    {
                        primarySelectedElement = selectedElements[0];
                    }

                    this.Invalidate();
                }

                // Сбрасываем все флаги перетаскивания
                isDragging = false;
                isDraggingBlock = false;
                isResizing = false;
                isDraggingArrowEnd = false;
                isDraggingArrow = false;
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

            // Сначала рисуем стрелки (под блоками)
            if (arrows != null)
            {
                foreach (var arrow in arrows)
                {
                    bool isSelected = selectedElements.Contains(arrow);
                    arrow.Draw(g, isSelected);
                }
            }

            // Затем блоки (поверх стрелок)
            if (blocks != null)
            {
                foreach (var block in blocks)
                {
                    bool isSelected = selectedElements.Contains(block);
                    block.Draw(g, isSelected);
                }
            }

            // Рисуем прямоугольник выделения
            if (isSelecting)
            {
                using (Pen selectPen = new Pen(Color.Blue, 2))
                {
                    selectPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                    g.DrawRectangle(selectPen, selectionRectangle.X, selectionRectangle.Y,
                                  selectionRectangle.Width, selectionRectangle.Height);
                }
            }

            g.ResetTransform();
            DrawZoomPercentage(g);
            UpdateEditTextBoxLocation();
        }

        private void DrawZoomPercentage(Graphics g)
        {
            string zoomText = $"Масштаб: {(int)(zoom * 100)}%";

            using (var font = new Font("Segoe UI", 10, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.Black))
            using (var backgroundBrush = new SolidBrush(Color.FromArgb(200, 255, 255, 255)))
            {
                SizeF textSize = g.MeasureString(zoomText, font);

                RectangleF backgroundRect = new RectangleF(
                    this.Width - textSize.Width - 15,
                    this.Height - textSize.Height - 50,
                    textSize.Width + 10,
                    textSize.Height + 5
                );

                g.FillRectangle(backgroundBrush, backgroundRect);
                g.DrawRectangle(Pens.Gray, backgroundRect.X, backgroundRect.Y, backgroundRect.Width, backgroundRect.Height);

                g.DrawString(zoomText, font, brush,
                    this.Width - textSize.Width - 10,
                    this.Height - textSize.Height - 47);
            }
        }

        public PointF ScreenToWorld(Point screenPt)
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

        private PointF GetElementCenter(object element)
        {
            if (element is BpmnBlock block)
            {
                return new PointF(
                    block.Bounds.X + block.Bounds.Width / 2,
                    block.Bounds.Y + block.Bounds.Height / 2
                );
            }
            else if (element is BpmnArrow arrow)
            {
                if (arrow.ConnectionPoints.Count > 0)
                {
                    PointF start = arrow.ConnectionPoints[0];
                    PointF end = arrow.ConnectionPoints[arrow.ConnectionPoints.Count - 1];
                    return new PointF(
                        (start.X + end.X) / 2,
                        (start.Y + end.Y) / 2
                    );
                }
                else
                {
                    return new PointF(
                        (arrow.StartPoint.X + arrow.EndPoint.X) / 2,
                        (arrow.StartPoint.Y + arrow.EndPoint.Y) / 2
                    );
                }
            }

            return new PointF(0, 0);
        }

        public void FocusOnElement(object element)
        {
            if (element == null) return;

            PointF elementCenter = GetElementCenter(element);

            canvasOffset.X = -elementCenter.X + (this.Width / 2) / zoom;
            canvasOffset.Y = -elementCenter.Y + (this.Height / 2) / zoom;

            this.Invalidate();
        }

        public void ZoomIn()
        {
            float newZoom = zoom * 1.2f;
            if (newZoom <= MAX_ZOOM)
            {
                zoom = newZoom;
                UpdateEditTextBoxLocation();
                this.Invalidate();
                ZoomChanged?.Invoke(zoom);
            }
        }

        public void ZoomOut()
        {
            float newZoom = zoom / ZOOM_STEP;
            if (newZoom >= MIN_ZOOM)
            {
                zoom = newZoom;
                UpdateEditTextBoxLocation();
                this.Invalidate();
                ZoomChanged?.Invoke(zoom);
            }
        }

        public void ResetZoom()
        {
            zoom = 1.0f;

            if (primarySelectedElement != null)
            {
                FocusOnElement(primarySelectedElement);
            }
            else
            {
                canvasOffset = PointF.Empty;
            }

            UpdateEditTextBoxLocation();
            this.Invalidate();
            ZoomChanged?.Invoke(zoom);
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
            selectedElements.Clear();
            primarySelectedElement = null;
            UpdateEditTextBoxLocation();
            this.Invalidate();
        }

        public PointF CanvasOffset => canvasOffset;
        public float Zoom => zoom;

        private void UpdateEditTextBoxLocation()
        {
            if (editTextBox != null && selectedBlock != null)
            {
                Point transformedLocation = Point.Round(VirtualToScreen(new PointF(selectedBlock.Bounds.X, selectedBlock.Bounds.Y)));

                editTextBox.Location = transformedLocation;
                editTextBox.Width = (int)(selectedBlock.Bounds.Width * zoom);
                editTextBox.Height = (int)(selectedBlock.Bounds.Height * zoom);
            }
        }

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