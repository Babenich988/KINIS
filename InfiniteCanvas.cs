using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Kinis.Models;
using Kinis.Services;
using static Kinis.Services.CommandManager;

namespace Kinis
{
    // Классы для хранения состояний элементов
    public class ArrowState
    {
        public PointF StartPoint { get; set; }
        public PointF EndPoint { get; set; }
        public BpmnBlock StartBlock { get; set; }
        public BpmnBlock EndBlock { get; set; }
    }

    public class InfiniteCanvas : Panel
    {
        private Point lastMousePos;
        private bool isDragging = false;
        private bool isDraggingBlock = false;
        private bool isResizing = false; // флаг изменения размера
        private PointF canvasOffset = PointF.Empty;
        private float zoom = 1.0f;
        private const float MIN_ZOOM = 0.25f;
        private const float MAX_ZOOM = 5.0f;
        private const float ZOOM_STEP = 1.2f;
        private List<BpmnBlock> blocks = new List<BpmnBlock>();

        // СИСТЕМА ВЫДЕЛЕНИЯ
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
        private bool isCreatingArrow = false;
        private bool isDraggingArrow = false;
        private int selectedHandleIndex = -1; // индекс выбранной ручки
        private PointF resizeStartPoint; // начальная точка изменения размера
        private RectangleF originalBounds; // оригинальные размеры блока
        private TextBox editTextBox = null; // TextBox для редактирования текста
        private bool autoAdjustCanvasOffset = true; // Флаг для автоматической корректировки смещения
        private bool isSelecting = false; // Добавлено: флаг выделения группы
        private RectangleF selectionRectangle; // Добавлено: прямоугольник выделения
        private List<BpmnBlock> selectedBlocks = new List<BpmnBlock>(); // Добавлено: список выделенных блоков
        private PointF selectionDragStartPoint; // ДОБАВЛЕНО: Начальная точка перетаскивания для выделения
        private ContextMenuStrip contextMenu;//Меню, вызываемые ПКМ.
        private ToolStripMenuItem deleteMenuItem;//Пункт "Удалить".
        private List<BpmnArrow> arrows = new List<BpmnArrow>();
        private bool isDraggingArrowEnd = false;
        private bool isDraggingStartPoint = false;
        private PointF arrowDragStart = PointF.Empty;
        public event Action<float> ZoomChanged;

        // СИСТЕМА ПЕРЕМЕЩЕНИЯ
        private bool isDraggingElements = false;
        private PointF dragStartPoint;
        private Dictionary<BpmnBlock, RectangleF> originalBlockBounds = new Dictionary<BpmnBlock, RectangleF>();
        private Dictionary<BpmnArrow, ArrowState> originalArrowStates = new Dictionary<BpmnArrow, ArrowState>();
        // Добавляем поле для отслеживания предыдущей позиции
        private RectangleF _previousBlockBounds;
        private bool _isBlockDragInProgress = false;
        private RectangleF _dragStartBounds;

        // Направляющие для выравнивания
        private readonly List<float> verticalGuides = new List<float>();
        private readonly List<float> horizontalGuides = new List<float>();

        // Допуск для срабатывания направляющих (в пикселях)
        private const float GUIDE_TOLERANCE = 8f;

        // Множественное выделение стрелок
        private readonly List<BpmnArrow> selectedArrows = new List<BpmnArrow>();

        // Контекстное меню для стрелок (используется при ПКМ)
        private BpmnArrow contextMenuArrow = null;

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

        // МЕТОДЫ ДЛЯ РАБОТЫ С ВЫДЕЛЕНИЕМ
        public List<BpmnBlock> GetSelectedBlocks() => selectedElements.OfType<BpmnBlock>().ToList();
        public List<BpmnArrow> GetSelectedArrows() => selectedElements.OfType<BpmnArrow>().ToList();
        public bool IsElementSelected(object element) => selectedElements.Contains(element);
        public List<object> GetSelectedElements() => selectedElements.ToList();
        public void ClearSelection()
        {
            selectedElements.Clear();
            primarySelectedElement = null;
            ClearDragStates();
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
            // --- Контекстное меню для удаления блоков ---
            contextMenu = new ContextMenuStrip();
            deleteMenuItem = new ToolStripMenuItem("Удалить");
            deleteMenuItem.ForeColor = Color.Red;
            deleteMenuItem.Click += DeleteMenuItem_Click;
            contextMenu.Items.Add(deleteMenuItem);

        }

        private void DeleteMenuItem_Click(object sender, EventArgs e)
        {
            var form = this.FindForm() as Form1;

            // --- Если есть выделенная группа ---
            if (selectedBlocks.Count > 0 || selectedArrows.Count > 0)
            {
                // Удаляем стрелки, связанные с блоками
                arrows.RemoveAll(a =>
                    selectedBlocks.Contains(a.StartBlock) ||
                    selectedBlocks.Contains(a.EndBlock) ||
                    selectedArrows.Contains(a));

                // Удаляем блоки
                blocks.RemoveAll(b => selectedBlocks.Contains(b));

                selectedBlocks.Clear();
                selectedArrows.Clear();
                selectedBlock = null;
                selectedArrow = null;
                Invalidate();
                return;
            }

            // --- Если один блок ---
            if (selectedBlock != null)
            {
                // Удаляем стрелки, связанные с этим блоком
                arrows.RemoveAll(a => a.StartBlock == selectedBlock || a.EndBlock == selectedBlock);

                blocks.Remove(selectedBlock);
                selectedBlock = null;
                Invalidate();
                return;
            }

            // --- Если одна стрелка ---
            if (selectedArrow != null)
            {
                arrows.Remove(selectedArrow);
                selectedArrow = null;
                Invalidate();
            }
        }


