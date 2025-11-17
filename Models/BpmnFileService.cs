using Kinis.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;

namespace Kinis.Services
{
    public static class BpmnFileService
    {
        private static BpmnProjectState _currentState = new BpmnProjectState();

        // События для уведомления об изменениях
        public static event EventHandler ProjectModified;
        public static event EventHandler ProjectSaved;
        public static event EventHandler ProjectLoaded;

        public static string CurrentFilePath => _currentState.FilePath;
        public static bool HasUnsavedChanges => _currentState.HasUnsavedChanges;
        public static string ProjectName => _currentState.ProjectName;

        /// <summary>
        /// Сохраняет проект в файл BPMN
        /// </summary>
        public static void SaveToBpmnFile(List<BpmnBlock> blocks, List<BpmnArrow> arrows, string filePath)
        {
            try
            {
                // Создаем упрощенную структуру для сериализации
                var project = new SerializableBpmnProject
                {
                    Blocks = blocks?.Select(b => new SerializableBlock(b)).ToList() ?? new List<SerializableBlock>(),
                    Arrows = arrows?.Select(a => new SerializableArrow(a)).ToList() ?? new List<SerializableArrow>(),
                    Created = DateTime.Now,
                    Version = "1.0"
                };

                var serializer = new XmlSerializer(typeof(SerializableBpmnProject));

                using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    serializer.Serialize(writer, project);
                }

                // Обновляем состояние после успешного сохранения
                _currentState.MarkAsSaved(filePath, blocks?.Count ?? 0, arrows?.Count ?? 0);
                ProjectSaved?.Invoke(null, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка сохранения BPMN файла: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Загружает проект из файла BPMN
        /// </summary>
        public static (List<BpmnBlock> blocks, List<BpmnArrow> arrows) LoadFromBpmnFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException("Файл не найден", filePath);

                var serializer = new XmlSerializer(typeof(SerializableBpmnProject));

                using (var reader = new StreamReader(filePath, Encoding.UTF8))
                {
                    var project = (SerializableBpmnProject)serializer.Deserialize(reader);

                    // Восстанавливаем блоки
                    var blocks = project.Blocks?.Select(b => b.ToBpmnBlock()).ToList() ?? new List<BpmnBlock>();

                    // Создаем словарь для восстановления связей
                    var blockDict = blocks.ToDictionary(b => b.Id, b => b);

                    // Восстанавливаем стрелки
                    var arrows = project.Arrows?.Select(a => a.ToBpmnArrow(blockDict)).ToList() ?? new List<BpmnArrow>();

                    // Обновляем состояние после успешной загрузки
                    _currentState.MarkAsLoaded(filePath, blocks.Count, arrows.Count);
                    ProjectLoaded?.Invoke(null, EventArgs.Empty);

                    return (blocks, arrows);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка загрузки BPMN файла: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Отмечает проект как измененный
        /// </summary>
        public static void MarkAsModified()
        {
            _currentState.MarkAsModified();
            ProjectModified?.Invoke(null, EventArgs.Empty);
        }

        /// <summary>
        /// Создает новый проект
        /// </summary>
        public static void NewProject()
        {
            _currentState = new BpmnProjectState();
            ProjectLoaded?.Invoke(null, EventArgs.Empty);
        }

        /// <summary>
        /// Показывает диалог сохранения при несохраненных изменениях
        /// </summary>
        public static DialogResult ShowSaveChangesDialog()
        {
            return MessageBox.Show(
                "У вас есть несохраненные изменения. Хотите сохранить проект перед выходом?",
                "Несохраненные изменения",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1
            );
        }

        /// <summary>
        /// Сохраняет проект с подтверждением (используется при закрытии)
        /// </summary>
        public static bool SaveWithConfirmation(List<BpmnBlock> blocks, List<BpmnArrow> arrows)
        {
            try
            {
                if (CurrentFilePath != null)
                {
                    // Сохраняем в текущий файл
                    SaveToBpmnFile(blocks, arrows, CurrentFilePath);
                    return true;
                }
                else
                {
                    // Показываем диалог сохранения
                    return SaveAsWithDialog(blocks, arrows);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Сохраняет проект как с диалогом выбора файла
        /// </summary>
        public static bool SaveAsWithDialog(List<BpmnBlock> blocks, List<BpmnArrow> arrows)
        {
            try
            {
                using (SaveFileDialog saveDialog = new SaveFileDialog())
                {
                    saveDialog.Filter = "BPMN Files (*.bpmn)|*.bpmn|All files (*.*)|*.*";
                    saveDialog.FilterIndex = 1;
                    saveDialog.DefaultExt = "bpmn";
                    saveDialog.Title = "Сохранить проект";
                    saveDialog.FileName = $"BPMN_Project_{DateTime.Now:yyyyMMdd_HHmmss}.bpmn";

                    if (saveDialog.ShowDialog() == DialogResult.OK)
                    {
                        SaveToBpmnFile(blocks, arrows, saveDialog.FileName);
                        return true;
                    }
                    else
                    {
                        return false; // Пользователь отменил сохранение
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Проверяет необходимость сохранения перед действием
        /// </summary>
        public static bool CheckSaveBeforeAction(List<BpmnBlock> blocks, List<BpmnArrow> arrows,
            FormClosingEventArgs e = null)
        {
            if (!HasUnsavedChanges && !HasAnyElements(blocks, arrows))
                return true;

            var result = ShowSaveChangesDialog();

            switch (result)
            {
                case DialogResult.Yes:
                    return SaveWithConfirmation(blocks, arrows);
                case DialogResult.No:
                    return true; // Продолжаем без сохранения
                case DialogResult.Cancel:
                    if (e != null)
                        e.Cancel = true;
                    return false; // Отменяем действие
                default:
                    return false;
            }
        }

        /// <summary>
        /// Получает заголовок окна с информацией о проекте
        /// </summary>
        public static string GetWindowTitle()
        {
            string title = "BPMN Editor";

            if (!string.IsNullOrEmpty(ProjectName))
            {
                title += " - " + ProjectName;
            }

            if (HasUnsavedChanges)
            {
                title += " *";
            }

            return title;
        }

        /// <summary>
        /// Получает статистику проекта для отображения
        /// </summary>
        public static string GetProjectStats()
        {
            return _currentState.GetStats();
        }

        /// <summary>
        /// Проверяет, есть ли в проекте какие-либо элементы
        /// </summary>
        public static bool HasAnyElements(List<BpmnBlock> blocks, List<BpmnArrow> arrows)
        {
            return (blocks != null && blocks.Count > 0) || (arrows != null && arrows.Count > 0);
        }
    }

    /// <summary>
    /// Класс для отслеживания состояния проекта
    /// </summary>
    public class BpmnProjectState
    {
        public string FilePath { get; private set; }
        public string ProjectName { get; private set; }
        public bool HasUnsavedChanges { get; private set; }
        public DateTime LastSaveTime { get; private set; }
        public int BlockCount { get; private set; }
        public int ArrowCount { get; private set; }
        public DateTime CreateTime { get; private set; }

        public BpmnProjectState()
        {
            CreateTime = DateTime.Now;
            HasUnsavedChanges = false;
        }

        public void MarkAsModified()
        {
            if (!HasUnsavedChanges)
            {
                HasUnsavedChanges = true;
            }
        }

        public void MarkAsSaved(string filePath, int blockCount, int arrowCount)
        {
            FilePath = filePath;
            ProjectName = Path.GetFileNameWithoutExtension(filePath);
            HasUnsavedChanges = false;
            LastSaveTime = DateTime.Now;
            BlockCount = blockCount;
            ArrowCount = arrowCount;
        }

        public void MarkAsLoaded(string filePath, int blockCount, int arrowCount)
        {
            FilePath = filePath;
            ProjectName = Path.GetFileNameWithoutExtension(filePath);
            HasUnsavedChanges = false;
            LastSaveTime = DateTime.Now;
            BlockCount = blockCount;
            ArrowCount = arrowCount;
            CreateTime = DateTime.Now;
        }

        public string GetStats()
        {
            if (string.IsNullOrEmpty(ProjectName))
                return "Новый проект";

            return $"{ProjectName} | Блоки: {BlockCount} | Связи: {ArrowCount} | " +
                   $"{(HasUnsavedChanges ? "Не сохранено" : "Сохранено")}";
        }
    }

    [Serializable]
    [XmlRoot("BpmnProject")]
    public class SerializableBpmnProject
    {
        [XmlArray("Blocks")]
        [XmlArrayItem("Block")]
        public List<SerializableBlock> Blocks { get; set; } = new List<SerializableBlock>();

        [XmlArray("Arrows")]
        [XmlArrayItem("Arrow")]
        public List<SerializableArrow> Arrows { get; set; } = new List<SerializableArrow>();

        [XmlElement("Created")]
        public DateTime Created { get; set; }

        [XmlElement("Version")]
        public string Version { get; set; } = "1.0";

        [XmlElement("Description")]
        public string Description { get; set; } = "BPMN Diagram created with Kinis Editor";
    }

    [Serializable]
    public class SerializableBlock
    {
        [XmlElement("Id")]
        public string Id { get; set; }

        [XmlElement("Type")]
        public string Type { get; set; }

        [XmlElement("Text")]
        public string Text { get; set; }

        [XmlElement("X")]
        public float X { get; set; }

        [XmlElement("Y")]
        public float Y { get; set; }

        [XmlElement("Width")]
        public float Width { get; set; }

        [XmlElement("Height")]
        public float Height { get; set; }

        [XmlElement("FillColor")]
        public string FillColor { get; set; }

        [XmlElement("BorderColor")]
        public string BorderColor { get; set; }

        // Конструктор по умолчанию для сериализации
        public SerializableBlock() { }

        public SerializableBlock(BpmnBlock block)
        {
            Id = block.Id;
            Type = block.Type;
            Text = block.Text;
            X = block.Bounds.X;
            Y = block.Bounds.Y;
            Width = block.Bounds.Width;
            Height = block.Bounds.Height;
            FillColor = block.FillColor.Name;
            BorderColor = block.BorderColor.Name;
        }

        public BpmnBlock ToBpmnBlock()
        {
            return new BpmnBlock(X, Y, Width, Height)
            {
                Id = Id,
                Type = Type,
                Text = Text,
                FillColor = Color.FromName(FillColor),
                BorderColor = Color.FromName(BorderColor)
            };
        }
    }

    [Serializable]
    public class SerializableArrow
    {
        [XmlElement("Id")]
        public string Id { get; set; }

        [XmlElement("Text")]
        public string Text { get; set; }

        [XmlElement("StartBlockId")]
        public string StartBlockId { get; set; }

        [XmlElement("StartX")]
        public float StartX { get; set; }

        [XmlElement("StartY")]
        public float StartY { get; set; }

        [XmlElement("EndBlockId")]
        public string EndBlockId { get; set; }

        [XmlElement("EndX")]
        public float EndX { get; set; }

        [XmlElement("EndY")]
        public float EndY { get; set; }

        [XmlElement("Color")]
        public string Color { get; set; }

        [XmlElement("Width")]
        public float Width { get; set; }

        // Конструктор по умолчанию для сериализации
        public SerializableArrow() { }

        public SerializableArrow(BpmnArrow arrow)
        {
            Id = arrow.Id;
            Text = arrow.Text;
            StartBlockId = arrow.StartBlock?.Id;
            StartX = arrow.StartPoint.X;
            StartY = arrow.StartPoint.Y;
            EndBlockId = arrow.EndBlock?.Id;
            EndX = arrow.EndPoint.X;
            EndY = arrow.EndPoint.Y;
            Color = arrow.Color.Name;
            Width = arrow.Width;
        }

        public BpmnArrow ToBpmnArrow(Dictionary<string, BpmnBlock> blockDictionary)
        {
            var arrow = new BpmnArrow
            {
                Id = Id,
                Text = Text,
                StartPoint = new System.Drawing.PointF(StartX, StartY),
                EndPoint = new System.Drawing.PointF(EndX, EndY),
                Color = System.Drawing.Color.FromName(Color),
                Width = Width
            };

            // Восстанавливаем связи с блоками
            if (!string.IsNullOrEmpty(StartBlockId) && blockDictionary.ContainsKey(StartBlockId))
                arrow.StartBlock = blockDictionary[StartBlockId];

            if (!string.IsNullOrEmpty(EndBlockId) && blockDictionary.ContainsKey(EndBlockId))
                arrow.EndBlock = blockDictionary[EndBlockId];

            return arrow;
        }
    }
}