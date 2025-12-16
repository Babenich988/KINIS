using Kinis.Models;
using Kinis.Services;
using Kinis.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Kinis.Services.CommandManager;

namespace Kinis
{
    /// <summary>
    /// Главная форма приложения BPMN-редактора.
    /// </summary>
    /// <remarks>
    /// Форма обеспечивает пользовательский интерфейс для работы с BPMN-диаграммами,
    /// включая панель инструментов, боковую панель с элементами и бесконечный холст.
    /// Также форма координирует работу сервисов: команд, файлов, автосохранения и др.
    /// </remarks>
    public partial class Form1 : Form
    {
        #region Поля класса

        private InfiniteCanvas canvas;
        private bool sidebarExpand;
        private int miniMinWidth = 48;
        private int miniMinHeight = 42;
        private int miniMaxHeight = 80;
        private BpmnBlock selectedSidebarBlock = null;
        private bool isDraggingFromSidebar = false;
        private Point dragStartPoint;
        private const float MIN_ZOOM = 0.25f;
        private const float MAX_ZOOM = 5.0f;
        private ToolTip toolTip = new ToolTip();
        private BlockCreationService _blockCreationService;
        private Keys _lastProcessedKey = Keys.None;
        private DateTime _lastKeyPressTime = DateTime.MinValue;
        private const int KEY_COOLDOWN_MS = 1000;
        private CommandManager _commandManager;

        /// <summary>
        /// Сервис автосохранения проекта.
        /// </summary>
        private AutoSaveService _autoSaveService;

        /// <summary>
        /// Флаг включения автосохранения.
        /// </summary>
        private bool _autoSaveEnabled = false;

        /// <summary>
        /// Интервал автосохранения в минутах (по умолчанию 5).
        /// </summary>
        private int _autoSaveInterval = 5;

        /// <summary>
        /// Менеджер команд для системы Undo/Redo.
        /// </summary>
        public CommandManager CommandManager => _commandManager;

        /// <summary>
        /// Максимальное количество листов (вкладок) в проекте.
        /// </summary>
        private const int MAX_SHEETS = 5;

        /// <summary>
        /// Список листов (вкладок) холста.
        /// </summary>
        private List<CanvasSheet> sheets = new List<CanvasSheet>();

        /// <summary>
        /// Индекс текущего активного листа.
        /// </summary>
        private int currentSheetIndex = -1;

        /// <summary>
        /// Панель предварительного просмотра элементов в боковой панели.
        /// </summary>
        private Panel sidebarPreviewPanel;

        /// <summary>
        /// Список мини-блоков для отображения в боковой панели.
        /// </summary>
        private List<BpmnBlock> sidebarBlocks = new List<BpmnBlock>();

        #endregion

        #region Вспомогательные методы

        /// <summary>
        /// Вычисляет максимальную ширину блока в боковой панели с учетом отступов.
        /// </summary>
        /// <returns>Максимальная ширина блока в пикселях.</returns>
        private int GetMaxSidebarBlockWidth()
        {
            int margin = 8;
            int visible = sidebar.ClientSize.Width;
            if (visible <= 0) visible = sidebar.Width;
            return Math.Max(20, visible - 2 * margin);
        }

        #endregion

        #region Конструктор и инициализация

        /// <summary>
        /// Инициализирует новый экземпляр класса Form1.
        /// </summary>
        public Form1()
        {
            InitializeComponent();

            // Настройка боковой панели
            sidebar.Width = sidebar.MinimumSize.Width;
            sidebarExpand = false;
            menuButton.Click += (s, e) => sidebarTimer.Start();

            // Добавление холста на форму
            AddCanvasToExistingPanels();

            // Настройка панели инструментов
            panel2.SetRoundedShapeWithBorder(30, Color.Black, 2);
            panelFigures.FlowDirection = FlowDirection.TopDown;
            panelFigures.WrapContents = false;
            panelFigures.AutoScroll = true;
            panelFigures.Dock = DockStyle.Fill;
            panelFigures.Padding = new Padding(8);

            // Обработка клика по форме для снятия выделения с боковой панели
            this.MouseDown += (s, e) =>
            {
                if (selectedSidebarBlock != null)
                {
                    selectedSidebarBlock = null;
                    sidebarPreviewPanel?.Invalidate();
                }
            };

            // Подключение кнопок масштабирования
            ConnectZoomButtons();

            // Инициализация всплывающих подсказок
            toolTip = new ToolTip();
            toolTip.AutoPopDelay = 5000;
            toolTip.InitialDelay = 500;
            toolTip.ReshowDelay = 100;

            // Подписка на события изменения масштаба
            if (canvas != null)
            {
                canvas.ZoomChanged += (zoom) => UpdateZoomButtonsState(zoom);
            }

            // Инициализация сервиса создания блоков
            _blockCreationService = new BlockCreationService(canvas, canvas.GetBlocks());

            // Включение обработки клавиш
            this.KeyPreview = true;
            this.KeyDown += Form1_KeyDown;

            // Инициализация менеджера команд
            _commandManager = new CommandManager();
            _commandManager.OnStateChanged += UpdateUndoRedoButtons;
            UpdateUndoRedoButtons();

            // Подписка на событие отпускания клавиш
            this.KeyUp += Form1_KeyUp;

            // Подписка на события BpmnFileService
            BpmnFileService.ProjectModified += (s, e) => UpdateWindowTitle();
            BpmnFileService.ProjectSaved += (s, e) => UpdateWindowTitle();
            BpmnFileService.ProjectLoaded += (s, e) => UpdateWindowTitle();

            // Подписка на события изменения в canvas
            if (canvas != null)
            {
                canvas.BlockModified += (s, e) => BpmnFileService.MarkAsModified();
                canvas.ArrowModified += (s, e) => BpmnFileService.MarkAsModified();
                canvas.ElementAdded += (s, e) => BpmnFileService.MarkAsModified();
            }

            // Подписка команд на отслеживание изменений
            _commandManager.OnStateChanged += () =>
            {
                if (_commandManager.CanUndo)
                {
                    BpmnFileService.MarkAsModified();
                }
            };

            // Инициализация сервиса автосохранения
            InitializeAutoSaveService();
        }

        /// <summary>
        /// Инициализирует сервис автосохранения с заданными делегатами для получения данных.
        /// </summary>
        private void InitializeAutoSaveService()
        {
            _autoSaveService = new AutoSaveService(
                () => canvas?.GetBlocks() ?? new List<BpmnBlock>(),
                () => canvas?.GetArrows() ?? new List<BpmnArrow>(),
                () => canvas?.GetCurvedArrows() ?? new List<BpmnCurvedArrow>(),
                () => BpmnFileService.CurrentFilePath
            );

            _autoSaveService.AutoSavePerformed += (time) =>
            {
                Console.WriteLine($"Автосохранение выполнено в {time}");
            };
        }

        #endregion

        #region Работа с элементами (клонирование)

        /// <summary>
        /// Создает глубокую копию блока BPMN.
        /// </summary>
        /// <param name="src">Исходный блок для клонирования.</param>
        /// <returns>Новый экземпляр блока с теми же свойствами.</returns>
        private BpmnBlock CloneBlock(BpmnBlock src)
        {
            var nb = new BpmnBlock(src.Bounds.X, src.Bounds.Y, src.Bounds.Width, src.Bounds.Height)
            {
                Id = src.Id,
                Text = src.Text,
                Type = src.Type,
                FillColor = src.FillColor,
                BorderColor = src.BorderColor
            };
            return nb;
        }

