using Kinis.Models;
using Kinis.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using static Kinis.Services.CommandManager;
using static Kinis.Services.CommandManager.MoveBlockCommand;

namespace Kinis
{
    // Классы для хранения состояний элементов
    public class ArrowState
    {
        public PointF StartPoint { get; set; }
        public PointF EndPoint { get; set; }
        public BpmnBlock StartBlock { get; set; }
        public BpmnBlock EndBlock { get; set; }

        // ДЛЯ КРИВЫХ СТРЕЛОК
        public PointF ControlPoint1 { get; set; }
        public PointF ControlPoint2 { get; set; }

        // ДОБАВЛЯЕМ ИНДЕКСЫ ПРИВЯЗКИ
        public int StartConnectionPointIndex { get; set; } = -1;
        public int EndConnectionPointIndex { get; set; } = -1;
    }

    public class InfiniteCanvas : Panel
    {
        private Point lastMousePos;
        private bool isDragging = false;
        private bool isResizing = false;
        private PointF canvasOffset = PointF.Empty;
        private float zoom = 1.0f;
        private const float MIN_ZOOM = 0.25f;
        private const float MAX_ZOOM = 5.0f;
        private const float ZOOM_STEP = 1.2f;
        //Для полей
        private Dictionary<int, (List<BpmnBlock> blocks, List<BpmnArrow> arrows, List<BpmnCurvedArrow> curvedArrows)> sheets;
        private int currentSheetIndex = 0;
        private const int MAX_SHEETS = 5; // можно менять при необходимости
        // УНИФИЦИРОВАННАЯ СИСТЕМА ВЫДЕЛЕНИЯ (из вашего кода)
        // --- selection & group-drag support ---
        private List<object> selectedElements = new List<object>(); // может содержать BpmnBlock и BpmnArrow
        private object primarySelectedElement = null; // текущий "активный" элемент (блок или стрелка)
        private ContextMenuStrip contextMenuForCanvas;
        private ContextMenuStrip contextMenuForElements;
        private ContextMenuStrip contextMenuForPool;

        private bool isDraggingLane = false;
        private PoolLine draggingLane = null;
        private BpmnBlock draggingLanePool = null;
        private List<PoolComposite> poolComposites = new List<PoolComposite>();

        private bool _showPoolErrorHighlight = false;
        private RectangleF _errorPoolBounds = RectangleF.Empty;
        private System.Windows.Forms.Timer _errorHighlightTimer;

        private bool _isResizingLane = false;
        private PoolLine _resizingLane = null;
        private BpmnBlock _resizingLanePool = null;
        private PointF _resizeLaneStartPoint;
        private RectangleF _originalLaneBounds;
        private const float LANE_MIN_HEIGHT = 40f;
        private const float LANE_RESIZE_MARGIN = 8f;

        private bool IsPointOnLaneBottomBorder(PoolLine lane, PointF point, float margin)
        {
            // Проверяем, находится ли точка около нижней границы дорожки
            // Учитываем только нижнюю границу и небольшой отступ по бокам
            return point.X >= lane.Bounds.Left + margin &&
                   point.X <= lane.Bounds.Right - margin &&
                   point.Y >= lane.Bounds.Bottom - margin &&
                   point.Y <= lane.Bounds.Bottom + margin;
        }

        private bool _isUpdatingLaneHeight = false;
        private bool _isDraggingLaneInternal = false;
        private PoolLine _draggingLaneInternal = null;
        private BpmnBlock _draggingLanePoolInternal = null;
        private PoolLine _draggingLaneParentInternal = null;
        private PointF _dragLaneInternalStartPoint;
        private RectangleF _originalLaneInternalBounds;
        private List<PoolLine> _draggingLaneChildren = null; // Для перемещения с детьми

        private BpmnBlock selectedBlock = null;
        private BpmnArrow selectedArrow = null;

        private BpmnBlock _highlightedPool = null;
        private PoolLine _highlightedLane = null;
        private Color _highlightColor = Color.FromArgb(100, 0, 255, 0); // Полупрозрачный зеленый
        private bool IsLaneWithinPoolBounds(PoolLine lane, BpmnBlock poolBlock)
        {
            return lane.Bounds.Y >= poolBlock.Bounds.Y + 40f && // Ниже названия
                   lane.Bounds.Bottom <= poolBlock.Bounds.Bottom && // Выше низа
                   lane.Bounds.X >= poolBlock.Bounds.X + 40f && // Правее названия
                   lane.Bounds.Right <= poolBlock.Bounds.Right; // Левее правого края
        }
        private PoolLine currentLaneUnderCursor = null;

        // Свойства для доступа к данным текущего листа
        private List<BpmnBlock> blocks => sheets.ContainsKey(currentSheetIndex) ? sheets[currentSheetIndex].blocks : new List<BpmnBlock>();
        private List<BpmnArrow> arrows => sheets.ContainsKey(currentSheetIndex) ? sheets[currentSheetIndex].arrows : new List<BpmnArrow>();
        private List<BpmnCurvedArrow> curvedArrows => sheets.ContainsKey(currentSheetIndex) ? sheets[currentSheetIndex].curvedArrows : new List<BpmnCurvedArrow>();

        // Метод доступа к curvedArrows
        // В классе InfiniteCanvas добавляем метод для получения кривых стрелок
        public List<BpmnCurvedArrow> GetCurvedArrows() => curvedArrows;

        private int selectedHandleIndex = -1;
        private PointF resizeStartPoint;
        private RectangleF originalBounds;
        private TextBox editTextBox = null;
        private bool autoAdjustCanvasOffset = true;
        private bool isSelecting = false;
        private RectangleF selectionRectangle;
        private PointF selectionDragStartPoint;

        private ToolStripMenuItem deleteMenuItem;

        // ФУНКЦИОНАЛ СТРЕЛОК ИЗ СТАРОГО КОДА
        private bool isCreatingArrow = false;
        private BpmnArrow tempArrow = null;
        private BpmnBlock arrowStartBlock = null;
        private PointF arrowStartPoint = PointF.Empty;
        private bool isDraggingArrow = false;
        private bool isDraggingArrowEnd = false;
        private bool isDraggingStartPoint = false;
        private PointF arrowDragStart = PointF.Empty;

        public event Action<float> ZoomChanged;

        // СИСТЕМА ПЕРЕМЕЩЕНИЯ ИЗ ВАШЕГО КОДА
        private bool isDraggingElements = false;
        private PointF dragStartPoint;// виртуальные координаты начала drag для группы
        private Dictionary<BpmnBlock, RectangleF> originalBlockBounds = new Dictionary<BpmnBlock, RectangleF>();
        private Dictionary<object, ArrowState> originalArrowStates = new Dictionary<object, ArrowState>();

        // НАПРАВЛЯЮЩИЕ ВЫРАВНИВАНИЯ ИЗ СТАРОГО КОДА
        private readonly List<float> verticalGuides = new List<float>();
        private readonly List<float> horizontalGuides = new List<float>();
        private const float GUIDE_TOLERANCE = 8f;

        // ДЛЯ КОМАНДНОЙ СИСТЕМЫ
        private object lastSelectedElement = null;
        private RectangleF _previousBlockBounds;
        private bool _isBlockDragInProgress = false;
        private RectangleF _dragStartBounds;

        // НОВЫЕ СОБЫТИЯ ДЛЯ ОТСЛЕЖИВАНИЯ ИЗМЕНЕНИЙ
        public event EventHandler BlockModified;
        public event EventHandler ArrowModified;
        public event EventHandler ElementAdded;

        //  ПОЛЯ ДЛЯ СОХРАНЕНИЯ ОРИГИНАЛЬНОГО СОСТОЯНИЯ СТРЕЛКИ
        private BpmnArrow _draggingArrow = null;
        private ArrowState _originalArrowStateBeforeDrag = null;
        private BpmnCurvedArrow _draggingCurvedArrow = null;
        private ArrowState _originalCurvedArrowStateBeforeDrag = null;

        public void SetBlocks(List<BpmnBlock> b)
        {
            if (sheets.ContainsKey(currentSheetIndex))
            {
                sheets[currentSheetIndex] = (new List<BpmnBlock>(b), sheets[currentSheetIndex].arrows, sheets[currentSheetIndex].curvedArrows);
            }
            Invalidate();
        }

        public void SetArrows(List<BpmnArrow> a)
        {
            if (sheets.ContainsKey(currentSheetIndex))
            {
                sheets[currentSheetIndex] = (sheets[currentSheetIndex].blocks, new List<BpmnArrow>(a ?? new List<BpmnArrow>()), sheets[currentSheetIndex].curvedArrows);
            }
            Invalidate();
        }

        // ДОБАВЛЯЕМ метод для curvedArrows
        public void SetCurvedArrows(List<BpmnCurvedArrow> c)
        {
            if (sheets.ContainsKey(currentSheetIndex))
            {
                sheets[currentSheetIndex] = (sheets[currentSheetIndex].blocks, sheets[currentSheetIndex].arrows, new List<BpmnCurvedArrow>(c ?? new List<BpmnCurvedArrow>()));
            }
            Invalidate();
        }

        public List<BpmnArrow> GetArrows() => arrows;
        public List<BpmnBlock> GetBlocks() => blocks;

        // МЕТОДЫ ДЛЯ РАБОТЫ С ВЫДЕЛЕНИЕМ (из вашего кода)
        public List<BpmnBlock> GetSelectedBlocks() => selectedElements.OfType<BpmnBlock>().ToList();
        public List<BpmnArrow> GetSelectedArrows() => selectedElements.OfType<BpmnArrow>().ToList();
        public bool IsElementSelected(object element) => selectedElements.Contains(element);
        public List<object> GetSelectedElements() => selectedElements.ToList();

        public void ClearSelection()
        {
            selectedElements.Clear();
            primarySelectedElement = null;

            // ДОБАВЛЯЕМ сброс полей выделения:
            selectedBlock = null;
            selectedArrow = null;

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

            // Создаем контекстное меню для элементов (только удалить)
            contextMenuForElements = new ContextMenuStrip();
            deleteMenuItem = new ToolStripMenuItem("Удалить");
            deleteMenuItem.ForeColor = Color.Red;
            deleteMenuItem.Click += (s, e) => DeleteSelectedElements();
            contextMenuForElements.Items.Add(deleteMenuItem);

            // Создаем контекстное меню для холста (только управление листами)
            contextMenuForCanvas = new ContextMenuStrip();
            var createSheetMenuItem = new ToolStripMenuItem("Создать новый лист");
            var selectSheetMenuItem = new ToolStripMenuItem("Выбрать лист");
            var deleteSheetMenuItem = new ToolStripMenuItem("Удалить лист");

            createSheetMenuItem.Click += (s, e) => CreateNewSheet();
            selectSheetMenuItem.Click += (s, e) => SelectSheet();
            deleteSheetMenuItem.Click += (s, e) => DeleteSheet();

            contextMenuForCanvas.Items.AddRange(new[] { createSheetMenuItem, selectSheetMenuItem, deleteSheetMenuItem });

            // ИСПРАВЛЯЕМ: создаем sheets с curvedArrows
            sheets = new Dictionary<int, (List<BpmnBlock> blocks, List<BpmnArrow> arrows, List<BpmnCurvedArrow> curvedArrows)>();
            sheets[0] = (new List<BpmnBlock>(), new List<BpmnArrow>(), new List<BpmnCurvedArrow>());
            currentSheetIndex = 0;

            // Контекстное меню для пула
            contextMenuForPool = new ContextMenuStrip();
            var addLineMenuItem = new ToolStripMenuItem("Добавить дорожку");
            var moveLaneUpMenuItem = new ToolStripMenuItem("Переместить дорожку выше");
            var moveLaneDownMenuItem = new ToolStripMenuItem("Переместить дорожку ниже");
            var nestLaneMenuItem = new ToolStripMenuItem("Вложить в другую дорожку");
            var unnestLaneMenuItem = new ToolStripMenuItem("Вывести из вложенности");
            var removeLaneMenuItem = new ToolStripMenuItem("Удалить дорожку");
            var deleteElementMenuItem = new ToolStripMenuItem("Удалить");

            addLineMenuItem.Click += (s, e) => AddLineToSelectedPool();
            moveLaneUpMenuItem.Click += (s, e) => MoveLaneUp();
            moveLaneDownMenuItem.Click += (s, e) => MoveLaneDown();
            nestLaneMenuItem.Click += (s, e) => NestLane();
            unnestLaneMenuItem.Click += (s, e) => UnnestLane();
            removeLaneMenuItem.Click += (s, e) => RemoveSelectedLane();
            deleteElementMenuItem.ForeColor = Color.Red;
            deleteElementMenuItem.Click += (s, e) => DeleteSelectedElements();

            contextMenuForPool.Items.Add(new ToolStripSeparator());
            contextMenuForPool.Items.AddRange(new[] { addLineMenuItem, removeLaneMenuItem, deleteElementMenuItem, moveLaneUpMenuItem, moveLaneDownMenuItem, nestLaneMenuItem, unnestLaneMenuItem });
            contextMenuForPool.Opening += (s, e) =>
            {
                Point clientPos = PointToClient(Cursor.Position);
                PointF virtualPos = ScreenToVirtual(clientPos);

                var pool = GetPoolAtPoint(virtualPos);
                if (pool != null)
                {
                    var lane = GetLaneAtPoint(pool, virtualPos);
                    currentLaneUnderCursor = lane;

                    if (lane != null)
                    {
                        addLineMenuItem.Text = "Добавить вложенную дорожку";
                        removeLaneMenuItem.Text = "Удалить эту дорожку";
                        removeLaneMenuItem.Enabled = true;
                    }
                    else
                    {
                        addLineMenuItem.Text = "Добавить дорожку";
                        removeLaneMenuItem.Text = "Удалить дорожку";
                        removeLaneMenuItem.Enabled = false;
                    }
                }
                else
                {
                    currentLaneUnderCursor = null;
                }
            };

            // Таймер для автоматического скрытия подсветки ошибки
            _errorHighlightTimer = new System.Windows.Forms.Timer();
            _errorHighlightTimer.Interval = 1000; // 1 секунда
            _errorHighlightTimer.Tick += (s, args) =>
            {
                _showPoolErrorHighlight = false;
                _errorHighlightTimer.Stop();
                Invalidate(); // Перерисовываем канвас
            };
            _errorHighlightTimer.Enabled = false;
        }

        private void CreateNewSheet()//Создание листов
        {
            if (sheets.Count >= MAX_SHEETS)
            {
                MessageBox.Show($"Достигнут лимит ({MAX_SHEETS}) листов.");
                return;
            }

            int newIndex = sheets.Keys.Max() + 1;

            // ОБНОВЛЯЕМ: добавляем curvedArrows
            sheets[newIndex] = (new List<BpmnBlock>(), new List<BpmnArrow>(), new List<BpmnCurvedArrow>());
            currentSheetIndex = newIndex;

            ClearSelection();
            Invalidate();
        }

