using Kinis.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Kinis.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kinis
{
    public partial class Form1 : Form
    {
        private InfiniteCanvas canvas;              // Главное поле для рисования
        private bool sidebarExpand;                 // Флаг — развернута ли боковая панель
        private List<BpmnBlock> blocks = new List<BpmnBlock>(); // Список всех блоков
        private int miniMinWidth = 48;    // ширина при свернутом меню
        private int miniMinHeight = 42;   // высота при свернутом меню
        private int miniMaxHeight = 80;   // высота при развернутом меню
        private BpmnBlock selectedSidebarBlock = null; // текущий выбранный блок из меню
        private bool isDraggingFromSidebar = false;
        private Point dragStartPoint;
        // вычисляемая ширина для раскрытого меню (автоматически подстраивается)
        private int GetMaxSidebarBlockWidth()
        {
            int margin = 8; // такой же отступ, как в отрисовке
                            // используем текущую видимую ширину панели (не MaximumSize)
            int visible = sidebar.ClientSize.Width;
            if (visible <= 0) visible = sidebar.Width; // запасной вариант
            return Math.Max(20, visible - 2 * margin);
        }
        public Form1()
        {
            InitializeComponent();
            sidebar.Width = sidebar.MinimumSize.Width;
            sidebarExpand = false;
            // Остальной код
            menuButton.Click += (s, e) => sidebarTimer.Start();
            AddCanvasToExistingPanels();
            panel2.SetRoundedShapeWithBorder(30, Color.Black, 2);
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
            // ДОБАВЛЕНО: Подключаем обработчики кнопок зума
            ConnectZoomButtons();
        }

        private void button6_Click(object sender, EventArgs e)
        {

        }

        private Panel sidebarPreviewPanel;
        private List<BpmnBlock> sidebarBlocks = new List<BpmnBlock>();

        private void SidebarPreviewPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (sidebarBlocks == null || sidebarBlocks.Count == 0) return;

            foreach (var block in sidebarBlocks)
            {
                using (var brush = new SolidBrush(block.FillColor))
                    g.FillRectangle(brush, block.Bounds);

                using (var pen = new Pen(block.BorderColor, 1))
                    g.DrawRectangle(pen, block.Bounds.X, block.Bounds.Y, block.Bounds.Width, block.Bounds.Height);

                float fontSize = Math.Max(8f, Math.Min(12f, block.Bounds.Height / 6f + block.Bounds.Width / 60f));
                using (var font = new Font("Segoe UI", fontSize))
                using (var textBrush = new SolidBrush(Color.Black))
                {
                    var textSize = g.MeasureString(block.Text, font);
                    float textX = block.Bounds.X + (block.Bounds.Width - textSize.Width) / 2f;
                    float textY = block.Bounds.Y + (block.Bounds.Height - textSize.Height) / 2f;
                    g.DrawString(block.Text, font, textBrush, textX, textY);
                }
                // Если блок выбран — рисуем синюю рамку вокруг него
                if (block == selectedSidebarBlock)
                {
                    using (var pen = new Pen(Color.DeepSkyBlue, 3))
                    {
                        g.DrawRectangle(pen, block.Bounds.X - 1, block.Bounds.Y - 1, block.Bounds.Width + 2, block.Bounds.Height + 2);
                    }
                }
            }
        }

        /// <summary>
        /// Обрабатывает клик по мини-блокам в панели sidebar.
        /// При клике на блок — выделяет его, при клике мимо — снимает выделение.
        /// </summary>
        private void SidebarPreviewPanel_MouseDown(object sender, MouseEventArgs e)
        {
            // Проверяем, какой блок был нажат
            foreach (var block in sidebarBlocks)
            {
                if (block.Bounds.Contains(e.Location))
                {
                    selectedSidebarBlock = block; // сохраняем выбранный блок
                    sidebarPreviewPanel.Invalidate(); // перерисовываем, чтобы отобразить рамку

                    //  начинаем возможное перетаскивание
                    isDraggingFromSidebar = true;
                    dragStartPoint = e.Location;
                    return;
                }
            }

            // если кликнули не по блоку — снимаем выделение
            selectedSidebarBlock = null;
            sidebarPreviewPanel.Invalidate();
            isDraggingFromSidebar = false;
        }

        // Двигаем мышь — показываем, что идёт перетаскивание
        private void SidebarPreviewPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDraggingFromSidebar && selectedSidebarBlock != null)
            {
                // При начале движения запускаем "drag and drop"
                sidebarPreviewPanel.DoDragDrop(selectedSidebarBlock, DragDropEffects.Copy);
                isDraggingFromSidebar = false; // чтобы не запускалось повторно
            }
        }
        // Отпустили кнопку мыши в sidebar — прекращаем перетаскивание
        private void SidebarPreviewPanel_MouseUp(object sender, MouseEventArgs e)
        {
            isDraggingFromSidebar = false;
        }

        private void AddBlocksToSidebar()
        {
            // Удаляем старую панель, если она уже была
            if (sidebarPreviewPanel != null && sidebar.Controls.Contains(sidebarPreviewPanel))
                sidebar.Controls.Remove(sidebarPreviewPanel);

            sidebarPreviewPanel = new Panel
            {
                Name = "SidebarPreviewPanel",
                AutoScroll = true,
                BackColor = Color.Transparent,
                Width = sidebar.ClientSize.Width,      // важно: берем текущее видимое значение
                Height = sidebar.Height - 120,
                Margin = new Padding(0),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right // даёт гибкость
            };

            sidebar.Controls.Add(sidebarPreviewPanel);
            sidebarPreviewPanel.Width = Math.Max(20, sidebar.ClientSize.Width);
            // Создаём мини-блоки с минимальными размерами
            sidebarBlocks = new List<BpmnBlock>
            {
                new BpmnBlock(8, 8, miniMinWidth, miniMinHeight)
                { Text = "Event", Type = "Event", FillColor = Color.LightGreen },
                new BpmnBlock(8, 8 + (miniMinHeight + 12) * 1, miniMinWidth, miniMinHeight)
                { Text = "Task", Type = "Task", FillColor = Color.LightBlue },
                new BpmnBlock(8, 8 + (miniMinHeight + 12) * 2, miniMinWidth, miniMinHeight)
                { Text = "Gateway", Type = "Gateway", FillColor = Color.LightCoral }
            };

            sidebarPreviewPanel.Paint -= SidebarPreviewPanel_Paint;
            sidebarPreviewPanel.Paint += SidebarPreviewPanel_Paint;

            sidebarPreviewPanel.MouseDown += SidebarPreviewPanel_MouseDown;
            sidebarPreviewPanel.MouseMove += SidebarPreviewPanel_MouseMove;
            sidebarPreviewPanel.MouseUp += SidebarPreviewPanel_MouseUp;
            // Подписываем панель на событие клика мышью
            sidebarPreviewPanel.MouseDown -= SidebarPreviewPanel_MouseDown; // на всякий случай удаляем старую подписку
            sidebarPreviewPanel.MouseDown += SidebarPreviewPanel_MouseDown;
            sidebarPreviewPanel.Visible = true;
            sidebarPreviewPanel.Invalidate();
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
                // Подгоняем ширину превью под текущую ширину sidebar
                sidebarPreviewPanel.Width = Math.Max(20, sidebar.ClientSize.Width);
            }
            // Добавляем пересчёт размеров блоков
            UpdateSidebarBlocksSize();
        }

        private float Lerp(float a, float b, float t)//Добавил метод Lerp для плавного изменения размеров блоков в меню
        {
            return a + (b - a) * t;
        }

        private float GetSidebarScale()//Реализовал метод GetSidebarScale для расчета текущего состояния раскрытия меню
        {
            float min = sidebar.MinimumSize.Width; // 70
            float max = sidebar.MaximumSize.Width; // 225
            if (max <= min) return 1f;
            float s = (sidebar.Width - min) / (max - min);
            if (s < 0f) s = 0f;
            if (s > 1f) s = 1f;
            return s;
        }

        private void UpdateSidebarBlocksSize()
        {
            if (sidebarPreviewPanel == null || sidebarBlocks == null || sidebarBlocks.Count == 0)
                return;

            float scale = GetSidebarScale(); // от 0 (свернуто) до 1 (развернуто)
            int margin = 8;
            int spacing = 12;

            int panelAvailable = Math.Max(20, sidebarPreviewPanel.ClientSize.Width - 2 * 8);
            float curWidth = Lerp(miniMinWidth, panelAvailable, scale);
            float curHeight = Lerp(miniMinHeight, miniMaxHeight, scale);

            float x = margin;
            float y = margin;

            foreach (var block in sidebarBlocks)
            {
                block.Bounds = new RectangleF(x, y, curWidth, curHeight);
                y += curHeight + spacing;
            }

            sidebarPreviewPanel.Invalidate();
        }
        private PointF GetCanvasCenterWorldPoint()//метод для вычисления центра холста
        {
            if (canvas == null)
                return new PointF(100, 100); // запасной вариант

            // центр клиентской области
            Point screenCenter = new Point(canvas.Width / 2, canvas.Height / 2);

            // если в InfiniteCanvas реализовано смещение и зум
            if (canvas is Kinis.InfiniteCanvas ic)
            {
                // преобразуем экранные координаты в мировые (координаты холста)
                return ic.ScreenToWorld(screenCenter);
            }

            // fallback — без учёта смещения
            return new PointF(screenCenter.X, screenCenter.Y);
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            // Создаем тестовые BPMN-блоки
            blocks.Add(new BpmnBlock(50, 50)
            {
                Text = "Start",
                Type = "Event",
                FillColor = Color.LightGreen
            });

            blocks.Add(new BpmnBlock(200, 50)
            {
                Text = "Task",
                Type = "Task",
                FillColor = Color.LightBlue
            });

            blocks.Add(new BpmnBlock(350, 50)
            {
                Text = "End",
                Type = "Event",
                FillColor = Color.LightCoral
            });

            blocks.Add(new BpmnBlock(100, 200, 120, 80)
            {
                Text = "Custom",
                FillColor = Color.LightYellow,
                BorderColor = Color.Gray
            });

            // Передаем их на холст
            canvas.SetBlocks(blocks);

            // Обновляем только холст
            canvas.Invalidate();
            AddBlocksToSidebar();
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
            canvas.MouseDown += Canvas_MouseDown; // клик по холсту снимает выделение блока
            canvas.AllowDrop = true;
            canvas.DragEnter += Canvas_DragEnter;
            canvas.DragDrop += Canvas_DragDrop;
            // 1️⃣ УДАЛЯЕМ panel2 из контролов (временно)
            this.Controls.Remove(panel2);

            // 2️⃣ Добавляем холст в самый низ по Z-порядку
            this.Controls.Add(canvas);
            canvas.SendToBack();

            // 3️⃣ ДОБАВЛЯЕМ panel2 ОБРАТНО (теперь она будет поверх canvas)
            this.Controls.Add(panel2);

            // 4️⃣ Настраиваем позицию panel2
            panel2.Location = new Point(this.Width - panel2.Width - -18, -18);
            panel2.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            // 5️⃣ Возвращаем панели наверх в правильном порядке
            panel2.BringToFront();
            sidebar.BringToFront();

            // 6️⃣ Принудительно обновляем видимость
            panel2.Visible = true;
            panel2.Show();

            Console.WriteLine("Проверка элементов на форме:");
            foreach (Control c in this.Controls)
            {
                Console.WriteLine($"  - {c.Name}, Visible: {c.Visible}, Location: {c.Location}, Size: {c.Size}");
            }
        }

        /// <summary>
        /// Снимает выделение блока из меню при клике на холсте.
        /// </summary>
        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (selectedSidebarBlock != null)
            {
                selectedSidebarBlock = null;
                if (sidebarPreviewPanel != null)
                    sidebarPreviewPanel.Invalidate(); // перерисовываем, чтобы убрать рамку
            }
        }

        // Разрешаем перенос только наших блоков
        private void Canvas_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(BpmnBlock)))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }
        private void Canvas_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(BpmnBlock)))
            {
                var blockFromSidebar = (BpmnBlock)e.Data.GetData(typeof(BpmnBlock));

                // Получаем позицию на холсте
                Point dropPoint = canvas.PointToClient(new Point(e.X, e.Y));

                // Создаём копию
                var newBlock = new BpmnBlock(dropPoint.X, dropPoint.Y,
                    blockFromSidebar.Bounds.Width, blockFromSidebar.Bounds.Height)
                {
                    Text = blockFromSidebar.Text,
                    Type = blockFromSidebar.Type,
                    FillColor = blockFromSidebar.FillColor,
                    BorderColor = blockFromSidebar.BorderColor,
                    Id = Guid.NewGuid().ToString() // уникальный ID
                };

                // Добавляем в общий список
                blocks.Add(newBlock);
                canvas.SetBlocks(blocks);
                canvas.Invalidate();
            }
        }
        // Метод для подключения кнопок зума
        private void ConnectZoomButtons()
        {
            btnZoomIn.Click += (s, e) => canvas.ZoomIn();
            btnZoomOut.Click += (s, e) => canvas.ZoomOut();
            btnZoomReset.Click += (s, e) => canvas.ResetZoom();
        }

        private void SaveFormAsImage()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "PNG Image|*.png|JPEG Image|*.jpg|Bitmap Image|*.bmp";
            saveFileDialog.Title = "Save Form as Image";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    ImageFormat format = GetImageFormat(saveFileDialog.FilterIndex);

                    Bitmap bitmap = new Bitmap(this.Width, this.Height);

                    this.DrawToBitmap(bitmap, new Rectangle(0, 0, this.Width, this.Height));

                    bitmap.Save(saveFileDialog.FileName, format);
                    MessageBox.Show("Изображение успешно сохранено!", "Сохранение завершено", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при сохранении изображения: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private void SaveAsImageButton_Click_1(object sender, EventArgs e)
        {
            SaveFormAsImage();
        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        // ДОБАВЛЕНО: Обработчик изменения размера формы
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            // При изменении размера формы обновляем позицию panel2
            if (panel2 != null)
            {
                panel2.Location = new Point(this.Width - panel2.Width - -18, -18);
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {

        }
    }

    public static class ExtensionMethods
    {
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