        private void InfiniteCanvas_KeyDown(object sender, KeyEventArgs e)
        {
            bool changed = false;

            foreach (var element in selectedElements.ToList())
            {
                if (element is BpmnBlock block)
                {
                    blocks.Remove(block);
                    // ОТВЯЗЫВАЕМ стрелки от удаленного блока вместо удаления стрелок
                    foreach (var arrow in arrows.ToList())
                    {
                        if (arrow.StartBlock == block)
                        {
                            arrow.StartBlock = null;
                            // Сохраняем текущую позицию стрелки
                            arrow.StartPoint = arrow.StartPoint;
                        }
                        if (arrow.EndBlock == block)
                        {
                            arrow.EndBlock = null;
                            // Сохраняем текущую позицию стрелки
                            arrow.EndPoint = arrow.EndPoint;
                        }
                    }
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
                ClearSelection();
            }
        }

        protected override bool IsInputKey(Keys keyData)//обработчик клавиш на прямую
        {
            return true;
        }

        /// <summary>
        /// Обновляет направляющие при движении блока.
        /// Поддерживает выравнивание по центру и по граням (левая/правая/верхняя/нижняя),
        /// показывая максимум одну линию на ось.
        /// </summary>
        private void UpdateAlignmentGuides(BpmnBlock movingBlock)
        {
            verticalGuides.Clear();
            horizontalGuides.Clear();

            if (movingBlock == null) return;

            float left = movingBlock.Bounds.Left;
            float right = movingBlock.Bounds.Right;
            float top = movingBlock.Bounds.Top;
            float bottom = movingBlock.Bounds.Bottom;
            float centerX = left + movingBlock.Bounds.Width / 2;
            float centerY = top + movingBlock.Bounds.Height / 2;

            float? bestVertical = null;
            float? bestHorizontal = null;
            float minVerticalDistance = float.MaxValue;
            float minHorizontalDistance = float.MaxValue;
            string verticalType = null;
            string horizontalType = null;

            foreach (var block in blocks)
            {
                if (block == movingBlock) continue;

                float bLeft = block.Bounds.Left;
                float bRight = block.Bounds.Right;
                float bTop = block.Bounds.Top;
                float bBottom = block.Bounds.Bottom;
                float bCenterX = bLeft + block.Bounds.Width / 2;
                float bCenterY = bTop + block.Bounds.Height / 2;

                // --- Проверка по оси X ---
                var alignmentsX = new (float movingEdge, float targetEdge, string type)[]
                {
            (centerX, bCenterX, "center"),
            (left, bLeft, "left-left"),
            (right, bRight, "right-right"),
            (left, bRight, "left-right"),
            (right, bLeft, "right-left")
                };

                foreach (var (movingEdge, targetEdge, type) in alignmentsX)
                {
                    float dist = Math.Abs(movingEdge - targetEdge);
                    if (dist < GUIDE_TOLERANCE && dist < minVerticalDistance)
                    {
                        minVerticalDistance = dist;
                        bestVertical = targetEdge;
                        verticalType = type;
                    }
                }

                // --- Проверка по оси Y ---
                var alignmentsY = new (float movingEdge, float targetEdge, string type)[]
                {
            (centerY, bCenterY, "center"),
            (top, bTop, "top-top"),
            (bottom, bBottom, "bottom-bottom"),
            (top, bBottom, "top-bottom"),
            (bottom, bTop, "bottom-top")
                };

                foreach (var (movingEdge, targetEdge, type) in alignmentsY)
                {
                    float dist = Math.Abs(movingEdge - targetEdge);
                    if (dist < GUIDE_TOLERANCE && dist < minHorizontalDistance)
                    {
                        minHorizontalDistance = dist;
                        bestHorizontal = targetEdge;
                        horizontalType = type;
                    }
                }
            }

            // Приоритет центра над гранями
            if (verticalType != null && !verticalType.Contains("center"))
            {
                // Если центр ближе — показываем центр
                if (minVerticalDistance > GUIDE_TOLERANCE / 2)
                    verticalType = verticalType;
            }
            if (horizontalType != null && !horizontalType.Contains("center"))
            {
                if (minHorizontalDistance > GUIDE_TOLERANCE / 2)
                    horizontalType = horizontalType;
            }

            if (bestVertical.HasValue)
                verticalGuides.Add(bestVertical.Value);

            if (bestHorizontal.HasValue)
                horizontalGuides.Add(bestHorizontal.Value);

            Invalidate();
        }
        

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.KeyCode == Keys.Delete)
            {
                // --- Удаление группы ---
                if (selectedBlocks.Count > 0 || selectedArrows.Count > 0)
                {
                    arrows.RemoveAll(a =>
                        selectedBlocks.Contains(a.StartBlock) ||
                        selectedBlocks.Contains(a.EndBlock) ||
                        selectedArrows.Contains(a));

                    blocks.RemoveAll(b => selectedBlocks.Contains(b));

                    selectedBlocks.Clear();
                    selectedArrows.Clear();
                    selectedBlock = null;
                    selectedArrow = null;
                    Invalidate();
                    e.Handled = true;
                    return;
                }

                // --- Удаление одного блока ---
                if (selectedBlock != null)
                {
                    arrows.RemoveAll(a => a.StartBlock == selectedBlock || a.EndBlock == selectedBlock);
                    blocks.Remove(selectedBlock);
                    selectedBlock = null;
                    Invalidate();
                    e.Handled = true;
                    return;
                }

                // --- Удаление одной стрелки ---
                if (selectedArrow != null)
                {
                    arrows.Remove(selectedArrow);
                    selectedArrow = null;
                    Invalidate();
                    e.Handled = true;
                    return;
                }
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
                    ClearSelection();
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

                // Если был нажат Enter, или текст был изменен, обновляем текст.
                if ((enterPressed || textChanged) && textChanged)
                {
                    var form = this.FindForm() as Form1;
                    if (form?.CommandManager != null)
                    {
                        var command = new ChangeTextCommand(selectedBlock, selectedBlock.Text, newText, this);
                        form.CommandManager.Execute(command);
                    }
                    else
                    {
                        selectedBlock.Text = newText; // fallback
                    }
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

        /// <summary>
        /// Находит ближайшую точку соединения на блоке к указанной точке
        /// </summary>
        private PointF FindNearestConnectionPoint(BpmnBlock block, PointF targetPoint)
        {
            var points = block.GetConnectionPoints();
            PointF nearest = points[0];
            float minDistance = Distance(nearest, targetPoint);

            foreach (var point in points)
            {
                float dist = Distance(point, targetPoint);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearest = point;
                }
            }

            return nearest;
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
                    // Вычисляем смещение блока
                    float deltaX = movedBlock.Bounds.X - previousBounds.X;
                    float deltaY = movedBlock.Bounds.Y - previousBounds.Y;

                    // Обновляем точку привязки с учетом смещения
                    arrow.StartPoint = new PointF(
                        arrow.StartPoint.X + deltaX,
                        arrow.StartPoint.Y + deltaY
                    );
                }

                if (arrow.EndBlock == movedBlock)
                {
                    // Вычисляем смещение блока
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
            float newZoom = Math.Max(MIN_ZOOM, Math.Min(MAX_ZOOM, zoom * zoomFactor));

            if (newZoom != zoom)
            {
                // Точка под курсором в виртуальных координатах должна остаться неизменной
                PointF virtualMousePos = ScreenToVirtual(e.Location);

                // Устанавливаем новый зум
                zoom = newZoom;

                // Пересчитываем canvasOffset чтобы виртуальная точка под курсором осталась на месте
                PointF newScreenPos = VirtualToScreen(virtualMousePos);
                canvasOffset.X += (e.Location.X - newScreenPos.X) / zoom;
                canvasOffset.Y += (e.Location.Y - newScreenPos.Y) / zoom;

                // Обновляем UI
                UpdateEditTextBoxLocation();
                this.Invalidate();
                ZoomChanged?.Invoke(zoom);
            }
        }

        private void InfiniteCanvas_MouseClick(object sender, MouseEventArgs e)
        {
            // Обработка одиночного клика для выделения
            if (e.Button == MouseButtons.Left)
            {
                PointF virtualPos = ScreenToVirtual(e.Location);

                // Сначала проверяем стрелку
                var clickedArrow = GetArrowAtPoint(virtualPos);
                if (clickedArrow != null)
                {
                    // Если стрелка уже выделена - НЕ сбрасываем выделение
                    if (!selectedElements.Contains(clickedArrow))
                    {
                        ClearSelection();
                        selectedElements.Add(clickedArrow);
                        primarySelectedElement = clickedArrow;
                    }
                    Invalidate();
                    return;
                }

                // Затем проверяем блок
                var clickedBlock = GetBlockAtPoint(virtualPos);
                if (clickedBlock != null)
                {
                    // Если блок уже выделен - НЕ сбрасываем выделение
                    if (!selectedElements.Contains(clickedBlock))
                    {
                        ClearSelection();
                        selectedElements.Add(clickedBlock);
                        primarySelectedElement = clickedBlock;
                    }
                    Invalidate();
                    return;
                }

                // Если кликнули в пустое место - очищаем выделение
                ClearSelection();
            }
        }

        private void InfiniteCanvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (isCreatingArrow || isDraggingArrowEnd)
            {
                // Не сохраняем позицию если работаем со стрелками
                return;
            }
            if (e.Button == MouseButtons.Left)
            {
                PointF virtualPos = ScreenToVirtual(e.Location);
                this.Focus();

                // 1. ПРОВЕРЯЕМ КЛИК НА МАРКЕРЫ КОНЦОВ СТРЕЛКИ (высший приоритет)
                var clickedArrow = GetArrowAtPoint(virtualPos);
                if (clickedArrow != null)
                {
                    // Проверяем клик на маркеры концов
                    if (clickedArrow.HitTestEndpoint(virtualPos, true))
                    {
                        isDraggingArrowEnd = true;
                        isDraggingStartPoint = true;
                        arrowDragStart = virtualPos;

                        // Выделяем эту стрелку (сбрасываем предыдущее выделение)
                        ClearSelection();
                        selectedElements.Add(clickedArrow);
                        primarySelectedElement = clickedArrow;

                        this.Cursor = Cursors.Cross;
                        Invalidate();
                        return;
                    }
                    else if (clickedArrow.HitTestEndpoint(virtualPos, false))
                    {
                        isDraggingArrowEnd = true;
                        isDraggingStartPoint = false;
                        arrowDragStart = virtualPos;

                        // Выделяем эту стрелку (сбрасываем предыдущее выделение)
                        ClearSelection();
                        selectedElements.Add(clickedArrow);
                        primarySelectedElement = clickedArrow;

                        this.Cursor = Cursors.Cross;
                        Invalidate();
                        return;
                    }
                }

                // 2. Проверяем клик на ручки изменения размера (высокий приоритет)
                var clickedBlock = GetBlockAtPoint(virtualPos);
                if (clickedBlock != null)
                {
                    var resizeArea = GetResizeArea(clickedBlock.Bounds, virtualPos);
                    if (resizeArea != ResizeArea.None)
                    {
                        isResizing = true;
                        selectedHandleIndex = (int)resizeArea;
                        resizeStartPoint = virtualPos;
                        originalBounds = clickedBlock.Bounds;

                        // Выделяем этот блок (сбрасываем предыдущее выделение)
                        ClearSelection();
                        selectedElements.Add(clickedBlock);
                        primarySelectedElement = clickedBlock;

                        this.Cursor = GetResizeCursor(resizeArea);
                        Invalidate();
                        return;
                    }
                }

                // 3. ПРОВЕРЯЕМ КЛИК НА СТРЕЛКУ (для выделения и перемещения)
                if (clickedArrow != null)
                {
                    // Если кликнули по стрелке, которая уже выделена - начинаем перемещение группы
                    if (selectedElements.Contains(clickedArrow))
                    {
                        _previousBlockBounds = selectedBlock.Bounds;
                        var handles = selectedBlock.GetResizeHandles();
                        for (int i = 0; i < handles.Length; i++)
                        {
                            StartElementsDrag(virtualPos);
                            return;
                        }
                    }
                    else
                    {
                        // Клик по новой стрелке - выделяем ТОЛЬКО эту стрелку
                        ClearSelection();
                        selectedElements.Add(clickedArrow);
                        primarySelectedElement = clickedArrow;
                        StartElementsDrag(virtualPos);
                        return;
                    }
                }

                // 4. Проверяем клик на блок (для выделения и перемещения)
                selectedBlock = GetBlockAtPoint(virtualPos);
                if (selectedBlock != null)
                {
                    selectedArrow = null;
                    isDraggingBlock = true;
                    _isBlockDragInProgress = true; // ДОБАВЛЯЕМ
                    _dragStartBounds = selectedBlock.Bounds; // ДОБАВЛЯЕМ - сохраняем начальную позицию
                    blockDragStart = virtualPos;
                    this.Cursor = Cursors.SizeAll;
                }
                else
                {
                    // Если кликнули по блоку, который уже выделен - начинаем перемещение группы
                    if (selectedElements.Contains(clickedBlock))
                    {
                        StartElementsDrag(virtualPos);
                        return;
                    }
                    else
                    {
                        // Клик по новому блоку - выделяем ТОЛЬКО этот блок
                        ClearSelection();
                        selectedElements.Add(clickedBlock);
                        primarySelectedElement = clickedBlock;
                        StartElementsDrag(virtualPos);
                        return;
                    }
                }

                // 5. Если кликнули в пустое место
                if (IsCtrlPressed())
                {
                    // Панорамирование
                    isDragging = true;
                    lastMousePos = e.Location;
                    this.Cursor = Cursors.SizeAll;
                }
                else
                {
                    // Выделение прямоугольником (сбрасывает предыдущее выделение)
                    isSelecting = true;
                    selectionDragStartPoint = virtualPos;
                    selectionRectangle = new RectangleF(virtualPos.X, virtualPos.Y, 0, 0);
                    ClearSelection();
                    this.Invalidate();
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
                    {
                        ClearSelection();
                        selectedElements.Add(clickedArrow);
                    }
                    primarySelectedElement = clickedArrow;
                    contextMenuArrow = clickedArrow;   // <-- сюда сохраняем стрелку
                    selectedArrow = clickedArrow;
                    selectedBlock = null;
                    Invalidate();
                    contextMenu.Show(this, e.Location);
                    return;
                }

                // ПРОВЕРЯЕМ КЛИК НА БЛОК ДЛЯ КОНТЕКСТНОГО МЕНЮ
                var clickedBlock = GetBlockAtPoint(virtualPos);
                if (clickedBlock != null)
                {
                    if (!selectedElements.Contains(clickedBlock))
                    {
                        ClearSelection();
                        selectedElements.Add(clickedBlock);
                    }
                    primarySelectedElement = clickedBlock;
                    contextMenuArrow = null;           // <-- сбрасываем стрелку
                    selectedBlock = clickedBlock;
                    selectedArrow = null;
                    Invalidate();
                    contextMenu.Show(this, e.Location);
                    return;
                }

                // Проверяем, попали ли мы в стрелку
                foreach (var arrow in arrows)
                {
                    if (arrow.HitTest(ScreenToWorld(e.Location)))
                    {
                        contextMenuArrow = arrow;
                        break;
                    }
                }

                // Если клик по стрелке — показываем меню
                if (contextMenuArrow != null)
                {
                    contextMenu.Show(this, e.Location);
                }
                // Если кликнули в пустое место - прячем меню
                contextMenuArrow = null;
                contextMenu.Hide();
            }
        }

