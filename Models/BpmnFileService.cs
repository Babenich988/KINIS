using Kinis.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Kinis.Services
{
    /// <summary>
    /// Сервис для работы с BPMN файлами - сохранение и загрузка проектов
    /// </summary>
    public static class BpmnFileService
    {
        /// <summary>
        /// Сохраняет проект в BPMN файл
        /// Преобразует BpmnBlock/BpmnArrow в сериализуемые объекты и сохраняет в XML
        /// </summary>
        public static void SaveToBpmnFile(List<BpmnBlock> blocks, List<BpmnArrow> arrows, string filePath)
        {
            try
            {
                // Создаем проект с сериализуемыми объектами
                var project = new SerializableBpmnProject
                {
                    // Преобразуем каждый BpmnBlock в SerializableBlock
                    Blocks = blocks?.Select(b => new SerializableBlock(b)).ToList() ?? new List<SerializableBlock>(),
                    // Преобразуем каждую BpmnArrow в SerializableArrow
                    Arrows = arrows?.Select(a => new SerializableArrow(a)).ToList() ?? new List<SerializableArrow>(),
                    Created = DateTime.Now,
                    Version = "1.0"
                };

                var serializer = new XmlSerializer(typeof(SerializableBpmnProject));

                // Сохраняем в файл с UTF-8 кодировкой
                using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    serializer.Serialize(writer, project);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка сохранения BPMN файла: {ex.Message}", ex);
            }
        }

        // Метод загрузки будет добавлен в следующей части
    }

    /// <summary>
    /// Сериализуемая версия проекта BPMN - содержит метаданные проекта
    /// </summary>
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

    /// <summary>
    /// Сериализуемая версия блока - хранит основные свойства BpmnBlock в XML-дружественном формате
    /// </summary>
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

        /// <summary>
        /// Преобразует BpmnBlock в SerializableBlock для сериализации
        /// Сохраняет все основные свойства: позицию, размеры, текст, цвета
        /// </summary>
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
        /// Восстанавливает BpmnBlock из SerializableBlock после десериализации
        /// Создает новый BpmnBlock с сохраненными свойствами
        /// </summary>
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
    /// Сериализуемая версия стрелки - хранит связи и координаты BpmnArrow
    /// </summary>
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

        /// <summary>
        /// Преобразует BpmnArrow в SerializableArrow для сериализации
        /// Сохраняет координаты, связи по ID блоков, визуальные свойства
        /// </summary>
        public SerializableArrow(BpmnArrow arrow)
        {
            Id = arrow.Id;
            Text = arrow.Text;
            StartBlockId = arrow.StartBlock?.Id;  // Сохраняем ID связанного блока
            StartX = arrow.StartPoint.X;
            StartY = arrow.StartPoint.Y;
            EndBlockId = arrow.EndBlock?.Id;      // Сохраняем ID связанного блока
            EndX = arrow.EndPoint.X;
            EndY = arrow.EndPoint.Y;
            Color = arrow.Color.Name;
            Width = arrow.Width;
        }

        /// <summary>
        /// Восстанавливает BpmnArrow из SerializableArrow после десериализации
        /// Восстанавливает связи с блоками через словарь blockDictionary
        /// </summary>
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