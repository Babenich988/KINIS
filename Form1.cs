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

        public Form1()
        {
            InitializeComponent();

            // Привязываем кнопку меню к анимации панели
            menuButton.Click += (s, e) => sidebarTimer.Start();
            ConnectZoomButtons();
            // Добавляем "бесконечное" полотно
            AddCanvasToExistingPanels();
            panel2.SetRoundedShapeWithBorder(30, Color.Black, 2);

        }

        private void button6_Click(object sender, EventArgs e)
        {

        }



        // Анимация открытия/закрытия боковой панели
        private void sidebarTimer_Tick(object sender, EventArgs e)
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

            Invalidate();
            canvas.SetBlocks(blocks);

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
                Name = "InfiniteCanvas"
            };

            this.Controls.Add(canvas);
            canvas.SendToBack(); // Помещаем под остальные панели
        }

        private void SaveAsImageButton_Click(object sender, EventArgs e)
        {
            SaveFormAsImage();
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
        private void ConnectZoomButtons()
        {
            btnZoomIn.Click += (s, e) => canvas.ZoomIn();

            btnZoomOut.Click += (s, e) => canvas.ZoomOut();

            btnZoomReset.Click += (s, e) => canvas.ResetZoom();
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