        // Перечисление для областей изменения размера
        private enum ResizeArea
        {
            None = -1,
            Top = 0,
            Bottom = 1,
            Left = 2,
            Right = 3,
            TopLeft = 4,
            TopRight = 5,
            BottomLeft = 6,
            BottomRight = 7
        }

        private ResizeArea GetResizeArea(RectangleF bounds, PointF point)
        {
            const float resizeMargin = 6f;
            const float cornerSize = 12f;

            // Проверяем углы (высший приоритет)
            if (point.X >= bounds.Left - resizeMargin && point.X <= bounds.Left + cornerSize &&
                point.Y >= bounds.Top - resizeMargin && point.Y <= bounds.Top + cornerSize)
                return ResizeArea.TopLeft;

            if (point.X >= bounds.Right - cornerSize && point.X <= bounds.Right + resizeMargin &&
                point.Y >= bounds.Top - resizeMargin && point.Y <= bounds.Top + cornerSize)
                return ResizeArea.TopRight;

            if (point.X >= bounds.Left - resizeMargin && point.X <= bounds.Left + cornerSize &&
                point.Y >= bounds.Bottom - cornerSize && point.Y <= bounds.Bottom + resizeMargin)
                return ResizeArea.BottomLeft;

            if (point.X >= bounds.Right - cornerSize && point.X <= bounds.Right + resizeMargin &&
                point.Y >= bounds.Bottom - cornerSize && point.Y <= bounds.Bottom + resizeMargin)
                return ResizeArea.BottomRight;

            // Проверяем края
            if (point.Y >= bounds.Top - resizeMargin && point.Y <= bounds.Top + resizeMargin &&
                point.X >= bounds.Left && point.X <= bounds.Right)
                return ResizeArea.Top;

            if (point.Y >= bounds.Bottom - resizeMargin && point.Y <= bounds.Bottom + resizeMargin &&
                point.X >= bounds.Left && point.X <= bounds.Right)
                return ResizeArea.Bottom;

            if (point.X >= bounds.Left - resizeMargin && point.X <= bounds.Left + resizeMargin &&
                point.Y >= bounds.Top && point.Y <= bounds.Bottom)
                return ResizeArea.Left;

            if (point.X >= bounds.Right - resizeMargin && point.X <= bounds.Right + resizeMargin &&
                point.Y >= bounds.Top && point.Y <= bounds.Bottom)
                return ResizeArea.Right;

            return ResizeArea.None;
        }

