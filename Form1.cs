using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kinis
{
    public partial class Form1 : Form
    {
        private InfiniteCanvas canvas;
        bool sidebarExpand;
        public Form1()
        {
            InitializeComponent();
            menuButton.Click += (s, e) => sidebarTimer.Start();
            AddCanvasToExistingPanels();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

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