        private void SelectSheet()//Выбор листа
        {
            if (sheets.Count <= 1)
            {
                MessageBox.Show("Нет других листов.");
                return;
            }

            using (var form = new Form())
            {
                form.Text = "Выбрать лист";
                form.Size = new Size(250, 350);

                var listbox = new ListBox { Dock = DockStyle.Fill };
                var keys = sheets.Keys.OrderBy(k => k).ToArray();

                foreach (var k in keys)
                    listbox.Items.Add("Лист " + (k + 1));

                listbox.SelectedIndex = Array.IndexOf(keys, currentSheetIndex);

                var ok = new Button { Text = "OK", Dock = DockStyle.Bottom };
                ok.Click += (s, e) =>
                {
                    form.DialogResult = DialogResult.OK;
                };

                form.Controls.Add(listbox);
                form.Controls.Add(ok);

                if (form.ShowDialog() == DialogResult.OK && listbox.SelectedIndex >= 0)
                {
                    int selectedSheetKey = keys[listbox.SelectedIndex];
                    currentSheetIndex = selectedSheetKey;

                    ClearSelection();
                    Invalidate();
                }
            }
        }

        private void DeleteSheet()//Удаление листа
        {
            if (sheets.Count <= 1)
            {
                MessageBox.Show("Нельзя удалить единственный лист.");
                return;
            }

            if (MessageBox.Show("Удалить текущий лист?", "Подтверждение",
                MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                int deleteIndex = currentSheetIndex;

                var remaining = sheets.Keys.Where(k => k != deleteIndex).OrderBy(k => k).ToList();
                int newIndex = remaining.First();

                sheets.Remove(deleteIndex);

                currentSheetIndex = newIndex;

                ClearSelection();
                Invalidate();
            }
        }

        private void InfiniteCanvas_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                DeleteSelectedElements();
            }
        }