        private Cursor GetResizeCursor(ResizeArea area)
        {
            switch (area)
            {
                case ResizeArea.Top:
                case ResizeArea.Bottom:
                    return Cursors.SizeNS;
                case ResizeArea.Left:
                case ResizeArea.Right:
                    return Cursors.SizeWE;
                case ResizeArea.TopLeft:
                case ResizeArea.BottomRight:
                    return Cursors.SizeNWSE;
                case ResizeArea.TopRight:
                case ResizeArea.BottomLeft:
                    return Cursors.SizeNESW;
                default:
                    return Cursors.SizeAll;
            }
        }

        private void StartElementsDrag(PointF virtualPos)
        {
            if (selectedElements.Count > 0)
            {
                isDraggingElements = true;
                dragStartPoint = virtualPos;

                // ВСЕГДА обновляем сохраненные состояния при начале перемещения
                ClearDragStates();

                // Сохраняем оригинальные состояния ВСЕХ выделенных элементов
                foreach (var element in selectedElements)
                {
                    if (element is BpmnBlock block)
                    {
                        originalBlockBounds[block] = block.Bounds;
                    }
                    else if (element is BpmnArrow arrow)
                    {
                        originalArrowStates[arrow] = new ArrowState
                        {
                            StartPoint = arrow.StartPoint,
                            EndPoint = arrow.EndPoint,
                            StartBlock = arrow.StartBlock,
                            EndBlock = arrow.EndBlock
                        };
                    }
                    UpdateAlignmentGuides(selectedBlock);
                }

                this.Cursor = Cursors.SizeAll;
            }
        }

