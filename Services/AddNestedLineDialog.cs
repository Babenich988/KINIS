using System;
using System.Windows.Forms;

namespace Kinis
{
    /// <summary>
    /// Диалоговое окно для добавления вложенной дорожки в пул
    /// </summary>
    public partial class AddNestedLineDialog : Form
    {
        /// <summary>
        /// Получает название вложенной дорожки
        /// </summary>
        public string LineName { get; private set; }

        /// <summary>
        /// Получает название родительской дорожки
        /// </summary>
        public string ParentLaneName { get; private set; }

        /// <summary>
        /// Инициализирует диалог для добавления вложенной дорожки
        /// </summary>
        /// <param name="parentLaneName">Название родительской дорожки</param>
        public AddNestedLineDialog(string parentLaneName)
        {
            ParentLaneName = parentLaneName;
            InitializeComponent();
        }

        /// <summary>
        /// Инициализирует компоненты пользовательского интерфейса диалогового окна
        /// </summary>
        private void InitializeComponent()
        {
            this.Text = "Добавить вложенную дорожку";
            this.Size = new System.Drawing.Size(350, 150);
            this.StartPosition = FormStartPosition.CenterParent;

            // Label с информацией о родительской дорожке
            var label = new Label();
            label.Text = $"Вложенная дорожка для: {ParentLaneName}";
            label.Location = new System.Drawing.Point(20, 15);
            label.Size = new System.Drawing.Size(300, 20);
            this.Controls.Add(label);

            // Label с инструкцией
            var label2 = new Label();
            label2.Text = "Введите название вложенной дорожки:";
            label2.Location = new System.Drawing.Point(20, 40);
            label2.Size = new System.Drawing.Size(300, 20);
            this.Controls.Add(label2);

            // TextBox для ввода названия дорожки
            var textBox = new TextBox();
            textBox.Location = new System.Drawing.Point(20, 65);
            textBox.Size = new System.Drawing.Size(290, 20);
            textBox.Name = "txtLineName";
            textBox.Text = "Вложенная дорожка";
            this.Controls.Add(textBox);

            // Кнопка подтверждения
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

            // Кнопка отмены
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