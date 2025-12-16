using Kinis.Models;
using System;
using System.Collections.Generic;
using System.Timers;
using System.Windows.Forms;

namespace Kinis.Services
{
    /// <summary>
    /// Сервис автоматического сохранения проекта
    /// </summary>
    public class AutoSaveService
    {
        private System.Timers.Timer _autoSaveTimer;
        private Func<List<BpmnBlock>> _getBlocks;
        private Func<List<BpmnArrow>> _getArrows;
        private Func<List<BpmnCurvedArrow>> _getCurvedArrows;
        private Func<string> _getAutoSaveFilePath;

        /// <summary>
        /// Получает значение, указывающее включено ли автосохранение
        /// </summary>
        public bool IsEnabled { get; private set; }

        /// <summary>
        /// Получает интервал автосохранения в минутах
        /// </summary>
        public int IntervalMinutes { get; private set; }

        /// <summary>
        /// Событие, возникающее при выполнении автосохранения
        /// </summary>
        public event Action<string> AutoSavePerformed;

        /// <summary>
        /// Инициализирует сервис автосохранения
        /// </summary>
        /// <param name="getBlocks">Функция получения списка блоков</param>
        /// <param name="getArrows">Функция получения списка стрелок</param>
        /// <param name="getCurvedArrows">Функция получения списка кривых стрелок</param>
        /// <param name="getAutoSaveFilePath">Функция получения пути файла автосохранения</param>
        public AutoSaveService(Func<List<BpmnBlock>> getBlocks, Func<List<BpmnArrow>> getArrows,
                              Func<List<BpmnCurvedArrow>> getCurvedArrows, Func<string> getAutoSaveFilePath)
        {
            _getBlocks = getBlocks;
            _getArrows = getArrows;
            _getCurvedArrows = getCurvedArrows;
            _getAutoSaveFilePath = getAutoSaveFilePath;

            InitializeTimer();
        }

        /// <summary>
        /// Инициализирует таймер автосохранения
        /// </summary>
        private void InitializeTimer()
        {
            _autoSaveTimer = new System.Timers.Timer();
            _autoSaveTimer.Elapsed += OnAutoSaveTimerElapsed;
            _autoSaveTimer.AutoReset = true;
        }

        /// <summary>
        /// Запускает автосохранение с указанным интервалом
        /// </summary>
        /// <param name="intervalMinutes">Интервал в минутах (1-10)</param>
        /// <exception cref="ArgumentException">Выбрасывается при недопустимом интервале</exception>
        /// <exception cref="InvalidOperationException">Выбрасывается если файл не выбран</exception>
        public void Start(int intervalMinutes)
        {
            if (intervalMinutes < 1 || intervalMinutes > 10)
                throw new ArgumentException("Интервал должен быть от 1 до 10 минут");

            // Проверяем, что файл для автосохранения выбран
            var filePath = _getAutoSaveFilePath();
            if (string.IsNullOrEmpty(filePath))
            {
                throw new InvalidOperationException("Файл для автосохранения не выбран");
            }

            IntervalMinutes = intervalMinutes;
            IsEnabled = true;

            _autoSaveTimer.Interval = intervalMinutes * 60 * 1000; // Конвертируем в миллисекунды
            _autoSaveTimer.Start();

            Console.WriteLine($"Автосохранение запущено. Интервал: {intervalMinutes} мин., Файл: {filePath}");
        }

        /// <summary>
        /// Останавливает автосохранение
        /// </summary>
        public void Stop()
        {
            IsEnabled = false;
            _autoSaveTimer.Stop();
            Console.WriteLine("Автосохранение остановлено");
        }

        /// <summary>
        /// Обновляет интервал автосохранения
        /// </summary>
        /// <param name="intervalMinutes">Новый интервал в минутах</param>
        public void UpdateInterval(int intervalMinutes)
        {
            if (IsEnabled)
            {
                Stop();
                Start(intervalMinutes);
            }
            else
            {
                IntervalMinutes = intervalMinutes;
            }
        }

        /// <summary>
        /// Обработчик события таймера автосохранения
        /// </summary>
        private void OnAutoSaveTimerElapsed(object sender, ElapsedEventArgs e)
        {
            PerformAutoSave();
        }

        /// <summary>
        /// Выполняет автоматическое сохранение проекта
        /// </summary>
        private void PerformAutoSave()
        {
            try
            {
                // Выполняем в UI потоке для безопасного доступа к данным
                if (Application.OpenForms.Count > 0)
                {
                    var mainForm = Application.OpenForms[0];
                    if (mainForm != null && !mainForm.IsDisposed)
                    {
                        mainForm.Invoke(new Action(() =>
                        {
                            var blocks = _getBlocks();
                            var arrows = _getArrows();
                            var curvedArrows = _getCurvedArrows();
                            var filePath = _getAutoSaveFilePath();

                            if (blocks.Count > 0 || arrows.Count > 0 || curvedArrows.Count > 0)
                            {
                                if (!string.IsNullOrEmpty(filePath))
                                {
                                    // Используем автосохранение с кривыми стрелками
                                    BpmnFileService.SaveForAutoSave(blocks, arrows, curvedArrows, filePath);
                                    AutoSavePerformed?.Invoke(DateTime.Now.ToString("HH:mm:ss"));
                                    Console.WriteLine($"Автосохранение выполнено: {blocks.Count} блоков, {arrows.Count} стрелок, {curvedArrows.Count} кривых стрелок в файл: {filePath}");
                                }
                                else
                                {
                                    Console.WriteLine("Автосохранение: файл не выбран");
                                }
                            }
                        }));
                    }
                }
            }
            catch (Exception ex)
            {
                // Логируем ошибку, но не прерываем работу
                Console.WriteLine($"Ошибка автосохранения: {ex.Message}");
            }
        }

        /// <summary>
        /// Получает информацию о состоянии автосохранения
        /// </summary>
        /// <returns>Строка с информацией об автосохранении</returns>
        public string GetAutoSaveInfo()
        {
            if (!IsEnabled)
                return "Автосохранение отключено";

            var filePath = _getAutoSaveFilePath();
            if (string.IsNullOrEmpty(filePath))
                return "Автосохранение включено, но файл не выбран";

            var fileInfo = new System.IO.FileInfo(filePath);
            if (fileInfo.Exists)
            {
                return $"Автосохранение включено (каждые {IntervalMinutes} мин.)\n" +
                       $"Последнее сохранение: {fileInfo.LastWriteTime:HH:mm:ss}\n" +
                       $"Файл: {filePath}";
            }
            else
            {
                return $"Автосохранение включено (каждые {IntervalMinutes} мин.)\n" +
                       $"Файл: {filePath}";
            }
        }

        /// <summary>
        /// Освобождает ресурсы сервиса
        /// </summary>
        public void Dispose()
        {
            _autoSaveTimer?.Stop();
            _autoSaveTimer?.Dispose();
        }
    }
}