        private void InfiniteCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            PointF virtualPos = ScreenToVirtual(e.Location);

            // 1. ПЕРЕТАСКИВАНИЕ КОНЦА СТРЕЛКИ (высший приоритет)
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

            // 2. ИЗМЕНЕНИЕ РАЗМЕРА БЛОКА (высокий приоритет)
            if (isResizing && primarySelectedElement is BpmnBlock resizingBlock)
            {
                float deltaX = virtualPos.X - resizeStartPoint.X;
                float deltaY = virtualPos.Y - resizeStartPoint.Y;

                RectangleF newBounds = originalBounds;

                switch ((ResizeArea)selectedHandleIndex)
                {
                    case ResizeArea.Top:
                        newBounds.Y += deltaY;
                        newBounds.Height -= deltaY;
                        break;
                    case ResizeArea.Bottom:
                        newBounds.Height += deltaY;
                        break;
                    case ResizeArea.Left:
                        newBounds.X += deltaX;
                        newBounds.Width -= deltaX;
                        break;
                    case ResizeArea.Right:
                        newBounds.Width += deltaX;
                        break;
                    case ResizeArea.TopLeft:
                        newBounds.X += deltaX;
                        newBounds.Y += deltaY;
                        newBounds.Width -= deltaX;
                        newBounds.Height -= deltaY;
                        break;
                    case ResizeArea.TopRight:
                        newBounds.Y += deltaY;
                        newBounds.Width += deltaX;
                        newBounds.Height -= deltaY;
                        break;
                    case ResizeArea.BottomLeft:
                        newBounds.X += deltaX;
                        newBounds.Width -= deltaX;
                        newBounds.Height += deltaY;
                        break;
                    case ResizeArea.BottomRight:
                        newBounds.Width += deltaX;
                        newBounds.Height += deltaY;
                        break;
                }

                // Проверяем минимальные размеры
                if (newBounds.Width > 20 && newBounds.Height > 20)
                {
                    if (autoAdjustCanvasOffset)
                    {
                        AdjustCanvasOffsetForBlock(newBounds);
                    }

                    // Сохраняем предыдущие границы для обновления стрелок
                    RectangleF previousBounds = resizingBlock.Bounds;
                    resizingBlock.Bounds = newBounds;

                    // Обновляем прикрепленные стрелки
                    UpdateAttachedArrows(resizingBlock, previousBounds);
                    UpdateEditTextBoxLocation();
                    this.Invalidate();
                }
                return;
            }

