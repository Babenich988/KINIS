using Kinis.Models;
using Kinis.Services;
using System;
using System.Collections;
using System.Collections.Generic;
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
    public partial class Form1 : Form
    {
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
        private const int KEY_COOLDOWN_MS = 1000; // 1000ms задержка между нажатиями
        private CommandManager _commandManager;

        // Автосохранение
        private AutoSaveService _autoSaveService;
        private bool _autoSaveEnabled = false;
        private int _autoSaveInterval = 5; // минут по умолчанию

        public CommandManager CommandManager => _commandManager;
        // максимальное количество листов (поменяйте при необходимости)
        private const int MAX_SHEETS = 5;

        // менеджер листов
        private List<CanvasSheet> sheets = new List<CanvasSheet>();
        private int currentSheetIndex = -1;
        // вычисляемая ширина для раскрытого меню (автоматически подстраивается)
        private int GetMaxSidebarBlockWidth()
        {
            int margin = 8;
            int visible = sidebar.ClientSize.Width;
            if (visible <= 0) visible = sidebar.Width;
            return Math.Max(20, visible - 2 * margin);
        }

        public Form1()
        {
            InitializeComponent();

            sidebar.Width = sidebar.MinimumSize.Width;
            sidebarExpand = false;
            menuButton.Click += (s, e) => sidebarTimer.Start();
            AddCanvasToExistingPanels();
            panelFigures.FlowDirection = FlowDirection.TopDown;
            panelFigures.WrapContents = false;
            panelFigures.AutoScroll = true;
            panelFigures.Dock = DockStyle.Fill;
            panelFigures.Padding = new Padding(8);
            this.MouseDown += (s, e) =>
            {
                if (selectedSidebarBlock != null)
                {
                    selectedSidebarBlock = null;
                    sidebarPreviewPanel?.Invalidate();
                }
            };
            //Подключаем обработчики кнопок зума
            ConnectZoomButtons();
            //Инициализация ToolTip
            toolTip = new ToolTip();
            toolTip.AutoPopDelay = 5000;
            toolTip.InitialDelay = 500;
            toolTip.ReshowDelay = 100;

            // Подписываемся на события зума канваса
            if (canvas != null)
            {
                canvas.ZoomChanged += (zoom) => UpdateZoomButtonsState(zoom);
            }
            // Инициализация сервиса создания блоков
            _blockCreationService = new BlockCreationService(canvas, canvas.GetBlocks());

            // Включаем обработку клавиш
            this.KeyPreview = true;
            // Подписываемся на событие KeyDown (добавляем эту строку)
            this.KeyDown += Form1_KeyDown;
            // Инициализация менеджера команд
            _commandManager = new CommandManager();
            _commandManager.OnStateChanged += UpdateUndoRedoButtons;
            UpdateUndoRedoButtons();
            // Подписываемся на событие KeyUp
            this.KeyUp += Form1_KeyUp;
            // ПОДПИСЫВАЕМСЯ НА СОБЫТИЯ BpmnFileService
            BpmnFileService.ProjectModified += (s, e) => UpdateWindowTitle();
            BpmnFileService.ProjectSaved += (s, e) => UpdateWindowTitle();
            BpmnFileService.ProjectLoaded += (s, e) => UpdateWindowTitle();

            // Подписываемся на события изменения в canvas
            if (canvas != null)
            {
                canvas.BlockModified += (s, e) => BpmnFileService.MarkAsModified();
                canvas.ArrowModified += (s, e) => BpmnFileService.MarkAsModified();
                canvas.ElementAdded += (s, e) => BpmnFileService.MarkAsModified();
            }

            // Подписываем команды на отслеживание изменений
            _commandManager.OnStateChanged += () =>
            {
                // Если есть команды в стеке - значит были изменения
                if (_commandManager.CanUndo)
                {
                    BpmnFileService.MarkAsModified();
                }
            };

            // Инициализация сервиса автосохранения
            InitializeAutoSaveService();
        }

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
                // Можно добавить уведомление в статус бар если нужно
                Console.WriteLine($"Автосохранение выполнено в {time}");
            };
        }

        private void InfoButton_Click(object sender, EventArgs e)
        {
            // Показываем окно настроек автосохранения
            using (var settingsForm = new AutoSaveSettingsForm(_autoSaveEnabled, _autoSaveInterval))
            {
                if (settingsForm.ShowDialog(this) == DialogResult.OK)
                {
                    _autoSaveEnabled = settingsForm.AutoSaveEnabled;
                    _autoSaveInterval = settingsForm.AutoSaveInterval;

                    if (_autoSaveEnabled)
                    {
                        // ПРОВЕРЯЕМ, ЧТО ФАЙЛ УЖЕ СОХРАНЕН
                        if (string.IsNullOrEmpty(BpmnFileService.CurrentFilePath))
                        {
                            MessageBox.Show("❌ Для автосохранения необходимо сначала сохранить файл через 'Сохранить как...'",
                                          "Автосохранение",
                                          MessageBoxButtons.OK,
                                          MessageBoxIcon.Warning);
                            _autoSaveEnabled = false;
                            return;
                        }

                        // Запускаем автосохранение
                        _autoSaveService.Start(_autoSaveInterval);
                        MessageBox.Show($"✅ Автосохранение включено\nИнтервал: {_autoSaveInterval} минут\nФайл: {Path.GetFileName(BpmnFileService.CurrentFilePath)}",
                                      "Автосохранение",
                                      MessageBoxButtons.OK,
                                      MessageBoxIcon.Information);
                    }
                    else
                    {
                        // Останавливаем автосохранение
                        _autoSaveService.Stop();
                        MessageBox.Show("❌ Автосохранение отключено",
                                      "Автосохранение",
                                      MessageBoxButtons.OK,
                                      MessageBoxIcon.Information);
                    }
                }
            }
        }

        private BpmnBlock CloneBlock(BpmnBlock src)
        {
            // Копируем все полям, которые у вас есть в модели BpmnBlock.
            // При необходимости добавьте/скорректируйте поля.
            var nb = new BpmnBlock(src.Bounds.X, src.Bounds.Y, src.Bounds.Width, src.Bounds.Height)
            {
                Id = src.Id,               // если нужно - можно генерировать новый Id
                Text = src.Text,
                Type = src.Type,
                FillColor = src.FillColor,
                BorderColor = src.BorderColor
                // ... добавьте другие свойства из вашей модели BpmnBlock
            };
            return nb;
        }

        private BpmnArrow CloneArrow(BpmnArrow src)
        {
            var na = new BpmnArrow()
            {
                Id = src.Id,
                Text = src.Text,
                Color = src.Color,
                Width = src.Width
                // IsFloating - вычисляемое свойство, НЕ копируем его
                // ... при необходимости другие поля
            };

            // Клонируем точки (StartPoint/EndPoint и ConnectionPoints)
            na.StartPoint = src.StartPoint;
            na.EndPoint = src.EndPoint;
            na.ConnectionPoints = new System.Collections.Generic.List<PointF>(src.ConnectionPoints);

            // НЕ привязываем StartBlock/EndBlock к оригиналам (потом при загрузке мы привяжем блоки по Id, если нужно)
            na.StartBlock = null;
            na.EndBlock = null;

            return na;
        }
        private void button6_Click(object sender, EventArgs e)
        {

            ConnectZoomButtons();
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            // Проверяем, что нет активного TextBox для редактирования
            if (canvas.IsEditingText())
                return;

            // Горячие клавиши Undo/Redo - ДОБАВЛЯЕМ В НАЧАЛО
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

            var keyMappings = _blockCreationService.GetBlockKeyMappings();

            if (keyMappings.ContainsKey(e.KeyCode))
            {
                // ПРОВЕРКА ЗАДЕРЖКИ: блокируем повторное создание того же блока в течение KEY_COOLDOWN_MS
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

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            // Сбрасываем состояние задержки при отпускании клавиши
            // Это позволяет быстро переключаться между разными горячими клавишами
            var keyMappings = _blockCreationService.GetBlockKeyMappings();

            if (keyMappings.ContainsKey(e.KeyCode))
            {
                // Если отпустили клавишу, которая была последней обработанной - сбрасываем
                if (e.KeyCode == _lastProcessedKey)
                {
                    // Можно сбросить сразу или через небольшой таймаут
                    // _lastProcessedKey = Keys.None;
                }
            }
        }

        private void CreateBlockWithHotkey(Keys key)
        {
            // Получаем позицию курсора в виртуальных координатах через публичный метод
            PointF virtualPos = canvas.GetCursorVirtualPosition();

            var keyMappings = _blockCreationService.GetBlockKeyMappings();

            if (keyMappings.ContainsKey(key))
            {
                var mapping = keyMappings[key];

                if (mapping.Type == "Arrow")
                {
                    // СОЗДАЕМ СТРЕЛКУ ЧЕРЕЗ КОМАНДУ
                    CreateArrowWithCommand(virtualPos);
                    return;
                }
                else if (mapping.Type == "CurvedArrow") // ДОБАВЛЯЕМ проверку для кривых стрелок
                {
                    // СОЗДАЕМ КРИВУЮ СТРЕЛКУ ЧЕРЕЗ КОМАНДУ
                    CreateCurvedArrowWithCommand(virtualPos);
                    return;
                }

                // ИСПОЛЬЗУЕМ КОМАНДУ для блоков
                CreateBlockWithCommand(mapping.Type, mapping.Text, virtualPos);
                Console.WriteLine($"Block created via command: {mapping.Text} at {virtualPos}");
            }
        }

        private Panel sidebarPreviewPanel;
        private List<BpmnBlock> sidebarBlocks = new List<BpmnBlock>();

        private void SidebarPreviewPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (sidebarBlocks == null || sidebarBlocks.Count == 0) return;

            // Получаем текущее смещение скролла
            Point scrollOffset = sidebarPreviewPanel.AutoScrollPosition;

            foreach (var block in sidebarBlocks)
            {
                // Смещаем координаты блока на текущий scroll
                RectangleF drawRect = new RectangleF(
                    block.Bounds.X + scrollOffset.X,
                    block.Bounds.Y + scrollOffset.Y,
                    block.Bounds.Width,
                    block.Bounds.Height);

                using (var brush = new SolidBrush(block.FillColor))
                    g.FillRectangle(brush, drawRect);

                using (var pen = new Pen(block.BorderColor, 1))
                    g.DrawRectangle(pen, drawRect.X, drawRect.Y, drawRect.Width, drawRect.Height);

                float fontSize = Math.Max(8f, Math.Min(12f, drawRect.Height / 6f + drawRect.Width / 60f));
                using (var font = new Font("Segoe UI", fontSize))
                using (var textBrush = new SolidBrush(Color.Black))
                {
                    var textSize = g.MeasureString(block.Text, font);
                    float textX = drawRect.X + (drawRect.Width - textSize.Width) / 2f;
                    float textY = drawRect.Y + (drawRect.Height - textSize.Height) / 2f;
                    g.DrawString(block.Text, font, textBrush, textX, textY);
                }

                if (block == selectedSidebarBlock)
                {
                    using (var pen = new Pen(Color.DeepSkyBlue, 3))
                    {
                        g.DrawRectangle(pen, drawRect.X - 1, drawRect.Y - 1, drawRect.Width + 2, drawRect.Height + 2);
                    }
                }
            }
        }

        private void SidebarPreviewPanel_MouseDown(object sender, MouseEventArgs e)
        {
            // Берем смещение скролла
            Point scrollOffset = sidebarPreviewPanel.AutoScrollPosition;

            // Применяем смещение к координатам клика
            Point adjustedClick = new Point(
                e.X - scrollOffset.X,
                e.Y - scrollOffset.Y
            );

            // Проверяем, какой блок был нажат
            foreach (var block in sidebarBlocks)
            {
                if (block.Bounds.Contains(adjustedClick))
                {
                    selectedSidebarBlock = block;
                    sidebarPreviewPanel.Invalidate();

                    // начинаем возможное перетаскивание
                    isDraggingFromSidebar = true;
                    dragStartPoint = adjustedClick;
                    return;
                }
            }

            selectedSidebarBlock = null;
            sidebarPreviewPanel.Invalidate();
            isDraggingFromSidebar = false;
        }
        // Двигаем мышь — показываем, что идёт перетаскивание
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
                    // СОЗДАЕМ ПРАВИЛЬНЫЕ ДАННЫЕ ДЛЯ DRAG&DROP
                    var data = new DataObject();

                    if (selectedSidebarBlock.Type == "Arrow")
                    {
                        // ДЛЯ СТРЕЛКИ - ОТПРАВЛЯЕМ СПЕЦИАЛЬНЫЙ ФЛАГ
                        data.SetData("BpmnElementType", "Arrow");
                        data.SetData("BpmnBlock", selectedSidebarBlock);
                    }
                    else if (selectedSidebarBlock.Type == "CurvedArrow") // ДОБАВЛЯЕМ для кривых стрелок
                    {
                        // ДЛЯ КРИВОЙ СТРЕЛКИ - ОТПРАВЛЯЕМ СПЕЦИАЛЬНЫЙ ФЛАГ
                        data.SetData("BpmnElementType", "CurvedArrow");
                        data.SetData("BpmnBlock", selectedSidebarBlock);
                    }
                    else
                    {
                        // ДЛЯ БЛОКОВ - СТАНДАРТНЫЙ ФОРМАТ
                        data.SetData("BpmnElementType", "Block");
                        data.SetData("BpmnBlock", selectedSidebarBlock);
                    }

                    sidebarPreviewPanel.DoDragDrop(data, DragDropEffects.Copy);
                    isDraggingFromSidebar = false;
                }
            }
        }
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
                        // СОЗДАЕМ СТРЕЛКУ ЧЕРЕЗ КОМАНДУ
                        CreateArrowWithCommand(GetCanvasCenterWorldPoint());
                        return;
                    }
                    else if (block.Type == "CurvedArrow") // ДОБАВЛЯЕМ для кривых стрелок
                    {
                        // СОЗДАЕМ КРИВУЮ СТРЕЛКУ ЧЕРЕЗ КОМАНДУ
                        CreateCurvedArrowWithCommand(GetCanvasCenterWorldPoint());
                        return;
                    }
                    else
                    {
                        // СОЗДАЕМ БЛОК ЧЕРЕЗ КОМАНДУ
                        BpmnBlock newBlock = new BpmnBlock(0, 0, 120, 80)
                        {
                            Text = block.Text,
                            Type = block.Type,
                            FillColor = block.FillColor,
                            BorderColor = block.BorderColor,
                            Id = Guid.NewGuid().ToString()
                        };

                        // Определяем позицию для нового блока
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

                        // ИСПОЛЬЗУЕМ КОМАНДУ
                        var command = new CreateBlockCommand(newBlock, canvas.GetBlocks(), canvas);
                        _commandManager.Execute(command);
                        Console.WriteLine($"CreateBlockCommand executed via double-click: {newBlock.Text}");
                        return;
                    }
                }
            }
        }
        private void SidebarPreviewPanel_MouseUp(object sender, MouseEventArgs e)
        {
            isDraggingFromSidebar = false;
        }

        private void AddBlocksToSidebar()
        {
            if (sidebarPreviewPanel != null && sidebar.Controls.Contains(sidebarPreviewPanel))
                sidebar.Controls.Remove(sidebarPreviewPanel);

            // Создаем панель с автоскроллом
            sidebarPreviewPanel = new Panel
            {
                Name = "SidebarPreviewPanel",
                BackColor = Color.Transparent,
                Width = sidebar.ClientSize.Width,
                Height = sidebar.Height - 120,
                Margin = new Padding(0),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                AutoScroll = true // Включаем автоскролл
            };

            sidebar.Controls.Add(sidebarPreviewPanel);
            sidebarPreviewPanel.Width = Math.Max(20, sidebar.ClientSize.Width);
            // Добавляем DRAG&DROP для стрелок
            sidebarPreviewPanel.AllowDrop = true;
            // Создаём мини-блоки с минимальными размерами
            int AddY(int index) => 8 + (miniMinHeight + 12) * index;
            // Мини-блоки для панели
            sidebarBlocks = new List<BpmnBlock>
            {
                new BpmnBlock(8, AddY(0), miniMinWidth, miniMinHeight)
                    { Text = "Комментарий", Type = "Комментарий" },

                new BpmnBlock(8, AddY(1), miniMinWidth, miniMinHeight)
                    { Text = "Задача", Type = "Задача" },

                new BpmnBlock(8, 8 + (miniMinHeight + 12) * 2, miniMinWidth, miniMinHeight)
                    { Text = "↷", Type = "CurvedArrow", FillColor = Color.LightBlue, BorderColor = Color.DarkBlue },

                new BpmnBlock(8, 8 + (miniMinHeight + 12) * 3, miniMinWidth, miniMinHeight)
                    { Text = "→", Type = "Arrow", FillColor = Color.LightGray, BorderColor = Color.DarkGray },

                new BpmnBlock(8, AddY(2), miniMinWidth, miniMinHeight)
                    { Text = "Развилка", Type = "Развилка" },

                new BpmnBlock(8, AddY(3), miniMinWidth, miniMinHeight)
                    { Text = "Развилка И", Type = "Развилка И" },

                new BpmnBlock(8, AddY(4), miniMinWidth, miniMinHeight)
                    { Text = "Начальное событие", Type = "Начальное событие" },

                new BpmnBlock(8, AddY(5), miniMinWidth, miniMinHeight)
                    { Text = "Промежуточное событие", Type = "Промежуточное событие" },

                new BpmnBlock(8, AddY(6), miniMinWidth, miniMinHeight)
                    { Text = "Конечное событие", Type = "Конечное событие" },

                new BpmnBlock(8, AddY(7), miniMinWidth, miniMinHeight)
                    { Text = "Объект данных", Type = "Объект данных" },

                new BpmnBlock(8, AddY(8), miniMinWidth, miniMinHeight)
                    { Text = "Хранилище данных", Type = "Хранилище данных" },

                // 📨 Новые события (получение/отправка)
                new BpmnBlock(8, AddY(9), miniMinWidth, miniMinHeight)
                    { Text = "Получ. сообщ. (нач.)", Type = "Событие-получение сообщения" },

                new BpmnBlock(8, AddY(10), miniMinWidth, miniMinHeight)
                    { Text = "Получ. сообщ. (пром.)", Type = "Событие-получение сообщения (промежуточное)" },

                new BpmnBlock(8, AddY(11), miniMinWidth, miniMinHeight)
                    { Text = "Отпр. сообщ. (пром.)", Type = "Событие-отправка сообщения (промежуточное)" },

                new BpmnBlock(8, AddY(12), miniMinWidth, miniMinHeight)
                    { Text = "Отпр. сообщ. (кон.)", Type = "Событие-отправка сообщения" },

                // ⚠ Ошибка
                new BpmnBlock(8, AddY(13), miniMinWidth, miniMinHeight)
                    { Text = "Ошибка (обр.)", Type = "Событие-ошибка обработчик" },

                new BpmnBlock(8, AddY(14), miniMinWidth, miniMinHeight)
                    { Text = "Ошибка (иниц.)", Type = "Событие-ошибка инициатор" },

                // ❌ Отмена
                new BpmnBlock(8, AddY(15), miniMinWidth, miniMinHeight)
                    { Text = "Отмена (обр.)", Type = "Событие-отмена обработчик" },

                new BpmnBlock(8, AddY(16), miniMinWidth, miniMinHeight)
                    { Text = "Отмена (иниц.)", Type = "Событие-отмена инициатор" },

                // ⛔ Остановка
                new BpmnBlock(8, AddY(17), miniMinWidth, miniMinHeight)
                    { Text = "Остановка", Type = "Событие-остановка" }
            };


            // Подписываем обработчики
            sidebarPreviewPanel.Paint += SidebarPreviewPanel_Paint;
            sidebarPreviewPanel.MouseDoubleClick += SidebarPreviewPanel_MouseDoubleClick;
            sidebarPreviewPanel.MouseDown += SidebarPreviewPanel_MouseDown;
            sidebarPreviewPanel.MouseMove += SidebarPreviewPanel_MouseMove;
            sidebarPreviewPanel.MouseUp += SidebarPreviewPanel_MouseUp;

            // Перерисовка с учетом скролла
            UpdateSidebarBlocksSize();
        }
        private void SidebarPreviewPanel_MouseWheel(object sender, MouseEventArgs e)//обработчик прокрутки
        {
            Panel panel = sender as Panel;
            if (panel == null) return;

            int scrollStep = 20; // насколько пикселей прокручиваем за один "шаг" колеса
            int newValue = panel.VerticalScroll.Value - e.Delta / 120 * scrollStep;

            // Ограничиваем прокрутку в допустимых пределах
            if (newValue < panel.VerticalScroll.Minimum)
                newValue = panel.VerticalScroll.Minimum;
            if (newValue > panel.VerticalScroll.Maximum)
                newValue = panel.VerticalScroll.Maximum;

            panel.AutoScrollPosition = new Point(panel.AutoScrollPosition.X, newValue);
        }

        // Анимация открытия/закрытия боковой панели
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

        private float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

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

        private void UpdateSidebarBlocksSize()
        {
            if (sidebarPreviewPanel == null || sidebarBlocks == null || sidebarBlocks.Count == 0)
            {
                return;
            }

            float scale = GetSidebarScale(); // 0 = свернуто, 1 = развернуто
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

            // !!! добавляем запас для нижнего блока, чтобы он полностью прокручивался
            int totalHeight = (int)y + margin;

            sidebarPreviewPanel.AutoScrollMinSize = new Size(0, totalHeight);

            sidebarPreviewPanel.Invalidate();
        }
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
        private void Form1_Load(object sender, EventArgs e)
        {
            AddBlocksToSidebar();
            //Инициализация состояния кнопок зума
            UpdateZoomButtonsState(1.0f); // Начальный зум 100%
        }

        private void menuButton_Click(object sender, EventArgs e)
        {
            sidebarTimer.Start();
        }

        private void AddCanvasToExistingPanels()
        {
            canvas = new InfiniteCanvas()
            {
                Dock = DockStyle.Fill,
                Name = "InfiniteCanvas",
                BackColor = Color.White
            };
            //canvas.MouseDown += Canvas_MouseDown; // клик по холсту снимает выделение блока
            canvas.AllowDrop = true;
            canvas.DragEnter += Canvas_DragEnter;
            canvas.DragDrop += Canvas_DragDrop;
            this.Controls.Remove(panel2);

            this.Controls.Add(canvas);
            canvas.SendToBack();

            this.Controls.Add(panel2);

            panel2.Location = new Point(this.Width - panel2.Width - -18, -18);
            panel2.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            panel2.BringToFront();
            sidebar.BringToFront();

            panel2.Visible = true;
            panel2.Show();

            Console.WriteLine("Проверка элементов на форме:");
            foreach (Control c in this.Controls)
            {
                Console.WriteLine($"  - {c.Name}, Visible: {c.Visible}, Location: {c.Location}, Size: {c.Size}");
            }
        }

        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (selectedSidebarBlock != null)
            {
                selectedSidebarBlock = null;
                if (sidebarPreviewPanel != null)
                    sidebarPreviewPanel.Invalidate();
            }
        }

        // Разрешаем перенос только наших блоков и стрелок
        private void Canvas_DragEnter(object sender, DragEventArgs e)
        {
            // РАЗРЕШАЕМ И НОВЫЙ, И СТАРЫЙ ФОРМАТЫ ДАННЫХ
            if (e.Data.GetDataPresent(typeof(BpmnBlock)) || e.Data.GetDataPresent("BpmnElementType"))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void Canvas_DragDrop(object sender, DragEventArgs e)
        {
            Point clientPoint = canvas.PointToClient(new Point(e.X, e.Y));
            PointF worldPoint = canvas.ScreenToWorld(clientPoint);

            // ПРОВЕРЯЕМ НОВЫЙ ФОРМАТ ДАННЫХ С BpmnElementType
            if (e.Data.GetDataPresent("BpmnElementType"))
            {
                string elementType = (string)e.Data.GetData("BpmnElementType");

                if (elementType == "Arrow")
                {
                    // СОЗДАЕМ СТРЕЛКУ ЧЕРЕЗ КОМАНДУ
                    CreateArrowWithCommand(worldPoint);
                    return;
                }
                else if (elementType == "CurvedArrow") // ДОБАВЛЯЕМ для кривых стрелок
                {
                    // СОЗДАЕМ КРИВУЮ СТРЕЛКУ ЧЕРЕЗ КОМАНДУ
                    CreateCurvedArrowWithCommand(worldPoint);
                    return;
                }
                else if (elementType == "Block" && e.Data.GetDataPresent("BpmnBlock"))
                {
                    // СОЗДАЕМ БЛОК ИЗ НОВОГО ФОРМАТА
                    var blockFromSidebar = (BpmnBlock)e.Data.GetData("BpmnBlock");
                    CreateBlockFromDragDrop(blockFromSidebar, worldPoint);
                    return;
                }
            }

            // СТАРАЯ ЛОГИКА ДЛЯ СОВМЕСТИМОСТИ
            if (e.Data.GetDataPresent(typeof(BpmnBlock)))
            {
                var blockFromSidebar = (BpmnBlock)e.Data.GetData(typeof(BpmnBlock));

                // ДОБАВЛЯЕМ проверку типа для кривых стрелок
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

        // ВЫНОСИМ ЛОГИКУ СОЗДАНИЯ БЛОКА В ОТДЕЛЬНЫЙ МЕТОД
        private void CreateBlockFromDragDrop(BpmnBlock blockFromSidebar, PointF worldPoint)
        {

            // Для пула используем специальные размеры
            if (blockFromSidebar.Type == "Пул")
            {
                // СОЗДАЕМ ПУЛ с нормальными размерами
                var poolBlock = new BpmnBlock(worldPoint.X, worldPoint.Y, 400, 200)
                {
                    Text = blockFromSidebar.Text,
                    Type = "Пул",
                    FillColor = Color.White,
                    BorderColor = Color.Black,
                    Id = Guid.NewGuid().ToString()
                };

                // ИНИЦИАЛИЗИРУЕМ ДОРОЖКИ ДЛЯ ПУЛА
                poolBlock.InitializePoolLanes();

                System.Diagnostics.Debug.WriteLine($"Создан пул: Size={poolBlock.Bounds.Size}, Position={poolBlock.Bounds.Location}");

                // ИСПОЛЬЗУЕМ КОМАНДУ с другим именем переменной
                var poolCommand = new CreateBlockCommand(poolBlock, canvas.GetBlocks(), canvas);
                _commandManager.Execute(poolCommand);

                Console.WriteLine($"CreateBlockCommand executed for Pool: {poolBlock.Text}");

                // ВЫЗЫВАЕМ СОБЫТИЕ ДОБАВЛЕНИЯ ЭЛЕМЕНТА
                canvas?.RaiseElementAdded();
                return;
            }

            var newBlock = new BpmnBlock(worldPoint.X, worldPoint.Y,
                blockFromSidebar.Bounds.Width, blockFromSidebar.Bounds.Height)
            {
                Text = blockFromSidebar.Text,
                Type = blockFromSidebar.Type,
                FillColor = blockFromSidebar.FillColor,
                BorderColor = blockFromSidebar.BorderColor,
                Id = Guid.NewGuid().ToString()
            };

            // ИСПОЛЬЗУЕМ КОМАНДУ с другим именем переменной
            var blockCommand = new CreateBlockCommand(newBlock, canvas.GetBlocks(), canvas);
            _commandManager.Execute(blockCommand);

            Console.WriteLine($"CreateBlockCommand executed via drag&drop: {newBlock.Text}");

            // ВЫЗЫВАЕМ СОБЫТИЕ ДОБАВЛЕНИЯ ЭЛЕМЕНТА
            canvas?.RaiseElementAdded();
        }
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

        /// <summary>
        /// Создает новую стрелку на холсте
        /// </summary>
        private void CreateArrowOnCanvas()
        {
            PointF center = GetCanvasCenterWorldPoint();

            // Создаем стрелку в центре экрана
            var newArrow = new BpmnArrow()
            {
                StartPoint = new PointF(center.X - 50, center.Y),
                EndPoint = new PointF(center.X + 50, center.Y),
                Text = "connection",
                Color = Color.Black,
                Width = 2f
            };

            // ИСПОЛЬЗУЕМ КОМАНДУ вместо прямого добавления
            var command = new CreateArrowCommand(newArrow, canvas.GetArrows(), canvas);
            _commandManager.Execute(command);

            Console.WriteLine($"CreateArrowCommand executed via method at {center}");

            // ВЫЗЫВАЕМ СОБЫТИЕ ДОБАВЛЕНИЯ ЭЛЕМЕНТА
            canvas?.RaiseElementAdded();
        }
        // Метод для подключения кнопок зума
        private void ConnectZoomButtons()
        {
            btnZoomIn.Click += (s, e) => canvas.ZoomIn();
            btnZoomOut.Click += (s, e) => canvas.ZoomOut();
            btnZoomReset.Click += (s, e) =>
            {
                // ВЫЗЫВАЕМ ResetZoom ВМЕСТО ResetView ДЛЯ ФОКУСИРОВКИ
                canvas.ResetZoom();
            };
        }

        /// <summary>
        /// Обновляет состояние кнопок зума в зависимости от текущего масштаба
        /// </summary>
        private void UpdateZoomButtonsState(float currentZoom)
        {
            // Кнопка ZoomIn - отключается при максимальном зуме
            btnZoomIn.Enabled = currentZoom < MAX_ZOOM;

            // Кнопка ZoomOut - отключается при минимальном зуме  
            btnZoomOut.Enabled = currentZoom > MIN_ZOOM;

            // Кнопка Reset - всегда активна
            btnZoomReset.Enabled = true;

            // Обновляем ToolTip подсказки
            UpdateZoomToolTips(currentZoom);
        }

        /// <summary>
        /// Обновляет всплывающие подсказки для кнопок зума
        /// </summary>
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

                    // Сохраняем только область холста с элементами
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

        private void SaveCanvasAsImage(string filePath, ImageFormat format)
        {
            if (canvas == null) return;

            // Получаем границы всех элементов на холсте
            RectangleF elementsBounds = CalculateElementsBounds();

            // Добавляем отступы вокруг элементов
            float padding = 50f; // 50 пикселей отступ от границ
            RectangleF imageBounds = new RectangleF(
                elementsBounds.X - padding,
                elementsBounds.Y - padding,
                elementsBounds.Width + padding * 2,
                elementsBounds.Height + padding * 2
            );

            // Если нет элементов, создаем изображение минимального размера
            if (imageBounds.Width <= 0 || imageBounds.Height <= 0)
            {
                imageBounds = new RectangleF(0, 0, 800, 600); // Размер по умолчанию
            }

            // Создаем bitmap с вычисленными размерами
            using (Bitmap bitmap = new Bitmap((int)Math.Ceiling(imageBounds.Width),
                                            (int)Math.Ceiling(imageBounds.Height)))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.Clear(Color.White);

                    // Смещаем координаты так, чтобы элементы попали в изображение
                    g.TranslateTransform(-imageBounds.X, -imageBounds.Y);

                    // Отрисовываем сетку
                    DrawGridForExport(g, imageBounds);

                    // Отрисовываем элементы в правильном порядке
                    DrawElementsForExport(g);
                }

                // Сохраняем изображение
                bitmap.Save(filePath, format);
            }
        }

        private RectangleF CalculateElementsBounds()
        {
            if (canvas == null) return new RectangleF(0, 0, 0, 0);

            var blocks = canvas.GetBlocks();
            var arrows = canvas.GetArrows();
            var curvedArrows = canvas.GetCurvedArrows();

            // Если нет элементов, возвращаем пустую область
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

            // Обрабатываем блоки
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

            // Обрабатываем обычные стрелки
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

            // Обрабатываем кривые стрелки
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

            // Если все значения остались по умолчанию, возвращаем пустую область
            if (minX == float.MaxValue) return new RectangleF(0, 0, 0, 0);

            return new RectangleF(minX, minY, maxX - minX, maxY - minY);
        }

        private void DrawGridForExport(Graphics g, RectangleF bounds)
        {
            int gridSize = 20;
            Color gridColor = Color.FromArgb(240, 240, 240); // Светло-серый

            // Расширяем область отрисовки сетки на 2 сетки за границами элементов
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

        private void DrawElementsForExport(Graphics g)
        {
            if (canvas == null) return;

            var blocks = canvas.GetBlocks();
            var arrows = canvas.GetArrows();
            var curvedArrows = canvas.GetCurvedArrows();

            // Сначала рисуем стрелки (под блоками)
            if (arrows != null)
            {
                foreach (var arrow in arrows)
                {
                    arrow.Draw(g, false); // false - не выделенные
                }
            }

            // Затем кривые стрелки
            if (curvedArrows != null)
            {
                foreach (var curvedArrow in curvedArrows)
                {
                    curvedArrow.Draw(g, false); // false - не выделенные
                }
            }

            // Затем блоки (поверх стрелок)
            if (blocks != null)
            {
                foreach (var block in blocks)
                {
                    block.Draw(g, false); // false - не выделенные
                }
            }
        }

        private ImageFormat GetImageFormat(int filterIndex)
        {
            switch (filterIndex)
            {
                case 1:
                    return ImageFormat.Png;
                case 2:
                    return ImageFormat.Jpeg;
                case 3:
                    return ImageFormat.Bmp;
                default:
                    return ImageFormat.Png;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (panel2 != null)
            {
                panel2.Location = new Point(this.Width - panel2.Width - -18, -18);
            }
        }

        private void SaveAsImageButton_Click(object sender, EventArgs e)
        {
            SaveFormAsImage();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            _commandManager.Undo();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            _commandManager.Redo();
        }
        private void DebugCommandState()
        {
            Console.WriteLine($"=== Command Manager State ===");
            Console.WriteLine($"CanUndo: {_commandManager.CanUndo}");
            Console.WriteLine($"CanRedo: {_commandManager.CanRedo}");
            Console.WriteLine($"Blocks count: {canvas.GetBlocks().Count}");
            Console.WriteLine($"Arrows count: {canvas.GetArrows()?.Count ?? 0}");
            Console.WriteLine($"=============================");
        }
        private void UpdateUndoRedoButtons()
        {
            UndoBtn.Enabled = _commandManager.CanUndo;
            RedoBtn.Enabled = _commandManager.CanRedo;

            // ОТЛАДКА
            DebugCommandState();
            toolTip.SetToolTip(UndoBtn, _commandManager.CanUndo ? "Отменить (Ctrl+Z)" : "Нечего отменять");
            toolTip.SetToolTip(RedoBtn, _commandManager.CanRedo ? "Повторить (Ctrl+Y)" : "Нечего повторять");
        }
        private void CreateBlockWithCommand(string type, string text, PointF position)
        {
            var block = _blockCreationService.CreateBlockAtPosition(type, text, position);
            var command = new CreateBlockCommand(block, canvas.GetBlocks(), canvas);
            _commandManager.Execute(command);

            // ВЫЗЫВАЕМ СОБЫТИЕ ДОБАВЛЕНИЯ ЭЛЕМЕНТА
            canvas?.RaiseElementAdded();
        }
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

            // ВЫЗЫВАЕМ СОБЫТИЕ ДОБАВЛЕНИЯ ЭЛЕМЕНТА
            canvas?.RaiseElementAdded();
        }

        // ДОБАВЛЯЕМ метод для создания кривых стрелок
        private void CreateCurvedArrowWithCommand(PointF position)
        {
            var newCurvedArrow = new BpmnCurvedArrow()
            {
                StartPoint = new PointF(position.X - 40, position.Y - 20),
                EndPoint = new PointF(position.X + 40, position.Y + 20),
                Text = "curved connection",
                Color = Color.Black,
                Width = 2f,
                IsFloating = true // Делаем плавающей для редактирования
            };

            // Вычисляем контрольные точки
            newCurvedArrow.CalculateControlPoints();

            // ИСПОЛЬЗУЕМ КОМАНДУ для кривых стрелок
            var command = new CreateCurvedArrowCommand(newCurvedArrow, canvas.GetCurvedArrows(), canvas);
            _commandManager.Execute(command);
        }
        private void menuButton_Click_1(object sender, EventArgs e)
        {

        }

        // ОБНОВЛЕННЫЕ МЕТОДЫ РАБОТЫ С ФАЙЛАМИ
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

        private void SaveBpmnFileAs()
        {
            try
            {
                var currentBlocks = canvas?.GetBlocks() ?? new List<BpmnBlock>();
                var currentArrows = canvas?.GetArrows() ?? new List<BpmnArrow>();
                var currentCurvedArrows = canvas?.GetCurvedArrows() ?? new List<BpmnCurvedArrow>();

                if (BpmnFileService.SaveAsWithDialog(currentBlocks, currentArrows, currentCurvedArrows))
                {
                    // ЕСЛИ АВТОСОХРАНЕНИЕ ВКЛЮЧЕНО - ПЕРЕЗАПУСКАЕМ ЕГО С НОВЫМ ФАЙЛОМ
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

                        // ПРАВИЛЬНО ОЧИЩАЕМ И ЗАГРУЖАЕМ ДАННЫЕ
                        if (canvas != null)
                        {
                            // Получаем текущие коллекции и очищаем их
                            var currentBlocks = canvas.GetBlocks();
                            var currentArrows = canvas.GetArrows();
                            var currentCurvedArrows = canvas.GetCurvedArrows();

                            currentBlocks.Clear();
                            currentArrows.Clear();
                            currentCurvedArrows.Clear();

                            // Добавляем загруженные элементы
                            currentBlocks.AddRange(loadedBlocks);
                            currentArrows.AddRange(loadedArrows);
                            currentCurvedArrows.AddRange(loadedCurvedArrows);

                            // Обновляем канвас
                            canvas.SetBlocks(currentBlocks);
                            canvas.SetArrows(currentArrows);
                            canvas.SetCurvedArrows(currentCurvedArrows);
                            canvas.ClearSelection();
                            canvas.Invalidate();
                        }

                        string message = $"Проект успешно загружен!\nБлоков: {loadedBlocks.Count}, Связей: {loadedArrows.Count}, Кривых стрелок: {loadedCurvedArrows.Count}";
                        MessageBox.Show(message, "Загрузка завершена", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // ОБНОВЛЯЕМ СОСТОЯНИЕ АВТОСОХРАНЕНИЯ
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

        // НОВЫЙ МЕТОД ДЛЯ СОЗДАНИЯ ПРОЕКТА
        private void NewProject()
        {
            var blocksToCheck = canvas?.GetBlocks() ?? new List<BpmnBlock>();
            var arrowsToCheck = canvas?.GetArrows() ?? new List<BpmnArrow>();
            var curvedArrowsToCheck = canvas?.GetCurvedArrows() ?? new List<BpmnCurvedArrow>();

            if (!BpmnFileService.CheckSaveBeforeAction(blocksToCheck, arrowsToCheck, curvedArrowsToCheck))
                return;

            // Очищаем текущий проект
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

            // Создаем новый проект
            BpmnFileService.NewProject();

            // Останавливаем автосохранение при создании нового проекта
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

        // ОБНОВЛЕНИЕ ЗАГОЛОВКА ОКНА
        private void UpdateWindowTitle()
        {
            this.Text = BpmnFileService.GetWindowTitle();
        }

        // ОБРАБОТЧИК ЗАКРЫТИЯ ФОРМЫ
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                var currentBlocks = canvas?.GetBlocks() ?? new List<BpmnBlock>();
                var arrows = canvas?.GetArrows() ?? new List<BpmnArrow>();
                var curvedArrows = canvas?.GetCurvedArrows() ?? new List<BpmnCurvedArrow>();

                // ПРОВЕРЯЕМ ТОЛЬКО НЕСОХРАНЕННЫЕ ИЗМЕНЕНИЯ, не наличие элементов
                if (BpmnFileService.HasUnsavedChanges)
                {
                    BpmnFileService.CheckSaveBeforeAction(currentBlocks, arrows, curvedArrows, e);
                }
            }

            // Останавливаем автосохранение при закрытии формы
            _autoSaveService?.Stop();
            _autoSaveService?.Dispose();

            base.OnFormClosing(e);
        }

        private void LoadFileButton_Click(object sender, EventArgs e)
        {
            LoadBpmnFile();
        }

        private void SaveAsBpmnButton_Click(object sender, EventArgs e)
        {
            SaveBpmnFileAs();
        }

        private void SaveBpmnButton_Click(object sender, EventArgs e)
        {
            SaveBpmnFile();
        }

        private void NewProjectButton_Click(object sender, EventArgs e)
        {
            NewProject();
        }
    }

    public static class ExtensionMethods
    {
        public static void SetRoundedShapeWithBorder(this Control control, int radius, Color borderColor, int borderWidth)
        private void InfoButton_Click_1(object sender, EventArgs e)
        {
            using (HelpForm helpForm = new HelpForm())
            {
                helpForm.StartPosition = FormStartPosition.CenterParent;
                helpForm.ShowDialog(this);
            }
        }
    }
}