        /// <summary>
        /// Создает глубокую копию стрелки BPMN.
        /// </summary>
        /// <param name="src">Исходная стрелка для клонирования.</param>
        /// <returns>Новый экземпляр стрелки с теми же свойствами.</returns>
        private BpmnArrow CloneArrow(BpmnArrow src)
        {
            var na = new BpmnArrow()
            {
                Id = src.Id,
                Text = src.Text,
                Color = src.Color,
                Width = src.Width
            };

            na.StartPoint = src.StartPoint;
            na.EndPoint = src.EndPoint;
            na.ConnectionPoints = new System.Collections.Generic.List<PointF>(src.ConnectionPoints);
            na.StartBlock = null;
            na.EndBlock = null;

            return na;
        }

        #endregion

        #region Обработка событий клавиатуры

        /// <summary>
        /// Обрабатывает нажатия клавиш для горячих клавиш и команд Undo/Redo.
        /// </summary>
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            // Проверка активного редактирования текста
            if (canvas.IsEditingText())
                return;

            // Горячие клавиши Undo/Redo
            if (e.Control && e.KeyCode == Keys.Z)
            {
                _commandManager.Undo();
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }

            if (e.Control && e.KeyCode == Keys.Y)
            {
                _commandManager.Redo();
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }

            // Создание элементов по горячим клавишам
            var keyMappings = _blockCreationService.GetBlockKeyMappings();

            if (keyMappings.ContainsKey(e.KeyCode))
            {
                // Проверка задержки для предотвращения повторных нажатий
                TimeSpan timeSinceLastPress = DateTime.Now - _lastKeyPressTime;
                if (timeSinceLastPress.TotalMilliseconds < KEY_COOLDOWN_MS && e.KeyCode == _lastProcessedKey)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    return;
                }

                _lastProcessedKey = e.KeyCode;
                _lastKeyPressTime = DateTime.Now;

                CreateBlockWithHotkey(e.KeyCode);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        /// <summary>
        /// Обрабатывает отпускание клавиш, сбрасывая состояние задержки.
        /// </summary>
        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            var keyMappings = _blockCreationService.GetBlockKeyMappings();

            if (keyMappings.ContainsKey(e.KeyCode))
            {
                if (e.KeyCode == _lastProcessedKey)
                {
                    // Сброс состояния для быстрого переключения между разными горячими клавишами
                }
            }
        }

        /// <summary>
        /// Создает элемент на холсте по нажатой горячей клавише.
        /// </summary>
        /// <param name="key">Код клавиши, соответствующей элементу.</param>
        private void CreateBlockWithHotkey(Keys key)
        {
            PointF virtualPos = canvas.GetCursorVirtualPosition();
            var keyMappings = _blockCreationService.GetBlockKeyMappings();

            if (keyMappings.ContainsKey(key))
            {
                var mapping = keyMappings[key];

                if (mapping.Type == "Arrow")
                {
                    CreateArrowWithCommand(virtualPos);
                    return;
                }
                else if (mapping.Type == "CurvedArrow")
                {
                    CreateCurvedArrowWithCommand(virtualPos);
                    return;
                }

                CreateBlockWithCommand(mapping.Type, mapping.Text, virtualPos);
                Console.WriteLine($"Block created via command: {mapping.Text} at {virtualPos}");
            }
        }

        #endregion

        #region Боковая панель (Sidebar)

        /// <summary>
        /// Отрисовывает миниатюры элементов в боковой панели.
        /// </summary>
        private void SidebarPreviewPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (sidebarBlocks == null || sidebarBlocks.Count == 0)
                return;

            Point scrollOffset = sidebarPreviewPanel.AutoScrollPosition;

            foreach (var block in sidebarBlocks)
            {
                RectangleF rect = new RectangleF(
                    block.Bounds.X + scrollOffset.X,
                    block.Bounds.Y + scrollOffset.Y,
                    block.Bounds.Width,
                    block.Bounds.Height
                );

                // Рамка элемента
                using (var pen = new Pen(Color.Gray, 1))
                    g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);

