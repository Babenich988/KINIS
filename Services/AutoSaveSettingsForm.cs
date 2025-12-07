using System;
using System.Drawing;
using System.Windows.Forms;

namespace Kinis
{
    public partial class AutoSaveSettingsForm : Form
    {
        public bool AutoSaveEnabled { get; private set; }
        public int AutoSaveInterval { get; private set; }

        private CheckBox enableCheckBox;
        private NumericUpDown intervalNumeric;
        private Button saveButton;
        private Button cancelButton;
        private Label intervalLabel;

        public AutoSaveSettingsForm(bool currentEnabled, int currentInterval)
        {
            InitializeComponents();
            LoadSettings(currentEnabled, currentInterval);
        }

        private void InitializeComponents()
        {
            this.Text = "Настройки автосохранения";
            this.Size = new Size(300, 180);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // CheckBox для включения автосохранения
            enableCheckBox = new CheckBox
            {
                Text = "Включить автосохранение",
                Location = new Point(20, 20),
                Size = new Size(250, 20)
            };
            enableCheckBox.CheckedChanged += EnableCheckBox_CheckedChanged;

            // Label для интервала
            intervalLabel = new Label
            {
                Text = "Интервал автосохранения (минуты):",
                Location = new Point(20, 50),
                Size = new Size(200, 20)
            };

            // NumericUpDown для выбора интервала
            intervalNumeric = new NumericUpDown
            {
                Location = new Point(220, 48),
                Size = new Size(50, 20),
                Minimum = 1,
                Maximum = 10,
                Value = 5,
                DecimalPlaces = 0, // Только целые числа
                Increment = 1
            };

            // ДОБАВЛЯЕМ: Обработчик для блокировки нечислового ввода
            intervalNumeric.KeyPress += IntervalNumeric_KeyPress;

            // ДОБАВЛЯЕМ: Обработчик для блокировки Ctrl+V
            intervalNumeric.KeyDown += IntervalNumeric_KeyDown;

            // Кнопка сохранения
            saveButton = new Button
            {
                Text = "Сохранить",
                Location = new Point(120, 90),
                Size = new Size(75, 25),
                DialogResult = DialogResult.OK
            };

            // Кнопка отмены
            cancelButton = new Button
            {
                Text = "Отмена",
                Location = new Point(200, 90),
                Size = new Size(75, 25),
                DialogResult = DialogResult.Cancel
            };

            // Добавляем элементы на форму
            this.Controls.Add(enableCheckBox);
            this.Controls.Add(intervalLabel);
            this.Controls.Add(intervalNumeric);
            this.Controls.Add(saveButton);
            this.Controls.Add(cancelButton);

            this.AcceptButton = saveButton;
            this.CancelButton = cancelButton;
        }

        // ДОБАВЛЯЕМ: Метод для блокировки нечислового ввода
        private void IntervalNumeric_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Разрешаем только: цифры, Backspace, Delete, Tab, стрелки
            // NumericUpDown сам обрабатывает стрелки, но проверяем другие символы

            // Если это не управляющий символ и не цифра - блокируем
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true; // Блокируем ввод
            }
        }

        // ДОБАВЛЯЕМ: Метод для блокировки Ctrl+V
        private void IntervalNumeric_KeyDown(object sender, KeyEventArgs e)
        {
            // Блокируем Ctrl+V (Вставить)
            if (e.Control && e.KeyCode == Keys.V)
            {
                e.SuppressKeyPress = true; // Блокируем обработку клавиши
                e.Handled = true; // Помечаем как обработанное
            }

            // Блокируем Shift+Insert (альтернативная вставка)
            if (e.Shift && e.KeyCode == Keys.Insert)
            {
                e.SuppressKeyPress = true;
                e.Handled = true;
            }
        }

        private void LoadSettings(bool enabled, int interval)
        {
            // Гарантируем целое число в пределах допустимого
            int safeInterval = Math.Max(1, Math.Min(10, interval));

            enableCheckBox.Checked = enabled;
            intervalNumeric.Value = safeInterval;
            UpdateControlsState();
        }

        private void EnableCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            UpdateControlsState();
        }

        private void UpdateControlsState()
        {
            intervalLabel.Enabled = enableCheckBox.Checked;
            intervalNumeric.Enabled = enableCheckBox.Checked;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (this.DialogResult == DialogResult.OK)
            {
                // Гарантируем целое число
                AutoSaveEnabled = enableCheckBox.Checked;
                AutoSaveInterval = (int)intervalNumeric.Value;
            }
            base.OnFormClosing(e);
        }
    }
}