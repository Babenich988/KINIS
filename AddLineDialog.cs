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
        public string LineName { get; private set; } = "New Lane";

        public AddLineDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Добавить дорожку";
            this.Size = new Size(300, 150);
            this.StartPosition = FormStartPosition.CenterParent;

            var label = new Label { Text = "Название:", Location = new Point(20, 26) };
            var textBox = new TextBox { Text = "Новая дорожка", Location = new Point(20, 45), Width = 250 };
            var okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(120, 80) };
            var cancelButton = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Location = new Point(200, 80) };

            this.Controls.AddRange(new Control[] { label, textBox, okButton, cancelButton });

            okButton.Click += (s, e) => { LineName = textBox.Text; };
        }
    }
}
