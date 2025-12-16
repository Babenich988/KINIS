using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Kinis.UI;

namespace Kinis.UI
{
    public static class SidebarIconRegistry
    {
        private static Image Load(string name)
        {
            string path = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "images",
                name
            );

            return File.Exists(path) ? Image.FromFile(path) : null;
        }

        public static readonly Dictionary<string, Image> Icons =
            new Dictionary<string, Image>
            {
                { "Комментарий", Load("Comment.png") },
                { "Задача", Load("Task.png") },
                { "Развилка", Load("Fork.png") },
                { "Развилка И", Load("Fork_and.png") },

                { "Начальное событие", Load("Start_event.png") },
                { "Промежуточное событие", Load("Intermediate_event.png") },
                { "Конечное событие", Load("End_event.png") },

                { "Объект данных", Load("Data_object.png") },
                { "Хранилище данных", Load("Data_warehouse.png") },
                { "Пул", Load("Pull.png")},
                { "Событие-получение сообщения", Load("Received_message_(start).png") },
                { "Событие-получение сообщения (промежуточное)", Load("Received_message_(prom).png") },

                { "Событие-отправка сообщения (промежуточное)", Load("Send_message_(prom).png") },
                { "Событие-отправка сообщения", Load("Send_message_(end).png") },

                { "Событие-ошибка обработчик", Load("Error_(rev).png") },
                { "Событие-ошибка инициатор", Load("Error_(init).png") },

                { "Событие-отмена обработчик", Load("Cancel_(rev).png") },
                { "Событие-отмена инициатор", Load("Cancel_(init).png") },

                { "Событие-остановка", Load("Stop.png") }
            };
    }
}