using System;
using System.Windows.Forms;

namespace Kinis
{
    public partial class AddNestedLineDialog : Form
    {
        public string LineName { get; private set; }
        public string ParentLaneName { get; private set; }

        public AddNestedLineDialog(string parentLaneName)
        {
            ParentLaneName = parentLaneName;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Добавить вложенную дорожку";
            this.Size = new System.Drawing.Size(350, 150);
            this.StartPosition = FormStartPosition.CenterParent;

            // Label
            var label = new Label();
            label.Text = $"Вложенная дорожка для: {ParentLaneName}";
            label.Location = new System.Drawing.Point(20, 15);
            label.Size = new System.Drawing.Size(300, 20);
            this.Controls.Add(label);

            var label2 = new Label();
            label2.Text = "Введите название вложенной дорожки:";
            label2.Location = new System.Drawing.Point(20, 40);
            label2.Size = new System.Drawing.Size(300, 20);
            this.Controls.Add(label2);

            // TextBox
            var textBox = new TextBox();
            textBox.Location = new System.Drawing.Point(20, 65);
            textBox.Size = new System.Drawing.Size(290, 20);
            textBox.Name = "txtLineName";
            textBox.Text = "Вложенная дорожка";
            this.Controls.Add(textBox);

            // Кнопки
            var btnOk = new Button();
            btnOk.Text = "OK";
            btnOk.Location = new System.Drawing.Point(130, 100);
            btnOk.DialogResult = DialogResult.OK;
            btnOk.Click += (s, e) =>
            {
                LineName = textBox.Text;
                this.Close();
            };
            this.Controls.Add(btnOk);

            var btnCancel = new Button();
            btnCancel.Text = "Отмена";
            btnCancel.Location = new System.Drawing.Point(210, 100);
            btnCancel.DialogResult = DialogResult.Cancel;
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }
    }
}