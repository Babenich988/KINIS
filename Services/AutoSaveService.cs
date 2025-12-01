using Kinis.Models;
using System;
using System.Collections.Generic;
using System.Timers;
using System.Windows.Forms;

namespace Kinis.Services
{
    public class AutoSaveService
    {
        private System.Timers.Timer _autoSaveTimer;
        private Func<List<BpmnBlock>> _getBlocks;
        private Func<List<BpmnArrow>> _getArrows;
        private Func<List<BpmnCurvedArrow>> _getCurvedArrows;
        private Func<string> _getAutoSaveFilePath;

        public bool IsEnabled { get; private set; }
        public int IntervalMinutes { get; private set; }

        public event Action<string> AutoSavePerformed;

        public AutoSaveService(Func<List<BpmnBlock>> getBlocks, Func<List<BpmnArrow>> getArrows,
                              Func<List<BpmnCurvedArrow>> getCurvedArrows, Func<string> getAutoSaveFilePath)
        {
            _getBlocks = getBlocks;
            _getArrows = getArrows;
            _getCurvedArrows = getCurvedArrows;
            _getAutoSaveFilePath = getAutoSaveFilePath;

            InitializeTimer();
        }

        private void InitializeTimer()
        {
            _autoSaveTimer = new System.Timers.Timer();
            _autoSaveTimer.Elapsed += OnAutoSaveTimerElapsed;
            _autoSaveTimer.AutoReset = true;
        }

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

        public void Stop()
        {
            IsEnabled = false;
            _autoSaveTimer.Stop();
            Console.WriteLine("Автосохранение остановлено");
        }

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

        private void OnAutoSaveTimerElapsed(object sender, ElapsedEventArgs e)
        {
            PerformAutoSave();
        }

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

        public void Dispose()
        {
            _autoSaveTimer?.Stop();
            _autoSaveTimer?.Dispose();
        }
    }
}