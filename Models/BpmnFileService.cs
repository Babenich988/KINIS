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
    /// <summary>
    /// Статический сервис для работы с файлами BPMN
    /// </summary>
    public static class BpmnFileService
    {
        private static BpmnProjectState _currentState = new BpmnProjectState();

        // События для уведомления об изменениях
        /// <summary>
        /// Событие, возникающее при изменении проекта
        /// </summary>
        public static event EventHandler ProjectModified;

        /// <summary>
        /// Событие, возникающее при сохранении проекта
        /// </summary>
        public static event EventHandler ProjectSaved;

        /// <summary>
        /// Событие, возникающее при загрузке проекта
        /// </summary>
        public static event EventHandler ProjectLoaded;

        /// <summary>
        /// Получает путь к текущему файлу проекта
        /// </summary>
        public static string CurrentFilePath => _currentState.FilePath;

        /// <summary>
        /// Получает значение, указывающее есть ли несохраненные изменения
        /// </summary>
        public static bool HasUnsavedChanges => _currentState.HasUnsavedChanges;

        /// <summary>
        /// Получает имя текущего проекта
        /// </summary>
        public static string ProjectName => _currentState.ProjectName;

        /// <summary>
        /// Сохраняет проект в файл BPMN
        /// </summary>
        /// <param name="blocks">Список блоков для сохранения</param>
        /// <param name="arrows">Список стрелок для сохранения</param>
        /// <param name="curvedArrows">Список кривых стрелок для сохранения</param>
        /// <param name="filePath">Путь к файлу для сохранения</param>
        /// <exception cref="Exception">Выбрасывается при ошибке сохранения</exception>
        public static void SaveToBpmnFile(List<BpmnBlock> blocks, List<BpmnArrow> arrows, List<BpmnCurvedArrow> curvedArrows, string filePath)
        {
            try
            {
                // Создаем упрощенную структуру для сериализации
                var project = new SerializableBpmnProject
                {
                    Blocks = blocks?.Select(b => new SerializableBlock(b)).ToList() ?? new List<SerializableBlock>(),
                    Arrows = arrows?.Select(a => new SerializableArrow(a)).ToList() ?? new List<SerializableArrow>(),
                    CurvedArrows = curvedArrows?.Select(c => new SerializableCurvedArrow(c)).ToList() ?? new List<SerializableCurvedArrow>(),
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
        /// Сохраняет проект без изменения состояния (для автосохранения)
        /// </summary>
        /// <param name="blocks">Список блоков</param>
        /// <param name="arrows">Список стрелок</param>
        /// <param name="curvedArrows">Список кривых стрелок</param>
        /// <param name="filePath">Путь к файлу автосохранения</param>
        public static void SaveForAutoSave(List<BpmnBlock> blocks, List<BpmnArrow> arrows, List<BpmnCurvedArrow> curvedArrows, string filePath)
        {
            try
            {
                var project = new SerializableBpmnProject
                {
                    Blocks = blocks?.Select(b => new SerializableBlock(b)).ToList() ?? new List<SerializableBlock>(),
                    Arrows = arrows?.Select(a => new SerializableArrow(a)).ToList() ?? new List<SerializableArrow>(),
                    CurvedArrows = curvedArrows?.Select(c => new SerializableCurvedArrow(c)).ToList() ?? new List<SerializableCurvedArrow>(),
                    Created = DateTime.Now,
                    Version = "1.0"
                };

                var serializer = new XmlSerializer(typeof(SerializableBpmnProject));

                using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    serializer.Serialize(writer, project);
                }

                Console.WriteLine($"Автосохранение: файл {filePath} обновлен");
            }
            catch (Exception ex)
            {
                // Для автосохранения просто логируем ошибку, не прерываем работу
                System.Diagnostics.Debug.WriteLine($"Ошибка автосохранения: {ex.Message}");
            }
        }

        /// <summary>
        /// Загружает проект из файла BPMN
        /// </summary>
        /// <param name="filePath">Путь к файлу BPMN</param>
        /// <returns>Кортеж со списками блоков, стрелок и кривых стрелок</returns>
        /// <exception cref="FileNotFoundException">Выбрасывается если файл не найден</exception>
        /// <exception cref="Exception">Выбрасывается при ошибке загрузки</exception>
        public static (List<BpmnBlock> blocks, List<BpmnArrow> arrows, List<BpmnCurvedArrow> curvedArrows) LoadFromBpmnFile(string filePath)
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

                    // Восстанавливаем кривые стрелки
                    var curvedArrows = project.CurvedArrows?.Select(c => c.ToBpmnCurvedArrow(blockDict)).ToList() ?? new List<BpmnCurvedArrow>();

                    // Обновляем состояние после успешной загрузки
                    _currentState.MarkAsLoaded(filePath, blocks.Count, arrows.Count);
                    ProjectLoaded?.Invoke(null, EventArgs.Empty);

                    return (blocks, arrows, curvedArrows);
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
            if (!_currentState.HasUnsavedChanges)
            {
                _currentState.MarkAsModified();
                ProjectModified?.Invoke(null, EventArgs.Empty);
            }
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
        /// <returns>Результат диалога</returns>
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
        /// <param name="blocks">Список блоков</param>
        /// <param name="arrows">Список стрелок</param>
        /// <param name="curvedArrows">Список кривых стрелок</param>
        /// <returns>True если сохранение успешно, иначе False</returns>
        public static bool SaveWithConfirmation(List<BpmnBlock> blocks, List<BpmnArrow> arrows, List<BpmnCurvedArrow> curvedArrows = null)
        {
            try
            {
                if (CurrentFilePath != null)
                {
                    // Сохраняем в текущий файл
                    SaveToBpmnFile(blocks, arrows, curvedArrows ?? new List<BpmnCurvedArrow>(), CurrentFilePath);
                    return true;
                }
                else
                {
                    // Показываем диалог сохранения
                    return SaveAsWithDialog(blocks, arrows, curvedArrows);
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
        /// <param name="blocks">Список блоков</param>
        /// <param name="arrows">Список стрелок</param>
        /// <param name="curvedArrows">Список кривых стрелок</param>
        /// <returns>True если сохранение успешно, иначе False</returns>
        public static bool SaveAsWithDialog(List<BpmnBlock> blocks, List<BpmnArrow> arrows, List<BpmnCurvedArrow> curvedArrows = null)
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
                        SaveToBpmnFile(blocks, arrows, curvedArrows ?? new List<BpmnCurvedArrow>(), saveDialog.FileName);
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
        /// <param name="blocks">Список блоков</param>
        /// <param name="arrows">Список стрелок</param>
        /// <param name="curvedArrows">Список кривых стрелок</param>
        /// <param name="e">Аргументы события закрытия формы</param>
        /// <returns>True если можно продолжить действие, иначе False</returns>
        public static bool CheckSaveBeforeAction(List<BpmnBlock> blocks, List<BpmnArrow> arrows, List<BpmnCurvedArrow> curvedArrows = null,
            FormClosingEventArgs e = null)
        {
            // ИЗМЕНЕНИЕ: проверяем только несохраненные изменения, не наличие элементов
            if (!HasUnsavedChanges)
                return true;

            var result = ShowSaveChangesDialog();

            switch (result)
            {
                case DialogResult.Yes:
                    return SaveWithConfirmation(blocks, arrows, curvedArrows);
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
        /// <returns>Строка заголовка окна</returns>
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
        /// <returns>Строка со статистикой проекта</returns>
        public static string GetProjectStats()
        {
            return _currentState.GetStats();
        }

        /// <summary>
        /// Проверяет, есть ли в проекте какие-либо элементы
        /// </summary>
        /// <param name="blocks">Список блоков</param>
        /// <param name="arrows">Список стрелок</param>
        /// <returns>True если есть хотя бы один элемент</returns>
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
        /// <summary>
        /// Получает путь к текущему файлу проекта
        /// </summary>
        public string FilePath { get; private set; }

        /// <summary>
        /// Получает имя проекта
        /// </summary>
        public string ProjectName { get; private set; }

        /// <summary>
        /// Получает значение, указывающее есть ли несохраненные изменения
        /// </summary>
        public bool HasUnsavedChanges { get; private set; }

        /// <summary>
        /// Получает время последнего сохранения
        /// </summary>
        public DateTime LastSaveTime { get; private set; }

        /// <summary>
        /// Получает количество блоков в проекте
        /// </summary>
        public int BlockCount { get; private set; }

        /// <summary>
        /// Получает количество стрелок в проекте
        /// </summary>
        public int ArrowCount { get; private set; }

        /// <summary>
        /// Получает время создания проекта
        /// </summary>
        public DateTime CreateTime { get; private set; }

        /// <summary>
        /// Инициализирует новый экземпляр состояния проекта
        /// </summary>
        public BpmnProjectState()
        {
            CreateTime = DateTime.Now;
            HasUnsavedChanges = false;
        }

        /// <summary>
        /// Помечает проект как измененный
        /// </summary>
        public void MarkAsModified()
        {
            if (!HasUnsavedChanges)
            {
                HasUnsavedChanges = true;
            }
        }

        /// <summary>
        /// Помечает проект как сохраненный
        /// </summary>
        /// <param name="filePath">Путь к сохраненному файлу</param>
        /// <param name="blockCount">Количество блоков</param>
        /// <param name="arrowCount">Количество стрелок</param>
        public void MarkAsSaved(string filePath, int blockCount, int arrowCount)
        {
            FilePath = filePath;
            ProjectName = Path.GetFileNameWithoutExtension(filePath);
            HasUnsavedChanges = false;
            LastSaveTime = DateTime.Now;
            BlockCount = blockCount;
            ArrowCount = arrowCount;
        }

        /// <summary>
        /// Помечает проект как загруженный
        /// </summary>
        /// <param name="filePath">Путь к загруженному файлу</param>
        /// <param name="blockCount">Количество блоков</param>
        /// <param name="arrowCount">Количество стрелок</param>
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

        /// <summary>
        /// Получает статистику проекта в виде строки
        /// </summary>
        /// <returns>Строка со статистикой проекта</returns>
        public string GetStats()
        {
            if (string.IsNullOrEmpty(ProjectName))
                return "Новый проект";

            return $"{ProjectName} | Блоки: {BlockCount} | Связи: {ArrowCount} | " +
                   $"{(HasUnsavedChanges ? "Не сохранено" : "Сохранено")}";
        }
    }

    /// <summary>
    /// Сериализуемый проект BPMN для сохранения в XML
    /// </summary>
    [Serializable]
    [XmlRoot("BpmnProject")]
    public class SerializableBpmnProject
    {
        /// <summary>
        /// Список блоков проекта
        /// </summary>
        [XmlArray("Blocks")]
        [XmlArrayItem("Block")]
        public List<SerializableBlock> Blocks { get; set; } = new List<SerializableBlock>();

        /// <summary>
        /// Список стрелок проекта
        /// </summary>
        [XmlArray("Arrows")]
        [XmlArrayItem("Arrow")]
        public List<SerializableArrow> Arrows { get; set; } = new List<SerializableArrow>();

        /// <summary>
        /// Список кривых стрелок проекта
        /// </summary>
        [XmlArray("CurvedArrows")]
        [XmlArrayItem("CurvedArrow")]
        public List<SerializableCurvedArrow> CurvedArrows { get; set; } = new List<SerializableCurvedArrow>();

        /// <summary>
        /// Дата и время создания проекта
        /// </summary>
        [XmlElement("Created")]
        public DateTime Created { get; set; }

        /// <summary>
        /// Версия формата проекта
        /// </summary>
        [XmlElement("Version")]
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// Описание проекта
        /// </summary>
        [XmlElement("Description")]
        public string Description { get; set; } = "BPMN Diagram created with Kinis Editor";
    }

    /// <summary>
    /// Сериализуемый блок BPMN
    /// </summary>
    [Serializable]
    public class SerializableBlock
    {
        /// <summary>
        /// Идентификатор блока
        /// </summary>
        [XmlElement("Id")]
        public string Id { get; set; }

        /// <summary>
        /// Тип блока
        /// </summary>
        [XmlElement("Type")]
        public string Type { get; set; }

        /// <summary>
        /// Текст блока
        /// </summary>
        [XmlElement("Text")]
        public string Text { get; set; }

        /// <summary>
        /// Координата X блока
        /// </summary>
        [XmlElement("X")]
        public float X { get; set; }

        /// <summary>
        /// Координата Y блока
        /// </summary>
        [XmlElement("Y")]
        public float Y { get; set; }

        /// <summary>
        /// Ширина блока
        /// </summary>
        [XmlElement("Width")]
        public float Width { get; set; }

        /// <summary>
        /// Высота блока
        /// </summary>
        [XmlElement("Height")]
        public float Height { get; set; }

        /// <summary>
        /// Цвет заливки блока
        /// </summary>
        [XmlElement("FillColor")]
        public string FillColor { get; set; }

        /// <summary>
        /// Цвет границы блока
        /// </summary>
        [XmlElement("BorderColor")]
        public string BorderColor { get; set; }

        /// <summary>
        /// Инициализирует новый экземпляр сериализуемого блока
        /// </summary>
        public SerializableBlock() { }

        /// <summary>
        /// Инициализирует новый экземпляр сериализуемого блока на основе BpmnBlock
        /// </summary>
        /// <param name="block">Исходный блок BPMN</param>
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

        /// <summary>
        /// Преобразует сериализуемый блок в BpmnBlock
        /// </summary>
        /// <returns>Экземпляр BpmnBlock</returns>
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

    /// <summary>
    /// Сериализуемая стрелка BPMN
    /// </summary>
    [Serializable]
    public class SerializableArrow
    {
        /// <summary>
        /// Идентификатор стрелки
        /// </summary>
        [XmlElement("Id")]
        public string Id { get; set; }

        /// <summary>
        /// Текст стрелки
        /// </summary>
        [XmlElement("Text")]
        public string Text { get; set; }

        /// <summary>
        /// Идентификатор начального блока
        /// </summary>
        [XmlElement("StartBlockId")]
        public string StartBlockId { get; set; }

        /// <summary>
        /// Координата X начальной точки
        /// </summary>
        [XmlElement("StartX")]
        public float StartX { get; set; }

        /// <summary>
        /// Координата Y начальной точки
        /// </summary>
        [XmlElement("StartY")]
        public float StartY { get; set; }

        /// <summary>
        /// Идентификатор конечного блока
        /// </summary>
        [XmlElement("EndBlockId")]
        public string EndBlockId { get; set; }

        /// <summary>
        /// Координата X конечной точки
        /// </summary>
        [XmlElement("EndX")]
        public float EndX { get; set; }

        /// <summary>
        /// Координата Y конечной точки
        /// </summary>
        [XmlElement("EndY")]
        public float EndY { get; set; }

        /// <summary>
        /// Цвет стрелки
        /// </summary>
        [XmlElement("Color")]
        public string Color { get; set; }

        /// <summary>
        /// Толщина линии стрелки
        /// </summary>
        [XmlElement("Width")]
        public float Width { get; set; }

        /// <summary>
        /// Инициализирует новый экземпляр сериализуемой стрелки
        /// </summary>
        public SerializableArrow() { }

        /// <summary>
        /// Инициализирует новый экземпляр сериализуемой стрелки на основе BpmnArrow
        /// </summary>
        /// <param name="arrow">Исходная стрелка BPMN</param>
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

        /// <summary>
        /// Преобразует сериализуемую стрелку в BpmnArrow
        /// </summary>
        /// <param name="blockDictionary">Словарь блоков для восстановления связей</param>
        /// <returns>Экземпляр BpmnArrow</returns>
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

    /// <summary>
    /// Сериализуемая кривая стрелка BPMN
    /// </summary>
    [Serializable]
    public class SerializableCurvedArrow
    {
        /// <summary>
        /// Идентификатор кривой стрелки
        /// </summary>
        [XmlElement("Id")]
        public string Id { get; set; }

        /// <summary>
        /// Текст кривой стрелки
        /// </summary>
        [XmlElement("Text")]
        public string Text { get; set; }

        /// <summary>
        /// Идентификатор начального блока
        /// </summary>
        [XmlElement("StartBlockId")]
        public string StartBlockId { get; set; }

        /// <summary>
        /// Координата X начальной точки
        /// </summary>
        [XmlElement("StartX")]
        public float StartX { get; set; }

        /// <summary>
        /// Координата Y начальной точки
        /// </summary>
        [XmlElement("StartY")]
        public float StartY { get; set; }

        /// <summary>
        /// Идентификатор конечного блока
        /// </summary>
        [XmlElement("EndBlockId")]
        public string EndBlockId { get; set; }

        /// <summary>
        /// Координата X конечной точки
        /// </summary>
        [XmlElement("EndX")]
        public float EndX { get; set; }

        /// <summary>
        /// Координата Y конечной точки
        /// </summary>
        [XmlElement("EndY")]
        public float EndY { get; set; }

        /// <summary>
        /// Цвет кривой стрелки
        /// </summary>
        [XmlElement("Color")]
        public string Color { get; set; }

        /// <summary>
        /// Толщина линии кривой стрелки
        /// </summary>
        [XmlElement("Width")]
        public float Width { get; set; }

        /// <summary>
        /// Координата X первой контрольной точки
        /// </summary>
        [XmlElement("ControlPoint1X")]
        public float ControlPoint1X { get; set; }

        /// <summary>
        /// Координата Y первой контрольной точки
        /// </summary>
        [XmlElement("ControlPoint1Y")]
        public float ControlPoint1Y { get; set; }

        /// <summary>
        /// Координата X второй контрольной точки
        /// </summary>
        [XmlElement("ControlPoint2X")]
        public float ControlPoint2X { get; set; }

        /// <summary>
        /// Координата Y второй контрольной точки
        /// </summary>
        [XmlElement("ControlPoint2Y")]
        public float ControlPoint2Y { get; set; }

        /// <summary>
        /// Указывает является ли стрелка плавающей
        /// </summary>
        [XmlElement("IsFloating")]
        public bool IsFloating { get; set; }

        /// <summary>
        /// Индекс точки привязки начала стрелки
        /// </summary>
        [XmlElement("StartConnectionPointIndex")]
        public int StartConnectionPointIndex { get; set; } = -1;

        /// <summary>
        /// Индекс точки привязки конца стрелки
        /// </summary>
        [XmlElement("EndConnectionPointIndex")]
        public int EndConnectionPointIndex { get; set; } = -1;

        /// <summary>
        /// Инициализирует новый экземпляр сериализуемой кривой стрелки
        /// </summary>
        public SerializableCurvedArrow() { }

        /// <summary>
        /// Инициализирует новый экземпляр сериализуемой кривой стрелки на основе BpmnCurvedArrow
        /// </summary>
        /// <param name="curvedArrow">Исходная кривая стрелка BPMN</param>
        public SerializableCurvedArrow(BpmnCurvedArrow curvedArrow)
        {
            Id = curvedArrow.Id;
            Text = curvedArrow.Text;
            StartBlockId = curvedArrow.StartBlock?.Id;
            StartX = curvedArrow.StartPoint.X;
            StartY = curvedArrow.StartPoint.Y;
            EndBlockId = curvedArrow.EndBlock?.Id;
            EndX = curvedArrow.EndPoint.X;
            EndY = curvedArrow.EndPoint.Y;
            Color = curvedArrow.Color.Name;
            Width = curvedArrow.Width;
            ControlPoint1X = curvedArrow.ControlPoint1.X;
            ControlPoint1Y = curvedArrow.ControlPoint1.Y;
            ControlPoint2X = curvedArrow.ControlPoint2.X;
            ControlPoint2Y = curvedArrow.ControlPoint2.Y;
            IsFloating = curvedArrow.IsFloating;
            StartConnectionPointIndex = curvedArrow.StartConnectionPointIndex;
            EndConnectionPointIndex = curvedArrow.EndConnectionPointIndex;
        }

        /// <summary>
        /// Преобразует сериализуемую кривую стрелку в BpmnCurvedArrow
        /// </summary>
        /// <param name="blockDictionary">Словарь блоков для восстановления связей</param>
        /// <returns>Экземпляр BpmnCurvedArrow</returns>
        public BpmnCurvedArrow ToBpmnCurvedArrow(Dictionary<string, BpmnBlock> blockDictionary)
        {
            var curvedArrow = new BpmnCurvedArrow
            {
                Id = Id,
                Text = Text,
                StartPoint = new System.Drawing.PointF(StartX, StartY),
                EndPoint = new System.Drawing.PointF(EndX, EndY),
                Color = System.Drawing.Color.FromName(Color),
                Width = Width,
                ControlPoint1 = new System.Drawing.PointF(ControlPoint1X, ControlPoint1Y),
                ControlPoint2 = new System.Drawing.PointF(ControlPoint2X, ControlPoint2Y),
                IsFloating = IsFloating,
                StartConnectionPointIndex = StartConnectionPointIndex,
                EndConnectionPointIndex = EndConnectionPointIndex
            };

            // Восстанавливаем связи с блоками
            if (!string.IsNullOrEmpty(StartBlockId) && blockDictionary.ContainsKey(StartBlockId))
                curvedArrow.StartBlock = blockDictionary[StartBlockId];

            if (!string.IsNullOrEmpty(EndBlockId) && blockDictionary.ContainsKey(EndBlockId))
                curvedArrow.EndBlock = blockDictionary[EndBlockId];

            return curvedArrow;
        }
    }
}