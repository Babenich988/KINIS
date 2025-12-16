using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kinis.Models;

namespace Kinis.Services
{
    /// <summary>
    /// Интерфейс команды для реализации паттерна Command
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Выполняет команду
        /// </summary>
        void Execute();

        /// <summary>
        /// Отменяет выполнение команды
        /// </summary>
        void Undo();

        /// <summary>
        /// Получает описание команды для отображения в истории
        /// </summary>
        string Description { get; }
    }

    /// <summary>
    /// Менеджер команд для реализации функциональности Undo/Redo
    /// </summary>
    public class CommandManager
    {
        private readonly Stack<ICommand> _undoStack = new Stack<ICommand>();
        private readonly Stack<ICommand> _redoStack = new Stack<ICommand>();

        /// <summary>
        /// Событие, возникающее при изменении состояния менеджера команд
        /// </summary>
        public event Action OnStateChanged;

        /// <summary>
        /// Получает значение, указывающее возможность выполнения отмены
        /// </summary>
        public bool CanUndo => _undoStack.Count > 0;

        /// <summary>
        /// Получает значение, указывающее возможность выполнения повтора
        /// </summary>
        public bool CanRedo => _redoStack.Count > 0;

        /// <summary>
        /// Выполняет команду и добавляет её в историю
        /// </summary>
        /// <param name="command">Команда для выполнения</param>
        public void Execute(ICommand command)
        {
            command.Execute();
            _undoStack.Push(command);
            _redoStack.Clear();
            OnStateChanged?.Invoke();
        }

        /// <summary>
        /// Отменяет последнюю выполненную команду
        /// </summary>
        public void Undo()
        {
            if (CanUndo)
            {
                var command = _undoStack.Pop();
                command.Undo();
                _redoStack.Push(command);
                OnStateChanged?.Invoke();
            }
        }

        /// <summary>
        /// Повторяет последнюю отмененную команду
        /// </summary>
        public void Redo()
        {
            if (CanRedo)
            {
                var command = _redoStack.Pop();
                command.Execute();
                _undoStack.Push(command);
                OnStateChanged?.Invoke();
            }
        }

        /// <summary>
        /// Команда создания блока BPMN
        /// </summary>
        public class CreateBlockCommand : ICommand
        {
            private readonly BpmnBlock _block;
            private readonly List<BpmnBlock> _blocks;
            private readonly InfiniteCanvas _canvas;

            /// <summary>
            /// Получает описание команды
            /// </summary>
            public string Description => $"Create {_block.Type}";

            /// <summary>
            /// Инициализирует команду создания блока
            /// </summary>
            /// <param name="block">Создаваемый блок</param>
            /// <param name="blocks">Список блоков на холсте</param>
            /// <param name="canvas">Холст для отображения</param>
            public CreateBlockCommand(BpmnBlock block, List<BpmnBlock> blocks, InfiniteCanvas canvas)
            {
                _block = block;
                _blocks = blocks;
                _canvas = canvas;
            }

            /// <summary>
            /// Выполняет создание блока
            /// </summary>
            public void Execute()
            {
                _blocks.Add(_block);
                _canvas.SetBlocks(_blocks);
                _canvas.Invalidate();
                _canvas.RaiseElementAdded();
            }

            /// <summary>
            /// Отменяет создание блока
            /// </summary>
            public void Undo()
            {
                _blocks.Remove(_block);
                _canvas.SetBlocks(_blocks);
                _canvas.Invalidate();
            }
        }

        /// <summary>
        /// Команда создания прямой стрелки
        /// </summary>
        public class CreateArrowCommand : ICommand
        {
            private readonly BpmnArrow _arrow;
            private readonly List<BpmnArrow> _arrows;
            private readonly InfiniteCanvas _canvas;

            /// <summary>
            /// Получает описание команды
            /// </summary>
            public string Description => "Create Arrow";

            /// <summary>
            /// Инициализирует команду создания стрелки
            /// </summary>
            /// <param name="arrow">Создаваемая стрелка</param>
            /// <param name="arrows">Список стрелок на холсте</param>
            /// <param name="canvas">Холст для отображения</param>
            public CreateArrowCommand(BpmnArrow arrow, List<BpmnArrow> arrows, InfiniteCanvas canvas)
            {
                _arrow = arrow;
                _arrows = arrows; // Используем переданный список, а не создаем новый
                _canvas = canvas;
            }

            /// <summary>
            /// Выполняет создание стрелки
            /// </summary>
            public void Execute()
            {
                // ДОБАВЛЯЕМ в существующий список
                _arrows.Add(_arrow);
                _canvas.SetArrows(_arrows);
                _canvas.Invalidate();
                _canvas.RaiseElementAdded();
                Console.WriteLine($"Arrow added via command, total arrows: {_arrows.Count}");
            }

            /// <summary>
            /// Отменяет создание стрелки
            /// </summary>
            public void Undo()
            {
                // УДАЛЯЕМ из существующего списка
                _arrows.Remove(_arrow);
                _canvas.SetArrows(_arrows);
                _canvas.Invalidate();
                Console.WriteLine($"Arrow removed via undo, total arrows: {_arrows.Count}");
            }
        }

        /// <summary>
        /// Команда создания кривой стрелки
        /// </summary>
        public class CreateCurvedArrowCommand : ICommand
        {
            private readonly BpmnCurvedArrow _curvedArrow;
            private readonly List<BpmnCurvedArrow> _curvedArrows;
            private readonly InfiniteCanvas _canvas;

            /// <summary>
            /// Получает описание команды
            /// </summary>
            public string Description => "Create Curved Arrow";

            /// <summary>
            /// Инициализирует команду создания кривой стрелки
            /// </summary>
            /// <param name="curvedArrow">Создаваемая кривая стрелка</param>
            /// <param name="curvedArrows">Список кривых стрелок на холсте</param>
            /// <param name="canvas">Холст для отображения</param>
            public CreateCurvedArrowCommand(BpmnCurvedArrow curvedArrow, List<BpmnCurvedArrow> curvedArrows, InfiniteCanvas canvas)
            {
                _curvedArrow = curvedArrow;
                _curvedArrows = curvedArrows;
                _canvas = canvas;
            }

            /// <summary>
            /// Выполняет создание кривой стрелки
            /// </summary>
            public void Execute()
            {
                _curvedArrows.Add(_curvedArrow);
                _canvas.SetCurvedArrows(_curvedArrows);
                _canvas.Invalidate();
                Console.WriteLine($"Curved arrow added via command, total curved arrows: {_curvedArrows.Count}");
            }

            /// <summary>
            /// Отменяет создание кривой стрелки
            /// </summary>
            public void Undo()
            {
                _curvedArrows.Remove(_curvedArrow);
                _canvas.SetCurvedArrows(_curvedArrows);
                _canvas.Invalidate();
                Console.WriteLine($"Curved arrow removed via undo, total curved arrows: {_curvedArrows.Count}");
            }
        }

        /// <summary>
        /// Команда удаления блока BPMN
        /// </summary>
        public class DeleteBlockCommand : ICommand
        {
            private readonly BpmnBlock _block;
            private readonly List<BpmnBlock> _blocks;
            private readonly InfiniteCanvas _canvas;
            private readonly List<BpmnArrow> _arrows;
            private List<BpmnArrow> _removedArrows;

            /// <summary>
            /// Получает описание команды
            /// </summary>
            public string Description => $"Delete {_block.Type}";

            /// <summary>
            /// Инициализирует команду удаления блока
            /// </summary>
            /// <param name="block">Удаляемый блок</param>
            /// <param name="blocks">Список блоков на холсте</param>
            /// <param name="arrows">Список стрелок на холсте</param>
            /// <param name="canvas">Холст для отображения</param>
            public DeleteBlockCommand(BpmnBlock block, List<BpmnBlock> blocks, List<BpmnArrow> arrows, InfiniteCanvas canvas)
            {
                _block = block;
                _blocks = blocks;
                _arrows = arrows;
                _canvas = canvas;
                _removedArrows = new List<BpmnArrow>();
            }

            /// <summary>
            /// Выполняет удаление блока и связанных стрелок
            /// </summary>
            public void Execute()
            {
                _removedArrows = _arrows.Where(a => a.StartBlock == _block || a.EndBlock == _block).ToList();
                _blocks.Remove(_block);

                foreach (var arrow in _removedArrows)
                    _arrows.Remove(arrow);

                _canvas.SetBlocks(_blocks);
                _canvas.SetArrows(_arrows);
                _canvas.Invalidate();
            }

            /// <summary>
            /// Отменяет удаление блока и восстанавливает связанные стрелки
            /// </summary>
            public void Undo()
            {
                _blocks.Add(_block);

                foreach (var arrow in _removedArrows)
                    _arrows.Add(arrow);

                _canvas.SetBlocks(_blocks);
                _canvas.SetArrows(_arrows);
                _canvas.Invalidate();
            }
        }

        /// <summary>
        /// Команда удаления прямой стрелки
        /// </summary>
        public class DeleteArrowCommand : ICommand
        {
            private readonly BpmnArrow _arrow;
            private readonly List<BpmnArrow> _arrows;
            private readonly InfiniteCanvas _canvas;

            /// <summary>
            /// Получает описание команды
            /// </summary>
            public string Description => "Delete Arrow";

            /// <summary>
            /// Инициализирует команду удаления стрелки
            /// </summary>
            /// <param name="arrow">Удаляемая стрелка</param>
            /// <param name="arrows">Список стрелок на холсте</param>
            /// <param name="canvas">Холст для отображения</param>
            public DeleteArrowCommand(BpmnArrow arrow, List<BpmnArrow> arrows, InfiniteCanvas canvas)
            {
                _arrow = arrow;
                _arrows = arrows;
                _canvas = canvas;
            }

            /// <summary>
            /// Выполняет удаление стрелки
            /// </summary>
            public void Execute()
            {
                _arrows.Remove(_arrow);
                _canvas.SetArrows(_arrows);
                _canvas.Invalidate();
            }

            /// <summary>
            /// Отменяет удаление стрелки
            /// </summary>
            public void Undo()
            {
                _arrows.Add(_arrow);
                _canvas.SetArrows(_arrows);
                _canvas.Invalidate();
            }
        }

        /// <summary>
        /// Команда перемещения блока BPMN
        /// </summary>
        public class MoveBlockCommand : ICommand
        {
            private readonly BpmnBlock _block;
            private readonly RectangleF _originalBounds;
            private readonly RectangleF _newBounds;
            private readonly List<BpmnArrow> _arrows;
            private readonly InfiniteCanvas _canvas;
            private Dictionary<BpmnArrow, PointF> _originalStartPoints;
            private Dictionary<BpmnArrow, PointF> _originalEndPoints;

            /// <summary>
            /// Получает описание команды
            /// </summary>
            public string Description => $"Move {_block.Type}";

            /// <summary>
            /// Инициализирует команду перемещения блока
            /// </summary>
            /// <param name="block">Перемещаемый блок</param>
            /// <param name="originalBounds">Исходные границы блока</param>
            /// <param name="newBounds">Новые границы блока</param>
            /// <param name="arrows">Список стрелок на холсте</param>
            /// <param name="canvas">Холст для отображения</param>
            public MoveBlockCommand(BpmnBlock block, RectangleF originalBounds, RectangleF newBounds, List<BpmnArrow> arrows, InfiniteCanvas canvas)
            {
                _block = block;
                _originalBounds = originalBounds;
                _newBounds = newBounds;
                _arrows = arrows;
                _canvas = canvas;

                // Сохраняем оригинальные позиции стрелок
                _originalStartPoints = new Dictionary<BpmnArrow, PointF>();
                _originalEndPoints = new Dictionary<BpmnArrow, PointF>();

                foreach (var arrow in _arrows)
                {
                    if (arrow.StartBlock == _block)
                        _originalStartPoints[arrow] = arrow.StartPoint;
                    if (arrow.EndBlock == _block)
                        _originalEndPoints[arrow] = arrow.EndPoint;
                }
            }

            /// <summary>
            /// Выполняет перемещение блока
            /// </summary>
            public void Execute()
            {
                _block.Bounds = _newBounds;
                UpdateAttachedArrows(_newBounds);
                _canvas.Invalidate();
            }

            /// <summary>
            /// Отменяет перемещение блока
            /// </summary>
            public void Undo()
            {
                _block.Bounds = _originalBounds;
                RestoreArrowPositions();
                _canvas.Invalidate();
            }

            /// <summary>
            /// Обновляет позиции прикрепленных стрелок при перемещении блока
            /// </summary>
            private void UpdateAttachedArrows(RectangleF newBounds)
            {
                float deltaX = newBounds.X - _originalBounds.X;
                float deltaY = newBounds.Y - _originalBounds.Y;

                foreach (var arrow in _arrows)
                {
                    if (arrow.StartBlock == _block)
                    {
                        arrow.StartPoint = new PointF(
                            arrow.StartPoint.X + deltaX,
                            arrow.StartPoint.Y + deltaY
                        );
                    }
                    if (arrow.EndBlock == _block)
                    {
                        arrow.EndPoint = new PointF(
                            arrow.EndPoint.X + deltaX,
                            arrow.EndPoint.Y + deltaY
                        );
                    }
                }
            }

            /// <summary>
            /// Восстанавливает оригинальные позиции стрелок
            /// </summary>
            private void RestoreArrowPositions()
            {
                foreach (var kvp in _originalStartPoints)
                    kvp.Key.StartPoint = kvp.Value;

                foreach (var kvp in _originalEndPoints)
                    kvp.Key.EndPoint = kvp.Value;
            }

            /// <summary>
            /// Команда модификации прямой стрелки
            /// </summary>
            public class ModifyArrowCommand : ICommand
            {
                private readonly BpmnArrow _arrow;
                private readonly BpmnBlock _originalStartBlock;
                private readonly PointF _originalStartPoint;
                private readonly int _originalStartIndex;
                private readonly BpmnBlock _originalEndBlock;
                private readonly PointF _originalEndPoint;
                private readonly int _originalEndIndex;
                private readonly BpmnBlock _newStartBlock;
                private readonly PointF _newStartPoint;
                private readonly int _newStartIndex;
                private readonly BpmnBlock _newEndBlock;
                private readonly PointF _newEndPoint;
                private readonly int _newEndIndex;
                private readonly InfiniteCanvas _canvas;
                private readonly bool _isStartModified;

                /// <summary>
                /// Получает описание команды
                /// </summary>
                public string Description => "Modify Arrow";

                /// <summary>
                /// Инициализирует команду модификации стрелки
                /// </summary>
                /// <param name="arrow">Модифицируемая стрелка</param>
                /// <param name="originalStartBlock">Исходный начальный блок</param>
                /// <param name="originalStartPoint">Исходная начальная точка</param>
                /// <param name="originalStartIndex">Исходный индекс точки привязки начала</param>
                /// <param name="originalEndBlock">Исходный конечный блок</param>
                /// <param name="originalEndPoint">Исходная конечная точка</param>
                /// <param name="originalEndIndex">Исходный индекс точки привязки конца</param>
                /// <param name="newStartBlock">Новый начальный блок</param>
                /// <param name="newStartPoint">Новая начальная точка</param>
                /// <param name="newStartIndex">Новый индекс точки привязки начала</param>
                /// <param name="newEndBlock">Новый конечный блок</param>
                /// <param name="newEndPoint">Новая конечная точка</param>
                /// <param name="newEndIndex">Новый индекс точки привязки конца</param>
                /// <param name="isStartModified">Указывает модифицируется ли начало стрелки</param>
                /// <param name="canvas">Холст для отображения</param>
                public ModifyArrowCommand(BpmnArrow arrow,
                    BpmnBlock originalStartBlock, PointF originalStartPoint, int originalStartIndex,
                    BpmnBlock originalEndBlock, PointF originalEndPoint, int originalEndIndex,
                    BpmnBlock newStartBlock, PointF newStartPoint, int newStartIndex,
                    BpmnBlock newEndBlock, PointF newEndPoint, int newEndIndex,
                    bool isStartModified, InfiniteCanvas canvas)
                {
                    _arrow = arrow;
                    _originalStartBlock = originalStartBlock;
                    _originalStartPoint = originalStartPoint;
                    _originalStartIndex = originalStartIndex;
                    _originalEndBlock = originalEndBlock;
                    _originalEndPoint = originalEndPoint;
                    _originalEndIndex = originalEndIndex;
                    _newStartBlock = newStartBlock;
                    _newStartPoint = newStartPoint;
                    _newStartIndex = newStartIndex;
                    _newEndBlock = newEndBlock;
                    _newEndPoint = newEndPoint;
                    _newEndIndex = newEndIndex;
                    _isStartModified = isStartModified;
                    _canvas = canvas;
                }

                /// <summary>
                /// Выполняет модификацию стрелки
                /// </summary>
                public void Execute()
                {
                    if (_isStartModified)
                    {
                        _arrow.StartBlock = _newStartBlock;
                        _arrow.StartPoint = _newStartPoint;
                        _arrow.StartConnectionPointIndex = _newStartIndex;
                    }
                    else
                    {
                        _arrow.EndBlock = _newEndBlock;
                        _arrow.EndPoint = _newEndPoint;
                        _arrow.EndConnectionPointIndex = _newEndIndex;
                    }
                    _arrow.CalculateOrthogonalPath();
                    _canvas.Invalidate();
                }

                /// <summary>
                /// Отменяет модификацию стрелки
                /// </summary>
                public void Undo()
                {
                    if (_isStartModified)
                    {
                        _arrow.StartBlock = _originalStartBlock;
                        _arrow.StartPoint = _originalStartPoint;
                        _arrow.StartConnectionPointIndex = _originalStartIndex;
                    }
                    else
                    {
                        _arrow.EndBlock = _originalEndBlock;
                        _arrow.EndPoint = _originalEndPoint;
                        _arrow.EndConnectionPointIndex = _originalEndIndex;
                    }
                    _arrow.CalculateOrthogonalPath();
                    _canvas.Invalidate();
                }
            }

            /// <summary>
            /// Команда перемещения кривой стрелки
            /// </summary>
            public class MoveCurvedArrowCommand : ICommand
            {
                private readonly BpmnCurvedArrow _curvedArrow;
                private readonly PointF _originalStartPoint;
                private readonly PointF _originalEndPoint;
                private readonly PointF _originalControlPoint1;
                private readonly PointF _originalControlPoint2;
                private readonly PointF _newStartPoint;
                private readonly PointF _newEndPoint;
                private readonly PointF _newControlPoint1;
                private readonly PointF _newControlPoint2;
                private readonly InfiniteCanvas _canvas;

                /// <summary>
                /// Получает описание команды
                /// </summary>
                public string Description => "Move Curved Arrow";

                /// <summary>
                /// Инициализирует команду перемещения кривой стрелки
                /// </summary>
                /// <param name="curvedArrow">Перемещаемая кривая стрелка</param>
                /// <param name="originalStartPoint">Исходная начальная точка</param>
                /// <param name="originalEndPoint">Исходная конечная точка</param>
                /// <param name="originalControlPoint1">Исходная первая контрольная точка</param>
                /// <param name="originalControlPoint2">Исходная вторая контрольная точка</param>
                /// <param name="newStartPoint">Новая начальная точка</param>
                /// <param name="newEndPoint">Новая конечная точка</param>
                /// <param name="newControlPoint1">Новая первая контрольная точка</param>
                /// <param name="newControlPoint2">Новая вторая контрольная точка</param>
                /// <param name="canvas">Холст для отображения</param>
                public MoveCurvedArrowCommand(BpmnCurvedArrow curvedArrow,
                    PointF originalStartPoint, PointF originalEndPoint,
                    PointF originalControlPoint1, PointF originalControlPoint2,
                    PointF newStartPoint, PointF newEndPoint,
                    PointF newControlPoint1, PointF newControlPoint2,
                    InfiniteCanvas canvas)
                {
                    _curvedArrow = curvedArrow;
                    _originalStartPoint = originalStartPoint;
                    _originalEndPoint = originalEndPoint;
                    _originalControlPoint1 = originalControlPoint1;
                    _originalControlPoint2 = originalControlPoint2;
                    _newStartPoint = newStartPoint;
                    _newEndPoint = newEndPoint;
                    _newControlPoint1 = newControlPoint1;
                    _newControlPoint2 = newControlPoint2;
                    _canvas = canvas;
                }

                /// <summary>
                /// Выполняет перемещение кривой стрелки
                /// </summary>
                public void Execute()
                {
                    _curvedArrow.StartPoint = _newStartPoint;
                    _curvedArrow.EndPoint = _newEndPoint;
                    _curvedArrow.ControlPoint1 = _newControlPoint1;
                    _curvedArrow.ControlPoint2 = _newControlPoint2;
                    _canvas.Invalidate();
                }

                /// <summary>
                /// Отменяет перемещение кривой стрелки
                /// </summary>
                public void Undo()
                {
                    _curvedArrow.StartPoint = _originalStartPoint;
                    _curvedArrow.EndPoint = _originalEndPoint;
                    _curvedArrow.ControlPoint1 = _originalControlPoint1;
                    _curvedArrow.ControlPoint2 = _originalControlPoint2;
                    _canvas.Invalidate();
                }
            }

            /// <summary>
            /// Команда модификации кривой стрелки
            /// </summary>
            public class ModifyCurvedArrowCommand : ICommand
            {
                private readonly BpmnCurvedArrow _curvedArrow;
                private readonly BpmnBlock _originalStartBlock;
                private readonly PointF _originalStartPoint;
                private readonly int _originalStartConnectionIndex;
                private readonly BpmnBlock _originalEndBlock;
                private readonly PointF _originalEndPoint;
                private readonly int _originalEndConnectionIndex;
                private readonly PointF _originalControlPoint1;
                private readonly PointF _originalControlPoint2;
                private readonly BpmnBlock _newStartBlock;
                private readonly PointF _newStartPoint;
                private readonly int _newStartConnectionIndex;
                private readonly BpmnBlock _newEndBlock;
                private readonly PointF _newEndPoint;
                private readonly int _newEndConnectionIndex;
                private readonly PointF _newControlPoint1;
                private readonly PointF _newControlPoint2;
                private readonly InfiniteCanvas _canvas;
                private readonly bool _isStartModified;

                /// <summary>
                /// Получает описание команды
                /// </summary>
                public string Description => "Modify Curved Arrow";

                /// <summary>
                /// Инициализирует команду модификации кривой стрелки
                /// </summary>
                /// <param name="curvedArrow">Модифицируемая кривая стрелка</param>
                /// <param name="originalStartBlock">Исходный начальный блок</param>
                /// <param name="originalStartPoint">Исходная начальная точка</param>
                /// <param name="originalStartConnectionIndex">Исходный индекс точки привязки начала</param>
                /// <param name="originalEndBlock">Исходный конечный блок</param>
                /// <param name="originalEndPoint">Исходная конечная точка</param>
                /// <param name="originalEndConnectionIndex">Исходный индекс точки привязки конца</param>
                /// <param name="originalControlPoint1">Исходная первая контрольная точка</param>
                /// <param name="originalControlPoint2">Исходная вторая контрольная точка</param>
                /// <param name="newStartBlock">Новый начальный блок</param>
                /// <param name="newStartPoint">Новая начальная точка</param>
                /// <param name="newStartConnectionIndex">Новый индекс точки привязки начала</param>
                /// <param name="newEndBlock">Новый конечный блок</param>
                /// <param name="newEndPoint">Новая конечная точка</param>
                /// <param name="newEndConnectionIndex">Новый индекс точки привязки конца</param>
                /// <param name="newControlPoint1">Новая первая контрольная точка</param>
                /// <param name="newControlPoint2">Новая вторая контрольная точка</param>
                /// <param name="isStartModified">Указывает модифицируется ли начало стрелки</param>
                /// <param name="canvas">Холст для отображения</param>
                public ModifyCurvedArrowCommand(BpmnCurvedArrow curvedArrow,
                    BpmnBlock originalStartBlock, PointF originalStartPoint, int originalStartConnectionIndex,
                    BpmnBlock originalEndBlock, PointF originalEndPoint, int originalEndConnectionIndex,
                    PointF originalControlPoint1, PointF originalControlPoint2,
                    BpmnBlock newStartBlock, PointF newStartPoint, int newStartConnectionIndex,
                    BpmnBlock newEndBlock, PointF newEndPoint, int newEndConnectionIndex,
                    PointF newControlPoint1, PointF newControlPoint2,
                    bool isStartModified, InfiniteCanvas canvas)
                {
                    _curvedArrow = curvedArrow;
                    _originalStartBlock = originalStartBlock;
                    _originalStartPoint = originalStartPoint;
                    _originalStartConnectionIndex = originalStartConnectionIndex;
                    _originalEndBlock = originalEndBlock;
                    _originalEndPoint = originalEndPoint;
                    _originalEndConnectionIndex = originalEndConnectionIndex;
                    _originalControlPoint1 = originalControlPoint1;
                    _originalControlPoint2 = originalControlPoint2;
                    _newStartBlock = newStartBlock;
                    _newStartPoint = newStartPoint;
                    _newStartConnectionIndex = newStartConnectionIndex;
                    _newEndBlock = newEndBlock;
                    _newEndPoint = newEndPoint;
                    _newEndConnectionIndex = newEndConnectionIndex;
                    _newControlPoint1 = newControlPoint1;
                    _newControlPoint2 = newControlPoint2;
                    _isStartModified = isStartModified;
                    _canvas = canvas;
                }

                /// <summary>
                /// Выполняет модификацию кривой стрелки
                /// </summary>
                public void Execute()
                {
                    _curvedArrow.StartBlock = _newStartBlock;
                    _curvedArrow.StartPoint = _newStartPoint;
                    _curvedArrow.StartConnectionPointIndex = _newStartConnectionIndex;
                    _curvedArrow.EndBlock = _newEndBlock;
                    _curvedArrow.EndPoint = _newEndPoint;
                    _curvedArrow.EndConnectionPointIndex = _newEndConnectionIndex;
                    _curvedArrow.ControlPoint1 = _newControlPoint1;
                    _curvedArrow.ControlPoint2 = _newControlPoint2;
                    _canvas.Invalidate();
                }

                /// <summary>
                /// Отменяет модификацию кривой стрелки
                /// </summary>
                public void Undo()
                {
                    _curvedArrow.StartBlock = _originalStartBlock;
                    _curvedArrow.StartPoint = _originalStartPoint;
                    _curvedArrow.StartConnectionPointIndex = _originalStartConnectionIndex;
                    _curvedArrow.EndBlock = _originalEndBlock;
                    _curvedArrow.EndPoint = _originalEndPoint;
                    _curvedArrow.EndConnectionPointIndex = _originalEndConnectionIndex;
                    _curvedArrow.ControlPoint1 = _originalControlPoint1;
                    _curvedArrow.ControlPoint2 = _originalControlPoint2;
                    _canvas.Invalidate();
                }
            }
        }

        /// <summary>
        /// Команда изменения текста блока BPMN
        /// </summary>
        public class ChangeTextCommand : ICommand
        {
            private readonly BpmnBlock _block;
            private readonly string _oldText;
            private readonly string _newText;
            private readonly InfiniteCanvas _canvas;

            /// <summary>
            /// Получает описание команды
            /// </summary>
            public string Description => $"Change text of {_block.Type}";

            /// <summary>
            /// Инициализирует команду изменения текста
            /// </summary>
            /// <param name="block">Блок для изменения текста</param>
            /// <param name="oldText">Старый текст блока</param>
            /// <param name="newText">Новый текст блока</param>
            /// <param name="canvas">Холст для отображения</param>
            public ChangeTextCommand(BpmnBlock block, string oldText, string newText, InfiniteCanvas canvas)
            {
                _block = block;
                _oldText = oldText;
                _newText = newText;
                _canvas = canvas;
            }

            /// <summary>
            /// Выполняет изменение текста блока
            /// </summary>
            public void Execute()
            {
                _block.Text = _newText;
                _canvas.Invalidate();
            }

            /// <summary>
            /// Отменяет изменение текста блока
            /// </summary>
            public void Undo()
            {
                _block.Text = _oldText;
                _canvas.Invalidate();
            }
        }

        /// <summary>
        /// Команда изменения размера блока BPMN
        /// </summary>
        public class ResizeBlockCommand : ICommand
        {
            private readonly BpmnBlock _block;
            private readonly RectangleF _originalBounds;
            private readonly RectangleF _newBounds;
            private readonly Dictionary<BpmnArrow, (PointF startPoint, PointF endPoint)> _arrowStates;
            private readonly InfiniteCanvas _canvas;

            /// <summary>
            /// Получает описание команды
            /// </summary>
            public string Description => $"Resize {_block.Type}";

            /// <summary>
            /// Инициализирует команду изменения размера блока
            /// </summary>
            /// <param name="block">Изменяемый блок</param>
            /// <param name="originalBounds">Исходные границы блока</param>
            /// <param name="newBounds">Новые границы блока</param>
            /// <param name="arrowStates">Состояния связанных стрелок</param>
            /// <param name="canvas">Холст для отображения</param>
            public ResizeBlockCommand(BpmnBlock block, RectangleF originalBounds, RectangleF newBounds,
                                    Dictionary<BpmnArrow, (PointF startPoint, PointF endPoint)> arrowStates,
                                    InfiniteCanvas canvas)
            {
                _block = block;
                _originalBounds = originalBounds;
                _newBounds = newBounds;
                _arrowStates = arrowStates;
                _canvas = canvas;
            }

            /// <summary>
            /// Выполняет изменение размера блока
            /// </summary>
            public void Execute()
            {
                _block.Bounds = _newBounds;
                UpdateArrowPositions();
                _canvas.Invalidate();
            }

            /// <summary>
            /// Отменяет изменение размера блока
            /// </summary>
            public void Undo()
            {
                _block.Bounds = _originalBounds;
                RestoreArrowPositions();
                _canvas.Invalidate();
            }

            /// <summary>
            /// Обновляет позиции прикрепленных стрелок при изменении размера блока
            /// </summary>
            private void UpdateArrowPositions()
            {
                // При изменении размера пересчитываем позиции стрелок
                foreach (var arrowState in _arrowStates)
                {
                    var arrow = arrowState.Key;

                    if (arrow.StartBlock == _block)
                    {
                        // Находим новую точку привязки на измененном блоке
                        arrow.StartPoint = FindNearestConnectionPointOnBlock(_block, arrowState.Value.startPoint);
                    }

                    if (arrow.EndBlock == _block)
                    {
                        // Находим новую точку привязки на измененном блоке
                        arrow.EndPoint = FindNearestConnectionPointOnBlock(_block, arrowState.Value.endPoint);
                    }
                }
            }

            /// <summary>
            /// Восстанавливает оригинальные позиции стрелок
            /// </summary>
            private void RestoreArrowPositions()
            {
                // Восстанавливаем оригинальные позиции стрелок
                foreach (var arrowState in _arrowStates)
                {
                    var arrow = arrowState.Key;
                    arrow.StartPoint = arrowState.Value.startPoint;
                    arrow.EndPoint = arrowState.Value.endPoint;
                }
            }

            /// <summary>
            /// Находит ближайшую точку привязки на блоке
            /// </summary>
            private PointF FindNearestConnectionPointOnBlock(BpmnBlock block, PointF targetPoint)
            {
                var points = block.GetConnectionPoints();
                PointF nearest = points[0];
                float minDistance = Distance(nearest, targetPoint);

                foreach (var point in points)
                {
                    float dist = Distance(point, targetPoint);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        nearest = point;
                    }
                }

                return nearest;
            }

            /// <summary>
            /// Вычисляет расстояние между двумя точками
            /// </summary>
            private float Distance(PointF a, PointF b)
            {
                float dx = a.X - b.X;
                float dy = a.Y - b.Y;
                return (float)Math.Sqrt(dx * dx + dy * dy);
            }
        }

        /// <summary>
        /// Команда добавления дорожки в пул
        /// </summary>
        public class AddLaneCommand : ICommand
        {
            private readonly BpmnBlock _poolBlock;
            private readonly PoolLine _lane;
            private readonly List<BpmnBlock> _blocks;
            private readonly InfiniteCanvas _canvas;
            private readonly bool _isNested;
            private readonly PoolLine _parentLane;

            /// <summary>
            /// Получает описание команды
            /// </summary>
            public string Description => "Add Lane";

            /// <summary>
            /// Инициализирует команду добавления дорожки
            /// </summary>
            /// <param name="poolBlock">Пул для добавления дорожки</param>
            /// <param name="lane">Добавляемая дорожка</param>
            /// <param name="blocks">Список блоков на холсте</param>
            /// <param name="canvas">Холст для отображения</param>
            /// <param name="isNested">Указывает является ли дорожка вложенной</param>
            /// <param name="parentLane">Родительская дорожка (для вложенных дорожек)</param>
            public AddLaneCommand(BpmnBlock poolBlock, PoolLine lane, List<BpmnBlock> blocks,
                                 InfiniteCanvas canvas, bool isNested = false, PoolLine parentLane = null)
            {
                _poolBlock = poolBlock;
                _lane = lane;
                _blocks = blocks;
                _canvas = canvas;
                _isNested = isNested;
                _parentLane = parentLane;
            }

            /// <summary>
            /// Выполняет добавление дорожки
            /// </summary>
            public void Execute()
            {
                if (_isNested && _parentLane != null)
                {
                    _parentLane.ChildLines.Add(_lane);
                }
                else
                {
                    _poolBlock.PoolLanes.Add(_lane);
                }

                // Пересчитываем позиции дорожек
                RecalculateLanesPositions(_poolBlock);
                _canvas.SetBlocks(_blocks);
                _canvas.Invalidate();
            }

            /// <summary>
            /// Отменяет добавление дорожки
            /// </summary>
            public void Undo()
            {
                if (_isNested && _parentLane != null)
                {
                    _parentLane.ChildLines.Remove(_lane);
                }
                else
                {
                    _poolBlock.PoolLanes.Remove(_lane);
                }

                // Пересчитываем позиции дорожек
                RecalculateLanesPositions(_poolBlock);
                _canvas.SetBlocks(_blocks);
                _canvas.Invalidate();
            }

            /// <summary>
            /// Пересчитывает позиции дорожек в пуле
            /// </summary>
            private void RecalculateLanesPositions(BpmnBlock poolBlock)
            {
                if (poolBlock.PoolLanes == null) return;

                float currentY = poolBlock.Bounds.Y + 40f;
                float bodyX = poolBlock.Bounds.X + 40f;
                float bodyWidth = poolBlock.Bounds.Width - 40f;

                foreach (var lane in poolBlock.PoolLanes)
                {
                    lane.Bounds = new RectangleF(bodyX, currentY, bodyWidth, lane.Bounds.Height);
                    currentY += lane.Bounds.Height;

                    // Обновляем позиции вложенных дорожек
                    UpdateNestedLanesPositions(lane, bodyX + 20f, bodyWidth - 20f);
                }

                // Обновляем высоту пула
                float totalHeight = currentY - poolBlock.Bounds.Y;
                poolBlock.Bounds = new RectangleF(
                    poolBlock.Bounds.X,
                    poolBlock.Bounds.Y,
                    poolBlock.Bounds.Width,
                    Math.Max(120f, totalHeight)
                );
            }

            /// <summary>
            /// Обновляет позиции вложенных дорожек
            /// </summary>
            private void UpdateNestedLanesPositions(PoolLine parentLane, float x, float width)
            {
                if (parentLane.ChildLines == null) return;

                float currentY = parentLane.Bounds.Y;
                foreach (var childLane in parentLane.ChildLines)
                {
                    childLane.Bounds = new RectangleF(x, currentY, width, childLane.Bounds.Height);
                    currentY += childLane.Bounds.Height;

                    UpdateNestedLanesPositions(childLane, x + 20f, width - 20f);
                }

                float totalHeight = currentY - parentLane.Bounds.Y;
                if (totalHeight > parentLane.Bounds.Height)
                {
                    parentLane.Bounds = new RectangleF(
                        parentLane.Bounds.X,
                        parentLane.Bounds.Y,
                        parentLane.Bounds.Width,
                        totalHeight
                    );
                }
            }
        }

        /// <summary>
        /// Команда удаления дорожки из пула
        /// </summary>
        public class RemoveLaneCommand : ICommand
        {
            private readonly BpmnBlock _poolBlock;
            private readonly PoolLine _lane;
            private readonly List<BpmnBlock> _blocks;
            private readonly InfiniteCanvas _canvas;
            private readonly bool _isNested;
            private readonly PoolLine _parentLane;

            /// <summary>
            /// Получает описание команды
            /// </summary>
            public string Description => "Remove Lane";

            /// <summary>
            /// Инициализирует команду удаления дорожки
            /// </summary>
            /// <param name="poolBlock">Пул для удаления дорожки</param>
            /// <param name="lane">Удаляемая дорожка</param>
            /// <param name="blocks">Список блоков на холсте</param>
            /// <param name="canvas">Холст для отображения</param>
            /// <param name="isNested">Указывает является ли дорожка вложенной</param>
            /// <param name="parentLane">Родительская дорожка (для вложенных дорожек)</param>
            public RemoveLaneCommand(BpmnBlock poolBlock, PoolLine lane, List<BpmnBlock> blocks,
                                    InfiniteCanvas canvas, bool isNested = false, PoolLine parentLane = null)
            {
                _poolBlock = poolBlock;
                _lane = lane;
                _blocks = blocks;
                _canvas = canvas;
                _isNested = isNested;
                _parentLane = parentLane;
            }

            /// <summary>
            /// Выполняет удаление дорожки
            /// </summary>
            public void Execute()
            {
                if (_isNested && _parentLane != null)
                {
                    _parentLane.ChildLines.Remove(_lane);
                }
                else
                {
                    _poolBlock.PoolLanes.Remove(_lane);
                }

                // Пересчитываем позиции дорожек
                RecalculateLanesPositions(_poolBlock);
                _canvas.SetBlocks(_blocks);
                _canvas.Invalidate();
            }

            /// <summary>
            /// Отменяет удаление дорожки
            /// </summary>
            public void Undo()
            {
                if (_isNested && _parentLane != null)
                {
                    _parentLane.ChildLines.Add(_lane);
                }
                else
                {
                    _poolBlock.PoolLanes.Add(_lane);
                }

                // Пересчитываем позиции дорожек
                RecalculateLanesPositions(_poolBlock);
                _canvas.SetBlocks(_blocks);
                _canvas.Invalidate();
            }

            /// <summary>
            /// Пересчитывает позиции дорожек в пуле
            /// </summary>
            private void RecalculateLanesPositions(BpmnBlock poolBlock)
            {
                // Та же логика, что и в AddLaneCommand
                if (poolBlock.PoolLanes == null) return;

                float currentY = poolBlock.Bounds.Y + 40f;
                float bodyX = poolBlock.Bounds.X + 40f;
                float bodyWidth = poolBlock.Bounds.Width - 40f;

                foreach (var lane in poolBlock.PoolLanes)
                {
                    lane.Bounds = new RectangleF(bodyX, currentY, bodyWidth, lane.Bounds.Height);
                    currentY += lane.Bounds.Height;

                    UpdateNestedLanesPositions(lane, bodyX + 20f, bodyWidth - 20f);
                }

                float totalHeight = currentY - poolBlock.Bounds.Y;
                poolBlock.Bounds = new RectangleF(
                    poolBlock.Bounds.X,
                    poolBlock.Bounds.Y,
                    poolBlock.Bounds.Width,
                    Math.Max(120f, totalHeight)
                );
            }

            /// <summary>
            /// Обновляет позиции вложенных дорожек
            /// </summary>
            private void UpdateNestedLanesPositions(PoolLine parentLane, float x, float width)
            {
                if (parentLane.ChildLines == null) return;

                float currentY = parentLane.Bounds.Y;
                foreach (var childLane in parentLane.ChildLines)
                {
                    childLane.Bounds = new RectangleF(x, currentY, width, childLane.Bounds.Height);
                    currentY += childLane.Bounds.Height;

                    UpdateNestedLanesPositions(childLane, x + 20f, width - 20f);
                }

                float totalHeight = currentY - parentLane.Bounds.Y;
                if (totalHeight > parentLane.Bounds.Height)
                {
                    parentLane.Bounds = new RectangleF(
                        parentLane.Bounds.X,
                        parentLane.Bounds.Y,
                        parentLane.Bounds.Width,
                        totalHeight
                    );
                }
            }
        }

        /// <summary>
        /// Макрокоманда для группировки нескольких команд
        /// </summary>
        public class MacroCommand : ICommand
        {
            private readonly List<ICommand> _commands;
            private readonly string _description;

            /// <summary>
            /// Получает описание макрокоманды
            /// </summary>
            public string Description => _description;

            /// <summary>
            /// Инициализирует макрокоманду
            /// </summary>
            /// <param name="commands">Список команд для группировки</param>
            /// <param name="description">Описание макрокоманды</param>
            public MacroCommand(List<ICommand> commands, string description)
            {
                _commands = commands;
                _description = description;
            }

            /// <summary>
            /// Выполняет все команды в макрокоманде
            /// </summary>
            public void Execute()
            {
                // Выполняем все команды в списке
                foreach (var cmd in _commands)
                {
                    cmd.Execute();
                }
            }

            /// <summary>
            /// Отменяет все команды в макрокоманде в обратном порядке
            /// </summary>
            public void Undo()
            {
                // Отменяем в ОБРАТНОМ порядке
                for (int i = _commands.Count - 1; i >= 0; i--)
                {
                    _commands[i].Undo();
                }
            }
        }

        /// <summary>
        /// Команда перемещения прямой стрелки
        /// </summary>
        public class MoveArrowCommand : ICommand
        {
            private readonly BpmnArrow _arrow;
            private readonly PointF _originalStartPoint;
            private readonly PointF _originalEndPoint;
            private readonly PointF _newStartPoint;
            private readonly PointF _newEndPoint;
            private readonly InfiniteCanvas _canvas;

            /// <summary>
            /// Получает описание команды
            /// </summary>
            public string Description => "Move Arrow";

            /// <summary>
            /// Инициализирует команду перемещения стрелки
            /// </summary>
            /// <param name="arrow">Перемещаемая стрелка</param>
            /// <param name="originalStartPoint">Исходная начальная точка</param>
            /// <param name="originalEndPoint">Исходная конечная точка</param>
            /// <param name="newStartPoint">Новая начальная точка</param>
            /// <param name="newEndPoint">Новая конечная точка</param>
            /// <param name="canvas">Холст для отображения</param>
            public MoveArrowCommand(BpmnArrow arrow,
                                  PointF originalStartPoint, PointF originalEndPoint,
                                  PointF newStartPoint, PointF newEndPoint,
                                  InfiniteCanvas canvas)
            {
                _arrow = arrow;
                _originalStartPoint = originalStartPoint;
                _originalEndPoint = originalEndPoint;
                _newStartPoint = newStartPoint;
                _newEndPoint = newEndPoint;
                _canvas = canvas;
            }

            /// <summary>
            /// Выполняет перемещение стрелки
            /// </summary>
            public void Execute()
            {
                _arrow.StartPoint = _newStartPoint;
                _arrow.EndPoint = _newEndPoint;
                _arrow.CalculateOrthogonalPath();
                _canvas.Invalidate();
            }

            /// <summary>
            /// Отменяет перемещение стрелки
            /// </summary>
            public void Undo()
            {
                _arrow.StartPoint = _originalStartPoint;
                _arrow.EndPoint = _originalEndPoint;
                _arrow.CalculateOrthogonalPath();
                _canvas.Invalidate();
            }
        }

        /// <summary>
        /// Команда удаления кривой стрелки
        /// </summary>
        public class DeleteCurvedArrowCommand : ICommand
        {
            private readonly BpmnCurvedArrow _curvedArrow;
            private readonly List<BpmnCurvedArrow> _curvedArrows;
            private readonly InfiniteCanvas _canvas;

            /// <summary>
            /// Получает описание команды
            /// </summary>
            public string Description => "Delete Curved Arrow";

            /// <summary>
            /// Инициализирует команду удаления кривой стрелки
            /// </summary>
            /// <param name="curvedArrow">Удаляемая кривая стрелка</param>
            /// <param name="curvedArrows">Список кривых стрелок на холсте</param>
            /// <param name="canvas">Холст для отображения</param>
            public DeleteCurvedArrowCommand(BpmnCurvedArrow curvedArrow,
                                           List<BpmnCurvedArrow> curvedArrows,
                                           InfiniteCanvas canvas)
            {
                _curvedArrow = curvedArrow;
                _curvedArrows = curvedArrows;
                _canvas = canvas;
            }

            /// <summary>
            /// Выполняет удаление кривой стрелки
            /// </summary>
            public void Execute()
            {
                _curvedArrows.Remove(_curvedArrow);
                _canvas.SetCurvedArrows(_curvedArrows);
                _canvas.Invalidate();
            }

            /// <summary>
            /// Отменяет удаление кривой стрелки
            /// </summary>
            public void Undo()
            {
                _curvedArrows.Add(_curvedArrow);
                _canvas.SetCurvedArrows(_curvedArrows);
                _canvas.Invalidate();
            }
        }
    }
}