        // УЛУЧШЕННОЕ УДАЛЕНИЕ С КОМАНДНОЙ СИСТЕМОЙ
        private void DeleteSelectedElements()
        {
            var form = this.FindForm() as Form1;
            bool useCommandSystem = form?.CommandManager != null;

            // Собираем все элементы для удаления
            var blocksToDelete = selectedElements.OfType<BpmnBlock>().ToList();
            var arrowsToDelete = selectedElements.OfType<BpmnArrow>().ToList();
            var curvedArrowsToDelete = selectedElements.OfType<BpmnCurvedArrow>().ToList();

            if (useCommandSystem)
            {
                // Создаем список команд для макрокоманды
                var commands = new List<ICommand>();

                // Команды удаления блоков
                foreach (var block in blocksToDelete)
                {
                    commands.Add(new DeleteBlockCommand(block, blocks, arrows, this));
                }

                // Команды удаления обычных стрелок
                foreach (var arrow in arrowsToDelete)
                {
                    commands.Add(new DeleteArrowCommand(arrow, arrows, this));
                }

                // Команды удаления кривых стрелок
                foreach (var curvedArrow in curvedArrowsToDelete)
                {
                    commands.Add(new DeleteCurvedArrowCommand(curvedArrow, curvedArrows, this));
                }

                // Создаем и выполняем макрокоманду
                if (commands.Count > 0)
                {
                    var macroCommand = new MacroCommand(commands, "Удаление группы элементов");
                    form.CommandManager.Execute(macroCommand);
                }
            }
            else
            {
                // Fallback логика без командной системы
                foreach (var block in blocksToDelete)
                {
                    blocks.Remove(block);
                    foreach (var arrow in arrows.ToList())
                    {
                        if (arrow.StartBlock == block)
                        {
                            arrow.StartBlock = null;
                            arrow.StartPoint = arrow.StartPoint;
                        }
                        if (arrow.EndBlock == block)
                        {
                            arrow.EndBlock = null;
                            arrow.EndPoint = arrow.EndPoint;
                        }
                    }
                    ArrowModified?.Invoke(this, EventArgs.Empty);
                }

                foreach (var arrow in arrowsToDelete)
                {
                    arrows.Remove(arrow);
                    ArrowModified?.Invoke(this, EventArgs.Empty);
                }

                foreach (var curvedArrow in curvedArrowsToDelete)
                {
                    curvedArrows.Remove(curvedArrow);
                    SetCurvedArrows(curvedArrows);
                }
            }

            ClearSelection();
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
                e.Handled = true;
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

        // ОБНОВЛЕННЫЙ МЕТОД С КОМАНДНОЙ СИСТЕМОЙ
        private void UpdateBlockText(bool enterPressed)
        {
            if (selectedBlock != null && editTextBox != null)
            {
                string newText = editTextBox.Text;
                bool textChanged = newText != selectedBlock.Text;

                RemoveEditTextBox();

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
                        selectedBlock.Text = newText;
                    }
                    BlockModified?.Invoke(this, EventArgs.Empty);
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

        // СТАРАЯ версия для обратной совместимости (возвращает 2 элемента)
        private (BpmnBlock block, PointF point) FindNearestConnectionPoint(PointF virtualPos, float maxDistance = 15f)
        {
            var result = FindNearestConnectionPointWithIndex(virtualPos, maxDistance);
            return (result.block, result.point);
        }

        // НОВАЯ версия с индексом (возвращает 3 элемента)
        private (BpmnBlock block, PointF point, int index) FindNearestConnectionPointWithIndex(PointF virtualPos, float maxDistance = 15f)
        {
            BpmnBlock nearestBlock = null;
            PointF nearestPoint = PointF.Empty;
            int nearestIndex = -1;
            float minDistance = float.MaxValue;

            foreach (var block in blocks)
            {
                var points = block.GetConnectionPoints();
                for (int i = 0; i < points.Length; i++)
                {
                    float distance = Distance(points[i], virtualPos);
                    if (distance < minDistance && distance <= maxDistance)
                    {
                        minDistance = distance;
                        nearestBlock = block;
                        nearestPoint = points[i];
                        nearestIndex = i;
                    }
                }
            }

            return (nearestBlock, nearestPoint, nearestIndex);
        }

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

        // ДОБАВЛЯЕМ метод для получения кривой стрелки по точке
        private BpmnCurvedArrow GetCurvedArrowAtPoint(PointF point)
        {
            foreach (var curvedArrow in curvedArrows.AsEnumerable().Reverse())
            {
                if (curvedArrow.HitTest(point))
                    return curvedArrow;
            }
            return null;
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

        // ОБНОВЛЕННЫЙ МЕТОД ДЛЯ ОБНОВЛЕНИЯ СТРЕЛОК
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

            // ДОБАВЛЯЕМ обновление кривых стрелок
            foreach (var curvedArrow in curvedArrows)
            {
                if (curvedArrow.StartBlock == movedBlock)
                {
                    float deltaX = movedBlock.Bounds.X - previousBounds.X;
                    float deltaY = movedBlock.Bounds.Y - previousBounds.Y;

                    curvedArrow.StartPoint = new PointF(
                        curvedArrow.StartPoint.X + deltaX,
                        curvedArrow.StartPoint.Y + deltaY
                    );

                    // ИСПРАВЛЕНИЕ: ПЕРЕСЧИТЫВАЕМ КОНТРОЛЬНЫЕ ТОЧКИ ТОЛЬКО ЕСЛИ СТРЕЛКА ПРИКРЕПЛЕНА
                    if (curvedArrow.IsStartAttached || curvedArrow.IsEndAttached)
                    {
                        curvedArrow.CalculateControlPoints();
                    }
                    else
                    {
                        // ЕСЛИ СТРЕЛКА НЕПРИКРЕПЛЕНА, ПРОСТО ПЕРЕМЕЩАЕМ КОНТРОЛЬНЫЕ ТОЧКИ
                        curvedArrow.ControlPoint1 = new PointF(
                            curvedArrow.ControlPoint1.X + deltaX,
                            curvedArrow.ControlPoint1.Y + deltaY
                        );
                        curvedArrow.ControlPoint2 = new PointF(
                            curvedArrow.ControlPoint2.X + deltaX,
                            curvedArrow.ControlPoint2.Y + deltaY
                        );
                    }
                }

                if (curvedArrow.EndBlock == movedBlock)
                {
                    float deltaX = movedBlock.Bounds.X - previousBounds.X;
                    float deltaY = movedBlock.Bounds.Y - previousBounds.Y;

                    curvedArrow.EndPoint = new PointF(
                        curvedArrow.EndPoint.X + deltaX,
                        curvedArrow.EndPoint.Y + deltaY
                    );

                    // ИСПРАВЛЕНИЕ: ПЕРЕСЧИТЫВАЕМ КОНТРОЛЬНЫЕ ТОЧКИ ТОЛЬКО ЕСЛИ СТРЕЛКА ПРИКРЕПЛЕНА
                    if (curvedArrow.IsStartAttached || curvedArrow.IsEndAttached)
                    {
                        curvedArrow.CalculateControlPoints();
                    }
                    else
                    {
                        // ЕСЛИ СТРЕЛКА НЕПРИКРЕПЛЕНА, ПРОСТО ПЕРЕМЕЩАЕМ КОНТРОЛЬНЫЕ ТОЧКИ
                        curvedArrow.ControlPoint1 = new PointF(
                            curvedArrow.ControlPoint1.X + deltaX,
                            curvedArrow.ControlPoint1.Y + deltaY
                        );
                        curvedArrow.ControlPoint2 = new PointF(
                            curvedArrow.ControlPoint2.X + deltaX,
                            curvedArrow.ControlPoint2.Y + deltaY
                        );
                    }
                }
            }
        }

        /// <summary>
        /// Обновляет позиции стрелок, прикрепленных к блоку, после изменения размера
        /// Сохраняет привязку к той же точке привязки, а не ищет ближайшую
        /// </summary>
        private void UpdateArrowsAfterResize(BpmnBlock resizedBlock, RectangleF previousBounds)
        {
            foreach (var arrow in arrows)
            {
                if (arrow.StartBlock == resizedBlock && arrow.StartConnectionPointIndex >= 0)
                {
                    // Сохраняем привязку к той же точке привязки по индексу
                    var points = resizedBlock.GetConnectionPoints();
                    if (arrow.StartConnectionPointIndex < points.Length)
                    {
                        arrow.StartPoint = points[arrow.StartConnectionPointIndex];
                    }
                }

                if (arrow.EndBlock == resizedBlock && arrow.EndConnectionPointIndex >= 0)
                {
                    // Сохраняем привязку к той же точке привязки по индексу
                    var points = resizedBlock.GetConnectionPoints();
                    if (arrow.EndConnectionPointIndex < points.Length)
                    {
                        arrow.EndPoint = points[arrow.EndConnectionPointIndex];
                    }
                }
            }

            // ДОБАВЛЯЕМ обновление кривых стрелок
            foreach (var curvedArrow in curvedArrows)
            {
                if (curvedArrow.StartBlock == resizedBlock && curvedArrow.StartConnectionPointIndex >= 0)
                {
                    // Сохраняем привязку к той же точке привязки по индексу
                    var points = resizedBlock.GetConnectionPoints();
                    if (curvedArrow.StartConnectionPointIndex < points.Length)
                    {
                        curvedArrow.StartPoint = points[curvedArrow.StartConnectionPointIndex];
                    }
                }

                if (curvedArrow.EndBlock == resizedBlock && curvedArrow.EndConnectionPointIndex >= 0)
                {
                    // Сохраняем привязку к той же точке привязки по индексу
                    var points = resizedBlock.GetConnectionPoints();
                    if (curvedArrow.EndConnectionPointIndex < points.Length)
                    {
                        curvedArrow.EndPoint = points[curvedArrow.EndConnectionPointIndex];
                    }
                }

                // Пересчитываем контрольные точки для кривых стрелок
                if ((curvedArrow.StartBlock == resizedBlock || curvedArrow.EndBlock == resizedBlock) &&
                    (curvedArrow.StartConnectionPointIndex >= 0 || curvedArrow.EndConnectionPointIndex >= 0))
                {
                    curvedArrow.CalculateControlPoints();
                }
            }
        }

        /// <summary>
        /// Находит соответствующую точку привязки на измененном блоке
        /// </summary>
        private PointF GetConnectionPointOnResizedBlock(BpmnBlock block, PointF originalPoint, RectangleF previousBounds)
        {
            var points = block.GetConnectionPoints();
            if (points == null || points.Length == 0)
                return originalPoint;

            // Определяем, к какой стороне блока была прикреплена стрелка
            var side = GetAttachmentSide(originalPoint, previousBounds);

            // Находим точку на той же стороне измененного блока
            return FindPointOnSide(block.Bounds, side, originalPoint);
        }

        /// <summary>
        /// Определяет, к какой стороне блока прикреплена точка
        /// </summary>
        private string GetAttachmentSide(PointF point, RectangleF bounds)
        {
            float leftDist = Math.Abs(point.X - bounds.Left);
            float rightDist = Math.Abs(point.X - bounds.Right);
            float topDist = Math.Abs(point.Y - bounds.Top);
            float bottomDist = Math.Abs(point.Y - bounds.Bottom);

            // Находим минимальное расстояние до стороны
            float minDist = Math.Min(Math.Min(leftDist, rightDist), Math.Min(topDist, bottomDist));

            if (minDist == leftDist) return "Left";
            if (minDist == rightDist) return "Right";
            if (minDist == topDist) return "Top";
            return "Bottom";
        }

        /// <summary>
        /// Находит точку на указанной стороне блока, ближайшую к оригинальной точке
        /// </summary>
        private PointF FindPointOnSide(RectangleF bounds, string side, PointF originalPoint)
        {
            var points = GetConnectionPointsForBounds(bounds);
            PointF bestPoint = points[0];
            float minDistance = float.MaxValue;

            foreach (var point in points)
            {
                // Проверяем, находится ли точка на нужной стороне
                bool onTargetSide = false;
                switch (side)
                {
                    case "Left": onTargetSide = Math.Abs(point.X - bounds.Left) < 1; break;
                    case "Right": onTargetSide = Math.Abs(point.X - bounds.Right) < 1; break;
                    case "Top": onTargetSide = Math.Abs(point.Y - bounds.Top) < 1; break;
                    case "Bottom": onTargetSide = Math.Abs(point.Y - bounds.Bottom) < 1; break;
                }

                if (onTargetSide)
                {
                    float distance = Distance(point, originalPoint);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        bestPoint = point;
                    }
                }
            }

            return bestPoint;
        }

        /// <summary>
        /// Генерирует точки привязки для прямоугольника (аналогично BpmnBlock.GetConnectionPoints())
        /// </summary>
        private PointF[] GetConnectionPointsForBounds(RectangleF bounds)
        {
            var points = new List<PointF>();

            // Левая сторона
            points.Add(new PointF(bounds.Left, bounds.Top));
            points.Add(new PointF(bounds.Left, bounds.Top + bounds.Height / 3));
            points.Add(new PointF(bounds.Left, bounds.Top + 2 * bounds.Height / 3));
            points.Add(new PointF(bounds.Left, bounds.Bottom));

            // Правая сторона  
            points.Add(new PointF(bounds.Right, bounds.Top));
            points.Add(new PointF(bounds.Right, bounds.Top + bounds.Height / 3));
            points.Add(new PointF(bounds.Right, bounds.Top + 2 * bounds.Height / 3));
            points.Add(new PointF(bounds.Right, bounds.Bottom));

            // Верхняя сторона
            points.Add(new PointF(bounds.Left, bounds.Top));
            points.Add(new PointF(bounds.Left + bounds.Width / 3, bounds.Top));
            points.Add(new PointF(bounds.Left + 2 * bounds.Width / 3, bounds.Top));
            points.Add(new PointF(bounds.Right, bounds.Top));

            // Нижняя сторона
            points.Add(new PointF(bounds.Left, bounds.Bottom));
            points.Add(new PointF(bounds.Left + bounds.Width / 3, bounds.Bottom));
            points.Add(new PointF(bounds.Left + 2 * bounds.Width / 3, bounds.Bottom));
            points.Add(new PointF(bounds.Right, bounds.Bottom));

            // Убираем дубликаты (угловые точки повторяются)
            return points.Distinct().ToArray();
        }

        // МАСШТАБИРОВАНИЕ ИЗ СТАРОГО КОДА (улучшенное)
        private void InfiniteCanvas_MouseWheel(object sender, MouseEventArgs e)
        {
            float zoomFactor = e.Delta > 0 ? ZOOM_STEP : 1.0f / ZOOM_STEP;
            float newZoom = Math.Max(MIN_ZOOM, Math.Min(MAX_ZOOM, zoom * zoomFactor));

            if (newZoom != zoom)
            {
                PointF virtualMousePos = ScreenToVirtual(e.Location);
                zoom = newZoom;
                PointF newScreenPos = VirtualToScreen(virtualMousePos);
                canvasOffset.X += (e.Location.X - newScreenPos.X) / zoom;
                canvasOffset.Y += (e.Location.Y - newScreenPos.Y) / zoom;

                UpdateEditTextBoxLocation();
                this.Invalidate();
                ZoomChanged?.Invoke(zoom);
            }
        }

        private void InfiniteCanvas_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && !IsCtrlPressed())
            {
                PointF virtualPos = ScreenToVirtual(e.Location);

                // 1. Сначала проверяем КРИВЫЕ СТРЕЛКИ (высший приоритет)
                var clickedCurvedArrow = GetCurvedArrowAtPoint(virtualPos);
                if (clickedCurvedArrow != null)
                {
                    if (!selectedElements.Contains(clickedCurvedArrow))
                    {
                        ClearSelection();
                        selectedElements.Add(clickedCurvedArrow);
                        primarySelectedElement = clickedCurvedArrow;
                    }
                    lastSelectedElement = clickedCurvedArrow;
                    Invalidate();
                    return;
                }

                // 2. Проверяем ОБЫЧНЫЕ СТРЕЛКИ
                var clickedArrow = GetArrowAtPoint(virtualPos);
                if (clickedArrow != null)
                {
                    if (!selectedElements.Contains(clickedArrow))
                    {
                        ClearSelection();
                        selectedElements.Add(clickedArrow);
                        primarySelectedElement = clickedArrow;
                        selectedArrow = clickedArrow;
                    }
                    lastSelectedElement = clickedArrow;
                    Invalidate();
                    return;
                }

                // 3. Проверяем БЛОКИ (включая блоки внутри дорожек)
                var clickedBlock = GetBlockAtPoint(virtualPos);
                if (clickedBlock != null)
                {
                    // Важно: не проверяем, находится ли блок внутри пула/дорожки
                    // Блок должен быть доступен всегда
                    if (!selectedElements.Contains(clickedBlock))
                    {
                        ClearSelection();
                        selectedElements.Add(clickedBlock);
                        primarySelectedElement = clickedBlock;
                        selectedBlock = clickedBlock;
                    }
                    lastSelectedElement = clickedBlock;
                    Invalidate();
                    return;
                }

                // 4. Проверяем ДОРОЖКИ (только если не попали на блок или стрелку)
                var clickedPool = GetPoolAtPoint(virtualPos);
                if (clickedPool != null && clickedPool.Type == "Пул")
                {
                    var clickedLane = GetLaneAtPoint(clickedPool, virtualPos);
                    if (clickedLane != null)
                    {
                        // Выделяем пул, но запоминаем дорожку для контекстного меню
                        if (!selectedElements.Contains(clickedPool))
                        {
                            ClearSelection();
                            selectedElements.Add(clickedPool);
                            primarySelectedElement = clickedPool;
                        }
                        currentLaneUnderCursor = clickedLane;
                        lastSelectedElement = clickedPool;
                        Invalidate();
                        return;
                    }

                    // 5. Клик на ПУЛ (без дорожки)
                    if (!selectedElements.Contains(clickedPool))
                    {
                        ClearSelection();
                        selectedElements.Add(clickedPool);
                        primarySelectedElement = clickedPool;
                        selectedBlock = clickedPool;
                    }
                    lastSelectedElement = clickedPool;
                    Invalidate();
                    return;
                }

                // Если кликнули в пустое место - очищаем выделение
                ClearSelection();
            }
        }

        // ОБЪЕДИНЕННЫЙ МЕТОД MouseDown
        private void InfiniteCanvas_MouseDown(object sender, MouseEventArgs e)
        {
            PointF virtualPos = ScreenToVirtual(e.Location);
            this.Focus();

            if (isCreatingArrow || isDraggingArrowEnd)
                return;

            if (e.Button == MouseButtons.Left)
            {
                // ========== 1. ПРОВЕРКА МАРКЕРОВ СТРЕЛОК (высший приоритет) ==========

                // 1.1 Кривые стрелки
                var clickedCurvedArrow = GetCurvedArrowAtPoint(virtualPos);
                if (clickedCurvedArrow != null &&
                    (clickedCurvedArrow.HitTestEndpoint(virtualPos, true, 10f) ||
                     clickedCurvedArrow.HitTestEndpoint(virtualPos, false, 10f)))
                {
                    isDraggingArrowEnd = true;
                    isDraggingStartPoint = clickedCurvedArrow.HitTestEndpoint(virtualPos, true, 10f);
                    arrowDragStart = virtualPos;

                    ClearSelection();
                    selectedElements.Add(clickedCurvedArrow);
                    primarySelectedElement = clickedCurvedArrow;

                    // Сохраняем состояние для отмены
                    _draggingCurvedArrow = clickedCurvedArrow;
                    _originalCurvedArrowStateBeforeDrag = new ArrowState
                    {
                        StartPoint = clickedCurvedArrow.StartPoint,
                        EndPoint = clickedCurvedArrow.EndPoint,
                        StartBlock = clickedCurvedArrow.StartBlock,
                        EndBlock = clickedCurvedArrow.EndBlock,
                        ControlPoint1 = clickedCurvedArrow.ControlPoint1,
                        ControlPoint2 = clickedCurvedArrow.ControlPoint2,
                        StartConnectionPointIndex = clickedCurvedArrow.StartConnectionPointIndex,
                        EndConnectionPointIndex = clickedCurvedArrow.EndConnectionPointIndex
                    };

                    this.Cursor = Cursors.Cross;
                    Invalidate();
                    return;
                }

                // 1.2 Обычные стрелки
                var clickedArrow = GetArrowAtPoint(virtualPos);
                if (clickedArrow != null &&
                    (clickedArrow.HitTestEndpoint(virtualPos, true) ||
                     clickedArrow.HitTestEndpoint(virtualPos, false)))
                {
                    isDraggingArrowEnd = true;
                    isDraggingStartPoint = clickedArrow.HitTestEndpoint(virtualPos, true);
                    arrowDragStart = virtualPos;

                    ClearSelection();
                    selectedElements.Add(clickedArrow);
                    primarySelectedElement = clickedArrow;

                    // Сохраняем состояние для отмены
                    _draggingArrow = clickedArrow;
                    _originalArrowStateBeforeDrag = new ArrowState
                    {
                        StartPoint = clickedArrow.StartPoint,
                        EndPoint = clickedArrow.EndPoint,
                        StartBlock = clickedArrow.StartBlock,
                        EndBlock = clickedArrow.EndBlock,
                        StartConnectionPointIndex = clickedArrow.StartConnectionPointIndex,
                        EndConnectionPointIndex = clickedArrow.EndConnectionPointIndex
                    };

                    this.Cursor = Cursors.Cross;
                    Invalidate();
                    return;
                }

                // ========== 2. ПРОВЕРКА РУЧЕК ИЗМЕНЕНИЯ РАЗМЕРА БЛОКА ==========
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
                        _previousBlockBounds = clickedBlock.Bounds;

                        ClearSelection();
                        selectedElements.Add(clickedBlock);
                        primarySelectedElement = clickedBlock;

                        this.Cursor = GetResizeCursor(resizeArea);
                        Invalidate();
                        return;
                    }
                }

                // ========== 3. ПРОВЕРКА ИЗМЕНЕНИЯ РАЗМЕРА ДОРОЖКИ ==========
                var clickedPoolForResize = GetPoolAtPoint(virtualPos);
                if (clickedPoolForResize != null && clickedPoolForResize.Type == "Пул")
                {
                    var clickedLane = GetLaneAtPoint(clickedPoolForResize, virtualPos);
                    if (clickedLane != null && IsPointOnLaneBottomBorder(clickedLane, virtualPos, LANE_RESIZE_MARGIN * 2))
                    {
                        _isResizingLane = true;
                        _resizingLane = clickedLane;
                        _resizingLanePool = clickedPoolForResize;
                        _resizeLaneStartPoint = virtualPos;
                        _originalLaneBounds = clickedLane.Bounds;

                        this.Cursor = Cursors.SizeNS;
                        ClearSelection();
                        selectedElements.Add(clickedPoolForResize);
                        primarySelectedElement = clickedPoolForResize;
                        Invalidate();
                        return;
                    }
                }

                // ========== 4. ПРОВЕРКА НАЧАЛА ПЕРЕМЕЩЕНИЯ ДОРОЖКИ ==========
                // Важно: перемещение дорожки начинается ТОЛЬКО если:
                // 1. Пул уже выделен (primarySelectedElement - это пул)
                // 2. Клик был именно на дорожке этого пула
                // 3. Не идет групповое выделение
                // 4. Не попали на границу изменения размера дорожки

                if (primarySelectedElement is BpmnBlock selectedPool &&
                    selectedPool.Type == "Пул" &&
                    !isSelecting) // УБИРАЕМ возможность начала перемещения дорожки при групповом выделении
                {
                    var clickedLane = GetLaneAtPoint(selectedPool, virtualPos);
                    if (clickedLane != null)
                    {
                        // Проверяем, не попали ли мы на границу изменения размера
                        if (!IsPointOnLaneBottomBorder(clickedLane, virtualPos, LANE_RESIZE_MARGIN * 2))
                        {
                            // Начинаем перемещение дорожки
                            _isDraggingLaneInternal = true;
                            _draggingLaneInternal = clickedLane;
                            _draggingLanePoolInternal = selectedPool;
                            _draggingLaneParentInternal = clickedLane.ParentLine;
                            _dragLaneInternalStartPoint = virtualPos;
                            _originalLaneInternalBounds = clickedLane.Bounds;
                            _draggingLaneChildren = clickedLane.GetAllDescendants();

                            this.Cursor = Cursors.SizeAll;
                            Invalidate();
                            return;
                        }
                    }
                }

                // ========== 5. ВЫДЕЛЕНИЕ ИЛИ ПЕРЕМЕЩЕНИЕ СТРЕЛОК ==========
                // 5.1 Кривые стрелки
                if (clickedCurvedArrow != null)
                {
                    // Если стрелка уже выделена в группе - используем групповое перемещение
                    if (selectedElements.Contains(clickedCurvedArrow))
                    {
                        StartElementsDrag(virtualPos);
                    }
                    else
                    {
                        // Если стрелка не выделена - выделяем только ее
                        ClearSelection();
                        selectedElements.Add(clickedCurvedArrow);
                        primarySelectedElement = clickedCurvedArrow;

                        // Для неприкрепленных стрелок начинаем перемещение
                        if (!clickedCurvedArrow.IsFullyAttached)
                        {
                            StartElementsDrag(virtualPos);
                        }
                    }
                    return;
                }

                // 5.2 Обычные стрелки
                if (clickedArrow != null)
                {
                    if (selectedElements.Contains(clickedArrow))
                    {
                        StartElementsDrag(virtualPos);
                    }
                    else
                    {
                        ClearSelection();
                        selectedElements.Add(clickedArrow);
                        primarySelectedElement = clickedArrow;

                        if (!clickedArrow.IsFullyAttached)
                        {
                            StartElementsDrag(virtualPos);
                        }
                    }
                    return;
                }

                // ========== 6. ВЫДЕЛЕНИЕ ИЛИ ПЕРЕМЕЩЕНИЕ БЛОКОВ ==========
                if (clickedBlock != null)
                {
                    if (selectedElements.Contains(clickedBlock))
                    {
                        // Блок уже выделен - используем групповое перемещение
                        StartElementsDrag(virtualPos);
                    }
                    else
                    {
                        // Блок не выделен - выделяем его
                        ClearSelection();
                        selectedElements.Add(clickedBlock);
                        primarySelectedElement = clickedBlock;
                        StartElementsDrag(virtualPos);
                    }
                    return;
                }

                // ========== 7. ПРОВЕРКА НА ДОРОЖКИ (ДО ГРУППОВОГО ВЫДЕЛЕНИЯ) ==========
                // Проверяем дорожки только если пул уже выделен
                if (primarySelectedElement is BpmnBlock selectedPoolBlock && selectedPoolBlock.Type == "Пул")
                {
                    var clickedLane = GetLaneAtPoint(selectedPoolBlock, virtualPos);
                    if (clickedLane != null)
                    {
                        // ПЕРЕМЕЩЕНИЕ ДОРОЖКИ: начинаем ТОЛЬКО если пул уже выделен
                        // и не попали на границу изменения размера
                        if (!IsPointOnLaneBottomBorder(clickedLane, virtualPos, LANE_RESIZE_MARGIN * 2))
                        {
                            _isDraggingLaneInternal = true;
                            _draggingLaneInternal = clickedLane;
                            _draggingLanePoolInternal = selectedPoolBlock; // Используем selectedPoolBlock
                            _draggingLaneParentInternal = clickedLane.ParentLine;
                            _dragLaneInternalStartPoint = virtualPos;
                            _originalLaneInternalBounds = clickedLane.Bounds;
                            _draggingLaneChildren = clickedLane.GetAllDescendants();

                            this.Cursor = Cursors.SizeAll;
                            Invalidate();
                            return;
                        }
                    }
                }

                // ========== 8. КЛИК В ПУСТОЕ МЕСТО ==========
                // Если мы дошли сюда, значит не кликнули ни на стрелку, ни на блок, ни на дорожку (или дорожка не готова к перемещению)

                if (IsCtrlPressed())
                {
                    // Панорамирование
                    isDragging = true;
                    lastMousePos = e.Location;
                    this.Cursor = Cursors.SizeAll;
                }
                else
                {
                    // Начало выделения области - ОБЯЗАТЕЛЬНО сбрасываем флаги перемещения дорожки
                    isSelecting = true;
                    selectionDragStartPoint = virtualPos;
                    selectionRectangle = new RectangleF(virtualPos.X, virtualPos.Y, 0, 0);
                    ClearSelection();

                    // Сбрасываем все флаги перемещения дорожек
                    _isDraggingLaneInternal = false;
                    _draggingLaneInternal = null;
                    _draggingLanePoolInternal = null;
                    _draggingLaneParentInternal = null;
                    _draggingLaneChildren = null;

                    Invalidate();
                }
            }

            else if (e.Button == MouseButtons.Right)
            {
                // ========== ОБРАБОТКА ПРАВОЙ КНОПКИ МЫШИ ==========
                // Важно: эта часть должна быть ВНЕ блока для левой кнопки

                // Контекстное меню для элементов или холста
                // ОБЪЯВЛЯЕМ переменные ЗДЕСЬ, чтобы они не конфликтовали с переменными из левой кнопки
                var clickedArrowRight = GetArrowAtPoint(virtualPos);
                var clickedBlockRight = GetBlockAtPoint(virtualPos);
                var clickedCurvedArrowRight = GetCurvedArrowAtPoint(virtualPos);
                var clickedPoolRight = GetPoolAtPoint(virtualPos);

                if (clickedPoolRight != null)
                {
                    if (!selectedElements.Contains(clickedPoolRight))
                    {
                        ClearSelection();
                        selectedElements.Add(clickedPoolRight);
                        primarySelectedElement = clickedPoolRight;
                    }

                    // Обновляем currentLaneUnderCursor для контекстного меню
                    currentLaneUnderCursor = GetLaneAtPoint(clickedPoolRight, virtualPos);

                    contextMenuForPool.Show(this, e.Location);
                    return;
                }

                if (clickedArrowRight != null || clickedBlockRight != null || clickedCurvedArrowRight != null)
                {
                    if (clickedArrowRight != null && !selectedElements.Contains(clickedArrowRight))
                    {
                        ClearSelection();
                        selectedElements.Add(clickedArrowRight);
                        primarySelectedElement = clickedArrowRight;
                    }
                    else if (clickedBlockRight != null && !selectedElements.Contains(clickedBlockRight))
                    {
                        ClearSelection();
                        selectedElements.Add(clickedBlockRight);
                        primarySelectedElement = clickedBlockRight;
                    }
                    else if (clickedCurvedArrowRight != null && !selectedElements.Contains(clickedCurvedArrowRight))
                    {
                        ClearSelection();
                        selectedElements.Add(clickedCurvedArrowRight);
                        primarySelectedElement = clickedCurvedArrowRight;
                    }

                    contextMenuForElements.Show(this, e.Location);
                    return;
                }
                else
                {
                    contextMenuForCanvas.Show(this, e.Location);
                }
            }
        }

        // СИСТЕМА ИЗМЕНЕНИЯ РАЗМЕРА ИЗ ВАШЕГО КОДА
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
                    return Cursors.Default;
            }
        }

        // СИСТЕМА ПЕРЕМЕЩЕНИЯ
        // Обновлённый StartElementsDrag для корректного группового перемещения
        private void StartElementsDrag(PointF virtualPos)
        {
            isDraggingElements = true;
            dragStartPoint = virtualPos;

            originalBlockBounds.Clear();
            originalArrowStates.Clear();

            foreach (var el in selectedElements)
            {
                if (el is BpmnBlock block)
                    originalBlockBounds[block] = block.Bounds;
                else if (el is BpmnArrow arrow)
                    originalArrowStates[arrow] = new ArrowState
                    {
                        StartPoint = arrow.StartPoint,
                        EndPoint = arrow.EndPoint,
                        StartBlock = arrow.StartBlock,
                        EndBlock = arrow.EndBlock
                    };
                else if (el is BpmnCurvedArrow curvedArrow)
                {
                    // ИСПРАВЛЕНИЕ: СОХРАНЯЕМ КОНТРОЛЬНЫЕ ТОЧКИ
                    originalArrowStates[curvedArrow] = new ArrowState
                    {
                        StartPoint = curvedArrow.StartPoint,
                        EndPoint = curvedArrow.EndPoint,
                        StartBlock = curvedArrow.StartBlock,
                        EndBlock = curvedArrow.EndBlock,
                        ControlPoint1 = curvedArrow.ControlPoint1,  // СОХРАНИЛИ
                        ControlPoint2 = curvedArrow.ControlPoint2   // СОХРАНИЛИ
                    };
                }
            }

            // Устанавливаем курсор для перемещения
            this.Cursor = Cursors.SizeAll;
        }

        // НАПРАВЛЯЮЩИЕ ВЫРАВНИВАНИЯ ИЗ СТАРОГО КОДА
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

            foreach (var block in blocks)
            {
                if (block == movingBlock) continue;

                float bLeft = block.Bounds.Left;
                float bRight = block.Bounds.Right;
                float bTop = block.Bounds.Top;
                float bBottom = block.Bounds.Bottom;
                float bCenterX = bLeft + block.Bounds.Width / 2;
                float bCenterY = bTop + block.Bounds.Height / 2;

                // Проверка по оси X
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
                    }
                }

                // Проверка по оси Y
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
                    }
                }
            }

            if (bestVertical.HasValue)
                verticalGuides.Add(bestVertical.Value);

            if (bestHorizontal.HasValue)
                horizontalGuides.Add(bestHorizontal.Value);

            Invalidate();
        }

        // ОБЪЕДИНЕННЫЙ МЕТОД MouseMove с добавленной автопрокруткой
        private void InfiniteCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            PointF virtualPos = ScreenToVirtual(e.Location);

            // ========== 1. ОБРАБОТКА ГРУППОВОГО ВЫДЕЛЕНИЯ (НАИВЫСШИЙ ПРИОРИТЕТ) ==========
            if (isSelecting)
            {
                // Рассчитываем прямоугольник выделения
                float x = Math.Min(selectionDragStartPoint.X, virtualPos.X);
                float y = Math.Min(selectionDragStartPoint.Y, virtualPos.Y);
                float width = Math.Abs(virtualPos.X - selectionDragStartPoint.X);
                float height = Math.Abs(virtualPos.Y - selectionDragStartPoint.Y);
                selectionRectangle = new RectangleF(x, y, width, height);

                // Сбрасываем ВСЕ флаги перемещения дорожек (чтобы точно не мешало)
                _isDraggingLaneInternal = false;
                _draggingLaneInternal = null;
                _draggingLanePoolInternal = null;
                _draggingLaneParentInternal = null;
                _draggingLaneChildren = null;

                // Временно выделяем элементы в области
                var tempSelected = new List<object>();

                foreach (var block in blocks)
                {
                    if (selectionRectangle.IntersectsWith(block.Bounds))
                        tempSelected.Add(block);
                }

                foreach (var arrow in arrows)
                {
                    if (selectionRectangle.IntersectsWith(arrow.GetBounds()))
                        tempSelected.Add(arrow);
                }

                foreach (var curvedArrow in curvedArrows)
                {
                    if (selectionRectangle.IntersectsWith(curvedArrow.GetBounds()))
                        tempSelected.Add(curvedArrow);
                }

                // Обновляем выделение
                selectedElements.Clear();
                selectedElements.AddRange(tempSelected);

                if (selectedElements.Count > 0)
                    primarySelectedElement = selectedElements[0];

                Invalidate();
                return; // ВАЖНО: возвращаемся здесь, не обрабатываем другие действия
            }

            // ========== 2. ПЕРЕМЕЩЕНИЕ ДОРОЖКИ ВНУТРИ РОДИТЕЛЬСКОЙ ДОРОЖКИ ==========
            if (_isDraggingLaneInternal && _draggingLaneInternal != null && _draggingLanePoolInternal != null)
            {
                float deltaX = virtualPos.X - _dragLaneInternalStartPoint.X;
                float deltaY = virtualPos.Y - _dragLaneInternalStartPoint.Y;

                // Вычисляем новую позицию
                float newX = _originalLaneInternalBounds.X + deltaX;
                float newY = _originalLaneInternalBounds.Y + deltaY;

                // Получаем границы, в которые можно перемещать дорожку
                RectangleF containerBounds = GetLaneContainerBounds(_draggingLaneInternal, _draggingLanePoolInternal);

                // Ограничиваем позицию границами контейнера
                newX = Math.Max(containerBounds.Left, Math.Min(newX, containerBounds.Right - _draggingLaneInternal.Bounds.Width));
                newY = Math.Max(containerBounds.Top, Math.Min(newY, containerBounds.Bottom - _draggingLaneInternal.Bounds.Height));

                // Применяем новую позицию
                _draggingLaneInternal.Bounds = new RectangleF(
                    newX,
                    newY,
                    _draggingLaneInternal.Bounds.Width,
                    _draggingLaneInternal.Bounds.Height
                );

                // Сохраняем относительную позицию дорожки
                _draggingLaneInternal.UpdateRelativePosition(_draggingLanePoolInternal.Bounds);

                // Перемещаем всех детей относительно перемещения родителя
                if (_draggingLaneChildren != null)
                {
                    float actualDeltaX = newX - _originalLaneInternalBounds.X;
                    float actualDeltaY = newY - _originalLaneInternalBounds.Y;

                    foreach (var child in _draggingLaneChildren)
                    {
                        child.Bounds = new RectangleF(
                            child.Bounds.X + actualDeltaX,
                            child.Bounds.Y + actualDeltaY,
                            child.Bounds.Width,
                            child.Bounds.Height
                        );

                        // Сохраняем относительную позицию и для детей
                        child.UpdateRelativePosition(_draggingLanePoolInternal.Bounds);
                    }
                }

                Invalidate();
                return;
            }

            // 3. ПЕРЕТАСКИВАНИЕ КОНЦА СТРЕЛКИ (высший приоритет)
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
                    // ИСПРАВЛЯЕМ: используем правильное имя переменной
                    var (block, point, index) = FindNearestConnectionPointWithIndex(virtualPos);
                    if (block != null)
                    {
                        selectedArrowForDrag.Attach(isDraggingStartPoint, block, point, index);
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

                // ДОБАВЛЯЕМ: Автопрокрутка при перетаскивании конца стрелки
                AdjustCanvasOffsetForPoint(virtualPos, 10f);

                this.Invalidate();
                return;
            }

            // 3.1 ДОБАВЛЯЕМ ПЕРЕТАСКИВАНИЕ КОНЦА КРИВОЙ СТРЕЛКИ
            if (isDraggingArrowEnd && primarySelectedElement is BpmnCurvedArrow selectedCurvedArrowForDrag)
            {
                if (IsCtrlPressed())
                {
                    selectedCurvedArrowForDrag.Detach(isDraggingStartPoint);
                    if (isDraggingStartPoint)
                        selectedCurvedArrowForDrag.StartPoint = virtualPos;
                    else
                        selectedCurvedArrowForDrag.EndPoint = virtualPos;
                }
                else
                {
                    var (block, point, index) = FindNearestConnectionPointWithIndex(virtualPos, 10f);
                    if (block != null)
                    {
                        selectedCurvedArrowForDrag.Attach(isDraggingStartPoint, block, point, index);
                    }
                    else
                    {
                        selectedCurvedArrowForDrag.Detach(isDraggingStartPoint);
                        if (isDraggingStartPoint)
                            selectedCurvedArrowForDrag.StartPoint = virtualPos;
                        else
                            selectedCurvedArrowForDrag.EndPoint = virtualPos;
                    }
                }

                // Пересчитываем контрольные точки при перемещении концов - ТОЛЬКО ЕСЛИ СТРЕЛКА ПРИКРЕПЛЕНА
                if (selectedCurvedArrowForDrag.IsStartAttached || selectedCurvedArrowForDrag.IsEndAttached)
                {
                    selectedCurvedArrowForDrag.CalculateControlPoints();
                }

                // ДОБАВЛЯЕМ: Автопрокрутка при перетаскивании конца кривой стрелки
                AdjustCanvasOffsetForPoint(virtualPos, 10f);

                this.Invalidate();
                return;
            }

            // 3.3 ПЕРЕМЕЩЕНИЕ ВСЕЙ СТРЕЛКИ - УПРОЩАЕМ ЛОГИКУ:
            if (isDraggingArrow && primarySelectedElement is BpmnArrow floatingArrow)
            {
                // УБИРАЕМ сложные проверки - просто перемещаем
                float deltaX = virtualPos.X - arrowDragStart.X;
                float deltaY = virtualPos.Y - arrowDragStart.Y;

                // Используем метод Move стрелки
                floatingArrow.Move(deltaX, deltaY);

                arrowDragStart = virtualPos;

                // ДОБАВЛЯЕМ: Автопрокрутка при перемещении стрелки
                AdjustCanvasOffsetForPoint(virtualPos, 10f);

                this.Invalidate();
                return;
            }

            // 3.4 ДОБАВЛЯЕМ ПЕРЕМЕЩЕНИЕ ВСЕЙ КРИВОЙ СТРЕЛКИ
            if (isDraggingArrow && primarySelectedElement is BpmnCurvedArrow floatingCurvedArrow)
            {
                // ИСПРАВЛЕНИЕ: ПЕРЕМЕЩАЕМ ТОЛЬКО НЕПРИКРЕПЛЕННЫХ СТРЕЛКИ
                if (!floatingCurvedArrow.IsFullyAttached)
                {
                    float deltaX = virtualPos.X - arrowDragStart.X;
                    float deltaY = virtualPos.Y - arrowDragStart.Y;

                    // ПЕРЕМЕЩАЕМ ВСЕГДА, без сложных проверок
                    floatingCurvedArrow.Move(deltaX, deltaY);
                    arrowDragStart = virtualPos;

                    // ДОБАВЛЯЕМ: Автопрокрутка при перемещении кривой стрелки
                    AdjustCanvasOffsetForPoint(virtualPos, 10f);
                }
                this.Invalidate();
                return;
            }
            //3.5 ПЕРЕМЕЩЕНИЕ ПУЛА
            if (!isDragging && !isDraggingElements && !isResizing && e.Button == MouseButtons.Left)
            {
                var clickedPool = GetPoolAtPoint(virtualPos);
                if (clickedPool != null)
                {
                    var clickedLane = GetLaneAtPoint(clickedPool, virtualPos);
                    if (clickedLane != null && !isDraggingLane)
                    {
                        // Начинаем перемещение дорожки
                        isDraggingLane = true;
                        draggingLane = clickedLane;
                        draggingLanePool = clickedPool;
                        dragStartPoint = virtualPos;
                        this.Cursor = Cursors.SizeNS; // Курсор для вертикального перемещения
                        return;
                    }
                }
            }

            // 4. ИЗМЕНЕНИЕ РАЗМЕРА БЛОКА
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

                if (newBounds.Width > 20 && newBounds.Height > 20)
                {
                    if (autoAdjustCanvasOffset)
                    {
                        AdjustCanvasOffsetForBlock(newBounds);
                    }

                    RectangleF previousBounds = resizingBlock.Bounds;

                    // Используем новый метод ResizePool вместо прямого изменения Bounds
                    if (resizingBlock.Type == "Пул")
                    {
                        resizingBlock.ResizePool(newBounds.Width, newBounds.Height);
                    }
                    else
                    {
                        resizingBlock.Bounds = newBounds;
                    }

                    // ДОБАВЛЯЕМ ОБНОВЛЕНИЕ СТРЕЛОК после изменения размера
                    UpdateArrowsAfterResize(resizingBlock, previousBounds);
                    UpdateAttachedArrows(resizingBlock, previousBounds);
                    UpdateEditTextBoxLocation();
                    this.Invalidate();
                }
                return;
            }

            // 5. ИЗМЕНЕНИЕ РАЗМЕРА ДОРОЖКИ
            if (_isResizingLane && _resizingLane != null && _resizingLanePool != null)
            {
                float deltaY = virtualPos.Y - _resizeLaneStartPoint.Y;
                float newHeight = Math.Max(LANE_MIN_HEIGHT, _originalLaneBounds.Height + deltaY);
                float maxHeight = GetMaxLaneHeight(_resizingLane, _resizingLanePool);
                newHeight = Math.Min(newHeight, maxHeight);

                _resizingLane.Bounds = new RectangleF(
                    _resizingLane.Bounds.X,
                    _resizingLane.Bounds.Y,
                    _resizingLane.Bounds.Width,
                    newHeight
                );

                RecalculateLanesPositions(_resizingLanePool);

                _isUpdatingLaneHeight = true;
                UpdatePoolHeight(_resizingLanePool);
                _isUpdatingLaneHeight = false;

                Invalidate();
                return;
            }

            // 6. ПЕРЕМЕЩЕНИЕ ВЫДЕЛЕННЫХ ЭЛЕМЕНТОВ - ОСНОВНОЙ РЕЖИМ
            if (isDraggingElements && selectedElements.Count > 0)
            {
                float deltaX = virtualPos.X - dragStartPoint.X;
                float deltaY = virtualPos.Y - dragStartPoint.Y;

                // Проверяем, не пытаемся ли мы переместить пул в другой пул
                if (IsDraggingPool())
                {
                    // Находим контейнер под курсором
                    var containerUnderCursor = GetContainerAtPoint(virtualPos);

                    // Ключевое изменение: проверяем, что контейнер - это ДРУГОЙ пул (не тот, который мы перемещаем)
                    if (containerUnderCursor.pool != null && !IsPoolInSelection(containerUnderCursor.pool))
                    {
                        // Устанавливаем флаг и сохраняем границы для отрисовки ошибки
                        _showPoolErrorHighlight = true;
                        _errorPoolBounds = containerUnderCursor.pool.Bounds;

                        // Запускаем таймер для скрытия подсветки через 1 секунду
                        if (_errorHighlightTimer != null)
                        {
                            _errorHighlightTimer.Start();
                        }

                        // Отменяем перемещение для всех пулов в выделении
                        foreach (var element in selectedElements)
                        {
                            if (element is BpmnBlock block && block.Type == "Пул")
                            {
                                // Возвращаем пул на исходную позицию
                                if (originalBlockBounds.TryGetValue(block, out RectangleF originalBounds))
                                {
                                    block.Bounds = originalBounds;
                                    // Также возвращаем дорожки пула
                                    if (block.PoolLanes != null)
                                    {
                                        foreach (var lane in block.PoolLanes)
                                        {
                                            // Пересчитываем позиции дорожек относительно исходного положения пула
                                            lane.UpdatePosition(
                                                originalBounds.X - block.Bounds.X,
                                                originalBounds.Y - block.Bounds.Y
                                            );
                                        }
                                    }
                                }
                            }
                        }

                        // Прерываем дальнейшую обработку перемещения
                        Invalidate(); // Перерисовываем, чтобы показать ошибку
                        return;
                    }
                    else
                    {
                        // Если не навели на другой пул, сбрасываем подсветку ошибки
                        _showPoolErrorHighlight = false;
                    }
                }

                // Обновляем направляющие для первого блока
                var firstBlock = selectedElements.OfType<BpmnBlock>().FirstOrDefault();
                if (firstBlock != null)
                {
                    UpdateAlignmentGuides(firstBlock);
                }

                // Обновляем подсветку контейнера под курсором
                var container = GetContainerAtPoint(virtualPos);
                if (!IsDraggingPool() || container.pool == null)
                {
                    _highlightedPool = container.pool;
                    _highlightedLane = container.lane;
                }
                else
                {
                    // Не подсвечиваем, если перемещаем пул в другой пул
                    _highlightedPool = null;
                    _highlightedLane = null;
                }

                foreach (var element in selectedElements)
                {
                    if (element is BpmnBlock block)
                    {
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

                            block.Bounds = newBounds;
                            if (block.Type == "Пул")
                            {
                                block.UpdatePoolLanesPosition(deltaX, deltaY);
                                block.ValidateLanePositions(); // ДОБАВЛЯЕМ ПРОВЕРКУ ПОЗИЦИЙ
                            }
                            UpdateAttachedArrows(block, previousBounds);
                        }
                    }
                    else if (element is BpmnArrow arrow)
                    {
                        if (originalArrowStates.TryGetValue(arrow, out ArrowState arrowState))
                        {
                            // ИСПРАВЛЕНИЕ: ВСЕГДА перемещаем стрелку в групповом выделении
                            arrow.StartPoint = new PointF(arrowState.StartPoint.X + deltaX, arrowState.StartPoint.Y + deltaY);
                            arrow.EndPoint = new PointF(arrowState.EndPoint.X + deltaX, arrowState.EndPoint.Y + deltaY);

                            // Пересчитываем путь стрелки
                            arrow.CalculateOrthogonalPath();

                            // ДОБАВЛЯЕМ: Автопрокрутка для стрелки
                            AdjustCanvasOffsetForPoint(arrow.StartPoint, 10f);
                            AdjustCanvasOffsetForPoint(arrow.EndPoint, 10f);
                        }
                    }
                    else if (element is BpmnCurvedArrow curvedArrow)
                    {
                        // ИСПРАВЛЕНИЕ: ВСЕГДА перемещаем кривую стрелку, НО С СОХРАНЕНИЕМ ФОРМЫ
                        if (originalArrowStates.TryGetValue(curvedArrow, out ArrowState arrowState))
                        {
                            // ПЕРЕМЕЩАЕМ ВСЕ ТОЧКИ НА ОДИНАКОВУЮ ДЕЛЬТУ
                            // Используем сохраненные исходные значения, а не текущие
                            curvedArrow.StartPoint = new PointF(arrowState.StartPoint.X + deltaX, arrowState.StartPoint.Y + deltaY);
                            curvedArrow.EndPoint = new PointF(arrowState.EndPoint.X + deltaX, arrowState.EndPoint.Y + deltaY);

                            // ИСПРАВЛЕНИЕ: ПЕРЕМЕЩАЕМ КОНТРОЛЬНЫЕ ТОЧКИ ИЗ СОХРАНЕННОГО СОСТОЯНИЯ
                            curvedArrow.ControlPoint1 = new PointF(arrowState.ControlPoint1.X + deltaX, arrowState.ControlPoint1.Y + deltaY);
                            curvedArrow.ControlPoint2 = new PointF(arrowState.ControlPoint2.X + deltaX, arrowState.ControlPoint2.Y + deltaY);

                            // ДОБАВЛЯЕМ: Автопрокрутка для кривой стрелки
                            AdjustCanvasOffsetForPoint(curvedArrow.StartPoint, 10f);
                            AdjustCanvasOffsetForPoint(curvedArrow.EndPoint, 10f);
                        }
                    }
                }

                // ДОБАВЛЯЕМ: Автопрокрутка к текущей позиции мыши
                AdjustCanvasOffsetForPoint(virtualPos, 10f);

                UpdateEditTextBoxLocation();
                this.Invalidate();
                return;
            }

            else if (isDraggingLane && draggingLane != null && draggingLanePool != null)
            {
                float deltaY = virtualPos.Y - dragStartPoint.Y;

                // Рассчитываем новую позицию с ограничениями
                float newY = draggingLane.Bounds.Y + deltaY;
                float minY = draggingLanePool.Bounds.Y + 40f; // Ниже названия
                float maxY = draggingLanePool.Bounds.Bottom - draggingLane.Bounds.Height; // Выше низа

                // Ограничиваем позицию
                newY = Math.Max(minY, Math.Min(maxY, newY));

                draggingLane.Bounds = new RectangleF(
                    draggingLane.Bounds.X,
                    newY,
                    draggingLane.Bounds.Width,
                    draggingLane.Bounds.Height
                );

                dragStartPoint = virtualPos;
                Invalidate();
                return;
            }

            // 7. ПАНОРАМИРОВАНИЕ ХОЛСТА
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

            // 8. ПРОВЕРКА КУРСОРОВ ДЛЯ ОБЛАСТЕЙ ИЗМЕНЕНИЯ РАЗМЕРА
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
            // СБРОС КУРСОРА
            else if (!isDragging && !isDraggingElements && !isResizing && !isSelecting)
            {
                if (this.Cursor != Cursors.Default)
                    this.Cursor = Cursors.Default;
            }
            // Добавляем проверку isSelecting - не меняем курсор во время группового выделения
            if (!isDragging && !isDraggingElements && !isResizing && !isSelecting && !_isResizingLane)
            {
                var poolUnderCursor = GetPoolAtPoint(virtualPos);
                if (poolUnderCursor != null && poolUnderCursor.Type == "Пул")
                {
                    var laneUnderCursor = GetLaneAtPoint(poolUnderCursor, virtualPos);
                    if (laneUnderCursor != null && IsPointOnLaneBottomBorder(laneUnderCursor, virtualPos, LANE_RESIZE_MARGIN))
                    {
                        this.Cursor = Cursors.SizeNS;
                        return;
                    }
                }

                // Проверка курсора для перемещения дорожки (только если пул выделен)
                if (primarySelectedElement is BpmnBlock selectedPool && selectedPool.Type == "Пул")
                {
                    var laneUnderCursor = GetLaneAtPoint(selectedPool, virtualPos);
                    if (laneUnderCursor != null && !IsPointOnLaneBottomBorder(laneUnderCursor, virtualPos, LANE_RESIZE_MARGIN))
                    {
                        this.Cursor = Cursors.SizeAll;
                        return;
                    }
                }

                // Сброс курсора
                if (this.Cursor != Cursors.Default)
                    this.Cursor = Cursors.Default;
            }
        }

        // ОБЪЕДИНЕННЫЙ МЕТОД MouseUp С КОМАНДНОЙ СИСТЕМОЙ
        private void InfiniteCanvas_MouseUp(object sender, MouseEventArgs e)
        {
            // ОБЪЯВЛЯЕМ virtualPos В НАЧАЛЕ МЕТОДА
            PointF virtualPos = ScreenToVirtual(e.Location);

            if (e.Button == MouseButtons.Left)
            {
                var form = this.FindForm() as Form1;

                // 1. Завершение перетаскивания конца ОБЫЧНОЙ стрелки с командной системой
                if (isDraggingArrowEnd && _draggingArrow != null)
                {
                    // Используем сохраненную стрелку
                    var selectedArrowForAttach = _draggingArrow;

                    if (form?.CommandManager != null && _originalArrowStateBeforeDrag != null)
                    {
                        // Сохраняем текущее состояние как новое
                        var newState = new ArrowState
                        {
                            StartBlock = selectedArrowForAttach.StartBlock,
                            StartPoint = selectedArrowForAttach.StartPoint,
                            StartConnectionPointIndex = selectedArrowForAttach.StartConnectionPointIndex,
                            EndBlock = selectedArrowForAttach.EndBlock,
                            EndPoint = selectedArrowForAttach.EndPoint,
                            EndConnectionPointIndex = selectedArrowForAttach.EndConnectionPointIndex
                        };

                        // Создаем команду с правильным количеством параметров
                        var command = new ModifyArrowCommand(
                            selectedArrowForAttach,
                            _originalArrowStateBeforeDrag.StartBlock,
                            _originalArrowStateBeforeDrag.StartPoint,
                            _originalArrowStateBeforeDrag.StartConnectionPointIndex,
                            _originalArrowStateBeforeDrag.EndBlock,
                            _originalArrowStateBeforeDrag.EndPoint,
                            _originalArrowStateBeforeDrag.EndConnectionPointIndex,
                            newState.StartBlock,
                            newState.StartPoint,
                            newState.StartConnectionPointIndex,
                            newState.EndBlock,
                            newState.EndPoint,
                            newState.EndConnectionPointIndex,
                            isDraggingStartPoint,
                            this
                        );
                        form.CommandManager.Execute(command);
                    }
                    else
                    {
                        // Fallback логика без командной системы
                        var (block, point, index) = FindNearestConnectionPointWithIndex(virtualPos);
                        if (block != null)
                        {
                            selectedArrowForAttach.Attach(isDraggingStartPoint, block, point, index);
                        }
                        else
                        {
                            selectedArrowForAttach.Detach(isDraggingStartPoint);
                            if (isDraggingStartPoint)
                                selectedArrowForAttach.StartPoint = virtualPos;
                            else
                                selectedArrowForAttach.EndPoint = virtualPos;
                        }
                    }

                    // СБРАСЫВАЕМ СОХРАНЕННОЕ СОСТОЯНИЕ
                    _draggingArrow = null;
                    _originalArrowStateBeforeDrag = null;

                    isDraggingArrowEnd = false;
                    ArrowModified?.Invoke(this, EventArgs.Empty);
                    this.Invalidate();
                }

                // 2. Завершение перетаскивания конца КРИВОЙ стрелки с командной системой
                if (isDraggingArrowEnd && _draggingCurvedArrow != null)
                {
                    // Используем сохраненную кривую стрелку
                    var selectedCurvedArrowForAttach = _draggingCurvedArrow;

                    if (form?.CommandManager != null && _originalCurvedArrowStateBeforeDrag != null)
                    {
                        var command = new CommandManager.MoveBlockCommand.ModifyCurvedArrowCommand(
                            selectedCurvedArrowForAttach,
                            _originalCurvedArrowStateBeforeDrag.StartBlock,
                            _originalCurvedArrowStateBeforeDrag.StartPoint,
                            _originalCurvedArrowStateBeforeDrag.StartConnectionPointIndex,
                            _originalCurvedArrowStateBeforeDrag.EndBlock,
                            _originalCurvedArrowStateBeforeDrag.EndPoint,
                            _originalCurvedArrowStateBeforeDrag.EndConnectionPointIndex,
                            _originalCurvedArrowStateBeforeDrag.ControlPoint1,
                            _originalCurvedArrowStateBeforeDrag.ControlPoint2,
                            selectedCurvedArrowForAttach.StartBlock,
                            selectedCurvedArrowForAttach.StartPoint,
                            selectedCurvedArrowForAttach.StartConnectionPointIndex,
                            selectedCurvedArrowForAttach.EndBlock,
                            selectedCurvedArrowForAttach.EndPoint,
                            selectedCurvedArrowForAttach.EndConnectionPointIndex,
                            selectedCurvedArrowForAttach.ControlPoint1,
                            selectedCurvedArrowForAttach.ControlPoint2,
                            isDraggingStartPoint,
                            this
                        );
                        form.CommandManager.Execute(command);
                    }
                    else
                    {
                        // Fallback логика без командной системы
                        var (block, point, index) = FindNearestConnectionPointWithIndex(virtualPos);
                        if (block != null)
                        {
                            selectedCurvedArrowForAttach.Attach(isDraggingStartPoint, block, point, index);
                        }
                        else
                        {
                            selectedCurvedArrowForAttach.Detach(isDraggingStartPoint);
                            if (isDraggingStartPoint)
                                selectedCurvedArrowForAttach.StartPoint = virtualPos;
                            else
                                selectedCurvedArrowForAttach.EndPoint = virtualPos;
                        }

                        // Пересчитываем контрольные точки
                        selectedCurvedArrowForAttach.CalculateControlPoints();
                    }

                    // СБРАСЫВАЕМ СОХРАНЕННОЕ СОСТОЯНИЕ
                    _draggingCurvedArrow = null;
                    _originalCurvedArrowStateBeforeDrag = null;

                    isDraggingArrowEnd = false;
                    this.Invalidate();
                }

                // 3. Командная система для перемещения выделенных элементов
                if (isDraggingElements && selectedElements.Count > 0)
                {
                    var movedElements = new List<object>();
                    var originalPositions = new Dictionary<object, object>();

                    foreach (var element in selectedElements)
                    {
                        if (element is BpmnBlock block)
                        {
                            if (originalBlockBounds.TryGetValue(block, out RectangleF originalBounds))
                            {
                                var currentBounds = block.Bounds;
                                if (currentBounds.X != originalBounds.X || currentBounds.Y != originalBounds.Y)
                                {
                                    movedElements.Add(block);
                                    originalPositions[block] = originalBounds;
                                }
                            }
                        }
                        else if (element is BpmnArrow arrow)
                        {
                            if (originalArrowStates.TryGetValue(arrow, out ArrowState arrowState))
                            {
                                if (arrow.StartPoint != arrowState.StartPoint ||
                                    arrow.EndPoint != arrowState.EndPoint)
                                {
                                    movedElements.Add(arrow);
                                    originalPositions[arrow] = arrowState;
                                }
                            }
                        }
                        else if (element is BpmnCurvedArrow curvedArrow)
                        {
                            if (originalArrowStates.TryGetValue(curvedArrow, out ArrowState arrowState))
                            {
                                if (curvedArrow.StartPoint != arrowState.StartPoint ||
                                    curvedArrow.EndPoint != arrowState.EndPoint)
                                {
                                    movedElements.Add(curvedArrow);
                                    originalPositions[curvedArrow] = arrowState;
                                }
                            }
                        }
                    }

                    if (form?.CommandManager != null && movedElements.Count > 0)
                    {
                        // Создаем макрокоманду для перемещения всех элементов
                        var commands = new List<ICommand>();

                        foreach (var element in movedElements)
                        {
                            if (element is BpmnBlock block)
                            {
                                if (originalPositions[block] is RectangleF originalBounds)
                                {
                                    var currentBounds = block.Bounds;
                                    var command = new MoveBlockCommand(
                                        block,
                                        originalBounds,
                                        currentBounds,
                                        arrows,
                                        this
                                    );
                                    commands.Add(command);
                                }
                            }
                            else if (element is BpmnArrow arrow)
                            {
                                if (originalPositions[arrow] is ArrowState arrowState)
                                {
                                    // Используем MoveArrowCommand (не вложенный)
                                    var command = new MoveArrowCommand(
                                        arrow,
                                        arrowState.StartPoint,
                                        arrowState.EndPoint,
                                        arrow.StartPoint,
                                        arrow.EndPoint,
                                        this
                                    );
                                    commands.Add(command);
                                }
                            }
                            else if (element is BpmnCurvedArrow curvedArrow)
                            {
                                if (originalPositions[curvedArrow] is ArrowState arrowState)
                                {
                                    // MoveCurvedArrowCommand вложен в MoveBlockCommand, используем полное имя
                                    var command = new MoveCurvedArrowCommand(
                                        curvedArrow,
                                        arrowState.StartPoint,
                                        arrowState.EndPoint,
                                        arrowState.ControlPoint1,
                                        arrowState.ControlPoint2,
                                        curvedArrow.StartPoint,
                                        curvedArrow.EndPoint,
                                        curvedArrow.ControlPoint1,
                                        curvedArrow.ControlPoint2,
                                        this
                                    );
                                    commands.Add(command);
                                }
                            }
                        }

                        if (commands.Count > 0)
                        {
                            var macroCommand = new MacroCommand(commands, "Перемещение группы элементов");
                            form.CommandManager.Execute(macroCommand);
                        }
                    }

                    BlockModified?.Invoke(this, EventArgs.Empty);
                }

                // 4. Командная система для изменения размера блока
                if (isResizing && primarySelectedElement is BpmnBlock resizedBlock)
                {
                    var finalBounds = resizedBlock.Bounds;
                    if (finalBounds.Width != originalBounds.Width || finalBounds.Height != originalBounds.Height)
                    {
                        if (form?.CommandManager != null)
                        {
                            // Сохраняем состояния стрелок для команды
                            var arrowStates = new Dictionary<BpmnArrow, (PointF startPoint, PointF endPoint)>();
                            foreach (var arrow in arrows)
                            {
                                if (arrow.StartBlock == resizedBlock || arrow.EndBlock == resizedBlock)
                                {
                                    arrowStates[arrow] = (arrow.StartPoint, arrow.EndPoint);
                                }
                            }

                            var command = new ResizeBlockCommand(
                                resizedBlock,
                                originalBounds,
                                finalBounds,
                                arrowStates,
                                this
                            );
                            form.CommandManager.Execute(command);
                        }
                    }
                }

                // 5. Завершение группового выделения
                if (isSelecting)
                {
                    isSelecting = false;

                    // Очищаем выделение только если мы действительно выделяли область
                    if (selectionRectangle.Width > 5 || selectionRectangle.Height > 5)
                    {
                        // Выделяем элементы, попавшие в область выделения
                        foreach (var block in blocks)
                        {
                            if (selectionRectangle.IntersectsWith(block.Bounds) && !selectedElements.Contains(block))
                                selectedElements.Add(block);
                        }

                        foreach (var arrow in arrows)
                        {
                            bool arrowInRect = selectionRectangle.IntersectsWith(arrow.GetBounds());
                            if (arrowInRect && !selectedElements.Contains(arrow))
                                selectedElements.Add(arrow);
                        }

                        foreach (var curvedArrow in curvedArrows)
                        {
                            bool curvedArrowInRect = selectionRectangle.IntersectsWith(curvedArrow.GetBounds());
                            if (curvedArrowInRect && !selectedElements.Contains(curvedArrow))
                                selectedElements.Add(curvedArrow);
                        }

                        if (selectedElements.Count > 0)
                            primarySelectedElement = selectedElements[0];
                    }

                    // Сбрасываем прямоугольник выделения
                    selectionRectangle = RectangleF.Empty;
                    Invalidate();
                }

                // 6. После завершения перемещения обновляем все стрелки
                if (isDraggingElements)
                {
                    foreach (var element in selectedElements)
                    {
                        if (element is BpmnArrow arrow)
                        {
                            arrow.CalculateOrthogonalPath();
                        }
                        else if (element is BpmnCurvedArrow curvedArrow)
                        {
                            // ИСПРАВЛЕНИЕ: НЕ ПЕРЕСЧИТЫВАЕМ КОНТРОЛЬНЫЕ ТОЧКИ ДЛЯ НЕПРИКРЕПЛЕННЫХ СТРЕЛОК
                            // Они уже перемещены методом Move с сохранением формы
                            // Пересчитываем только для прикрепленных стрелок
                            if (curvedArrow.IsFullyAttached)
                            {
                                curvedArrow.CalculateControlPoints();
                            }
                        }
                    }
                    BlockModified?.Invoke(this, EventArgs.Empty);
                }
                // 6.1 Завершение изменения размера дорожки
                if (_isResizingLane)
                {
                    _isResizingLane = false;
                    _resizingLane = null;
                    _resizingLanePool = null;
                    _isUpdatingLaneHeight = false; // Сбросить на всякий случай
                    this.Cursor = Cursors.Default;
                }

                // 7. Сбрасываем ВСЕ флаги перетаскивания, НО НЕ ВЫДЕЛЕНИЕ
                isDragging = false;
                isDraggingElements = false;
                isResizing = false;
                isDraggingArrowEnd = false;
                // Сброс перемещения дорожки
                isDraggingLane = false;
                draggingLane = null;
                draggingLanePool = null;
                selectedHandleIndex = -1;
                _draggingArrow = null;
                _originalArrowStateBeforeDrag = null;
                _draggingCurvedArrow = null;
                _originalCurvedArrowStateBeforeDrag = null;
                verticalGuides.Clear();
                horizontalGuides.Clear();
                // Сбрасываем подсветку
                _highlightedPool = null;
                _highlightedLane = null;
                // Сбрасываем подсветку ошибки перемещения пула
                _showPoolErrorHighlight = false;
                _errorPoolBounds = RectangleF.Empty;
                // Останавливаем таймер, если он запущен
                if (_errorHighlightTimer != null && _errorHighlightTimer.Enabled)
                {
                    _errorHighlightTimer.Stop();
                }

                if (_isDraggingLaneInternal)
                {
                    _isDraggingLaneInternal = false;
                    _draggingLaneInternal = null;
                    _draggingLanePoolInternal = null;
                    _draggingLaneParentInternal = null;
                    _draggingLaneChildren = null;
                    this.Cursor = Cursors.Default;
                }

                this.Cursor = Cursors.Default;
                this.Invalidate();
            }
        }

        // ОБНОВЛЕННЫЙ МЕТОД Paint С НАПРАВЛЯЮЩИМИ
        private void InfiniteCanvas_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            g.TranslateTransform(canvasOffset.X * zoom, canvasOffset.Y * zoom);
            g.ScaleTransform(zoom, zoom);

            DrawGrid(g);

            // РИСУЕМ НАПРАВЛЯЮЩИЕ ВЫРАВНИВАНИЯ
            using (Pen guidePen = new Pen(Color.Orange, 1))
            {
                guidePen.DashStyle = DashStyle.Solid;

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
                    bool isSelected = selectedElements.Contains(arrow);
                    arrow.Draw(g, isSelected);
                }
            }

            // ДОБАВЛЯЕМ отрисовку кривых стрелок
            if (curvedArrows != null)
            {
                foreach (var curvedArrow in curvedArrows)
                {
                    bool isSelected = selectedElements.Contains(curvedArrow);
                    curvedArrow.Draw(g, isSelected);
                }
            }

            // Затем блоки (поверх стрелок и пулов)
            if (blocks != null)
            {
                System.Diagnostics.Debug.WriteLine($"InfiniteCanvas_Paint: всего блоков = {blocks.Count}");

                foreach (var block in blocks)
                {
                    if (block.Type == "Пул")
                    {
                        System.Diagnostics.Debug.WriteLine($"Отрисовываю пул: {block.Bounds}");
                    }

                    bool isSelected = selectedElements.Contains(block);
                    block.Draw(g, isSelected);
                }
            }

            // РИСУЕМ ПОДСВЕТКУ ГРАНИЦ КОНТЕЙНЕРА ПРИ ПЕРЕМЕЩЕНИИ ДОРОЖКИ
            if (_isDraggingLaneInternal && _draggingLaneInternal != null && _draggingLanePoolInternal != null)
            {
                RectangleF containerBounds = GetLaneContainerBounds(_draggingLaneInternal, _draggingLanePoolInternal);

                using (var containerPen = new Pen(Color.LightGreen, 2))
                {
                    containerPen.DashStyle = DashStyle.Dash;
                    containerPen.DashPattern = new float[] { 4, 4 };

                    g.DrawRectangle(containerPen,
                        containerBounds.X,
                        containerBounds.Y,
                        containerBounds.Width,
                        containerBounds.Height);
                }
            }

            // Рисуем подсветку активного контейнера
            if (_highlightedPool != null)
            {
                DrawContainerHighlight(g, _highlightedPool, _highlightedLane);
            }

            // Рисуем подсветку ошибки перемещения пула (если нужно)
            if (_showPoolErrorHighlight && !_errorPoolBounds.IsEmpty)
            {
                using (var errorPen = new Pen(Color.Red, 2))
                {
                    errorPen.DashStyle = DashStyle.Dash;
                    errorPen.DashPattern = new float[] { 5, 5 }; // Пунктирная линия

                    g.DrawRectangle(errorPen,
                        _errorPoolBounds.X,
                        _errorPoolBounds.Y,
                        _errorPoolBounds.Width,
                        _errorPoolBounds.Height);
                }
            }

            // Рисуем прямоугольник выделения
            if (isSelecting)
            {
                using (Pen selectPen = new Pen(Color.Blue, 2))
                {
                    selectPen.DashStyle = DashStyle.Dash;
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

        // МЕТОДЫ ТРАНСФОРМАЦИИ КООРДИНАТ ИЗ СТАРОГО КОДА
        public PointF ScreenToWorld(Point screenPt)
        {
            return ScreenToVirtual(screenPt);
        }

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


        // В методе GetBlockAtPoint добавим проверку для пула:
        private BpmnBlock GetBlockAtPoint(PointF point)
        {
            if (editTextBox != null)
            {
                return selectedBlock;
            }

            foreach (var block in blocks.AsEnumerable().Reverse())
            {
                // Для пула проверяем попадание в его границы (включая полосу названия)
                if (block.Type == "Пул")
                {
                    // Пулу при клике на полосу названия или тело
                    if (block.Bounds.Contains(point))
                        return block;
                }
                else if (block.Bounds.Contains(point))
                {
                    return block;
                }
            }
            return null;
        }

        private BpmnBlock GetPoolAtPoint(PointF point)
        {
            foreach (var block in blocks.AsEnumerable().Reverse())
            {
                if (block.Type == "Пул" && block.Bounds.Contains(point))
                    return block;
            }
            return null;
        }

        private void AddLineToSelectedPool()
        {
            if (primarySelectedElement is BpmnBlock poolBlock && poolBlock.Type == "Пул")
            {
                // Если есть currentLaneUnderCursor, то добавляем вложенную дорожку
                if (currentLaneUnderCursor != null)
                {
                    AddNestedLineToLane(poolBlock, currentLaneUnderCursor);
                }
                else
                {
                    // Иначе добавляем верхнеуровневую дорожку
                    AddTopLevelLineToPool(poolBlock);
                }
            }
        }

        private void AddTopLevelLineToPool(BpmnBlock poolBlock)
        {
            using (var dialog = new AddLineDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var newLane = new PoolLine
                    {
                        Text = dialog.LineName,
                        Bounds = new RectangleF(0, 0, poolBlock.Bounds.Width - 40f, 60f),
                        FillColor = Color.Transparent,
                        BackgroundColor = Color.Transparent,
                        NameStripBackgroundColor = Color.White,
                        BorderColor = Color.Black,
                        BorderWidth = 1f,
                        IsTransparent = true,
                        NameStripWidth = 40f,
                        NestingLevel = 0,
                        ParentLine = null // Верхний уровень
                    };

                    // Используем команду вместо прямого добавления
                    var command = new AddLaneCommand(poolBlock, newLane, blocks, this);
                    var form = this.FindForm() as Form1;
                    if (form?.CommandManager != null)
                    {
                        form.CommandManager.Execute(command);
                    }
                    else
                    {
                        // Fallback: прямое выполнение
                        command.Execute();
                    }
                }
            }
        }

        private void AddNestedLineToLane(BpmnBlock poolBlock, PoolLine parentLane)
        {
            // Проверяем ограничение вложенности
            if (!CanAddNestedLane(parentLane))
                return;

            using (var dialog = new AddNestedLineDialog(parentLane.Text))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var newLane = new PoolLine
                    {
                        Text = dialog.LineName,
                        Bounds = new RectangleF(0, 0, parentLane.Bounds.Width - 20f, 50f),
                        FillColor = Color.Transparent,
                        BackgroundColor = Color.Transparent,
                        NameStripBackgroundColor = Color.White,
                        BorderColor = Color.DarkGray,
                        BorderWidth = 1f,
                        IsTransparent = true,
                        NameStripWidth = 30f,
                        NestingLevel = parentLane.NestingLevel + 1,
                        ParentLine = parentLane // Устанавливаем родителя
                    };

                    // Используем команду
                    var command = new AddLaneCommand(poolBlock, newLane, blocks, this, true, parentLane);
                    var form = this.FindForm() as Form1;
                    if (form?.CommandManager != null)
                    {
                        form.CommandManager.Execute(command);
                    }
                    else
                    {
                        command.Execute();
                    }
                }
            }
        }

        private void RecalculateLanesPositions(BpmnBlock poolBlock)
        {
            if (poolBlock.PoolLanes == null) return;

            float currentY = poolBlock.Bounds.Y; // УБИРАЕМ отступ в 40px
            float nameStripWidth = 40f;
            float bodyX = poolBlock.Bounds.X + nameStripWidth;
            float bodyWidth = poolBlock.Bounds.Width - nameStripWidth;

            foreach (var lane in poolBlock.PoolLanes)
            {
                lane.NameStripWidth = 40f;
                lane.IsTransparent = true;

                float laneBodyWidth = bodyWidth - (lane.NestingLevel * 20f);
                lane.Bounds = new RectangleF(
                    bodyX + (lane.NestingLevel * 20f),
                    currentY,
                    lane.NameStripWidth + laneBodyWidth,
                    lane.Bounds.Height
                );

                currentY += lane.Bounds.Height;

                UpdateNestedLanesPositions(lane, bodyX + 20f, bodyWidth - 20f);
            }
        }

        private void UpdateNestedLanesPositions(PoolLine parentLane, float startX, float availableWidth)
        {
            if (parentLane.ChildLines == null) return;

            float currentY = parentLane.Bounds.Y;

            foreach (var childLane in parentLane.ChildLines)
            {
                // Для вложенных дорожек уменьшаем ширину полосы названия
                childLane.NameStripWidth = 30f;

                // Рассчитываем ширину тела вложенной дорожки
                float childBodyWidth = availableWidth - (childLane.NestingLevel * 20f) - childLane.NameStripWidth;

                childLane.Bounds = new RectangleF(
                    startX + (childLane.NestingLevel * 10f), // Меньший сдвиг для вложенных
                    currentY,
                    childLane.NameStripWidth + childBodyWidth,
                    childLane.Bounds.Height
                );

                currentY += childLane.Bounds.Height;

                // Рекурсивно обновляем позиции для более глубоких уровней
                UpdateNestedLanesPositions(childLane, startX + 10f, availableWidth - 10f);
            }

            // Обновляем высоту родительской дорожки, если нужно
            float totalHeight = currentY - parentLane.Bounds.Y;
            if (totalHeight > parentLane.Bounds.Height)
            {
                parentLane.Bounds = new RectangleF(
                    parentLane.Bounds.X,
                    parentLane.Bounds.Y,
                    parentLane.Bounds.Width,
                    totalHeight
                );
            }
        }

        private void RemoveSelectedLane()
        {
            if (primarySelectedElement is BpmnBlock poolBlock && poolBlock.Type == "Пул")
            {
                // Если есть currentLaneUnderCursor, то удаляем эту дорожку
                if (currentLaneUnderCursor != null)
                {
                    RemoveLane(poolBlock, currentLaneUnderCursor);
                }
                else
                {
                    // Иначе ищем дорожку под курсором
                    PointF virtualPos = GetCursorVirtualPosition();
                    var laneToRemove = GetLaneAtPoint(poolBlock, virtualPos);
                    if (laneToRemove != null)
                    {
                        RemoveLane(poolBlock, laneToRemove);
                    }
                    else
                    {
                        MessageBox.Show("Выберите дорожку для удаления",
                                      "Удаление дорожки",
                                      MessageBoxButtons.OK,
                                      MessageBoxIcon.Information);
                    }
                }
            }
        }

        private void RemoveLane(BpmnBlock poolBlock, PoolLine laneToRemove)
        {
            // Определяем, является ли дорожка вложенной
            bool isNested = false;
            PoolLine parentLane = null;

            // Ищем родительскую дорожку
            if (!poolBlock.PoolLanes.Contains(laneToRemove))
            {
                parentLane = FindParentLane(poolBlock.PoolLanes, laneToRemove);
                isNested = parentLane != null;
            }

            // Используем команду
            var command = new RemoveLaneCommand(poolBlock, laneToRemove, blocks, this, isNested, parentLane);
            var form = this.FindForm() as Form1;
            if (form?.CommandManager != null)
            {
                form.CommandManager.Execute(command);
            }
            else
            {
                command.Execute();
            }
        }

        private bool RemoveLaneRecursive(List<PoolLine> lanes, PoolLine laneToRemove)
        {
            foreach (var lane in lanes)
            {
                if (lane.ChildLines.Contains(laneToRemove))
                {
                    lane.ChildLines.Remove(laneToRemove);
                    return true;
                }
                if (RemoveLaneRecursive(lane.ChildLines, laneToRemove))
                    return true;
            }
            return false;
        }

        private PoolLine FindParentLane(List<PoolLine> lanes, PoolLine laneToFind)
        {
            foreach (var lane in lanes)
            {
                if (lane.ChildLines.Contains(laneToFind))
                    return lane;

                var parent = FindParentLane(lane.ChildLines, laneToFind);
                if (parent != null)
                    return parent;
            }
            return null;
        }

        private void ShowNestingLimitWarning()
        {
            MessageBox.Show("Достигнут максимальный уровень вложенности линий (3 уровня).\n" +
                           "Невозможно добавить больше линий.",
                           "Ограничение вложенности",
                           MessageBoxButtons.OK,
                           MessageBoxIcon.Warning);
        }

        // 
        private RectangleF CalculateNewLineBounds(BpmnBlock poolBlock)
        {
            float lineHeight = 60f;

            // Всегда используем реальные границы пула (Bounds)
            if (poolBlock.PoolLanes.Count == 0)
            {
                return new RectangleF(
                    poolBlock.Bounds.X + 40f,    // От левого края пула
                    poolBlock.Bounds.Y + 40f,    // От верхнего края пула (под названием)
                    poolBlock.Bounds.Width - 40f, // Ширина пула минус отступ
                    lineHeight
                );
            }
            else
            {
                var lastLane = poolBlock.PoolLanes.Last();
                return new RectangleF(
                    poolBlock.Bounds.X + 40f,    // Всегда от левого края пула
                    lastLane.Bounds.Bottom,      // Под последней дорожкой
                    poolBlock.Bounds.Width - 40f, // Ширина пула минус отступ
                    lineHeight
                );
            }
        }

        private void UpdatePoolSize(BpmnBlock poolBlock)
        {
            if (poolBlock.PoolLanes.Count == 0)
            {
                // Минимальная высота пула
                poolBlock.Bounds = new RectangleF(
                    poolBlock.Bounds.X,
                    poolBlock.Bounds.Y,
                    poolBlock.Bounds.Width,
                    120f // минимальная высота
                );
            }
            else
            {
                // Высота = отступ сверху + высота всех дорожек
                float totalHeight = 40f; // отступ для названия
                foreach (var lane in poolBlock.PoolLanes)
                {
                    totalHeight += lane.Bounds.Height;
                }

                poolBlock.Bounds = new RectangleF(
                    poolBlock.Bounds.X,
                    poolBlock.Bounds.Y,
                    poolBlock.Bounds.Width,
                    totalHeight
                );
            }
        }

        private PoolLine GetLaneAtPoint(BpmnBlock poolBlock, PointF point)
        {
            if (poolBlock.PoolLanes == null) return null;

            // Проверяем вложенные дорожки рекурсивно
            foreach (var lane in poolBlock.PoolLanes.AsEnumerable().Reverse())
            {
                var nestedLane = GetNestedLaneAtPoint(lane, point);
                if (nestedLane != null)
                    return nestedLane;

                if (lane.Bounds.Contains(point))
                    return lane;
            }
            return null;
        }

        private PoolLine GetNestedLaneAtPoint(PoolLine parentLane, PointF point)
        {
            if (parentLane.ChildLines == null) return null;

            foreach (var childLane in parentLane.ChildLines.AsEnumerable().Reverse())
            {
                var deeperLane = GetNestedLaneAtPoint(childLane, point);
                if (deeperLane != null)
                    return deeperLane;

                if (childLane.Bounds.Contains(point))
                    return childLane;
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

            if (lastSelectedElement != null)
            {
                FocusOnElement(lastSelectedElement);
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
            lastSelectedElement = null;
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

            // НАСТРАИВАЕМАЯ СКОРОСТЬ ПЕРЕДВИЖЕНИЯ ПОЛЯ
            float scrollSpeed = 0.05f;
            float padding = 10f;

            // Проверяем все 4 угла блока

            // Левый верхний угол
            if (blockBounds.Left < -canvasOffset.X + padding)
            {
                canvasOffset.X = Math.Max(canvasOffset.X - scrollSpeed, -blockBounds.Left + padding);
            }

            // Правый нижний угол
            if (blockBounds.Right > -canvasOffset.X + virtualWidth - padding)
            {
                canvasOffset.X = Math.Min(canvasOffset.X + scrollSpeed, -(blockBounds.Right - virtualWidth + padding));
            }

            // Правый верхний угол
            if (blockBounds.Top < -canvasOffset.Y + padding)
            {
                canvasOffset.Y = Math.Max(canvasOffset.Y - scrollSpeed, -blockBounds.Top + padding);
            }

            // Левый нижний угол
            if (blockBounds.Bottom > -canvasOffset.Y + virtualHeight - padding)
            {
                canvasOffset.Y = Math.Min(canvasOffset.Y + scrollSpeed, -(blockBounds.Bottom - virtualHeight + padding));
            }
        }

        // ДОБАВЛЯЕМ: Метод для автопрокрутки при перетаскивании стрелок и элементов
        private void AdjustCanvasOffsetForPoint(PointF point, float padding = 10f)
        {
            float virtualWidth = this.Width / zoom;
            float virtualHeight = this.Height / zoom;

            float visibleLeft = -canvasOffset.X;
            float visibleRight = visibleLeft + virtualWidth;
            float visibleTop = -canvasOffset.Y;
            float visibleBottom = visibleTop + virtualHeight;

            float scrollSpeed = 0.05f;

            if (point.X < visibleLeft + padding)
                canvasOffset.X = Math.Max(canvasOffset.X - scrollSpeed, -point.X + padding);
            else if (point.X > visibleRight - padding)
                canvasOffset.X = Math.Min(canvasOffset.X + scrollSpeed, -(point.X - virtualWidth + padding));

            if (point.Y < visibleTop + padding)
                canvasOffset.Y = Math.Max(canvasOffset.Y - scrollSpeed, -point.Y + padding);
            else if (point.Y > visibleBottom - padding)
                canvasOffset.Y = Math.Min(canvasOffset.Y + scrollSpeed, -(point.Y - virtualHeight + padding));
        }

        public bool IsEditingText()
        {
            return editTextBox != null && editTextBox.Focused;
        }

        private bool IsShiftPressed()
        {
            return (Control.ModifierKeys & Keys.Shift) == Keys.Shift;
        }

        public void SelectBlock(BpmnBlock block)
        {
            selectedBlock = block;
            selectedElements.Clear();
            selectedElements.Add(block);
            primarySelectedElement = block;
            selectedArrow = null;
            Invalidate();
        }

        public PointF GetCursorVirtualPosition()
        {
            Point cursorPos = PointToClient(Cursor.Position);
            return ScreenToVirtual(cursorPos);
        }

        public void DeleteSelectedElement(CommandManager commandManager)
        {
            DeleteSelectedElements();
        }

        // Новый метод для очистки сохраненных состояний
        public void ClearDragStates()
        {
            originalBlockBounds.Clear();
            originalArrowStates.Clear();
        }

        // НОВЫЙ МЕТОД ДЛЯ ВЫЗОВА СОБЫТИЯ ДОБАВЛЕНИЯ ЭЛЕМЕНТА
        public void RaiseElementAdded()
        {
            ElementAdded?.Invoke(this, EventArgs.Empty);
        }

        // МОДИФИЦИРУЕМ МЕТОДЫ ДОБАВЛЕНИЯ ЭЛЕМЕНТОВ
        public void AddBlock(BpmnBlock block)
        {
            blocks.Add(block);
            SetBlocks(blocks);
            BlockModified?.Invoke(this, EventArgs.Empty);
            ElementAdded?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }

        public void AddArrow(BpmnArrow arrow)
        {
            arrows.Add(arrow);
            SetArrows(arrows);

            // ПОДПИСЫВАЕМСЯ НА СОБЫТИЕ ИЗМЕНЕНИЯ СТРЕЛКИ
            arrow.ArrowModified += (s, e1) => ArrowModified?.Invoke(this, EventArgs.Empty);

            ArrowModified?.Invoke(this, EventArgs.Empty);
            ElementAdded?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }

        public void AddCurvedArrow(BpmnCurvedArrow curvedArrow)
        {
            curvedArrows.Add(curvedArrow);
            SetCurvedArrows(curvedArrows);
            ElementAdded?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }

        public List<PoolComposite> GetPoolComposites() => poolComposites;
        public void SetPoolComposites(List<PoolComposite> composites)
        {
            poolComposites = composites ?? new List<PoolComposite>();
            Invalidate();
        }

        private PoolComposite GetPoolCompositeAtPoint(PointF point)
        {
            foreach (var pool in poolComposites.AsEnumerable().Reverse())
            {
                if (pool.Bounds.Contains(point))
                    return pool;
            }
            return null;
        }

        private bool CanAddNestedLane(PoolLine lane)
        {
            if (lane.NestingLevel >= 2) // Максимум 3 уровня: пул(0) → дорожка(1) → вложенная(2)
            {
                ShowNestingLimitWarning();
                return false;
            }

            // Проверяем рекурсивно дочерние линии
            return lane.CanAddNestedLine();
        }
        private (BpmnBlock pool, PoolLine lane) GetContainerAtPoint(PointF point)
        {
            // Ищем пул, содержащий точку
            foreach (var block in blocks)
            {
                if (block.Type == "Пул" && block.Bounds.Contains(point))
                {
                    // Ищем дорожку внутри пула
                    var lane = GetLaneAtPoint(block, point);
                    return (block, lane);
                }
            }
            return (null, null);
        }

        private (BpmnBlock pool, PoolLine lane) GetContainerForElement(RectangleF elementBounds)
        {
            // Проверяем все пулы на пересечение/содержание
            foreach (var block in blocks)
            {
                if (block.Type == "Пул")
                {
                    // Если элемент полностью внутри пула
                    if (block.Bounds.Contains(elementBounds))
                    {
                        // Ищем конкретную дорожку
                        foreach (var lane in block.PoolLanes)
                        {
                            if (lane.Bounds.Contains(elementBounds))
                            {
                                // Проверяем вложенные дорожки
                                var nestedLane = GetNestedLaneContainingElement(lane, elementBounds);
                                if (nestedLane != null)
                                {
                                    return (block, nestedLane);
                                }
                                return (block, lane);
                            }
                        }
                        // Элемент в пуле, но не в дорожке
                        return (block, null);
                    }
                }
            }
            return (null, null);
        }

        private PoolLine GetNestedLaneContainingElement(PoolLine parentLane, RectangleF elementBounds)
        {
            if (parentLane.ChildLines == null) return null;

            foreach (var childLane in parentLane.ChildLines)
            {
                if (childLane.Bounds.Contains(elementBounds))
                {
                    // Рекурсивно проверяем более глубокие уровни
                    var deeper = GetNestedLaneContainingElement(childLane, elementBounds);
                    return deeper ?? childLane;
                }
            }
            return null;
        }

        // Добавим вспомогательный метод для подсветки:
        private void DrawContainerHighlight(Graphics g, BpmnBlock pool, PoolLine lane)
        {
            using (var highlightPen = new Pen(_highlightColor, 3))
            {
                highlightPen.DashStyle = DashStyle.Dash;

                if (lane != null)
                {
                    // Подсветка конкретной дорожки
                    g.DrawRectangle(highlightPen, lane.Bounds.X, lane.Bounds.Y,
                                  lane.Bounds.Width, lane.Bounds.Height);
                }
                else
                {
                    // Подсветка всего пула (только тело, без полосы названия)
                    float bodyX = pool.Bounds.X + 40f;
                    float bodyWidth = pool.Bounds.Width - 40f;
                    RectangleF bodyRect = new RectangleF(bodyX, pool.Bounds.Y,
                                                        bodyWidth, pool.Bounds.Height);
                    g.DrawRectangle(highlightPen, bodyRect.X, bodyRect.Y,
                                  bodyRect.Width, bodyRect.Height);
                }
            }
        }

        // Добавим метод для проверки, перемещается ли пул:
        private bool IsDraggingPool()
        {
            foreach (var element in selectedElements)
            {
                if (element is BpmnBlock block && block.Type == "Пул")
                {
                    return true;
                }
            }
            return false;
        }
        private float GetMaxLaneHeight(PoolLine lane, BpmnBlock pool)
        {
            // Максимальная высота - это высота контейнера минус позиция дорожки
            if (IsLaneNested(lane, pool, out PoolLine parentLane))
            {
                // Для вложенной дорожки ограничение - высота родительской дорожки
                return parentLane.Bounds.Height - (lane.Bounds.Y - parentLane.Bounds.Y);
            }
            else
            {
                // Для дорожки верхнего уровня ограничение - высота тела пула
                float poolBodyHeight = pool.Bounds.Height;
                float laneYInPool = lane.Bounds.Y - pool.Bounds.Y;
                return poolBodyHeight - laneYInPool;
            }
        }

        private bool IsLaneNested(PoolLine lane, BpmnBlock pool, out PoolLine parentLane)
        {
            parentLane = null;

            // Проверяем все дорожки на наличие этой дорожки в ChildLines
            foreach (var topLane in pool.PoolLanes)
            {
                if (IsLaneInHierarchy(topLane, lane))
                {
                    parentLane = FindParentLane(topLane, lane);
                    return parentLane != null;
                }
            }

            return false;
        }

        private bool IsLaneInHierarchy(PoolLine rootLane, PoolLine targetLane)
        {
            if (rootLane == targetLane) return true;

            if (rootLane.ChildLines != null)
            {
                foreach (var child in rootLane.ChildLines)
                {
                    if (IsLaneInHierarchy(child, targetLane))
                        return true;
                }
            }

            return false;
        }

        private PoolLine FindParentLane(PoolLine rootLane, PoolLine targetLane)
        {
            if (rootLane.ChildLines != null)
            {
                if (rootLane.ChildLines.Contains(targetLane))
                    return rootLane;

                foreach (var child in rootLane.ChildLines)
                {
                    var parent = FindParentLane(child, targetLane);
                    if (parent != null)
                        return parent;
                }
            }

            return null;
        }

        private void UpdatePoolHeight(BpmnBlock pool)
        {
            if (pool.PoolLanes == null || pool.PoolLanes.Count == 0)
            {
                pool.Bounds = new RectangleF(
                    pool.Bounds.X,
                    pool.Bounds.Y,
                    pool.Bounds.Width,
                    120f
                );
                return;
            }

            float maxBottom = pool.Bounds.Y + 40f;
            foreach (var lane in pool.PoolLanes)
            {
                float laneBottom = GetLaneBottomRecursive(lane);
                if (laneBottom > maxBottom)
                    maxBottom = laneBottom;
            }

            float requiredHeight = maxBottom - pool.Bounds.Y;
            float newHeight = Math.Max(120f, requiredHeight);

            if (_isUpdatingLaneHeight)
            {
                // Только увеличиваем, если нужно
                if (newHeight > pool.Bounds.Height)
                {
                    pool.Bounds = new RectangleF(
                        pool.Bounds.X,
                        pool.Bounds.Y,
                        pool.Bounds.Width,
                        newHeight
                    );
                }
            }
            else
            {
                pool.Bounds = new RectangleF(
                    pool.Bounds.X,
                    pool.Bounds.Y,
                    pool.Bounds.Width,
                    newHeight
                );
            }
        }

        private float GetLaneBottomRecursive(PoolLine lane)
        {
            float bottom = lane.Bounds.Bottom;

            if (lane.ChildLines != null)
            {
                foreach (var child in lane.ChildLines)
                {
                    float childBottom = GetLaneBottomRecursive(child);
                    if (childBottom > bottom)
                        bottom = childBottom;
                }
            }

            return bottom;
        }

        // Получение границ контейнера для дорожки
        private RectangleF GetLaneContainerBounds(PoolLine lane, BpmnBlock pool)
        {
            if (lane.ParentLine != null)
            {
                RectangleF parentBounds = lane.ParentLine.Bounds;
                float leftBoundary = parentBounds.X + lane.ParentLine.NameStripWidth;
                float topBoundary = parentBounds.Y; // Без отступа
                float rightBoundary = parentBounds.Right;
                float bottomBoundary = parentBounds.Bottom;

                return new RectangleF(
                    leftBoundary,
                    topBoundary,
                    rightBoundary - leftBoundary,
                    bottomBoundary - topBoundary
                );
            }
            else
            {
                float leftBoundary = pool.Bounds.X + 40f;
                float topBoundary = pool.Bounds.Y; // Без отступа в 40
                float rightBoundary = pool.Bounds.Right;
                float bottomBoundary = pool.Bounds.Bottom;

                return new RectangleF(
                    leftBoundary,
                    topBoundary,
                    rightBoundary - leftBoundary,
                    bottomBoundary - topBoundary
                );
            }
        }

        /// <summary>
        /// Перемещает текущую дорожку выше по порядку в списке дорожек пула.
        /// Применяется только к дорожкам верхнего уровня.
        /// </summary>
        private void MoveLaneUp()
        {
            if (currentLaneUnderCursor != null && primarySelectedElement is BpmnBlock poolBlock)
            {
                // Находим индекс дорожки в списке дорожек пула
                int index = poolBlock.PoolLanes.IndexOf(currentLaneUnderCursor);

                // Проверяем, что дорожка не первая и ее можно переместить выше
                if (index > 0)
                {
                    // Меняем местами с предыдущей дорожкой
                    var temp = poolBlock.PoolLanes[index - 1];
                    poolBlock.PoolLanes[index - 1] = currentLaneUnderCursor;
                    poolBlock.PoolLanes[index] = temp;

                    // Пересчитываем позиции всех дорожек пула
                    RecalculateLanesPositions(poolBlock);
                    UpdatePoolHeight(poolBlock);
                    Invalidate(); // Перерисовываем канвас
                }
            }
        }

        /// <summary>
        /// Перемещает текущую дорожку ниже по порядку в списке дорожек пула.
        /// Применяется только к дорожкам верхнего уровня.
        /// </summary>
        private void MoveLaneDown()
        {
            if (currentLaneUnderCursor != null && primarySelectedElement is BpmnBlock poolBlock)
            {
                // Находим индекс дорожки в списке дорожек пула
                int index = poolBlock.PoolLanes.IndexOf(currentLaneUnderCursor);

                // Проверяем, что дорожка не последняя и ее можно переместить ниже
                if (index < poolBlock.PoolLanes.Count - 1)
                {
                    // Меняем местами со следующей дорожкой
                    var temp = poolBlock.PoolLanes[index + 1];
                    poolBlock.PoolLanes[index + 1] = currentLaneUnderCursor;
                    poolBlock.PoolLanes[index] = temp;

                    // Пересчитываем позиции всех дорожек пула
                    RecalculateLanesPositions(poolBlock);
                    UpdatePoolHeight(poolBlock);
                    Invalidate(); // Перерисовываем канвас
                }
            }
        }

        /// <summary>
        /// Вкладывает текущую дорожку в другую дорожку.
        /// Создает иерархическую структуру дорожек.
        /// </summary>
        private void NestLane()
        {
            // Реализация вложенности дорожки в другую дорожку
            if (currentLaneUnderCursor != null && primarySelectedElement is BpmnBlock poolBlock)
            {
                // Находим целевую родительскую дорожку (дорогу, под курсором в момент вызова меню)
                PointF virtualPos = GetCursorVirtualPosition();
                var targetLane = GetLaneAtPoint(poolBlock, virtualPos);

                // Проверяем условия для вложения:
                // 1. Целевая дорожка должна существовать и быть отличной от текущей
                // 2. Текущая дорожка не должна быть предком целевой (чтобы избежать циклических ссылок)
                // 3. Целевая дорожка должна быть не дальше 2-го уровня вложенности (ограничение системы)
                if (targetLane != null && targetLane != currentLaneUnderCursor &&
                    !currentLaneUnderCursor.IsAncestorOf(targetLane))
                {
                    // Проверяем ограничение вложенности (максимум 3 уровня)
                    if (targetLane.NestingLevel < 2)
                    {
                        // Устанавливаем родителя для текущей дорожки
                        currentLaneUnderCursor.SetParent(targetLane);

                        // Пересчитываем позиции всех дорожек пула
                        RecalculateLanesPositions(poolBlock);
                        UpdatePoolHeight(poolBlock);
                        Invalidate(); // Перерисовываем канвас
                    }
                    else
                    {
                        // Показываем сообщение об ошибке, если достигнут максимальный уровень вложенности
                        MessageBox.Show("Достигнут максимальный уровень вложенности (3 уровня)",
                            "Ограничение вложенности",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }
                }
            }
        }

        /// <summary>
        /// Выводит текущую дорожку из вложенности, делая ее дорожкой верхнего уровня.
        /// Разрывает связь с родительской дорожкой.
        /// </summary>
        private void UnnestLane()
        {
            // Проверяем, что есть дорожка под курсором и у нее есть родитель
            if (currentLaneUnderCursor != null && currentLaneUnderCursor.ParentLine != null)
            {
                // Удаляем связь с родительской дорожкой
                currentLaneUnderCursor.SetParent(null);

                // Если пул выбран как основной элемент, обновляем его
                if (primarySelectedElement is BpmnBlock poolBlock)
                {
                    // Пересчитываем позиции всех дорожек пула
                    RecalculateLanesPositions(poolBlock);
                    UpdatePoolHeight(poolBlock);
                    Invalidate(); // Перерисовываем канвас
                }
            }
        }

        // Добавим вспомогательный метод для проверки, находится ли пул в выделении:
        private bool IsPoolInSelection(BpmnBlock pool)
        {
            return selectedElements.Contains(pool);
        }
    }
}