            // 3. ПЕРЕМЕЩЕНИЕ ВЫДЕЛЕННЫХ ЭЛЕМЕНТОВ (ОСНОВНОЙ БЛОК)
            if (isDraggingElements && selectedElements.Count > 0)
            {
                float deltaX = virtualPos.X - dragStartPoint.X;
                float deltaY = virtualPos.Y - dragStartPoint.Y;

                // Перемещаем ВСЕ выделенные элементы
                foreach (var element in selectedElements)
                {
                    if (element is BpmnBlock block)
                    {
                        // Восстанавливаем оригинальные границы и применяем смещение
                        if (originalBlockBounds.TryGetValue(block, out RectangleF originalBounds))
                        {
                            RectangleF previousBounds = block.Bounds;
                            RectangleF newBounds = new RectangleF(
                                originalBounds.X + deltaX,
                                originalBounds.Y + deltaY,
                                originalBounds.Width,
                                originalBounds.Height
                            );

                            if (autoAdjustCanvasOffset)
                            {
                                AdjustCanvasOffsetForBlock(newBounds);
                            }

                            // ПРИМЕНЯЕМ новые границы
                            block.Bounds = newBounds;

                            // Обновляем стрелки, прикрепленные к этому блоку
                            UpdateAttachedArrows(block, previousBounds);
                        }
                    }
                    else if (element is BpmnArrow arrow)
                    {
                        // Восстанавливаем оригинальные позиции и применяем смещение
                        if (originalArrowStates.TryGetValue(arrow, out ArrowState arrowState))
                        {
                            // Упрощенная логика: перемещаем стрелку только если она полностью свободна
                            // или если оба привязанных блока тоже выделены
                            bool shouldMoveArrow = arrow.IsFloating ||
                                                 (arrow.IsStartAttached && arrow.IsEndAttached &&
                                                  selectedElements.Contains(arrow.StartBlock) &&
                                                  selectedElements.Contains(arrow.EndBlock));

                            if (shouldMoveArrow)
                            {
                                arrow.StartPoint = new PointF(arrowState.StartPoint.X + deltaX, arrowState.StartPoint.Y + deltaY);
                                arrow.EndPoint = new PointF(arrowState.EndPoint.X + deltaX, arrowState.EndPoint.Y + deltaY);
                            }
                        }
                    }
                }

                UpdateEditTextBoxLocation();
                this.Invalidate();
                return;
            }

            // 4. ПАНОРАМИРОВАНИЕ ХОЛСТА
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

            // 5. ВЫДЕЛЕНИЕ ГРУППЫ
            if (isSelecting)
            {
                float x = Math.Min(selectionDragStartPoint.X, virtualPos.X);
                float y = Math.Min(selectionDragStartPoint.Y, virtualPos.Y);
                float width = Math.Abs(virtualPos.X - selectionDragStartPoint.X);
                float height = Math.Abs(virtualPos.Y - selectionDragStartPoint.Y);

                selectionRectangle = new RectangleF(x, y, width, height);
                Invalidate();
                return;
            }

            // 6. ПРОВЕРКА КУРСОРОВ ДЛЯ ОБЛАСТЕЙ ИЗМЕНЕНИЯ РАЗМЕРА
            if (primarySelectedElement is BpmnBlock blockForCursor && !isDragging && !isDraggingElements && !isResizing && !isSelecting)
            {
                var resizeArea = GetResizeArea(blockForCursor.Bounds, virtualPos);
                if (resizeArea != ResizeArea.None)
                {
                    this.Cursor = GetResizeCursor(resizeArea);
                }
                else
                {
                    this.Cursor = Cursors.Default;
                }
            }
            // 7. СБРОС КУРСОРА
            else if (!isDragging && !isDraggingElements && !isResizing && !isSelecting)
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

