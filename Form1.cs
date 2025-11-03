using Kinis.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Linq;

namespace Kinis
{
    public partial class Form1 : Form
    {
        private InfiniteCanvas canvas;
        private bool sidebarExpand;
        private List<BpmnBlock> blocks = new List<BpmnBlock>();
        private int miniMinWidth = 48;
        private int miniMinHeight = 42;
        private int miniMaxHeight = 80;
        private BpmnBlock selectedSidebarBlock = null;
        private bool isDraggingFromSidebar = false;
        private Point dragStartPoint;

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
            panel2.SetRoundedShapeWithBorder(30, Color.Black, 2);
            //panelFigures.FlowDirection = FlowDirection.TopDown;
            //panelFigures.WrapContents = false;
            //panelFigures.AutoScroll = true;
            //panelFigures.Dock = DockStyle.Fill;
            //panelFigures.Padding = new Padding(8);

            ConnectZoomButtons();
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
                if (block == selectedSidebarBlock)
                {
                    using (var pen = new Pen(Color.DeepSkyBlue, 3))
                    {
                        g.DrawRectangle(pen, block.Bounds.X - 1, block.Bounds.Y - 1, block.Bounds.Width + 2, block.Bounds.Height + 2);
                    }
                }
            }
        }

        private void SidebarPreviewPanel_MouseDown(object sender, MouseEventArgs e)
        {
            foreach (var block in sidebarBlocks)
            {
                if (block.Bounds.Contains(e.Location))
                {
                    selectedSidebarBlock = block;
                    sidebarPreviewPanel.Invalidate();

                    isDraggingFromSidebar = true;
                    dragStartPoint = e.Location;
                    return;
                }
            }

            selectedSidebarBlock = null;
            sidebarPreviewPanel.Invalidate();
            isDraggingFromSidebar = false;
        }

        private void SidebarPreviewPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDraggingFromSidebar && selectedSidebarBlock != null)
            {
                sidebarPreviewPanel.DoDragDrop(selectedSidebarBlock, DragDropEffects.Copy);
                isDraggingFromSidebar = false;
            }
        }
        private void SidebarPreviewPanel_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            foreach (var block in sidebarBlocks)
            {
                if (block.Bounds.Contains(e.Location))
                {
                    BpmnBlock newBlock = new BpmnBlock(0, 0, 120, 80)
                    {
                        Text = block.Text,
                        Type = block.Type,
                        FillColor = block.FillColor,
                        BorderColor = block.BorderColor,
                        Id = Guid.NewGuid().ToString()
                    };

                    if (blocks.Count > 0)
                    {
                        var last = blocks.Last();
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

                    blocks.Add(newBlock);
                    canvas.SetBlocks(blocks); // Обновляем блоки на канве
                    canvas.Invalidate();

                    Console.WriteLine($" Добавлен блок '{newBlock.Text}' с ID {newBlock.Id}");
                    return;
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

            sidebarPreviewPanel = new Panel
            {
                Name = "SidebarPreviewPanel",
                AutoScroll = true,
                BackColor = Color.Transparent,
                Width = sidebar.ClientSize.Width,
                Height = sidebar.Height - 120,
                Margin = new Padding(0),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            sidebar.Controls.Add(sidebarPreviewPanel);
            sidebarPreviewPanel.Width = Math.Max(20, sidebar.ClientSize.Width);

            sidebarBlocks = new List<BpmnBlock>
            {
                new BpmnBlock(8, 8, miniMinWidth, miniMinHeight)
                { Text = "Event", Type = "Event", FillColor = Color.LightGreen },
                new BpmnBlock(8, 8 + (miniMinHeight + 12) * 1, miniMinWidth, miniMinHeight)
                { Text = "Task", Type = "Task", FillColor = Color.LightBlue },
                new BpmnBlock(8, 8 + (miniMinHeight + 12) * 2, miniMinWidth, miniMinHeight)
                { Text = "Gateway", Type = "Gateway", FillColor = Color.LightCoral }
            };

            sidebarPreviewPanel.Paint += SidebarPreviewPanel_Paint;
            sidebarPreviewPanel.MouseDoubleClick += SidebarPreviewPanel_MouseDoubleClick;
            sidebarPreviewPanel.MouseDown += SidebarPreviewPanel_MouseDown;
            sidebarPreviewPanel.MouseMove += SidebarPreviewPanel_MouseMove;
            sidebarPreviewPanel.MouseUp += SidebarPreviewPanel_MouseUp;
            sidebarPreviewPanel.Visible = true;
            sidebarPreviewPanel.Invalidate();
        }

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
                return;

            float scale = GetSidebarScale();
            int margin = 8;
            int spacing = 12;

            int panelAvailable = Math.Max(20, sidebarPreviewPanel.ClientSize.Width - 2 * 8);
            float curWidth = Lerp(miniMinWidth, panelAvailable, scale);
            float curHeight = Lerp(miniMinHeight, panelAvailable, scale);

            float x = margin;
            float y = margin;

            foreach (var block in sidebarBlocks)
            {
                block.Bounds = new RectangleF(x, y, curWidth, curHeight);
                y += curHeight + spacing;
            }

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
            blocks.Add(new BpmnBlock(50, 50) { Text = "Start", Type = "Event", FillColor = Color.LightGreen });
            blocks.Add(new BpmnBlock(200, 50) { Text = "Task", Type = "Task", FillColor = Color.LightBlue });
            blocks.Add(new BpmnBlock(350, 50) { Text = "End", Type = "Event", FillColor = Color.LightCoral });
            blocks.Add(new BpmnBlock(100, 200, 120, 80) { Text = "Custom", FillColor = Color.LightYellow, BorderColor = Color.Gray });

            canvas.SetBlocks(blocks); // Инициализируем канву блоками
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

                Point clientPoint = canvas.PointToClient(new Point(e.X, e.Y));
                PointF worldPoint = canvas.ScreenToWorld(clientPoint);

                var newBlock = new BpmnBlock(worldPoint.X, worldPoint.Y,
                    blockFromSidebar.Bounds.Width, blockFromSidebar.Bounds.Height)
                {
                    Text = blockFromSidebar.Text,
                    Type = blockFromSidebar.Type,
                    FillColor = blockFromSidebar.FillColor,
                    BorderColor = blockFromSidebar.BorderColor,
                    Id = Guid.NewGuid().ToString()
                };

                blocks.Add(newBlock);
                canvas.SetBlocks(blocks); // Обновляем блоки на канве
                canvas.Invalidate();

                Console.WriteLine($" Блок '{newBlock.Text}' добавлен через перетаскивание.");
            }
        }

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
