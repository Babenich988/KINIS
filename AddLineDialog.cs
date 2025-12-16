using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace Kinis
{
    /// <summary>
    /// Диалоговое окно для добавления новой дорожки в пул
    /// </summary>
    public partial class AddLineDialog : Form
    {
        /// <summary>
        /// Получает название дорожки, введенное пользователем
        /// </summary>
        public string LineName { get; private set; }

        /// <summary>
        /// Инициализирует новый экземпляр диалога добавления дорожки
        /// </summary>
        public AddLineDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Инициализирует компоненты пользовательского интерфейса диалогового окна
        /// </summary>
        private void InitializeComponent()
        {
            this.Text = "Добавить дорожку";
            this.Size = new System.Drawing.Size(300, 150);
            this.StartPosition = FormStartPosition.CenterParent;

            // Label - описание поля ввода
            var label = new Label();
            label.Text = "Введите название дорожки:";
            label.Location = new System.Drawing.Point(20, 20);
            label.Size = new System.Drawing.Size(250, 20);
            this.Controls.Add(label);

            // TextBox - поле для ввода названия дорожки
            var textBox = new TextBox();
            textBox.Location = new System.Drawing.Point(20, 50);
            textBox.Size = new System.Drawing.Size(240, 20);
            textBox.Name = "txtLineName";
            textBox.Text = "Дорожка";
            this.Controls.Add(textBox);

            // Кнопка подтверждения
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

            // Кнопка отмены
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