                    // СОЗДАЕМ КОМАНДУ ДЛЯ ИЗМЕНЕНИЯ СТРЕЛКИ
                    var form = this.FindForm() as Form1;
                    if (form?.CommandManager != null)
                    {
                        // Сохраняем состояние до изменения
                        var originalStartBlock = selectedArrow.StartBlock;
                        var originalEndBlock = selectedArrow.EndBlock;
                        var originalStartPoint = selectedArrow.StartPoint;
                        var originalEndPoint = selectedArrow.EndPoint;

                        if (block != null)
                        {
                            selectedArrow.Attach(isDraggingStartPoint, block, point);

                            // ВЫПОЛНЯЕМ КОМАНДУ
                            var command = new ModifyArrowCommand(
                                selectedArrow,
                                originalStartBlock, originalStartPoint,
                                originalEndBlock, originalEndPoint,
                                selectedArrow.StartBlock, selectedArrow.StartPoint,
                                selectedArrow.EndBlock, selectedArrow.EndPoint,
                                this
                            );
                            form.CommandManager.Execute(command);
                            Console.WriteLine($"ModifyArrowCommand executed for endpoint attachment");
                        }
                        else
                        {
                            // Отвязываем и перемещаем свободный конец
                            selectedArrow.Detach(isDraggingStartPoint);
                            if (isDraggingStartPoint)
                                selectedArrow.StartPoint = virtualPos;
                            else
                                selectedArrow.EndPoint = virtualPos;

                            // ВЫПОЛНЯЕМ КОМАНДУ
                            var command = new ModifyArrowCommand(
                                selectedArrow,
                                originalStartBlock, originalStartPoint,
                                originalEndBlock, originalEndPoint,
                                selectedArrow.StartBlock, selectedArrow.StartPoint,
                                selectedArrow.EndBlock, selectedArrow.EndPoint,
                                this
                            );
                            form.CommandManager.Execute(command);
                            Console.WriteLine($"ModifyArrowCommand executed for endpoint detachment");
                        }
                    }
                    else
                    {
                        // Fallback: старый код
                        if (block != null)
                        {
                            selectedArrow.Attach(isDraggingStartPoint, block, point);
                        }
                        else
                        {
                            selectedArrow.Detach(isDraggingStartPoint);
                            if (isDraggingStartPoint)
                                selectedArrow.StartPoint = virtualPos;
                            else
                                selectedArrow.EndPoint = virtualPos;
                        }
                    }
                }

                // ДОБАВЛЯЕМ логику для блоков ПОСЛЕ сброса флагов
                if (_isBlockDragInProgress && selectedBlock != null)
                {
                    var finalBounds = selectedBlock.Bounds;

                    // Проверяем, что блок действительно переместился
                    if (finalBounds.X != _dragStartBounds.X || finalBounds.Y != _dragStartBounds.Y)
                    {
                        var form = this.FindForm() as Form1;
                        if (form?.CommandManager != null)
                        {
                            var command = new MoveBlockCommand(
                                selectedBlock,
                                _dragStartBounds,
                                finalBounds,
                                arrows,
                                this
                            );
                            form.CommandManager.Execute(command);
                            Console.WriteLine($"MoveBlockCommand executed: {selectedBlock.Text} moved from {_dragStartBounds} to {finalBounds}");
                        }
                    }

                    _isBlockDragInProgress = false;
                }

                // Сбрасываем все флаги перетаскивания
                isDragging = false;
                isDraggingBlock = false;
                verticalGuides.Clear();
                horizontalGuides.Clear();
                Invalidate();
                isResizing = false;
                isDraggingArrowEnd = false;
                isDraggingArrow = false;
                selectedHandleIndex = -1;

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
                        selectedBlocks.Clear();
                        selectedArrows.Clear();

                        // --- Выделяем блоки ---
                        foreach (var block in blocks)
                        {
                            if (selectionRectangle.Contains(block.Bounds))
                                selectedBlocks.Add(block);
                        }

                        // --- Выделяем стрелки, которые соединяют выделенные блоки ---
                        foreach (var arrow in arrows)
                        {
                            bool startInGroup = arrow.StartBlock != null && selectedBlocks.Contains(arrow.StartBlock);
                            bool endInGroup = arrow.EndBlock != null && selectedBlocks.Contains(arrow.EndBlock);

                            if (startInGroup && endInGroup)
                            {
                                selectedArrows.Add(arrow);
                            }
                            else if (arrow.IsFloating && selectionRectangle.Contains(arrow.StartPoint) && selectionRectangle.Contains(arrow.EndPoint))
                            {
                                // добавляем плавающие стрелки, полностью попавшие в рамку
                                selectedArrows.Add(arrow);
                            }
                            else
                            {
                                foreach (var point in arrow.ConnectionPoints)
                                {
                                    if (selectionRectangle.Contains(point))
                                    {
                                        selectedArrows.Add(arrow);
                                        break;
                                    }
                                }
                            }
                        }

