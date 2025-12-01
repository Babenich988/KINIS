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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            sidebar = new FlowLayoutPanel();
            panel1 = new Panel();
            menuButton = new PictureBox();
            panelFigures = new FlowLayoutPanel();
            sidebarTimer = new System.Windows.Forms.Timer(components);
            panel2 = new Panel();
            btnZoomReset = new Button();
            btnZoomOut = new Button();
            btnZoomIn = new Button();
            RedoBtn = new Button();
            UndoBtn = new Button();
            SaveAsBpmnButton = new Button();
            SaveAsImageButton = new Button();
            InfoButton = new Button();
            LoadFileButton = new Button();
            sidebar.SuspendLayout();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)menuButton).BeginInit();
            panel2.SuspendLayout();
            SuspendLayout();
            // 
            // sidebar
            // 
            sidebar.AutoScroll = true;
            sidebar.BackColor = Color.DimGray;
            sidebar.Controls.Add(panel1);
            sidebar.Dock = DockStyle.Left;
            sidebar.Location = new Point(0, 0);
            sidebar.Margin = new Padding(4, 5, 4, 5);
            sidebar.MaximumSize = new Size(197, 865);
            sidebar.MinimumSize = new Size(76, 865);
            sidebar.Name = "sidebar";
            sidebar.Size = new Size(197, 865);
            sidebar.TabIndex = 0;
            // 
            // panel1
            // 
            panel1.Controls.Add(menuButton);
            panel1.Controls.Add(panelFigures);
            panel1.Location = new Point(4, 5);
            panel1.Margin = new Padding(4, 5, 4, 5);
            panel1.Name = "panel1";
            panel1.Size = new Size(197, 73);
            panel1.TabIndex = 0;
            // 
            // menuButton
            // 
            menuButton.BackgroundImage = (Image)resources.GetObject("menuButton.BackgroundImage");
            menuButton.Cursor = Cursors.Hand;
            menuButton.Image = (Image)resources.GetObject("menuButton.Image");
            menuButton.Location = new Point(0, 4);
            menuButton.Margin = new Padding(4);
            menuButton.Name = "menuButton";
            menuButton.Size = new Size(55, 60);
            menuButton.SizeMode = PictureBoxSizeMode.StretchImage;
            menuButton.TabIndex = 2;
            menuButton.TabStop = false;
            menuButton.Click += menuButton_Click_1;
            // 
            // panelFigures
            // 
            panelFigures.FlowDirection = FlowDirection.RightToLeft;
            panelFigures.Location = new Point(-4, 82);
            panelFigures.Margin = new Padding(4, 5, 4, 5);
            panelFigures.Name = "panelFigures";
            panelFigures.Size = new Size(199, 654);
            panelFigures.TabIndex = 2;
            // 
            // sidebarTimer
            // 
            sidebarTimer.Interval = 10;
            sidebarTimer.Tick += sidebarTimer_Tick_1;
            // 
            // panel2
            // 
            panel2.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            panel2.BackColor = SystemColors.ControlLightLight;
            panel2.Controls.Add(btnZoomReset);
            panel2.Controls.Add(btnZoomOut);
            panel2.Controls.Add(btnZoomIn);
            panel2.Controls.Add(RedoBtn);
            panel2.Controls.Add(UndoBtn);
            panel2.Controls.Add(SaveAsBpmnButton);
            panel2.Controls.Add(SaveAsImageButton);
            panel2.Controls.Add(InfoButton);
            panel2.Controls.Add(LoadFileButton);
            panel2.Location = new Point(585, -25);
            panel2.Margin = new Padding(4);
            panel2.Name = "panel2";
            panel2.Size = new Size(470, 86);
            panel2.TabIndex = 1;
            // 
            // btnZoomReset
            // 
            btnZoomReset.BackColor = Color.Transparent;
            btnZoomReset.BackgroundImage = (Image)resources.GetObject("btnZoomReset.BackgroundImage");
            btnZoomReset.BackgroundImageLayout = ImageLayout.Zoom;
            btnZoomReset.Cursor = Cursors.Hand;
            btnZoomReset.FlatAppearance.BorderSize = 0;
            btnZoomReset.FlatStyle = FlatStyle.Flat;
            btnZoomReset.Font = new Font("Microsoft Sans Serif", 40F, FontStyle.Bold, GraphicsUnit.Pixel, 0);
            btnZoomReset.Location = new Point(388, 34);
            btnZoomReset.Margin = new Padding(4);
            btnZoomReset.Name = "btnZoomReset";
            btnZoomReset.Size = new Size(38, 39);
            btnZoomReset.TabIndex = 10;
            btnZoomReset.UseVisualStyleBackColor = false;
            // 
            // btnZoomOut
            // 
            btnZoomOut.BackColor = Color.Transparent;
            btnZoomOut.BackgroundImage = (Image)resources.GetObject("btnZoomOut.BackgroundImage");
            btnZoomOut.BackgroundImageLayout = ImageLayout.Zoom;
            btnZoomOut.Cursor = Cursors.Hand;
            btnZoomOut.FlatAppearance.BorderSize = 0;
            btnZoomOut.FlatStyle = FlatStyle.Flat;
            btnZoomOut.Font = new Font("Microsoft Sans Serif", 40F, FontStyle.Bold, GraphicsUnit.Pixel, 0);
            btnZoomOut.Location = new Point(342, 34);
            btnZoomOut.Margin = new Padding(4);
            btnZoomOut.Name = "btnZoomOut";
            btnZoomOut.Size = new Size(38, 39);
            btnZoomOut.TabIndex = 9;
            btnZoomOut.UseVisualStyleBackColor = false;
            // 
            // btnZoomIn
            // 
            btnZoomIn.BackgroundImage = (Image)resources.GetObject("btnZoomIn.BackgroundImage");
            btnZoomIn.BackgroundImageLayout = ImageLayout.Zoom;
            btnZoomIn.Cursor = Cursors.Hand;
            btnZoomIn.FlatAppearance.BorderSize = 0;
            btnZoomIn.FlatStyle = FlatStyle.Flat;
            btnZoomIn.Font = new Font("Microsoft Sans Serif", 40F, FontStyle.Bold, GraphicsUnit.Pixel, 0);
            btnZoomIn.Location = new Point(297, 34);
            btnZoomIn.Margin = new Padding(4);
            btnZoomIn.Name = "btnZoomIn";
            btnZoomIn.Size = new Size(38, 39);
            btnZoomIn.TabIndex = 8;
            btnZoomIn.UseVisualStyleBackColor = false;
            // 
            // RedoBtn
            // 
            RedoBtn.BackColor = Color.Transparent;
            RedoBtn.BackgroundImage = Properties.Resources.redo;
            RedoBtn.BackgroundImageLayout = ImageLayout.Zoom;
            RedoBtn.Cursor = Cursors.Hand;
            RedoBtn.FlatAppearance.BorderSize = 0;
            RedoBtn.FlatStyle = FlatStyle.Flat;
            RedoBtn.Location = new Point(251, 34);
            RedoBtn.Margin = new Padding(4, 5, 4, 5);
            RedoBtn.Name = "RedoBtn";
            RedoBtn.Size = new Size(38, 39);
            RedoBtn.TabIndex = 4;
            RedoBtn.UseVisualStyleBackColor = false;
            RedoBtn.Click += button3_Click;
            // 
            // UndoBtn
            // 
            UndoBtn.BackColor = Color.Transparent;
            UndoBtn.BackgroundImage = Properties.Resources.undo;
            UndoBtn.BackgroundImageLayout = ImageLayout.Zoom;
            UndoBtn.Cursor = Cursors.Hand;
            UndoBtn.FlatAppearance.BorderSize = 0;
            UndoBtn.FlatStyle = FlatStyle.Flat;
            UndoBtn.Location = new Point(206, 32);
            UndoBtn.Margin = new Padding(4, 5, 4, 5);
            UndoBtn.Name = "UndoBtn";
            UndoBtn.Size = new Size(38, 39);
            UndoBtn.TabIndex = 6;
            UndoBtn.UseVisualStyleBackColor = false;
            UndoBtn.Click += button5_Click;
            // 
            // SaveAsBpmnButton
            // 
            SaveAsBpmnButton.BackColor = Color.Transparent;
            SaveAsBpmnButton.BackgroundImage = (Image)resources.GetObject("SaveAsBpmnButton.BackgroundImage");
            SaveAsBpmnButton.BackgroundImageLayout = ImageLayout.Zoom;
            SaveAsBpmnButton.Cursor = Cursors.Hand;
            SaveAsBpmnButton.FlatAppearance.BorderSize = 0;
            SaveAsBpmnButton.FlatStyle = FlatStyle.Flat;
            SaveAsBpmnButton.Location = new Point(83, 27);
            SaveAsBpmnButton.Name = "SaveAsBpmnButton";
            SaveAsBpmnButton.Size = new Size(26, 32);
            SaveAsBpmnButton.TabIndex = 7;
            SaveAsBpmnButton.UseVisualStyleBackColor = false;
            SaveAsBpmnButton.Click += SaveAsBpmnButton_Click;
            // 
            // SaveAsImageButton
            // 
            SaveAsImageButton.BackColor = Color.Transparent;
            SaveAsImageButton.BackgroundImage = (Image)resources.GetObject("SaveAsImageButton.BackgroundImage");
            SaveAsImageButton.BackgroundImageLayout = ImageLayout.Zoom;
            SaveAsImageButton.FlatAppearance.BorderSize = 0;
            SaveAsImageButton.FlatStyle = FlatStyle.Flat;
            SaveAsImageButton.Location = new Point(151, 34);
            SaveAsImageButton.Margin = new Padding(3, 2, 3, 2);
            SaveAsImageButton.Name = "SaveAsImageButton";
            SaveAsImageButton.Size = new Size(48, 40);
            SaveAsImageButton.TabIndex = 5;
            SaveAsImageButton.UseVisualStyleBackColor = false;
            SaveAsImageButton.Click += SaveAsImageButton_Click;
            // 
            // InfoButton
            // 
            InfoButton.BackColor = Color.Transparent;
            InfoButton.BackgroundImage = (Image)resources.GetObject("InfoButton.BackgroundImage");
            InfoButton.BackgroundImageLayout = ImageLayout.Zoom;
            InfoButton.FlatAppearance.BorderSize = 0;
            InfoButton.FlatStyle = FlatStyle.Flat;
            InfoButton.Location = new Point(13, 32);
            InfoButton.Margin = new Padding(3, 2, 3, 2);
            InfoButton.Name = "InfoButton";
            InfoButton.Size = new Size(50, 41);
            InfoButton.TabIndex = 2;
            InfoButton.UseVisualStyleBackColor = false;
            // 
            // LoadFileButton
            // 
            LoadFileButton.BackColor = Color.Transparent;
            LoadFileButton.BackgroundImage = (Image)resources.GetObject("LoadFileButton.BackgroundImage");
            LoadFileButton.BackgroundImageLayout = ImageLayout.Zoom;
            LoadFileButton.Cursor = Cursors.Hand;
            LoadFileButton.FlatAppearance.BorderSize = 0;
            LoadFileButton.FlatStyle = FlatStyle.Flat;
            LoadFileButton.Location = new Point(52, 27);
            LoadFileButton.Name = "LoadFileButton";
            LoadFileButton.Size = new Size(26, 32);
            LoadFileButton.TabIndex = 3;
            LoadFileButton.UseVisualStyleBackColor = false;
            LoadFileButton.Click += LoadFileButton_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoValidate = AutoValidate.Disable;
            ClientSize = new Size(1033, 743);
            Controls.Add(panel2);
            Controls.Add(sidebar);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            KeyPreview = true;
            Margin = new Padding(4);
            MaximizeBox = false;
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "BPMN editor";
            Load += Form1_Load;
            sidebar.ResumeLayout(false);
            panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)menuButton).EndInit();
            panel2.ResumeLayout(false);
            ResumeLayout(false);

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
        private System.Windows.Forms.Button SaveAsBpmnButton;
        private System.Windows.Forms.Button SaveAsImageButton;
        private System.Windows.Forms.Button LoadFileButton;
        private System.Windows.Forms.Button btnZoomIn;
        private System.Windows.Forms.Button btnZoomReset;
        private System.Windows.Forms.Button btnZoomOut;
        private System.Windows.Forms.FlowLayoutPanel panelFigures;
    }
}