                // Иконка или текст элемента
                if (SidebarIconRegistry.Icons.TryGetValue(block.Type, out Image icon) && icon != null)
                {
                    float padding = 6f;
                    RectangleF imgRect = new RectangleF(
                        rect.X + padding,
                        rect.Y + padding,
                        rect.Width - padding * 2,
                        rect.Height - padding * 2
                    );
                    g.DrawImage(icon, imgRect);
                }
                else
                {
                    // Fallback: отображение текста если нет иконки
                    float fontSize = Math.Max(8f, Math.Min(12f, rect.Height / 6f + rect.Width / 60f));
                    float fs = Math.Max(6f, fontSize);

                    Font miniFont = new Font("Segoe UI", fs);
                    var textBrush = new SolidBrush(Color.Black);

                    try
                    {
                        SizeF textSize = g.MeasureString(block.Text, miniFont);

                        while ((textSize.Width > rect.Width - 6 || textSize.Height > rect.Height - 6) && fs > 6f)
                        {
                            fs -= 0.5f;
                            miniFont.Dispose();
                            miniFont = new Font("Segoe UI", fs);
                            textSize = g.MeasureString(block.Text, miniFont);
                        }

                        float textX = rect.X + (rect.Width - textSize.Width) / 2f;
                        float textY = rect.Y + (rect.Height - textSize.Height) / 2f;

                        g.DrawString(block.Text, miniFont, textBrush, textX, textY);
                    }
                    finally
                    {
                        miniFont.Dispose();
                        textBrush.Dispose();
                    }
                }
            }
        }

        /// <summary>
        /// Обрабатывает нажатие мыши на панели предварительного просмотра.
        /// </summary>
        private void SidebarPreviewPanel_MouseDown(object sender, MouseEventArgs e)
        {
            Point scrollOffset = sidebarPreviewPanel.AutoScrollPosition;
            Point adjustedClick = new Point(
                e.X - scrollOffset.X,
                e.Y - scrollOffset.Y
            );

            // Проверка клика по блоку
            foreach (var block in sidebarBlocks)
            {
                if (block.Bounds.Contains(adjustedClick))
                {
                    selectedSidebarBlock = block;
                    sidebarPreviewPanel.Invalidate();

                    // Начало перетаскивания
                    isDraggingFromSidebar = true;
                    dragStartPoint = adjustedClick;
                    return;
                }
            }

            selectedSidebarBlock = null;
            sidebarPreviewPanel.Invalidate();
            isDraggingFromSidebar = false;
        }

        /// <summary>
        /// Обрабатывает движение мыши для реализации Drag&Drop из боковой панели.
        /// </summary>
        private void SidebarPreviewPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDraggingFromSidebar && selectedSidebarBlock != null)
            {
                int dragThreshold = SystemInformation.DragSize.Width / 2;
                Point dragDelta = new Point(
                    Math.Abs(e.X - dragStartPoint.X),
                    Math.Abs(e.Y - dragStartPoint.Y)
                );

                if (dragDelta.X > dragThreshold || dragDelta.Y > dragThreshold)
                {
                    var data = new DataObject();

                    if (selectedSidebarBlock.Type == "Arrow")
                    {
                        data.SetData("BpmnElementType", "Arrow");
                        data.SetData("BpmnBlock", selectedSidebarBlock);
                    }
                    else if (selectedSidebarBlock.Type == "CurvedArrow")
                    {
                        data.SetData("BpmnElementType", "CurvedArrow");
                        data.SetData("BpmnBlock", selectedSidebarBlock);
                    }
                    else
                    {
                        data.SetData("BpmnElementType", "Block");
                        data.SetData("BpmnBlock", selectedSidebarBlock);
                    }

                    sidebarPreviewPanel.DoDragDrop(data, DragDropEffects.Copy);
                    isDraggingFromSidebar = false;
                }
            }
        }

        /// <summary>
        /// Обрабатывает двойной клик по элементу в боковой панели для создания его на холсте.
        /// </summary>
        private void SidebarPreviewPanel_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Point scrollOffset = sidebarPreviewPanel.AutoScrollPosition;
            Point adjustedClick = new Point(e.X - scrollOffset.X, e.Y - scrollOffset.Y);

            foreach (var block in sidebarBlocks)
            {
                if (block.Bounds.Contains(adjustedClick))
                {
                    if (block.Type == "Arrow")
                    {
                        CreateArrowWithCommand(GetCanvasCenterWorldPoint());
                        return;
                    }
                    else if (block.Type == "CurvedArrow")
                    {
                        CreateCurvedArrowWithCommand(GetCanvasCenterWorldPoint());
                        return;
                    }
                    else
                    {
                        // Создание блока через команду
                        BpmnBlock newBlock = new BpmnBlock(0, 0, 120, 80)
                        {
                            Text = block.Text,
                            Type = block.Type,
                            FillColor = block.FillColor,
                            BorderColor = block.BorderColor,
                            Id = Guid.NewGuid().ToString()
                        };

                        // Определение позиции для нового блока
                        if (canvas.GetBlocks().Count > 0)
                        {
                            var last = canvas.GetBlocks().Last();
                            newBlock.Bounds = new RectangleF(
                                last.Bounds.X + last.Bounds.Width + 30,
                                last.Bounds.Y,
                                newBlock.Bounds.Width,
                                newBlock.Bounds.Height
                            );
                        }
                        else
                        {
                            PointF center = GetCanvasCenterWorldPoint();
                            newBlock.Bounds = new RectangleF(
                                center.X - newBlock.Bounds.Width / 2,
                                center.Y - newBlock.Bounds.Height / 2,
                                newBlock.Bounds.Width,
                                newBlock.Bounds.Height
                            );
                        }

                        var command = new CreateBlockCommand(newBlock, canvas.GetBlocks(), canvas);
                        _commandManager.Execute(command);
                        Console.WriteLine($"CreateBlockCommand executed via double-click: {newBlock.Text}");
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Обрабатывает отпускание кнопки мыши в боковой панели.
        /// </summary>
        private void SidebarPreviewPanel_MouseUp(object sender, MouseEventArgs e)
        {
            isDraggingFromSidebar = false;
        }

        /// <summary>
        /// Добавляет элементы BPMN в боковую панель для предварительного просмотра.
        /// </summary>
        private void AddBlocksToSidebar()
        {
            // Удаление старой панели если существует
            if (sidebarPreviewPanel != null && sidebar.Controls.Contains(sidebarPreviewPanel))
                sidebar.Controls.Remove(sidebarPreviewPanel);

            // Создание новой панели с автоскроллом
            sidebarPreviewPanel = new Panel
            {
                Name = "SidebarPreviewPanel",
                BackColor = Color.Transparent,
                Width = sidebar.ClientSize.Width,
                Height = sidebar.Height - 120,
                Margin = new Padding(0),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                AutoScroll = true
            };

            sidebar.Controls.Add(sidebarPreviewPanel);
            sidebarPreviewPanel.Width = Math.Max(20, sidebar.ClientSize.Width);
            sidebarPreviewPanel.AllowDrop = true;

            // Создание мини-блоков для боковой панели
            sidebarBlocks = new List<BpmnBlock>
            {
                new BpmnBlock(8, 8, miniMinWidth, miniMinHeight)
                    { Text = "Комментарий", Type = "Комментарий" },
                new BpmnBlock(8, 8 + (miniMinHeight + 12) * 1, miniMinWidth, miniMinHeight)
                    { Text = "Задача", Type = "Задача" },
                new BpmnBlock(8, 8 + (miniMinHeight + 12) * 2, miniMinWidth, miniMinHeight)
                    { Text = "→", Type = "Arrow", FillColor = Color.LightGray, BorderColor = Color.DarkGray },
                new BpmnBlock(8, 8 + (miniMinHeight + 12) * 3, miniMinWidth, miniMinHeight)
                    { Text = "↷", Type = "CurvedArrow", FillColor = Color.LightBlue, BorderColor = Color.DarkBlue },
                new BpmnBlock(8, 8 + (miniMinHeight + 12) * 4, miniMinWidth, miniMinHeight)
                    { Text = "Развилка", Type = "Развилка" },
                new BpmnBlock(8, 8 + (miniMinHeight + 12) * 5, miniMinWidth, miniMinHeight)
                    { Text = "Развилка И", Type = "Развилка И" },
                new BpmnBlock(8, 8 + (miniMinHeight + 12) * 6, miniMinWidth, miniMinHeight)
                    { Text = "Начальное событие", Type = "Начальное событие" },
                new BpmnBlock(8, 8 + (miniMinHeight + 12) * 7, miniMinWidth, miniMinHeight)
                    { Text = "Промежуточное событие", Type = "Промежуточное событие" },
                new BpmnBlock(8, 8 + (miniMinHeight + 12) * 8, miniMinWidth, miniMinHeight)
                    { Text = "Конечное событие", Type = "Конечное событие" },
                new BpmnBlock(8, 8 + (miniMinHeight + 12) * 9, miniMinWidth, miniMinHeight)
                    { Text = "Объект данных", Type = "Объект данных" },
                new BpmnBlock(8, 8 + (miniMinHeight + 12) * 10, miniMinWidth, miniMinHeight)
                    { Text = "Хранилище данных", Type = "Хранилище данных" },
                new BpmnBlock(8, 8 + (miniMinHeight + 12) * 11, miniMinWidth, miniMinHeight)
                    { Text = "Пул", Type = "Пул", BorderColor = Color.Black },
                new BpmnBlock(8, 8 + (miniMinHeight + 12) * 12, miniMinWidth, miniMinHeight)
                    { Text = "Получ. сообщ. (нач.)", Type = "Событие-получение сообщения" },
                new BpmnBlock(8, 8 + (miniMinHeight + 12) * 13, miniMinWidth, miniMinHeight)
                    { Text = "Получ. сообщ. (пром.)", Type = "Событие-получение сообщения (промежуточное)" },
                new BpmnBlock(8, 8 + (miniMinHeight + 12) * 14, miniMinWidth, miniMinHeight)
                    { Text = "Отпр. сообщ. (пром.)", Type = "Событие-отправка сообщения (промежуточное)" },
                new BpmnBlock(8, 8 + (miniMinHeight + 12) * 15, miniMinWidth, miniMinHeight)
                    { Text = "Отпр. сообщ. (кон.)", Type = "Событие-отправка сообщения" },
                new BpmnBlock(8, 8 + (miniMinHeight + 12) * 16, miniMinWidth, miniMinHeight)
                    { Text = "Ошибка (обр.)", Type = "Событие-ошибка обработчик" },
                new BpmnBlock(8, 8 + (miniMinHeight + 12) * 17, miniMinWidth, miniMinHeight)
                    { Text = "Ошибка (иниц.)", Type = "Событие-ошибка инициатор" },
                new BpmnBlock(8, 8 + (miniMinHeight + 12) * 18, miniMinWidth, miniMinHeight)
                    { Text = "Отмена (обр.)", Type = "Событие-отмена обработчик" },
                new BpmnBlock(8, 8 + (miniMinHeight + 12) * 19, miniMinWidth, miniMinHeight)
                    { Text = "Отмена (иниц.)", Type = "Событие-отмена инициатор" },
                new BpmnBlock(8, 8 + (miniMinHeight + 12) * 20, miniMinWidth, miniMinHeight)
                    { Text = "Остановка", Type = "Событие-остановка" }
            };

            // Подписка на события панели
            sidebarPreviewPanel.Paint += SidebarPreviewPanel_Paint;
            sidebarPreviewPanel.MouseDoubleClick += SidebarPreviewPanel_MouseDoubleClick;
            sidebarPreviewPanel.MouseDown += SidebarPreviewPanel_MouseDown;
            sidebarPreviewPanel.MouseMove += SidebarPreviewPanel_MouseMove;
            sidebarPreviewPanel.MouseUp += SidebarPreviewPanel_MouseUp;

            // Обновление размеров блоков
            UpdateSidebarBlocksSize();
        }

        /// <summary>
        /// Обрабатывает прокрутку колесика мыши в боковой панели.
        /// </summary>
        private void SidebarPreviewPanel_MouseWheel(object sender, MouseEventArgs e)
        {
            Panel panel = sender as Panel;
            if (panel == null) return;

            int scrollStep = 20;
            int newValue = panel.VerticalScroll.Value - e.Delta / 120 * scrollStep;

            // Ограничение прокрутки в допустимых пределах
            if (newValue < panel.VerticalScroll.Minimum)
                newValue = panel.VerticalScroll.Minimum;
            if (newValue > panel.VerticalScroll.Maximum)
                newValue = panel.VerticalScroll.Maximum;

            panel.AutoScrollPosition = new Point(panel.AutoScrollPosition.X, newValue);
        }

        /// <summary>
        /// Обработчик анимации открытия/закрытия боковой панели.
        /// </summary>
        private void sidebarTimer_Tick_1(object sender, EventArgs e)
        {
            if (sidebarExpand)
            {
                sidebar.Width -= 10;
                if (sidebar.Width <= sidebar.MinimumSize.Width)
                {
                    sidebarExpand = false;
                    sidebarTimer.Stop();
                }
            }
            else
            {
                sidebar.Width += 10;
                if (sidebar.Width >= sidebar.MaximumSize.Width)
                {
                    sidebarExpand = true;
                    sidebarTimer.Stop();
                }
            }

            if (sidebarPreviewPanel != null)
            {
                sidebarPreviewPanel.Width = Math.Max(20, sidebar.ClientSize.Width);
            }
            UpdateSidebarBlocksSize();
        }

        /// <summary>
        /// Линейная интерполяция между двумя значениями.
        /// </summary>
        /// <param name="a">Начальное значение.</param>
        /// <param name="b">Конечное значение.</param>
        /// <param name="t">Коэффициент интерполяции (0-1).</param>
        /// <returns>Интерполированное значение.</returns>
        private float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        /// <summary>
        /// Вычисляет коэффициент масштаба боковой панели (от 0 до 1).
        /// </summary>
        /// <returns>Коэффициент масштаба (0 - свернуто, 1 - развернуто).</returns>
        private float GetSidebarScale()
        {
            float min = sidebar.MinimumSize.Width;
            float max = sidebar.MaximumSize.Width;
            if (max <= min) return 1f;
            float s = (sidebar.Width - min) / (max - min);
            if (s < 0f) s = 0f;
            if (s > 1f) s = 1f;
            return s;
        }

        /// <summary>
        /// Обновляет размеры блоков в боковой панели в зависимости от ее ширины.
        /// </summary>
        private void UpdateSidebarBlocksSize()
        {
            if (sidebarPreviewPanel == null || sidebarBlocks == null || sidebarBlocks.Count == 0)
            {
                return;
            }

            float scale = GetSidebarScale();
            int margin = 8;
            int spacing = 12;

            int panelAvailable = Math.Max(20, sidebarPreviewPanel.ClientSize.Width - 2 * margin);
            float curWidth = Lerp(miniMinWidth, panelAvailable, scale);
            float curHeight = Lerp(miniMinHeight, panelAvailable, scale);

            float x = margin;
            float y = margin;

            foreach (var block in sidebarBlocks)
            {
                block.Bounds = new RectangleF(x, y, curWidth, curHeight);
                y += curHeight + spacing;
            }

            // Добавление запаса для корректной прокрутки
            int totalHeight = (int)y + margin;
            sidebarPreviewPanel.AutoScrollMinSize = new Size(0, totalHeight);
            sidebarPreviewPanel.Invalidate();
        }

        #endregion

        #region Методы работы с холстом

        /// <summary>
        /// Получает центральную точку холста в мировых координатах.
        /// </summary>
        /// <returns>Точка в мировых координатах.</returns>
        private PointF GetCanvasCenterWorldPoint()
        {
            if (canvas == null)
                return new PointF(100, 100);

            Point screenCenter = new Point(canvas.Width / 2, canvas.Height / 2);

            if (canvas is Kinis.InfiniteCanvas ic)
            {
                return ic.ScreenToWorld(screenCenter);
            }

            return new PointF(screenCenter.X, screenCenter.Y);
        }

        /// <summary>
        /// Обрабатывает нажатие мыши на холсте для снятия выделения с боковой панели.
        /// </summary>
        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (selectedSidebarBlock != null)
            {
                selectedSidebarBlock = null;
                if (sidebarPreviewPanel != null)
                    sidebarPreviewPanel.Invalidate();
            }
        }

        /// <summary>
        /// Разрешает перенос только наших блоков и стрелок при Drag&Drop.
        /// </summary>
        private void Canvas_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(BpmnBlock)) || e.Data.GetDataPresent("BpmnElementType"))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        /// <summary>
        /// Обрабатывает завершение Drag&Drop на холсте.
        /// </summary>
        private void Canvas_DragDrop(object sender, DragEventArgs e)
        {
            Point clientPoint = canvas.PointToClient(new Point(e.X, e.Y));
            PointF worldPoint = canvas.ScreenToWorld(clientPoint);

            // Проверка нового формата данных с BpmnElementType
            if (e.Data.GetDataPresent("BpmnElementType"))
            {
                string elementType = (string)e.Data.GetData("BpmnElementType");

                if (elementType == "Arrow")
                {
                    CreateArrowWithCommand(worldPoint);
                    return;
                }
                else if (elementType == "CurvedArrow")
                {
                    CreateCurvedArrowWithCommand(worldPoint);
                    return;
                }
                else if (elementType == "Block" && e.Data.GetDataPresent("BpmnBlock"))
                {
                    var blockFromSidebar = (BpmnBlock)e.Data.GetData("BpmnBlock");
                    CreateBlockFromDragDrop(blockFromSidebar, worldPoint);
                    return;
                }
            }

            // Старая логика для совместимости
            if (e.Data.GetDataPresent(typeof(BpmnBlock)))
            {
                var blockFromSidebar = (BpmnBlock)e.Data.GetData(typeof(BpmnBlock));

                if (blockFromSidebar.Type == "CurvedArrow")
                {
                    CreateCurvedArrowWithCommand(worldPoint);
                }
                else
                {
                    CreateBlockFromDragDrop(blockFromSidebar, worldPoint);
                }
            }
        }

        /// <summary>
        /// Создает блок на холсте из данных Drag&Drop.
        /// </summary>
        /// <param name="blockFromSidebar">Блок-шаблон из боковой панели.</param>
        /// <param name="worldPoint">Позиция для создания в мировых координатах.</param>
        private void CreateBlockFromDragDrop(BpmnBlock blockFromSidebar, PointF worldPoint)
        {
            // Специальная обработка пулов
            if (blockFromSidebar.Type == "Пул")
            {
                var poolBlock = new BpmnBlock(worldPoint.X, worldPoint.Y, 400, 200)
                {
                    Text = blockFromSidebar.Text,
                    Type = "Пул",
                    FillColor = Color.White,
                    BorderColor = Color.Black,
                    Id = Guid.NewGuid().ToString()
                };

                poolBlock.InitializePoolLanes();
                System.Diagnostics.Debug.WriteLine($"Создан пул: Size={poolBlock.Bounds.Size}, Position={poolBlock.Bounds.Location}");

                var poolCommand = new CreateBlockCommand(poolBlock, canvas.GetBlocks(), canvas);
                _commandManager.Execute(poolCommand);
                Console.WriteLine($"CreateBlockCommand executed for Pool: {poolBlock.Text}");

                canvas?.RaiseElementAdded();
                return;
            }

            // Создание обычного блока
            var newBlock = new BpmnBlock(worldPoint.X, worldPoint.Y,
                blockFromSidebar.Bounds.Width, blockFromSidebar.Bounds.Height)
            {
                Text = blockFromSidebar.Text,
                Type = blockFromSidebar.Type,
                FillColor = blockFromSidebar.FillColor,
                BorderColor = blockFromSidebar.BorderColor,
                Id = Guid.NewGuid().ToString()
            };

            var blockCommand = new CreateBlockCommand(newBlock, canvas.GetBlocks(), canvas);
            _commandManager.Execute(blockCommand);
            Console.WriteLine($"CreateBlockCommand executed via drag&drop: {newBlock.Text}");

            canvas?.RaiseElementAdded();
        }

        /// <summary>
        /// Обрабатывает отзывчивость курсора при Drag&Drop.
        /// </summary>
        private void SidebarPreviewPanel_GiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            if (e.Effect == DragDropEffects.Copy)
            {
                e.UseDefaultCursors = false;
                Cursor.Current = Cursors.Cross;
            }
            else
            {
                e.UseDefaultCursors = true;
            }
        }

        #endregion

        #region Масштабирование (Zoom)

        /// <summary>
        /// Подключает обработчики событий для кнопок масштабирования.
        /// </summary>
        private void ConnectZoomButtons()
        {
            btnZoomIn.Click += (s, e) => canvas.ZoomIn();
            btnZoomOut.Click += (s, e) => canvas.ZoomOut();
            btnZoomReset.Click += (s, e) =>
            {
                canvas.ResetZoom();
            };
        }

        /// <summary>
        /// Обновляет состояние кнопок масштабирования в зависимости от текущего масштаба.
        /// </summary>
        /// <param name="currentZoom">Текущий коэффициент масштабирования.</param>
        private void UpdateZoomButtonsState(float currentZoom)
        {
            btnZoomIn.Enabled = currentZoom < MAX_ZOOM;
            btnZoomOut.Enabled = currentZoom > MIN_ZOOM;
            btnZoomReset.Enabled = true;
            UpdateZoomToolTips(currentZoom);
        }

        /// <summary>
        /// Обновляет всплывающие подсказки для кнопок масштабирования.
        /// </summary>
        /// <param name="currentZoom">Текущий коэффициент масштабирования.</param>
        private void UpdateZoomToolTips(float currentZoom)
        {
            toolTip.SetToolTip(btnZoomIn, btnZoomIn.Enabled ?
                "Увеличить масштаб (Ctrl + Колесо мыши)" :
                "Достигнут максимальный масштаб (500%)");

            toolTip.SetToolTip(btnZoomOut, btnZoomOut.Enabled ?
                "Уменьшить масштаб (Ctrl + Колесо мыши)" :
                "Достигнут минимальный масштаб (25%)");

            toolTip.SetToolTip(btnZoomReset, "Сбросить масштаб к 100% и перейти к выделенному элементу");
        }

        #endregion

        #region Экспорт в изображение

        /// <summary>
        /// Сохраняет текущую диаграмму в файл изображения.
        /// </summary>
        private void SaveFormAsImage()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "PNG Image|*.png|JPEG Image|*.jpg|Bitmap Image|*.bmp";
            saveFileDialog.Title = "Save Canvas as Image";
            saveFileDialog.FileName = $"BPMN_Diagram_{DateTime.Now:yyyyMMdd_HHmmss}";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    ImageFormat format = GetImageFormat(saveFileDialog.FilterIndex);
                    SaveCanvasAsImage(saveFileDialog.FileName, format);

                    MessageBox.Show("Диаграмма успешно сохранена как изображение!", "Сохранение завершено",
                                  MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при сохранении изображения: " + ex.Message, "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Сохраняет содержимое холста в файл изображения.
        /// </summary>
        /// <param name="filePath">Путь к файлу для сохранения.</param>
        /// <param name="format">Формат изображения.</param>
        private void SaveCanvasAsImage(string filePath, ImageFormat format)
        {
            if (canvas == null) return;

            // Получение границ всех элементов
            RectangleF elementsBounds = CalculateElementsBounds();

            // Добавление отступов
            float padding = 50f;
            RectangleF imageBounds = new RectangleF(
                elementsBounds.X - padding,
                elementsBounds.Y - padding,
                elementsBounds.Width + padding * 2,
                elementsBounds.Height + padding * 2
            );

            // Размер по умолчанию если нет элементов
            if (imageBounds.Width <= 0 || imageBounds.Height <= 0)
            {
                imageBounds = new RectangleF(0, 0, 800, 600);
            }

            // Создание bitmap
            using (Bitmap bitmap = new Bitmap((int)Math.Ceiling(imageBounds.Width),
                                            (int)Math.Ceiling(imageBounds.Height)))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.Clear(Color.White);
                    g.TranslateTransform(-imageBounds.X, -imageBounds.Y);

                    // Отрисовка элементов
                    DrawGridForExport(g, imageBounds);
                    DrawElementsForExport(g);
                }

                bitmap.Save(filePath, format);
            }
        }

        /// <summary>
        /// Вычисляет общие границы всех элементов на холсте.
        /// </summary>
        /// <returns>Прямоугольник, охватывающий все элементы.</returns>
        private RectangleF CalculateElementsBounds()
        {
            if (canvas == null) return new RectangleF(0, 0, 0, 0);

            var blocks = canvas.GetBlocks();
            var arrows = canvas.GetArrows();
            var curvedArrows = canvas.GetCurvedArrows();

            if ((blocks == null || blocks.Count == 0) &&
                (arrows == null || arrows.Count == 0) &&
                (curvedArrows == null || curvedArrows.Count == 0))
            {
                return new RectangleF(0, 0, 0, 0);
            }

            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;

            // Блоки
            if (blocks != null)
            {
                foreach (var block in blocks)
                {
                    var bounds = block.Bounds;
                    minX = Math.Min(minX, bounds.Left);
                    minY = Math.Min(minY, bounds.Top);
                    maxX = Math.Max(maxX, bounds.Right);
                    maxY = Math.Max(maxY, bounds.Bottom);
                }
            }

            // Стрелки
            if (arrows != null)
            {
                foreach (var arrow in arrows)
                {
                    var bounds = arrow.GetBounds();
                    minX = Math.Min(minX, bounds.Left);
                    minY = Math.Min(minY, bounds.Top);
                    maxX = Math.Max(maxX, bounds.Right);
                    maxY = Math.Max(maxY, bounds.Bottom);
                }
            }

            // Кривые стрелки
            if (curvedArrows != null)
            {
                foreach (var curvedArrow in curvedArrows)
                {
                    var bounds = curvedArrow.GetBounds();
                    minX = Math.Min(minX, bounds.Left);
                    minY = Math.Min(minY, bounds.Top);
                    maxX = Math.Max(maxX, bounds.Right);
                    maxY = Math.Max(maxY, bounds.Bottom);
                }
            }

            if (minX == float.MaxValue) return new RectangleF(0, 0, 0, 0);

            return new RectangleF(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// Отрисовывает сетку для экспорта изображения.
        /// </summary>
        private void DrawGridForExport(Graphics g, RectangleF bounds)
        {
            int gridSize = 20;
            Color gridColor = Color.FromArgb(240, 240, 240);

            float extendedLeft = bounds.Left - gridSize * 2;
            float extendedTop = bounds.Top - gridSize * 2;
            float extendedRight = bounds.Right + gridSize * 2;
            float extendedBottom = bounds.Bottom + gridSize * 2;

            using (Pen gridPen = new Pen(gridColor, 1))
            {
                // Вертикальные линии
                for (float x = extendedLeft; x <= extendedRight; x += gridSize)
                {
                    if (x >= bounds.Left && x <= bounds.Right)
                    {
                        g.DrawLine(gridPen, x, extendedTop, x, extendedBottom);
                    }
                }

                // Горизонтальные линии
                for (float y = extendedTop; y <= extendedBottom; y += gridSize)
                {
                    if (y >= bounds.Top && y <= bounds.Bottom)
                    {
                        g.DrawLine(gridPen, extendedLeft, y, extendedRight, y);
                    }
                }
            }
        }

        /// <summary>
        /// Отрисовывает элементы холста для экспорта.
        /// </summary>
        private void DrawElementsForExport(Graphics g)
        {
            if (canvas == null) return;

            var blocks = canvas.GetBlocks();
            var arrows = canvas.GetArrows();
            var curvedArrows = canvas.GetCurvedArrows();

            // Стрелки (под блоками)
            if (arrows != null)
            {
                foreach (var arrow in arrows)
                {
                    arrow.Draw(g, false);
                }
            }

            // Кривые стрелки
            if (curvedArrows != null)
            {
                foreach (var curvedArrow in curvedArrows)
                {
                    curvedArrow.Draw(g, false);
                }
            }

            // Блоки (поверх стрелок)
            if (blocks != null)
            {
                foreach (var block in blocks)
                {
                    block.Draw(g, false);
                }
            }
        }

        /// <summary>
        /// Определяет формат изображения по индексу фильтра.
        /// </summary>
        /// <param name="filterIndex">Индекс фильтра в диалоге сохранения.</param>
        /// <returns>Соответствующий ImageFormat.</returns>
        private ImageFormat GetImageFormat(int filterIndex)
        {
            switch (filterIndex)
            {
                case 1: return ImageFormat.Png;
                case 2: return ImageFormat.Jpeg;
                case 3: return ImageFormat.Bmp;
                default: return ImageFormat.Png;
            }
        }

        #endregion

        #region Командная система (Undo/Redo)

        /// <summary>
        /// Создает блок через командную систему с поддержкой Undo/Redo.
        /// </summary>
        /// <param name="type">Тип создаваемого блока.</param>
        /// <param name="text">Текст блока.</param>
        /// <param name="position">Позиция создания в мировых координатах.</param>
        private void CreateBlockWithCommand(string type, string text, PointF position)
        {
            var block = _blockCreationService.CreateBlockAtPosition(type, text, position);
            var command = new CreateBlockCommand(block, canvas.GetBlocks(), canvas);
            _commandManager.Execute(command);
            canvas?.RaiseElementAdded();
        }

        /// <summary>
        /// Создает стрелку через командную систему с поддержкой Undo/Redo.
        /// </summary>
        /// <param name="position">Позиция создания в мировых координатах.</param>
        private void CreateArrowWithCommand(PointF position)
        {
            var newArrow = new BpmnArrow()
            {
                StartPoint = new PointF(position.X - 40, position.Y - 20),
                EndPoint = new PointF(position.X + 40, position.Y + 20),
                Text = "connection",
                Color = Color.Black,
                Width = 2f
            };

            var command = new CreateArrowCommand(newArrow, canvas.GetArrows(), canvas);
            _commandManager.Execute(command);
            canvas?.RaiseElementAdded();
        }

        /// <summary>
        /// Создает кривую стрелку через командную систему с поддержкой Undo/Redo.
        /// </summary>
        /// <param name="position">Позиция создания в мировых координатах.</param>
        private void CreateCurvedArrowWithCommand(PointF position)
        {
            var newCurvedArrow = new BpmnCurvedArrow()
            {
                StartPoint = new PointF(position.X - 40, position.Y - 20),
                EndPoint = new PointF(position.X + 40, position.Y + 20),
                Text = "curved connection",
                Color = Color.Black,
                Width = 2f,
                IsFloating = true
            };

            newCurvedArrow.CalculateControlPoints();
            var command = new CreateCurvedArrowCommand(newCurvedArrow, canvas.GetCurvedArrows(), canvas);
            _commandManager.Execute(command);
        }

        /// <summary>
        /// Выводит отладочную информацию о состоянии командной системы.
        /// </summary>
        private void DebugCommandState()
        {
            Console.WriteLine($"=== Command Manager State ===");
            Console.WriteLine($"CanUndo: {_commandManager.CanUndo}");
            Console.WriteLine($"CanRedo: {_commandManager.CanRedo}");
            Console.WriteLine($"Blocks count: {canvas.GetBlocks().Count}");
            Console.WriteLine($"Arrows count: {canvas.GetArrows()?.Count ?? 0}");
            Console.WriteLine($"=============================");
        }

        /// <summary>
        /// Обновляет состояние кнопок Undo/Redo на панели инструментов.
        /// </summary>
        private void UpdateUndoRedoButtons()
        {
            UndoBtn.Enabled = _commandManager.CanUndo;
            RedoBtn.Enabled = _commandManager.CanRedo;

            DebugCommandState();
            toolTip.SetToolTip(UndoBtn, _commandManager.CanUndo ? "Отменить (Ctrl+Z)" : "Нечего отменять");
            toolTip.SetToolTip(RedoBtn, _commandManager.CanRedo ? "Повторить (Ctrl+Y)" : "Нечего повторять");
        }

        #endregion

        #region Работа с файлами

        /// <summary>
        /// Сохраняет текущий проект в BPMN файл.
        /// </summary>
        private void SaveBpmnFile()
        {
            try
            {
                var currentBlocks = canvas?.GetBlocks() ?? new List<BpmnBlock>();
                var currentArrows = canvas?.GetArrows() ?? new List<BpmnArrow>();
                var currentCurvedArrows = canvas?.GetCurvedArrows() ?? new List<BpmnCurvedArrow>();

                if (BpmnFileService.CurrentFilePath != null)
                {
                    BpmnFileService.SaveToBpmnFile(currentBlocks, currentArrows, currentCurvedArrows, BpmnFileService.CurrentFilePath);
                }
                else
                {
                    SaveBpmnFileAs();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении BPMN файла:\n{ex.Message}",
                    "Ошибка сохранения", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Сохраняет текущий проект в новый BPMN файл (Сохранить как...).
        /// </summary>
        private void SaveBpmnFileAs()
        {
            try
            {
                var currentBlocks = canvas?.GetBlocks() ?? new List<BpmnBlock>();
                var currentArrows = canvas?.GetArrows() ?? new List<BpmnArrow>();
                var currentCurvedArrows = canvas?.GetCurvedArrows() ?? new List<BpmnCurvedArrow>();

                if (BpmnFileService.SaveAsWithDialog(currentBlocks, currentArrows, currentCurvedArrows))
                {
                    // Перезапуск автосохранения для нового файла
                    if (_autoSaveEnabled)
                    {
                        _autoSaveService.Stop();
                        _autoSaveService.Start(_autoSaveInterval);

                        MessageBox.Show($"✅ Файл сохранен и автосохранение перезапущено для файла: {Path.GetFileName(BpmnFileService.CurrentFilePath)}",
                                      "Сохранение",
                                      MessageBoxButtons.OK,
                                      MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении BPMN файла:\n{ex.Message}",
                    "Ошибка сохранения", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Загружает проект из BPMN файла.
        /// </summary>
        private void LoadBpmnFile()
        {
            var blocksToCheck = canvas?.GetBlocks() ?? new List<BpmnBlock>();
            var arrowsToCheck = canvas?.GetArrows() ?? new List<BpmnArrow>();
            var curvedArrowsToCheck = canvas?.GetCurvedArrows() ?? new List<BpmnCurvedArrow>();

            if (!BpmnFileService.CheckSaveBeforeAction(blocksToCheck, arrowsToCheck, curvedArrowsToCheck))
                return;

            try
            {
                using (OpenFileDialog openDialog = new OpenFileDialog())
                {
                    openDialog.Filter = "BPMN Files (*.bpmn)|*.bpmn|All files (*.*)|*.*";
                    openDialog.FilterIndex = 1;
                    openDialog.Title = "Загрузить BPMN проект";

                    if (openDialog.ShowDialog() == DialogResult.OK)
                    {
                        var (loadedBlocks, loadedArrows, loadedCurvedArrows) = BpmnFileService.LoadFromBpmnFile(openDialog.FileName);

                        if (canvas != null)
                        {
                            var currentBlocks = canvas.GetBlocks();
                            var currentArrows = canvas.GetArrows();
                            var currentCurvedArrows = canvas.GetCurvedArrows();

                            currentBlocks.Clear();
                            currentArrows.Clear();
                            currentCurvedArrows.Clear();

                            currentBlocks.AddRange(loadedBlocks);
                            currentArrows.AddRange(loadedArrows);
                            currentCurvedArrows.AddRange(loadedCurvedArrows);

                            canvas.SetBlocks(currentBlocks);
                            canvas.SetArrows(currentArrows);
                            canvas.SetCurvedArrows(currentCurvedArrows);
                            canvas.ClearSelection();
                            canvas.Invalidate();
                        }

                        string message = $"Проект успешно загружен!\nБлоков: {loadedBlocks.Count}, Связей: {loadedArrows.Count}, Кривых стрелок: {loadedCurvedArrows.Count}";
                        MessageBox.Show(message, "Загрузка завершена", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // Обновление автосохранения
                        if (_autoSaveEnabled && !string.IsNullOrEmpty(BpmnFileService.CurrentFilePath))
                        {
                            _autoSaveService.Stop();
                            _autoSaveService.Start(_autoSaveInterval);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке BPMN файла:\n{ex.Message}",
                    "Ошибка загрузки", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Создает новый пустой проект.
        /// </summary>
        private void NewProject()
        {
            var blocksToCheck = canvas?.GetBlocks() ?? new List<BpmnBlock>();
            var arrowsToCheck = canvas?.GetArrows() ?? new List<BpmnArrow>();
            var curvedArrowsToCheck = canvas?.GetCurvedArrows() ?? new List<BpmnCurvedArrow>();

            if (!BpmnFileService.CheckSaveBeforeAction(blocksToCheck, arrowsToCheck, curvedArrowsToCheck))
                return;

            // Очистка текущего проекта
            if (canvas != null)
            {
                var currentBlocks = canvas.GetBlocks();
                var currentArrows = canvas.GetArrows();
                var currentCurvedArrows = canvas.GetCurvedArrows();

                currentBlocks.Clear();
                currentArrows.Clear();
                currentCurvedArrows.Clear();

                canvas.SetBlocks(currentBlocks);
                canvas.SetArrows(currentArrows);
                canvas.SetCurvedArrows(currentCurvedArrows);
                canvas.ClearSelection();
                canvas.Invalidate();
            }

            // Создание нового проекта
            BpmnFileService.NewProject();

            // Остановка автосохранения
            if (_autoSaveEnabled)
            {
                _autoSaveService.Stop();
                _autoSaveEnabled = false;
                MessageBox.Show("Автосохранение отключено для нового проекта. Включите его после сохранения файла.",
                              "Автосохранение",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Обновляет заголовок окна с информацией о текущем проекте.
        /// </summary>
        private void UpdateWindowTitle()
        {
            this.Text = BpmnFileService.GetWindowTitle();
        }

        #endregion

        #region Обработчики событий формы

        /// <summary>
        /// Обрабатывает изменение размера формы для корректного позиционирования панели инструментов.
        /// </summary>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (panel2 != null)
            {
                panel2.Location = new Point(this.Width - panel2.Width + 18, -18);
            }
        }

        /// <summary>
        /// Обрабатывает закрытие формы с проверкой несохраненных изменений.
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                var currentBlocks = canvas?.GetBlocks() ?? new List<BpmnBlock>();
                var arrows = canvas?.GetArrows() ?? new List<BpmnArrow>();
                var curvedArrows = canvas?.GetCurvedArrows() ?? new List<BpmnCurvedArrow>();

                if (BpmnFileService.HasUnsavedChanges)
                {
                    BpmnFileService.CheckSaveBeforeAction(currentBlocks, arrows, curvedArrows, e);
                }
            }

            // Остановка автосохранения
            _autoSaveService?.Stop();
            _autoSaveService?.Dispose();

            base.OnFormClosing(e);
        }

        #endregion

        #region Методы инициализации

        /// <summary>
        /// Инициализирует всплывающие подсказки для элементов панели инструментов.
        /// </summary>
        private void InitPanel2ToolTips()
        {
            toolTip.AutoPopDelay = 5000;
            toolTip.InitialDelay = 300;
            toolTip.ReshowDelay = 200;
            toolTip.ShowAlways = true;

            toolTip.SetToolTip(btnZoomIn, "Увеличить масштаб (Ctrl + колесо мыши)");
            toolTip.SetToolTip(btnZoomOut, "Уменьшить масштаб (Ctrl + колесо мыши)");
            toolTip.SetToolTip(btnZoomReset, "Сбросить масштаб (100%) и центрировать");
            toolTip.SetToolTip(UndoBtn, "Отменить последнее действие (Ctrl + Z)");
            toolTip.SetToolTip(RedoBtn, "Повторить отменённое действие (Ctrl + Y)");
            toolTip.SetToolTip(SaveAsBpmnButton, "Сохранить диаграмму в файл BPMN");
            toolTip.SetToolTip(LoadFileButton, "Открыть существующий BPMN-файл");
            toolTip.SetToolTip(SaveAsImageButton, "Сохранить текущее окно как изображение (PNG/JPG)");
            toolTip.SetToolTip(InfoButton, "Открыть руководство пользователя и описание возможностей");
        }

        /// <summary>
        /// Выполняет начальную настройку формы после загрузки.
        /// </summary>
        private void Form1_Load(object sender, EventArgs e)
        {
            AddBlocksToSidebar();
            InitPanel2ToolTips();
            UpdateZoomButtonsState(1.0f);
        }

        /// <summary>
        /// Добавляет бесконечный холст на форму и настраивает расположение элементов.
        /// </summary>
        private void AddCanvasToExistingPanels()
        {
            canvas = new InfiniteCanvas()
            {
                Dock = DockStyle.Fill,
                Name = "InfiniteCanvas",
                BackColor = Color.White
            };

            canvas.AllowDrop = true;
            canvas.DragEnter += Canvas_DragEnter;
            canvas.DragDrop += Canvas_DragDrop;

            // Реорганизация элементов управления
            this.Controls.Remove(panel2);
            this.Controls.Add(canvas);
            canvas.SendToBack();
            this.Controls.Add(panel2);

            // Позиционирование панели инструментов
            panel2.Location = new Point(this.Width - panel2.Width + 18, -18);
            panel2.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            panel2.BringToFront();
            sidebar.BringToFront();
            panel2.Visible = true;
            panel2.Show();

            // Отладочный вывод
            Console.WriteLine("Проверка элементов на форме:");
            foreach (Control c in this.Controls)
            {
                Console.WriteLine($"  - {c.Name}, Visible: {c.Visible}, Location: {c.Location}, Size: {c.Size}");
            }
        }

        #endregion

        #region Обработчики событий кнопок

        /// <summary>
        /// Обрабатывает нажатие кнопки для обновления подключения кнопок масштабирования.
        /// </summary>
        private void button6_Click(object sender, EventArgs e)
        {
            ConnectZoomButtons();
        }

        /// <summary>
        /// Обрабатывает нажатие кнопки "Отменить" (Undo).
        /// </summary>
        private void button5_Click(object sender, EventArgs e)
        {
            _commandManager.Undo();
        }

        /// <summary>
        /// Обрабатывает нажатие кнопки "Повторить" (Redo).
        /// </summary>
        private void button3_Click(object sender, EventArgs e)
        {
            _commandManager.Redo();
        }

        /// <summary>
        /// Обрабатывает нажатие кнопки "Сохранить как изображение".
        /// </summary>
        private void SaveAsImageButton_Click(object sender, EventArgs e)
        {
            SaveFormAsImage();
        }

        /// <summary>
        /// Обрабатывает нажатие кнопки "Новый проект".
        /// </summary>
        private void NewProjectButton_Click(object sender, EventArgs e)
        {
            NewProject();
        }

        /// <summary>
        /// Обрабатывает нажатие кнопки "Справка".
        /// </summary>
        private void InfoButton_Click_1(object sender, EventArgs e)
        {
            using (HelpForm helpForm = new HelpForm())
            {
                helpForm.StartPosition = FormStartPosition.CenterParent;
                helpForm.ShowDialog(this);
            }
        }

        /// <summary>
        /// Обрабатывает нажатие кнопки "Сохранить BPMN".
        /// </summary>
        private void SaveAsBpmnButton_Click_1(object sender, EventArgs e)
        {
            SaveBpmnFile();
        }

        /// <summary>
        /// Обрабатывает нажатие кнопки "Загрузить файл".
        /// </summary>
        private void LoadFileButton_Click_1(object sender, EventArgs e)
        {
            LoadBpmnFile();
        }

        /// <summary>
        /// Обрабатывает нажатие кнопки "Настройки".
        /// </summary>
        private void SettingsBtn_Click(object sender, EventArgs e)
        {
            using (var settingsForm = new AutoSaveSettingsForm(_autoSaveEnabled, _autoSaveInterval))
            {
                if (settingsForm.ShowDialog(this) == DialogResult.OK)
                {
                    _autoSaveEnabled = settingsForm.AutoSaveEnabled;
                    _autoSaveInterval = settingsForm.AutoSaveInterval;

                    if (_autoSaveEnabled)
                    {
                        if (string.IsNullOrEmpty(BpmnFileService.CurrentFilePath))
                        {
                            MessageBox.Show("❌ Для автосохранения необходимо сначала сохранить файл через 'Сохранить как...'",
                                          "Автосохранение",
                                          MessageBoxButtons.OK,
                                          MessageBoxIcon.Warning);
                            _autoSaveEnabled = false;
                            return;
                        }

                        _autoSaveService.Start(_autoSaveInterval);
                        MessageBox.Show($"✅ Автосохранение включено\nИнтервал: {_autoSaveInterval} минут\nФайл: {Path.GetFileName(BpmnFileService.CurrentFilePath)}",
                                      "Автосохранение",
                                      MessageBoxButtons.OK,
                                      MessageBoxIcon.Information);
                    }
                    else
                    {
                        _autoSaveService.Stop();
                        MessageBox.Show("❌ Автосохранение отключено",
                                      "Автосохранение",
                                      MessageBoxButtons.OK,
                                      MessageBoxIcon.Information);
                    }
                }
            }
        }

        /// <summary>
        /// Обрабатывает нажатие кнопки меню для анимации боковой панели.
        /// </summary>
        private void menuButton_Click(object sender, EventArgs e)
        {
            sidebarTimer.Start();
        }

        private void menuButton_Click_1(object sender, EventArgs e)
        {
            // Заглушка для обработчика
        }

        #endregion
    }

    /// <summary>
    /// Статический класс с методами расширения для элементов управления.
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Устанавливает скругленную форму с рамкой для элемента управления.
        /// </summary>
        /// <param name="control">Элемент управления для оформления.</param>
        /// <param name="radius">Радиус скругления углов.</param>
        /// <param name="borderColor">Цвет рамки.</param>
        /// <param name="borderWidth">Толщина рамки.</param>
        public static void SetRoundedShapeWithBorder(this Control control, int radius, Color borderColor, int borderWidth)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddLine(radius, 0, control.Width - radius, 0);
            path.AddArc(control.Width - radius, 0, radius, radius, 270, 90);
            path.AddLine(control.Width, radius, control.Width, control.Height - radius);
            path.AddArc(control.Width - radius, control.Height - radius, radius, radius, 0, 90);
            path.AddLine(control.Width - radius, control.Height, radius, control.Height);
            path.AddArc(0, control.Height - radius, radius, radius, 90, 90);
            path.AddLine(0, control.Height - radius, 0, radius);
            path.AddArc(0, 0, radius, radius, 180, 90);
            path.CloseFigure();
            control.Region = new Region(path);

            control.Paint += (sender, e) =>
            {
                Control ctrl = (Control)sender;
                using (Pen borderPen = new Pen(borderColor, borderWidth))
                {
                    borderPen.Alignment = PenAlignment.Inset;
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.DrawPath(borderPen, path);
                }
            };
        }
    }
}