                        // Если выделено более одного блока — сбрасываем одиночное выделение
                        if (selectedBlocks.Count > 1 || selectedArrows.Count > 1)
                        {
                            selectedBlock = null;
                            selectedArrow = null;
                        }

                        // Устанавливаем основной элемент
                        if (selectedElements.Count > 0)
                        {
                            primarySelectedElement = selectedElements[0];
                            selectedBlock = selectedBlocks[0];
                        }
                        else if (selectedArrows.Count == 1)
                        {
                            selectedArrow = selectedArrows[0];
                        }

                        Invalidate();
                    }

                    // После завершения перемещения обновляем все стрелки
                    if (isDraggingElements)
                    {
                        foreach (var element in selectedElements)
                        {
                            if (element is BpmnArrow arrow)
                            {
                                // Пересчитываем путь для всех перемещенных стрелок
                                arrow.CalculateOrthogonalPath();
                            }
                        }
                    }

                    // Сбрасываем ВСЕ флаги перетаскивания, НО НЕ ОЧИЩАЕМ ВЫДЕЛЕНИЕ
                    isDragging = false;
                    isDraggingElements = false;
                    isResizing = false;
                    selectedHandleIndex = -1;

                    this.Cursor = Cursors.Default;

                    // Перерисовываем для обновления выделения
                    this.Invalidate();
                }
            }
        }

        private void InfiniteCanvas_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            g.TranslateTransform(canvasOffset.X * zoom, canvasOffset.Y * zoom);
            g.ScaleTransform(zoom, zoom);

            DrawGrid(g);
            // --- РИСУЕМ НАПРАВЛЯЮЩИЕ ---
            using (Pen guidePen = new Pen(Color.Orange, 1))
            {
                guidePen.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;

                foreach (float x in verticalGuides)
                {
                    g.DrawLine(guidePen, x, -10000, x, 10000);
                }

                foreach (float y in horizontalGuides)
                {
                    g.DrawLine(guidePen, -10000, y, 10000, y);
                }
            }
            // Сначала рисуем стрелки (под блоками)
            if (arrows != null)
            {
                foreach (var arrow in arrows)
                {
                    bool isSelected = (arrow == selectedArrow) || selectedArrows.Contains(arrow);
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

        // Упрощаем методы трансформации координат
        private PointF ScreenToVirtual(Point screenPoint)
        {
            return new PointF(
                screenPoint.X / zoom - canvasOffset.X,
                screenPoint.Y / zoom - canvasOffset.Y
            );
        }

        private PointF VirtualToScreen(PointF virtualPoint)
        {
            return new PointF(
                (virtualPoint.X + canvasOffset.X) * zoom,
                (virtualPoint.Y + canvasOffset.Y) * zoom
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

        /// <summary>
        /// Удаляет выделенные блоки и стрелки, связанные с ними.
        /// </summary>
        private void DeleteSelectedBlocksAndArrows()
        {
            if ((selectedBlocks == null || selectedBlocks.Count == 0) && selectedBlock == null)
                return;

            // Формируем список блоков для удаления
            List<BpmnBlock> blocksToDelete = new List<BpmnBlock>();

            if (selectedBlocks != null && selectedBlocks.Count > 0)
                blocksToDelete.AddRange(selectedBlocks);

            if (selectedBlock != null && !blocksToDelete.Contains(selectedBlock))
                blocksToDelete.Add(selectedBlock);

            // 1️⃣ Удаляем стрелки, привязанные к удаляемым блокам
            if (arrows != null && arrows.Count > 0)
            {
                arrows.RemoveAll(a =>
                    (a.StartBlock != null && blocksToDelete.Contains(a.StartBlock)) ||
                    (a.EndBlock != null && blocksToDelete.Contains(a.EndBlock)));
            }

            // 2️⃣ Удаляем сами блоки
            foreach (var b in blocksToDelete)
                blocks.Remove(b);

            // 3️⃣ Очищаем выделение
            selectedBlocks.Clear();
            selectedBlock = null;
            selectedArrow = null;

            // 4️⃣ Обновляем отрисовку
            Invalidate();
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
            ClearSelection();
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

        // Новый метод для очистки сохраненных состояний при изменении выделения
        public void ClearDragStates()
        {
            originalBlockBounds.Clear();
            originalArrowStates.Clear();
        }
        public bool IsEditingText()
        {
            return editTextBox != null && editTextBox.Focused;
        }

        public void SelectBlock(BpmnBlock block)
        {
            selectedBlock = block;
            selectedBlocks.Clear();
            selectedBlocks.Add(block);
            selectedArrow = null;
            Invalidate();
        }
        // Добавляем публичный метод для получения позиции курсора
        public PointF GetCursorVirtualPosition()
        {
            Point cursorPos = PointToClient(Cursor.Position);
            return ScreenToVirtual(cursorPos);
        }

        public void DeleteSelectedElement(CommandManager commandManager)
        {
            if (selectedBlock != null)
            {
                var command = new DeleteBlockCommand(selectedBlock, blocks, arrows, this);
                commandManager.Execute(command);
                selectedBlock = null;
            }
            else if (selectedArrow != null)
            {
                var command = new DeleteArrowCommand(selectedArrow, arrows, this);
                commandManager.Execute(command);
                selectedArrow = null;
            }
            Invalidate();
        }
    }
}