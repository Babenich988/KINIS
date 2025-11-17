using Kinis.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Kinis.Services
{

    public static class BpmnFileService
    {

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
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка сохранения BPMN файла: {ex.Message}", ex);
            }
        }


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

                    return (blocks, arrows);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка загрузки BPMN файла: {ex.Message}", ex);
            }
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