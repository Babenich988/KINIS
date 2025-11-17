using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Kinis.Services
{
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

        // Конструктор для преобразования BpmnBlock -> SerializableBlock будет добавлен позже
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

        // Конструктор для преобразования BpmnArrow -> SerializableArrow будет добавлен позже
    }
}