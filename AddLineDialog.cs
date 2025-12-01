using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace Kinis
{
    public partial class AddLineDialog : Form
    {
        public string LineName { get; private set; }

        public AddLineDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Добавить дорожку";
            this.Size = new System.Drawing.Size(300, 150);
            this.StartPosition = FormStartPosition.CenterParent;

            // Label
            var label = new Label();
            label.Text = "Введите название дорожки:";
            label.Location = new System.Drawing.Point(20, 20);
            label.Size = new System.Drawing.Size(250, 20);
            this.Controls.Add(label);

            // TextBox
            var textBox = new TextBox();
            textBox.Location = new System.Drawing.Point(20, 50);
            textBox.Size = new System.Drawing.Size(240, 20);
            textBox.Name = "txtLineName";
            textBox.Text = "Дорожка";
            this.Controls.Add(textBox);

            // Кнопки
            var btnOk = new Button();
            btnOk.Text = "OK";
            btnOk.Location = new System.Drawing.Point(100, 85);
            btnOk.DialogResult = DialogResult.OK;
            btnOk.Click += (s, e) =>
            {
                LineName = textBox.Text;
                this.Close();
            };
            this.Controls.Add(btnOk);

            var btnCancel = new Button();
            btnCancel.Text = "Отмена";
            btnCancel.Location = new System.Drawing.Point(180, 85);
            btnCancel.DialogResult = DialogResult.Cancel;
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }
    }
}
