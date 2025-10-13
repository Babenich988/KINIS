using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Kinis.Models;

namespace Kinis
{
    public partial class Form1 : Form
    {
        private InfiniteCanvas canvas;
        bool sidebarExpand;
        private List<BpmnBlock> blocks = new List<BpmnBlock>();
        public Form1()
        {
            InitializeComponent();
            menuButton.Click += (s, e) => sidebarTimer.Start();
            AddCanvasToExistingPanels();
            SetRoundedShape(panel2, 30);
            
        }
        static void SetRoundedShape(Control control, int radius)
        {
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddLine(radius, 0, control.Width - radius, 0);
            path.AddArc(control.Width - radius, 0, radius, radius, 270, 90);
            path.AddLine(control.Width, radius, control.Width, control.Height - radius);
            path.AddArc(control.Width - radius, control.Height - radius, radius, radius, 0, 90);
            path.AddLine(control.Width - radius, control.Height, radius, control.Height);
            path.AddArc(0, control.Height - radius, radius, radius, 90, 90);
            path.AddLine(0, control.Height - radius, 0, radius);
            path.AddArc(0, 0, radius, radius, 180, 90);
            control.Region = new Region(path);
        }

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

            Invalidate(); // Перерисовываем форму, чтобы блоки появились
            canvas.SetBlocks(blocks);

        }
        private void menuButton_Click(object sender, EventArgs e)
        {
            sidebarTimer.Start();
        }
        private void AddCanvasToExistingPanels()
        {
            //Создание бесконечного поля
            canvas = new InfiniteCanvas()
            {
                Dock = DockStyle.Fill,
                Name = "InfiniteCanvas"
            };

            this.Controls.Add(canvas);
            canvas.SendToBack();
        }
    }
}
