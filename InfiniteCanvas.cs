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

        // ДОБАВЛЯЕМ ДЛЯ КРИВЫХ СТРЕЛОК
        public PointF ControlPoint1 { get; set; }
        public PointF ControlPoint2 { get; set; }
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

        private BpmnBlock selectedBlock = null;
        private BpmnArrow selectedArrow = null;
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
            var removeLaneMenuItem = new ToolStripMenuItem("Удалить дорожку");
            var deleteElementMenuItem = new ToolStripMenuItem("Удалить");
            addLineMenuItem.Click += (s, e) => AddLineToSelectedPool();
            removeLaneMenuItem.Click += (s, e) => RemoveSelectedLane();
            deleteElementMenuItem.ForeColor = Color.Red;
            deleteElementMenuItem.Click += (s, e) => DeleteSelectedElements();

            contextMenuForPool.Items.AddRange(new[] { addLineMenuItem, removeLaneMenuItem, deleteElementMenuItem });
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

                // Сначала проверяем обычную стрелку
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

                // Затем проверяем кривую стрелку
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

                // Затем проверяем блок
                var clickedBlock = GetBlockAtPoint(virtualPos);
                if (clickedBlock != null)
                {
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
                var clickedPool = GetPoolCompositeAtPoint(virtualPos);
                if (clickedPool != null)
                {
                    if (!selectedElements.Contains(clickedPool))
                    {
                        ClearSelection();
                        selectedElements.Add(clickedPool);
                        primarySelectedElement = clickedPool;
                    }

                    if (e.Button == MouseButtons.Right)
                    {
                        contextMenuForPool.Show(this, e.Location);
                    }
                    else
                    {
                        StartElementsDrag(virtualPos);
                    }
                    return;
                }
                // Находим элемент под курсором
                var clickedArrow = GetArrowAtPoint(virtualPos);
                var clickedBlock = GetBlockAtPoint(virtualPos);
                var clickedCurvedArrow = GetCurvedArrowAtPoint(virtualPos);

                // 1. Проверяем клик на маркеры концов обычной стрелки
                if (clickedArrow != null)
                {
                    if (clickedArrow.HitTestEndpoint(virtualPos, true) || clickedArrow.HitTestEndpoint(virtualPos, false))
                    {
                        isDraggingArrowEnd = true;
                        isDraggingStartPoint = clickedArrow.HitTestEndpoint(virtualPos, true);
                        arrowDragStart = virtualPos;

                        ClearSelection();
                        selectedElements.Add(clickedArrow);
                        primarySelectedElement = clickedArrow;

                        this.Cursor = Cursors.Cross;
                        Invalidate();
                        return;
                    }
                }

                // 1.1 ДОБАВЛЯЕМ проверку на маркеры концов кривой стрелки
                if (clickedCurvedArrow != null)
                {
                    // УВЕЛИЧИВАЕМ tolerance с 6f до 10f для лучшего попадания
                    if (clickedCurvedArrow.HitTestEndpoint(virtualPos, true, 10f) ||
                        clickedCurvedArrow.HitTestEndpoint(virtualPos, false, 10f))
                    {
                        isDraggingArrowEnd = true;
                        isDraggingStartPoint = clickedCurvedArrow.HitTestEndpoint(virtualPos, true, 10f);
                        arrowDragStart = virtualPos;

                        ClearSelection();
                        selectedElements.Add(clickedCurvedArrow);
                        primarySelectedElement = clickedCurvedArrow;

                        this.Cursor = Cursors.Cross;
                        Invalidate();
                        return;
                    }
                }

                // 2. Проверяем клик на ручки изменения размера блока
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

                // 3. ВЫДЕЛЕНИЕ И ПЕРЕМЕЩЕНИЕ СТРЕЛКИ
                if (clickedArrow != null)
                {
                    // Если стрелка уже выделена в группе - используем групповое перемещение
                    if (selectedElements.Contains(clickedArrow))
                    {
                        StartElementsDrag(virtualPos);
                    }
                    else
                    {
                        // Если стрелка не выделена - выделяем только ее
                        ClearSelection();
                        selectedElements.Add(clickedArrow);
                        primarySelectedElement = clickedArrow;

                        // ИСПРАВЛЕНИЕ: ДЛЯ ПРИКРЕПЛЕННЫХ СТРЕЛОК НЕ НАЧИНАЕМ ПЕРЕМЕЩЕНИЕ
                        if (!clickedArrow.IsFullyAttached)
                        {
                            StartElementsDrag(virtualPos);
                        }
                    }
                    return;
                }

                // 3.1 ВЫДЕЛЕНИЕ И ПЕРЕМЕЩЕНИЕ КРИВОЙ СТРЕЛКИ
                if (clickedCurvedArrow != null)
                {
                    // Если кривая стрелка уже выделена в группе - используем групповое перемещение
                    if (selectedElements.Contains(clickedCurvedArrow))
                    {
                        StartElementsDrag(virtualPos);
                    }
                    else
                    {
                        // Если кривая стрелка не выделена - выделяем только ее
                        ClearSelection();
                        selectedElements.Add(clickedCurvedArrow);
                        primarySelectedElement = clickedCurvedArrow;

                        // ИСПРАВЛЕНИЕ: ДЛЯ ПРИКРЕПЛЕННЫХ СТРЕЛОК НЕ НАЧИНАЕМ ПЕРЕМЕЩЕНИЕ
                        if (!clickedCurvedArrow.IsFullyAttached)
                        {
                            StartElementsDrag(virtualPos);
                        }
                    }
                    return;
                }

                // 4. Выделение или перемещение блока
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

                // 5. Клик в пустое место — панорамирование или выделение
                if (IsCtrlPressed())
                {
                    isDragging = true;
                    lastMousePos = e.Location;
                    this.Cursor = Cursors.SizeAll;
                }
                else
                {
                    isSelecting = true;
                    selectionDragStartPoint = virtualPos;
                    selectionRectangle = new RectangleF(virtualPos.X, virtualPos.Y, 0, 0);
                    ClearSelection();
                    Invalidate();
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                // Контекстное меню для элементов или холста
                var clickedArrow = GetArrowAtPoint(virtualPos);
                var clickedBlock = GetBlockAtPoint(virtualPos);
                var clickedCurvedArrow = GetCurvedArrowAtPoint(virtualPos); // ДОБАВЛЯЕМ
                var clickedPool = GetPoolAtPoint(virtualPos);
                if (clickedPool != null)
                {
                    if (!selectedElements.Contains(clickedPool))
                    {
                        ClearSelection();
                        selectedElements.Add(clickedPool);
                        primarySelectedElement = clickedPool;
                    }

                    contextMenuForPool.Show(this, e.Location);
                    return;
                }
                if (clickedArrow != null || clickedBlock != null || clickedCurvedArrow != null) // ОБНОВЛЯЕМ условие
                {
                    if (clickedArrow != null && !selectedElements.Contains(clickedArrow))
                    {
                        ClearSelection();
                        selectedElements.Add(clickedArrow);
                        primarySelectedElement = clickedArrow;
                    }
                    else if (clickedBlock != null && !selectedElements.Contains(clickedBlock))
                    {
                        ClearSelection();
                        selectedElements.Add(clickedBlock);
                        primarySelectedElement = clickedBlock;
                    }
                    else if (clickedCurvedArrow != null && !selectedElements.Contains(clickedCurvedArrow))
                    {
                        ClearSelection();
                        selectedElements.Add(clickedCurvedArrow);
                        primarySelectedElement = clickedCurvedArrow;
                    }

                    contextMenuForElements.Show(this, e.Location);
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

        // ОБЪЕДИНЕННЫЙ МЕТОД MouseMove
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

                this.Invalidate();
                return;
            }

            // 1.1 ДОБАВЛЯЕМ ПЕРЕТАСКИВАНИЕ КОНЦА КРИВОЙ СТРЕЛКИ
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

                this.Invalidate();
                return;
            }

            // 2. ПЕРЕМЕЩЕНИЕ ВСЕЙ СТРЕЛКИ - УПРОЩАЕМ ЛОГИКУ:
            if (isDraggingArrow && primarySelectedElement is BpmnArrow floatingArrow)
            {
                // УБИРАЕМ сложные проверки - просто перемещаем
                float deltaX = virtualPos.X - arrowDragStart.X;
                float deltaY = virtualPos.Y - arrowDragStart.Y;

                // Используем метод Move стрелки
                floatingArrow.Move(deltaX, deltaY);

                arrowDragStart = virtualPos;
                this.Invalidate();
                return;
            }

            // 2.1 ДОБАВЛЯЕМ ПЕРЕМЕЩЕНИЕ ВСЕЙ КРИВОЙ СТРЕЛКИ
            if (isDraggingArrow && primarySelectedElement is BpmnCurvedArrow floatingCurvedArrow)
            {
                // ИСПРАВЛЕНИЕ: ПЕРЕМЕЩАЕМ ТОЛЬКО НЕПРИКРЕПЛЕННЫЕ СТРЕЛКИ
                if (!floatingCurvedArrow.IsFullyAttached)
                {
                    float deltaX = virtualPos.X - arrowDragStart.X;
                    float deltaY = virtualPos.Y - arrowDragStart.Y;

                    // ПЕРЕМЕЩАЕМ ВСЕГДА, без сложных проверок
                    floatingCurvedArrow.Move(deltaX, deltaY);
                    arrowDragStart = virtualPos;
                }
                this.Invalidate();
                return;
            }
            //2.2 ПЕРЕМЕЩЕНИЕ ПУЛА
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

            // 3 ИЗМЕНЕНИЕ РАЗМЕРА БЛОКА
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
                    resizingBlock.Bounds = newBounds;

                    if (resizingBlock.Type == "Пул")
                    {
                        resizingBlock.ValidateLanePositions();
                    }

                    // ДОБАВЛЯЕМ ОБНОВЛЕНИЕ СТРЕЛОК после изменения размера
                    UpdateArrowsAfterResize(resizingBlock, previousBounds);
                    UpdateAttachedArrows(resizingBlock, previousBounds);
                    UpdateEditTextBoxLocation();
                    this.Invalidate();
                }
                return;
            }

            // 3. ПЕРЕМЕЩЕНИЕ ВЫДЕЛЕННЫХ ЭЛЕМЕНТОВ - ОСНОВНОЙ РЕЖИМ
            if (isDraggingElements && selectedElements.Count > 0)
            {
                float deltaX = virtualPos.X - dragStartPoint.X;
                float deltaY = virtualPos.Y - dragStartPoint.Y;

                // Обновляем направляющие для первого блока
                var firstBlock = selectedElements.OfType<BpmnBlock>().FirstOrDefault();
                if (firstBlock != null)
                {
                    UpdateAlignmentGuides(firstBlock);
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
                        }
                    }
                }

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

                // ОЧИЩАЕМ выделение перед новым выделением
                selectedElements.Clear();

                foreach (var block in blocks)
                {
                    if (selectionRectangle.IntersectsWith(block.Bounds))
                        selectedElements.Add(block);
                }
                foreach (var arrow in arrows)
                {
                    if (selectionRectangle.IntersectsWith(arrow.GetBounds()))
                        selectedElements.Add(arrow);
                }
                // ДОБАВЛЯЕМ кривые стрелки в групповое выделение
                foreach (var curvedArrow in curvedArrows)
                {
                    if (selectionRectangle.IntersectsWith(curvedArrow.GetBounds()))
                        selectedElements.Add(curvedArrow);
                }

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

        // ОБЪЕДИНЕННЫЙ МЕТОД MouseUp С КОМАНДНОЙ СИСТЕМОЙ
        private void InfiniteCanvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                var form = this.FindForm() as Form1;

                // 1. Завершение перетаскивания конца ОБЫЧНОЙ стрелки с командной системой
                if (isDraggingArrowEnd && primarySelectedElement is BpmnArrow selectedArrowForAttach)
                {
                    PointF virtualPos = ScreenToVirtual(e.Location);

                    if (form?.CommandManager != null)
                    {
                        // Сохраняем оригинальные состояния
                        var originalStartBlock = selectedArrowForAttach.StartBlock;
                        var originalStartPoint = selectedArrowForAttach.StartPoint;
                        var originalStartIndex = selectedArrowForAttach.StartConnectionPointIndex;
                        var originalEndBlock = selectedArrowForAttach.EndBlock;
                        var originalEndPoint = selectedArrowForAttach.EndPoint;
                        var originalEndIndex = selectedArrowForAttach.EndConnectionPointIndex;

                        // Применяем изменения
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

                        // Создаем команду с сохранением индексов
                        var command = new ModifyArrowCommand(
                            selectedArrowForAttach,
                            originalStartBlock, originalStartPoint,
                            originalEndBlock, originalEndPoint,
                            selectedArrowForAttach.StartBlock, selectedArrowForAttach.StartPoint,
                            selectedArrowForAttach.EndBlock, selectedArrowForAttach.EndPoint,
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

                    isDraggingArrowEnd = false;
                    ArrowModified?.Invoke(this, EventArgs.Empty);
                    this.Invalidate();
                }

                // 2. Завершение перетаскивания конца КРИВОЙ стрелки с командной системой
                if (isDraggingArrowEnd && primarySelectedElement is BpmnCurvedArrow selectedCurvedArrowForAttach)
                {
                    PointF virtualPos = ScreenToVirtual(e.Location);

                    if (form?.CommandManager != null)
                    {
                        var originalStartBlock = selectedCurvedArrowForAttach.StartBlock;
                        var originalStartPoint = selectedCurvedArrowForAttach.StartPoint;
                        var originalStartIndex = selectedCurvedArrowForAttach.StartConnectionPointIndex;
                        var originalEndBlock = selectedCurvedArrowForAttach.EndBlock;
                        var originalEndPoint = selectedCurvedArrowForAttach.EndPoint;
                        var originalEndIndex = selectedCurvedArrowForAttach.EndConnectionPointIndex;
                        var originalControlPoint1 = selectedCurvedArrowForAttach.ControlPoint1;
                        var originalControlPoint2 = selectedCurvedArrowForAttach.ControlPoint2;

                        // Применяем изменения
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

                        // Создаем команду
                        var command = new ModifyCurvedArrowCommand(
                            selectedCurvedArrowForAttach,
                            originalStartBlock, originalStartPoint, originalStartIndex,
                            originalEndBlock, originalEndPoint, originalEndIndex,
                            originalControlPoint1, originalControlPoint2,
                            selectedCurvedArrowForAttach.StartBlock, selectedCurvedArrowForAttach.StartPoint, selectedCurvedArrowForAttach.StartConnectionPointIndex,
                            selectedCurvedArrowForAttach.EndBlock, selectedCurvedArrowForAttach.EndPoint, selectedCurvedArrowForAttach.EndConnectionPointIndex,
                            selectedCurvedArrowForAttach.ControlPoint1, selectedCurvedArrowForAttach.ControlPoint2,
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

                    isDraggingArrowEnd = false;
                    this.Invalidate();
                }

                // 3. Командная система для перемещения блоков
                if (_isBlockDragInProgress && primarySelectedElement is BpmnBlock movedBlock)
                {
                    var finalBounds = movedBlock.Bounds;
                    if (finalBounds.X != _dragStartBounds.X || finalBounds.Y != _dragStartBounds.Y)
                    {
                        if (form?.CommandManager != null)
                        {
                            var command = new MoveBlockCommand(
                                movedBlock,
                                _dragStartBounds,
                                finalBounds,
                                arrows,
                                this
                            );
                            form.CommandManager.Execute(command);
                        }
                        BlockModified?.Invoke(this, EventArgs.Empty);
                    }
                    _isBlockDragInProgress = false;
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

                // 5. ИСПРАВЛЕННАЯ ЛОГИКА ВЫДЕЛЕНИЯ ГРУППЫ
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

                        // ДОБАВЛЯЕМ кривые стрелки в финальное выделение
                        foreach (var curvedArrow in curvedArrows)
                        {
                            bool curvedArrowInRect = selectionRectangle.IntersectsWith(curvedArrow.GetBounds());
                            if (curvedArrowInRect && !selectedElements.Contains(curvedArrow))
                                selectedElements.Add(curvedArrow);
                        }

                        if (selectedElements.Count > 0)
                            primarySelectedElement = selectedElements[0];
                    }

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
                verticalGuides.Clear();
                horizontalGuides.Clear();

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
                        FillColor = Color.LightGray,
                        BorderColor = Color.Black,
                        NestingLevel = 0
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
                        FillColor = Color.LightBlue,
                        BorderColor = Color.DarkBlue,
                        NestingLevel = parentLane.NestingLevel + 1
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

            float currentY = poolBlock.Bounds.Y + 40f; // Отступ для названия
            float bodyX = poolBlock.Bounds.X + 40f;
            float bodyWidth = poolBlock.Bounds.Width - 40f;

            foreach (var lane in poolBlock.PoolLanes)
            {
                lane.Bounds = new RectangleF(bodyX, currentY, bodyWidth, lane.Bounds.Height);
                currentY += lane.Bounds.Height;

                // Обновляем позиции вложенных дорожек
                UpdateNestedLanesPositions(lane, bodyX + 20f, bodyWidth - 20f);
            }

            // Обновляем высоту пула
            float totalHeight = currentY - poolBlock.Bounds.Y;
            poolBlock.Bounds = new RectangleF(
                poolBlock.Bounds.X,
                poolBlock.Bounds.Y,
                poolBlock.Bounds.Width,
                Math.Max(120f, totalHeight)
            );

            // Проверяем, чтобы дорожки не выходили за границы
            poolBlock.ValidateLanePositions();
        }

        private void UpdateNestedLanesPositions(PoolLine parentLane, float x, float width)
        {
            if (parentLane.ChildLines == null) return;

            float currentY = parentLane.Bounds.Y;
            foreach (var childLane in parentLane.ChildLines)
            {
                childLane.Bounds = new RectangleF(x, currentY, width, childLane.Bounds.Height);
                currentY += childLane.Bounds.Height;

                // Рекурсивно обновляем позиции для более глубоких уровней
                UpdateNestedLanesPositions(childLane, x + 20f, width - 20f);
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

            // МЕДЛЕННОЕ СМЕЩЕНИЕ ПОЛЯ В НУЖНОМ НАПРАВЛЕНИИ
            if (blockBounds.Left < -canvasOffset.X)
            {
                canvasOffset.X = Math.Max(canvasOffset.X - scrollSpeed, -blockBounds.Left);
            }
            else if (blockBounds.Right > -canvasOffset.X + virtualWidth)
            {
                canvasOffset.X = Math.Min(canvasOffset.X + scrollSpeed, -(blockBounds.Right - virtualWidth));
            }

            if (blockBounds.Top < -canvasOffset.Y)
            {
                canvasOffset.Y = Math.Max(canvasOffset.Y - scrollSpeed, -blockBounds.Top);
            }
            else if (blockBounds.Bottom > -canvasOffset.Y + virtualHeight)
            {
                canvasOffset.Y = Math.Min(canvasOffset.Y + scrollSpeed, -(blockBounds.Bottom - virtualHeight));
            }
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
    }
}