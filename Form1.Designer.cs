namespace Kinis
{
    partial class Form1
    {
        /// <summary>
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        /// <summary>
        /// Требуемый метод для поддержки конструктора — не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.sidebar = new System.Windows.Forms.FlowLayoutPanel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panelFigures = new System.Windows.Forms.FlowLayoutPanel();
            this.sidebarTimer = new System.Windows.Forms.Timer(this.components);
            this.panel2 = new System.Windows.Forms.Panel();
            this.btnZoomReset = new System.Windows.Forms.Button();
            this.btnZoomOut = new System.Windows.Forms.Button();
            this.btnZoomIn = new System.Windows.Forms.Button();
            this.RedoBtn = new System.Windows.Forms.Button();
            this.UndoBtn = new System.Windows.Forms.Button();
            this.button6 = new System.Windows.Forms.Button();
            this.SaveAsImageButton = new System.Windows.Forms.Button();
            this.InfoButton = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.menuButton = new System.Windows.Forms.PictureBox();
            this.sidebar.SuspendLayout();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.menuButton)).BeginInit();
            this.SuspendLayout();
            // 
            // sidebar
            // 
            this.sidebar.AutoScroll = true;
            this.sidebar.BackColor = System.Drawing.Color.DimGray;
            this.sidebar.Controls.Add(this.panel1);
            this.sidebar.Dock = System.Windows.Forms.DockStyle.Left;
            this.sidebar.Location = new System.Drawing.Point(0, 0);
            this.sidebar.Margin = new System.Windows.Forms.Padding(4);
            this.sidebar.MaximumSize = new System.Drawing.Size(169, 750);
            this.sidebar.MinimumSize = new System.Drawing.Size(65, 750);
            this.sidebar.Name = "sidebar";
            this.sidebar.Size = new System.Drawing.Size(169, 750);
            this.sidebar.TabIndex = 0;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.menuButton);
            this.panel1.Controls.Add(this.panelFigures);
            this.panel1.Location = new System.Drawing.Point(4, 4);
            this.panel1.Margin = new System.Windows.Forms.Padding(4);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(169, 63);
            this.panel1.TabIndex = 0;
            // 
            // panelFigures
            // 
            this.panelFigures.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.panelFigures.Location = new System.Drawing.Point(-4, 71);
            this.panelFigures.Margin = new System.Windows.Forms.Padding(4);
            this.panelFigures.Name = "panelFigures";
            this.panelFigures.Size = new System.Drawing.Size(170, 567);
            this.panelFigures.TabIndex = 2;
            // 
            // sidebarTimer
            // 
            this.sidebarTimer.Interval = 10;
            this.sidebarTimer.Tick += new System.EventHandler(this.sidebarTimer_Tick_1);
            // 
            // panel2
            // 
            this.panel2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.panel2.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.panel2.Controls.Add(this.btnZoomReset);
            this.panel2.Controls.Add(this.btnZoomOut);
            this.panel2.Controls.Add(this.btnZoomIn);
            this.panel2.Controls.Add(this.RedoBtn);
            this.panel2.Controls.Add(this.UndoBtn);
            this.panel2.Controls.Add(this.button6);
            this.panel2.Controls.Add(this.SaveAsImageButton);
            this.panel2.Controls.Add(this.InfoButton);
            this.panel2.Controls.Add(this.button2);
            this.panel2.Location = new System.Drawing.Point(502, -22);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(403, 75);
            this.panel2.TabIndex = 1;
            // 
            // btnZoomReset
            // 
            this.btnZoomReset.BackColor = System.Drawing.Color.Transparent;
            this.btnZoomReset.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnZoomReset.BackgroundImage")));
            this.btnZoomReset.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.btnZoomReset.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnZoomReset.FlatAppearance.BorderSize = 0;
            this.btnZoomReset.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnZoomReset.Font = new System.Drawing.Font("Microsoft Sans Serif", 40F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel, ((byte)(0)));
            this.btnZoomReset.Location = new System.Drawing.Point(332, 29);
            this.btnZoomReset.Name = "btnZoomReset";
            this.btnZoomReset.Size = new System.Drawing.Size(33, 34);
            this.btnZoomReset.TabIndex = 10;
            this.btnZoomReset.UseVisualStyleBackColor = false;
            // 
            // btnZoomOut
            // 
            this.btnZoomOut.BackColor = System.Drawing.Color.Transparent;
            this.btnZoomOut.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnZoomOut.BackgroundImage")));
            this.btnZoomOut.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.btnZoomOut.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnZoomOut.FlatAppearance.BorderSize = 0;
            this.btnZoomOut.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnZoomOut.Font = new System.Drawing.Font("Microsoft Sans Serif", 40F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel, ((byte)(0)));
            this.btnZoomOut.Location = new System.Drawing.Point(293, 29);
            this.btnZoomOut.Name = "btnZoomOut";
            this.btnZoomOut.Size = new System.Drawing.Size(33, 34);
            this.btnZoomOut.TabIndex = 9;
            this.btnZoomOut.UseVisualStyleBackColor = false;
            // 
            // btnZoomIn
            // 
            this.btnZoomIn.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnZoomIn.BackgroundImage")));
            this.btnZoomIn.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.btnZoomIn.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnZoomIn.FlatAppearance.BorderSize = 0;
            this.btnZoomIn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnZoomIn.Font = new System.Drawing.Font("Microsoft Sans Serif", 40F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel, ((byte)(0)));
            this.btnZoomIn.Location = new System.Drawing.Point(254, 29);
            this.btnZoomIn.Name = "btnZoomIn";
            this.btnZoomIn.Size = new System.Drawing.Size(33, 34);
            this.btnZoomIn.TabIndex = 8;
            this.btnZoomIn.UseVisualStyleBackColor = false;
            // 
            // RedoBtn
            // 
            this.RedoBtn.BackColor = System.Drawing.Color.Transparent;
            this.RedoBtn.BackgroundImage = global::Kinis.Properties.Resources.redo;
            this.RedoBtn.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.RedoBtn.Cursor = System.Windows.Forms.Cursors.Hand;
            this.RedoBtn.FlatAppearance.BorderSize = 0;
            this.RedoBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.RedoBtn.Location = new System.Drawing.Point(215, 29);
            this.RedoBtn.Margin = new System.Windows.Forms.Padding(4);
            this.RedoBtn.Name = "RedoBtn";
            this.RedoBtn.Size = new System.Drawing.Size(33, 34);
            this.RedoBtn.TabIndex = 4;
            this.RedoBtn.UseVisualStyleBackColor = false;
            this.RedoBtn.Click += new System.EventHandler(this.button3_Click);
            // 
            // UndoBtn
            // 
            this.UndoBtn.BackColor = System.Drawing.Color.Transparent;
            this.UndoBtn.BackgroundImage = global::Kinis.Properties.Resources.undo;
            this.UndoBtn.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.UndoBtn.Cursor = System.Windows.Forms.Cursors.Hand;
            this.UndoBtn.FlatAppearance.BorderSize = 0;
            this.UndoBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.UndoBtn.Location = new System.Drawing.Point(176, 28);
            this.UndoBtn.Margin = new System.Windows.Forms.Padding(4);
            this.UndoBtn.Name = "UndoBtn";
            this.UndoBtn.Size = new System.Drawing.Size(33, 34);
            this.UndoBtn.TabIndex = 6;
            this.UndoBtn.UseVisualStyleBackColor = false;
            this.UndoBtn.Click += new System.EventHandler(this.button5_Click);
            // 
            // button6
            // 
            this.button6.BackColor = System.Drawing.Color.Transparent;
            this.button6.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("button6.BackgroundImage")));
            this.button6.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.button6.Cursor = System.Windows.Forms.Cursors.Hand;
            this.button6.FlatAppearance.BorderSize = 0;
            this.button6.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button6.Location = new System.Drawing.Point(95, 29);
            this.button6.Name = "button6";
            this.button6.Size = new System.Drawing.Size(30, 34);
            this.button6.TabIndex = 7;
            this.button6.UseVisualStyleBackColor = false;
            // 
            // SaveAsImageButton
            // 
            this.SaveAsImageButton.BackColor = System.Drawing.Color.Transparent;
            this.SaveAsImageButton.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("SaveAsImageButton.BackgroundImage")));
            this.SaveAsImageButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.SaveAsImageButton.FlatAppearance.BorderSize = 0;
            this.SaveAsImageButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.SaveAsImageButton.Location = new System.Drawing.Point(130, 29);
            this.SaveAsImageButton.Margin = new System.Windows.Forms.Padding(2);
            this.SaveAsImageButton.Name = "SaveAsImageButton";
            this.SaveAsImageButton.Size = new System.Drawing.Size(41, 35);
            this.SaveAsImageButton.TabIndex = 5;
            this.SaveAsImageButton.UseVisualStyleBackColor = false;
            this.SaveAsImageButton.Click += new System.EventHandler(this.SaveAsImageButton_Click);
            // 
            // InfoButton
            // 
            this.InfoButton.BackColor = System.Drawing.Color.Transparent;
            this.InfoButton.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("InfoButton.BackgroundImage")));
            this.InfoButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.InfoButton.FlatAppearance.BorderSize = 0;
            this.InfoButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.InfoButton.Location = new System.Drawing.Point(11, 28);
            this.InfoButton.Margin = new System.Windows.Forms.Padding(2);
            this.InfoButton.Name = "InfoButton";
            this.InfoButton.Size = new System.Drawing.Size(43, 36);
            this.InfoButton.TabIndex = 2;
            this.InfoButton.UseVisualStyleBackColor = false;
            // 
            // button2
            // 
            this.button2.BackColor = System.Drawing.Color.Transparent;
            this.button2.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("button2.BackgroundImage")));
            this.button2.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.button2.Cursor = System.Windows.Forms.Cursors.Hand;
            this.button2.FlatAppearance.BorderSize = 0;
            this.button2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button2.Location = new System.Drawing.Point(59, 29);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(30, 34);
            this.button2.TabIndex = 3;
            this.button2.UseVisualStyleBackColor = false;
            // 
            // menuButton
            // 
            this.menuButton.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("menuButton.BackgroundImage")));
            this.menuButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.menuButton.Image = ((System.Drawing.Image)(resources.GetObject("menuButton.Image")));
            this.menuButton.Location = new System.Drawing.Point(0, 3);
            this.menuButton.Name = "menuButton";
            this.menuButton.Size = new System.Drawing.Size(47, 52);
            this.menuButton.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.menuButton.TabIndex = 2;
            this.menuButton.TabStop = false;
            this.menuButton.Click += new System.EventHandler(this.menuButton_Click_1);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoValidate = System.Windows.Forms.AutoValidate.Disable;
            this.ClientSize = new System.Drawing.Size(886, 644);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.sidebar);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "BPMN editor";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.sidebar.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.menuButton)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel sidebar;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.PictureBox menuButton;
        private System.Windows.Forms.Timer sidebarTimer;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Button InfoButton;
        private System.Windows.Forms.Button RedoBtn;
        private System.Windows.Forms.Button UndoBtn;
        private System.Windows.Forms.Button button6;
        private System.Windows.Forms.Button SaveAsImageButton;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button btnZoomIn;
        private System.Windows.Forms.Button btnZoomReset;
        private System.Windows.Forms.Button btnZoomOut;
        private System.Windows.Forms.FlowLayoutPanel panelFigures;
    }
}

