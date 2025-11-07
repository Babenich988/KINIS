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
        private bool isResizing = false; // флаг изменения размера
        private PointF canvasOffset = PointF.Empty;
        private float zoom = 1.0f;
        private const float MIN_ZOOM = 0.25f;   // 25%
        private const float MAX_ZOOM = 5.0f;   // 500%
        private const float ZOOM_STEP = 1.2f;  // Шаг изменения зума
        private List<BpmnBlock> blocks = new List<BpmnBlock>();
        private BpmnBlock selectedBlock = null;
        private PointF blockDragStart;
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
        private BpmnArrow selectedArrow = null;
        private bool isCreatingArrow = false;
        private BpmnArrow tempArrow = null;
        private BpmnBlock arrowStartBlock = null;
        private PointF arrowStartPoint = PointF.Empty;
        private bool isDraggingArrow = false;
        private bool isDraggingArrowEnd = false;
        private bool isDraggingStartPoint = false;
        private PointF arrowDragStart = PointF.Empty;
        private object lastSelectedElement = null; // Может быть BpmnBlock или BpmnArrow
        public event Action<float> ZoomChanged;
        private List<BpmnArrow> selectedArrows = new List<BpmnArrow>(); // Новый список выделенных стрелок
        private BpmnArrow contextMenuArrow;

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
            this.MouseDoubleClick += InfiniteCanvas_MouseDoubleClick; // обработчик двойного клика
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

        //Контекстное меню для удаления
        private void DeleteMenuItem_Click(object sender, EventArgs e)
        {
            // 1️⃣ Если выделена группа (блоки или стрелки) — удаляем все сразу
            if ((selectedBlocks != null && selectedBlocks.Count > 0) || (selectedArrows != null && selectedArrows.Count > 0))
            {
                // Удаляем все стрелки, привязанные к выделенным блокам, плюс выделенные стрелки
                arrows.RemoveAll(a =>
                    (a.StartBlock != null && selectedBlocks.Contains(a.StartBlock)) ||
                    (a.EndBlock != null && selectedBlocks.Contains(a.EndBlock)) ||
                    selectedArrows.Contains(a));

                // Удаляем все выделенные блоки
                blocks.RemoveAll(b => selectedBlocks.Contains(b));

                // Очищаем выделение
                selectedBlocks.Clear();
                selectedArrows.Clear();
                selectedBlock = null;
                selectedArrow = null;
                Invalidate();
                return;
            }

            // 2️⃣ Если кликнули на стрелке — удаляем только её
            if (contextMenuArrow != null)
            {
                arrows.Remove(contextMenuArrow);
                selectedArrows?.Remove(contextMenuArrow);
                if (selectedArrow == contextMenuArrow)
                    selectedArrow = null;
                contextMenuArrow = null;
                Invalidate();
                return;
            }

            // 3️⃣ Если кликнули на блоке — удаляем блок и связанные стрелки
            DeleteSelectedBlocksAndArrows();
        }
        private void InfiniteCanvas_KeyDown(object sender, KeyEventArgs e)
        {
            // Если нажата клавиша Delete и блок выделен
            if (e.KeyCode == Keys.Delete && selectedBlock != null)
            {
                // Удаляем блок
                blocks.Remove(selectedBlock);
                selectedBlock = null;
                Invalidate();
            }
        }
        protected override bool IsInputKey(Keys keyData)//обработчик клавиш на прямую
        {
            return true;
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.KeyCode == Keys.Delete)
            {
                // 1️⃣ Удаление одиночного блока
                if (selectedBlock != null)
                {
                    blocks.Remove(selectedBlock);

                    // Удаляем стрелки, связанные с этим блоком
                    arrows.RemoveAll(a => a.StartBlock == selectedBlock || a.EndBlock == selectedBlock);

                    selectedBlock = null;
                    Invalidate();
                    return;
                }

                // 2️⃣ Удаление одиночной стрелки
                if (selectedArrow != null)
                {
                    arrows.Remove(selectedArrow);
                    selectedArrow = null;
                    Invalidate();
                    return;
                }

                // 3️⃣ Групповое удаление
                if (selectedBlocks.Count > 0 || selectedArrows.Count > 0)
                {
                    // Удаляем стрелки, связанные с выбранными блоками
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

        //Метод для поиска ближайшей точки привязки
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

        //Метод для поиска стрелки по точке
        private BpmnArrow GetArrowAtPoint(PointF point)
        {
            foreach (var arrow in arrows.AsEnumerable().Reverse())
            {
                if (arrow.HitTest(point))
                    return arrow;
            }
            return null;
        }

        //Метод для вычисления расстояния между двумя точками привязки
        private float Distance(PointF a, PointF b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Обновляет позиции стрелок, привязанных к перемещаемому блоку
        /// </summary>
        private void UpdateAttachedArrows(BpmnBlock movedBlock, RectangleF previousBounds)
        {
            foreach (var arrow in arrows)
            {
                if (arrow.StartBlock == movedBlock)
                {
                    // Вычисляем смещение точки относительно предыдущей позиции блока
                    float deltaX = movedBlock.Bounds.X - previousBounds.X;
                    float deltaY = movedBlock.Bounds.Y - previousBounds.Y;

                    // Просто сдвигаем точку на ту же дельту, что и блок
                    arrow.StartPoint = new PointF(
                        arrow.StartPoint.X + deltaX,
                        arrow.StartPoint.Y + deltaY
                    );
                }

                if (arrow.EndBlock == movedBlock)
                {
                    // Аналогично для конечной точки
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

            // ПРОВЕРЯЕМ ОГРАНИЧЕНИЯ
            if (newZoom >= MIN_ZOOM && newZoom <= MAX_ZOOM)
            {
                PointF mousePosBeforeZoom = ScreenToVirtual(e.Location);
                zoom = newZoom;
                PointF mousePosAfterZoom = ScreenToVirtual(e.Location);
                canvasOffset.X += (mousePosAfterZoom.X - mousePosBeforeZoom.X) * zoom;
                canvasOffset.Y += (mousePosAfterZoom.Y - mousePosBeforeZoom.Y) * zoom;
                // Обновляем положение и размер TextBox при изменении масштаба
                UpdateEditTextBoxLocation();
                this.Invalidate();
            }
        }

        private void InfiniteCanvas_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && !IsCtrlPressed())
            {
                PointF virtualPos = ScreenToVirtual(e.Location);
                BpmnBlock clickedBlock = GetBlockAtPoint(virtualPos);

                if (clickedBlock != null)
                {
                    if (selectedBlocks.Contains(clickedBlock))
                    {
                        // Если блок уже выделен, просто устанавливаем его как 'selectedBlock' для возможного перетаскивания.
                        selectedBlock = clickedBlock;
                    }
                    else
                    {
                        // Если блок не выделен, очищаем предыдущее выделение и выделяем только этот блок.
                        selectedBlocks.Clear();
                        selectedBlocks.Add(clickedBlock);
                        selectedBlock = clickedBlock;
                    }
                }
                else
                {
                    // Кликнули в пустое место, очищаем все выделения.
                    selectedBlocks.Clear();
                    selectedBlock = null;
                }

                // Сначала проверяем клик на стрелку
                selectedArrow = GetArrowAtPoint(virtualPos);
                if (selectedArrow != null)
                {
                    selectedBlock = null;
                    lastSelectedElement = selectedArrow; // СОХРАНЯЕМ ВЫБРАННЫЙ ЭЛЕМЕНТ
                    this.Invalidate();
                    return;
                }

                // Затем проверяем клик на блок
                selectedBlock = GetBlockAtPoint(virtualPos);
                if (selectedBlock != null)
                {
                    selectedArrow = null;
                    lastSelectedElement = selectedBlock; // СОХРАНЯЕМ ВЫБРАННЫЙ ЭЛЕМЕНТ
                }
                else
                {
                    // Если кликнули в пустое место - сбрасываем выделение, но сохраняем последний элемент
                    selectedArrow = null;
                }

                this.Invalidate();
            }
        }

        private void InfiniteCanvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                PointF virtualPos = ScreenToVirtual(e.Location);
                this.Focus();

                // 1. ПРОВЕРЯЕМ КЛИК НА МАРКЕРЫ КОНЦОВ СТРЕЛКИ
                if (selectedArrow != null)
                {
                    if (selectedArrow.HitTestEndpoint(virtualPos, true))
                    {
                        isDraggingArrowEnd = true;
                        isDraggingStartPoint = true;
                        arrowDragStart = virtualPos;
                        this.Cursor = Cursors.Cross;
                        return;
                    }
                    else if (selectedArrow.HitTestEndpoint(virtualPos, false))
                    {
                        isDraggingArrowEnd = true;
                        isDraggingStartPoint = false;
                        arrowDragStart = virtualPos;
                        this.Cursor = Cursors.Cross;
                        return;
                    }
                }

                // 2. ПРОВЕРЯЕМ КЛИК НА САМУ СТРЕЛКУ
                var clickedArrow = GetArrowAtPoint(virtualPos);
                if (clickedArrow != null)
                {
                    selectedArrow = clickedArrow;
                    selectedBlock = null;

                    if (clickedArrow.IsFloating)
                    {
                        isDraggingArrow = true;
                        arrowDragStart = virtualPos;
                        this.Cursor = Cursors.SizeAll;
                    }
                    return;
                }

                // 3. Проверяем клик на ручки изменения размера
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

                // 4. Проверяем клик на блок (для выделения и перемещения)
                selectedBlock = GetBlockAtPoint(virtualPos);
                if (selectedBlock != null)
                {
                    selectedArrow = null;
                    isDraggingBlock = true;
                    blockDragStart = virtualPos;
                    this.Cursor = Cursors.SizeAll;
                }
                else
                {
                    // 5. ✅ ДОБАВЛЯЕМ: Если кликнули в пустое место И зажат Ctrl - начинаем панорамирование
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
                        selectionDragStartPoint = virtualPos; // СОХРАНЯЕМ НАЧАЛЬНУЮ ТОЧКУ ЗДЕСЬ
                        selectionRectangle = new RectangleF(virtualPos.X, virtualPos.Y, 0, 0);
                        selectedBlocks.Clear(); // Начинаем новое выделение, очищаем старое
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

        private void InfiniteCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            PointF virtualPos = ScreenToVirtual(e.Location);

            // 1. ПЕРЕТАСКИВАНИЕ КОНЦА СТРЕЛКИ
            if (isDraggingArrowEnd && selectedArrow != null)
            {
                // Если зажата Ctrl - отвязываем конец и свободно перемещаем
                if (IsCtrlPressed())
                {
                    selectedArrow.Detach(isDraggingStartPoint);
                    if (isDraggingStartPoint)
                        selectedArrow.StartPoint = virtualPos;
                    else
                        selectedArrow.EndPoint = virtualPos;
                }
                else
                {
                    // Ищем ближайшую точку привязки
                    var (block, point) = FindNearestConnectionPoint(virtualPos);
                    if (block != null)
                    {
                        // Привязываем к найденной точке
                        selectedArrow.Attach(isDraggingStartPoint, block, point);
                    }
                    else
                    {
                        // Отвязываем и перемещаем свободный конец
                        selectedArrow.Detach(isDraggingStartPoint);
                        if (isDraggingStartPoint)
                            selectedArrow.StartPoint = virtualPos;
                        else
                            selectedArrow.EndPoint = virtualPos;
                    }
                }

                this.Invalidate();
                return; // ВАЖНО: завершаем обработку
            }

            // 2. ПЕРЕМЕЩЕНИЕ ВСЕЙ СТРЕЛКИ (ЕСЛИ ОНА СВОБОДНАЯ)
            if (isDraggingArrow && selectedArrow != null && selectedArrow.IsFloating)
            {
                float deltaX = virtualPos.X - arrowDragStart.X;
                float deltaY = virtualPos.Y - arrowDragStart.Y;
                selectedArrow.Move(deltaX, deltaY);
                arrowDragStart = virtualPos;
                this.Invalidate();
                return; // ВАЖНО: завершаем обработку
            }

            // 3. ✅ ПАНОРАМИРОВАНИЕ ХОЛСТА (ДОЛЖНО БЫТЬ ВЫШЕ ДРУГИХ ОПЕРАЦИЙ)
            if (isDragging && IsCtrlPressed())
            {
                // Перемещение поля с учетом зума
                float deltaX = (e.X - lastMousePos.X) / zoom;
                float deltaY = (e.Y - lastMousePos.Y) / zoom;

                canvasOffset.X += deltaX;
                canvasOffset.Y += deltaY;

                lastMousePos = e.Location;
                this.Invalidate();
                return; // ✅ ВАЖНО: return чтобы не мешать другим операциям
            }

            // 4. ПРОВЕРКА КУРСОРОВ ДЛЯ РУЧЕК РАЗМЕРА
            if (selectedBlock != null && !isDragging && !isDraggingBlock && !isResizing && !isSelecting)
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

            // 5. ПЕРЕМЕЩЕНИЕ БЛОКА ИЛИ ГРУППЫ БЛОКОВ
            if (isDraggingBlock)
            {
                float deltaX = virtualPos.X - blockDragStart.X;
                float deltaY = virtualPos.Y - blockDragStart.Y;

                // Если есть выделенная группа и выбранный блок находится в этой группе - перемещаем всю группу
                if (selectedBlocks.Count > 0 && (selectedBlock == null || selectedBlocks.Contains(selectedBlock)))
                {
                    // Перемещаем все блоки в выделенной группе
                    foreach (var block in selectedBlocks)
                    {
                        RectangleF newBounds = new RectangleF(
                            block.Bounds.X + deltaX,
                            block.Bounds.Y + deltaY,
                            block.Bounds.Width,
                            block.Bounds.Height
                        );

                        if (autoAdjustCanvasOffset)
                        {
                            AdjustCanvasOffsetForBlock(newBounds);
                        }

                        block.Bounds = newBounds;
                    }

                    // Обновляем прикрепленные стрелки для всех перемещенных блоков
                    foreach (var block in selectedBlocks)
                    {
                        // Здесь нужно сохранить предыдущие позиции для каждого блока
                        // Для простоты обновляем все стрелки
                        UpdateAttachedArrows(block, new RectangleF(
                            block.Bounds.X - deltaX,
                            block.Bounds.Y - deltaY,
                            block.Bounds.Width,
                            block.Bounds.Height
                        ));
                    }
                }
                else if (selectedBlock != null)
                {
                    // Перемещаем только один блок
                    RectangleF previousBounds = selectedBlock.Bounds;

                    RectangleF newBounds = new RectangleF(
                        selectedBlock.Bounds.X + deltaX,
                        selectedBlock.Bounds.Y + deltaY,
                        selectedBlock.Bounds.Width,
                        selectedBlock.Bounds.Height
                    );

                    if (autoAdjustCanvasOffset)
                    {
                        AdjustCanvasOffsetForBlock(newBounds);
                    }

                    selectedBlock.Bounds = newBounds;
                    UpdateAttachedArrows(selectedBlock, previousBounds);
                }

                blockDragStart = virtualPos;
                UpdateEditTextBoxLocation();
                this.Invalidate();
            }
            // 6. ИЗМЕНЕНИЕ РАЗМЕРА БЛОКА
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

                if (newBounds.Width > 20 && newBounds.Height > 20)
                {
                    if (autoAdjustCanvasOffset)
                    {
                        AdjustCanvasOffsetForBlock(newBounds);
                    }
                    selectedBlock.Bounds = newBounds;
                    UpdateAttachedArrows(selectedBlock, originalBounds);
                    UpdateEditTextBoxLocation();
                    this.Invalidate();
                }
            }
            // 7. ВЫДЕЛЕНИЕ ГРУППЫ БЛОКОВ
            else if (isSelecting)
            {
                // Используем selectionDragStartPoint (начальная точка) и virtualPos (текущая точка)
                // для определения корректных X, Y, Width, Height прямоугольника
                float x = Math.Min(selectionDragStartPoint.X, virtualPos.X);
                float y = Math.Min(selectionDragStartPoint.Y, virtualPos.Y);
                float width = Math.Abs(virtualPos.X - selectionDragStartPoint.X);
                float height = Math.Abs(virtualPos.Y - selectionDragStartPoint.Y);

                selectionRectangle = new RectangleF(x, y, width, height);
                Invalidate();
            }
            // 8. СБРОС КУРСОРА ЕСЛИ НИЧЕГО НЕ ПРОИСХОДИТ
            else if (!isDragging && !isDraggingBlock && !isResizing && !isSelecting)
            {
                // Если ничего не происходит, вернуть курсор по умолчанию, если он был изменен
                if (this.Cursor != Cursors.Default)
                    this.Cursor = Cursors.Default;
            }
        }

        private void InfiniteCanvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // Завершение перетаскивания конца стрелки - финальная привязка
                if (isDraggingArrowEnd && selectedArrow != null)
                {
                    PointF virtualPos = ScreenToVirtual(e.Location);
                    var (block, point) = FindNearestConnectionPoint(virtualPos);

                    if (block != null)
                    {
                        selectedArrow.Attach(isDraggingStartPoint, block, point);
                    }
                }

                // Сбрасываем все флаги перетаскивания
                isDragging = false;
                isDraggingBlock = false;
                isResizing = false;
                isDraggingArrowEnd = false;
                isDraggingArrow = false;
                selectedHandleIndex = -1;

                // Завершение выделения группы
                if (isSelecting)
                {
                    isSelecting = false;

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
                    else if (selectedBlocks.Count == 1)
                    {
                        selectedBlock = selectedBlocks[0];
                    }
                    else if (selectedArrows.Count == 1)
                    {
                        selectedArrow = selectedArrows[0];
                    }

                    Invalidate();
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
                    bool isSelected = (block == selectedBlock);
                    bool isGroupSelected = selectedBlocks.Contains(block);

                    block.Draw(g, isSelected || isGroupSelected);
                }
            }

            // Рисуем прямоугольник выделения
            if (isSelecting)
            {

                using (Pen selectPen = new Pen(Color.Blue, 2))
                {
                    selectPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                    g.DrawRectangle(selectPen, selectionRectangle.X, selectionRectangle.Y, selectionRectangle.Width, selectionRectangle.Height);
                }
            }

            g.ResetTransform();

            // ДОБАВЛЯЕМ: ОТОБРАЖЕНИЕ ПРОЦЕНТОВ ЗУМА
            DrawZoomPercentage(g);

            UpdateEditTextBoxLocation();
        }

        /// <summary>
        /// Рисует текущее значение зума в процентах
        /// </summary>
        private void DrawZoomPercentage(Graphics g)
        {
            string zoomText = $"Масштаб: {(int)(zoom * 100)}%";

            using (var font = new Font("Segoe UI", 10, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.Black))
            using (var backgroundBrush = new SolidBrush(Color.FromArgb(200, 255, 255, 255)))
            {
                SizeF textSize = g.MeasureString(zoomText, font);

                // Фон для текста - ПРАВЫЙ НИЖНИЙ УГОЛ (в экранных координатах)
                RectangleF backgroundRect = new RectangleF(
                    this.Width - textSize.Width - 15,
                    this.Height - textSize.Height - 50,
                    textSize.Width + 10,
                    textSize.Height + 5
                );

                g.FillRectangle(backgroundBrush, backgroundRect);
                g.DrawRectangle(Pens.Gray, backgroundRect.X, backgroundRect.Y, backgroundRect.Width, backgroundRect.Height);

                // Текст - ПРАВЫЙ НИЖНИЙ УГОЛ (в экранных координатах)
                g.DrawString(zoomText, font, brush,
                    this.Width - textSize.Width - 10,
                    this.Height - textSize.Height - 47);
            }
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

        /// <summary>
        /// Получаем центр выделенного элемента для фокусировки
        /// </summary>
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
                // Для стрелки берем середину пути
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

            return new PointF(0, 0); // fallback
        }

        /// <summary>
        /// Фокусирует канвас на указанном элементе
        /// </summary>
        public void FocusOnElement(object element)
        {
            if (element == null) return;

            PointF elementCenter = GetElementCenter(element);

            // Вычисляем смещение канваса чтобы элемент оказался в центре
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
                ZoomChanged?.Invoke(zoom); // ВЫЗЫВАЕМ СОБЫТИЕ
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
                ZoomChanged?.Invoke(zoom); // ВЫЗЫВАЕМ СОБЫТИЕ
            }
        }

        public void ResetZoom()
        {
            zoom = 1.0f;

            //Если есть выделенный элемент - фокусируемся на нем
            if (lastSelectedElement != null)
            {
                FocusOnElement(lastSelectedElement);
            }
            else
            {
                // Иначе сбрасываем позицию канваса
                canvasOffset = PointF.Empty;
            }

            UpdateEditTextBoxLocation();
            this.Invalidate();
            ZoomChanged?.Invoke(zoom); // ВЫЗЫВАЕМ СОБЫТИЕ
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

            //Сбрасываем фокус при полном сбросе вида
            lastSelectedElement = null;

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