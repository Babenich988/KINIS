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

        // вычисляемая ширина для раскрытого меню (автоматически подстраивается)
        private int GetMaxSidebarBlockWidth()
        {
            int margin = 8; // такой же отступ, как в отрисовке
            return sidebar.MaximumSize.Width - 2 * margin;
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
            }
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
                Width = sidebar.Width,
                Height = sidebar.Height - 120,
                Margin = new Padding(0),
            };

            sidebar.Controls.Add(sidebarPreviewPanel);

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

            sidebarPreviewPanel.Visible = true;
            sidebarPreviewPanel.Invalidate();
        }

        // ✅ ДОБАВЛЕНО В ЭТОМ КОММИТЕ
        private float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
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

            // 👇 пока без GetSidebarScale — используем временный scale
            float scale = (float)(sidebar.Width - sidebar.MinimumSize.Width)
                        / (float)(sidebar.MaximumSize.Width - sidebar.MinimumSize.Width);
            if (scale < 0f) scale = 0f;
            if (scale > 1f) scale = 1f;

            UpdateSidebarBlocksSize(scale);
        }

        private void UpdateSidebarBlocksSize(float scale)
        {
            if (sidebarPreviewPanel == null || sidebarBlocks == null || sidebarBlocks.Count == 0)
                return;

            int margin = 8;
            int spacing = 12;

            float curWidth = Lerp(miniMinWidth, GetMaxSidebarBlockWidth(), scale);
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

        private void Form1_Load(object sender, EventArgs e)
        {
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

            canvas.SetBlocks(blocks);
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
        }

        private void ConnectZoomButtons()
        {
            btnZoomIn.Click += (s, e) => canvas.ZoomIn();
            btnZoomOut.Click += (s, e) => canvas.ZoomOut();
            btnZoomReset.Click += (s, e) => canvas.ResetZoom();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (panel2 != null)
            {
                panel2.Location = new Point(this.Width - panel2.Width - -18, -18);
            }
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
