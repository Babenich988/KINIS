using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kinis.Models;

namespace Kinis.Services
{
    public interface ICommand
    {
        void Execute();
        void Undo();
        string Description { get; }
    }

    public class CommandManager
    {
        private readonly Stack<ICommand> _undoStack = new Stack<ICommand>();
        private readonly Stack<ICommand> _redoStack = new Stack<ICommand>();

        public event Action OnStateChanged;

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public void Execute(ICommand command)
        {
            command.Execute();
            _undoStack.Push(command);
            _redoStack.Clear();
            OnStateChanged?.Invoke();
        }

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

        public class CreateBlockCommand : ICommand
        {
            private readonly BpmnBlock _block;
            private readonly List<BpmnBlock> _blocks;
            private readonly InfiniteCanvas _canvas;

            public string Description => $"Create {_block.Type}";

            public CreateBlockCommand(BpmnBlock block, List<BpmnBlock> blocks, InfiniteCanvas canvas)
            {
                _block = block;
                _blocks = blocks;
                _canvas = canvas;
            }

            public void Execute()
            {
                _blocks.Add(_block);
                _canvas.SetBlocks(_blocks);
                _canvas.Invalidate();
            }

            public void Undo()
            {
                _blocks.Remove(_block);
                _canvas.SetBlocks(_blocks);
                _canvas.Invalidate();
            }
        }
        public class CreateArrowCommand : ICommand
        {
            private readonly BpmnArrow _arrow;
            private readonly List<BpmnArrow> _arrows;
            private readonly InfiniteCanvas _canvas;

            public string Description => "Create Arrow";

            public CreateArrowCommand(BpmnArrow arrow, List<BpmnArrow> arrows, InfiniteCanvas canvas)
            {
                _arrow = arrow;
                _arrows = arrows; // Используем переданный список, а не создаем новый
                _canvas = canvas;
            }

            public void Execute()
            {
                // ДОБАВЛЯЕМ в существующий список
                _arrows.Add(_arrow);
                _canvas.SetArrows(_arrows);
                _canvas.Invalidate();
                Console.WriteLine($"Arrow added via command, total arrows: {_arrows.Count}");
            }

            public void Undo()
            {
                // УДАЛЯЕМ из существующего списка
                _arrows.Remove(_arrow);
                _canvas.SetArrows(_arrows);
                _canvas.Invalidate();
                Console.WriteLine($"Arrow removed via undo, total arrows: {_arrows.Count}");
            }
        }

        public class DeleteBlockCommand : ICommand
        {
            private readonly BpmnBlock _block;
            private readonly List<BpmnBlock> _blocks;
            private readonly InfiniteCanvas _canvas;
            private readonly List<BpmnArrow> _arrows;
            private List<BpmnArrow> _removedArrows;

            public string Description => $"Delete {_block.Type}";

            public DeleteBlockCommand(BpmnBlock block, List<BpmnBlock> blocks, List<BpmnArrow> arrows, InfiniteCanvas canvas)
            {
                _block = block;
                _blocks = blocks;
                _arrows = arrows;
                _canvas = canvas;
                _removedArrows = new List<BpmnArrow>();
            }
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

        public class DeleteArrowCommand : ICommand
        {
            private readonly BpmnArrow _arrow;
            private readonly List<BpmnArrow> _arrows;
            private readonly InfiniteCanvas _canvas;

            public string Description => "Delete Arrow";

            public DeleteArrowCommand(BpmnArrow arrow, List<BpmnArrow> arrows, InfiniteCanvas canvas)
            {
                _arrow = arrow;
                _arrows = arrows;
                _canvas = canvas;
            }
            public void Execute()
            {
                _arrows.Remove(_arrow);
                _canvas.SetArrows(_arrows);
                _canvas.Invalidate();
            }
            public void Undo()
            {
                _arrows.Add(_arrow);
                _canvas.SetArrows(_arrows);
                _canvas.Invalidate();
            }
        }
        public class MoveBlockCommand : ICommand
        {
            private readonly BpmnBlock _block;
            private readonly RectangleF _originalBounds;
            private readonly RectangleF _newBounds;
            private readonly List<BpmnArrow> _arrows;
            private readonly InfiniteCanvas _canvas;
            private Dictionary<BpmnArrow, PointF> _originalStartPoints;
            private Dictionary<BpmnArrow, PointF> _originalEndPoints;

            public string Description => $"Move {_block.Type}";

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
            public void Execute()
            {
                _block.Bounds = _newBounds;
                UpdateAttachedArrows(_newBounds);
                _canvas.Invalidate();
            }
            public void Undo()
            {
                _block.Bounds = _originalBounds;
                RestoreArrowPositions();
                _canvas.Invalidate();
            }
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
            private void RestoreArrowPositions()
            {
                foreach (var kvp in _originalStartPoints)
                    kvp.Key.StartPoint = kvp.Value;

                foreach (var kvp in _originalEndPoints)
                    kvp.Key.EndPoint = kvp.Value;
            }
        }
        public class ChangeTextCommand : ICommand
        {
            private readonly BpmnBlock _block;
            private readonly string _oldText;
            private readonly string _newText;
            private readonly InfiniteCanvas _canvas;

            public string Description => $"Change text of {_block.Type}";

            public ChangeTextCommand(BpmnBlock block, string oldText, string newText, InfiniteCanvas canvas)
            {
                _block = block;
                _oldText = oldText;
                _newText = newText;
                _canvas = canvas;
            }
            public void Execute()
            {
                _block.Text = _newText;
                _canvas.Invalidate();
            }
            public void Undo()
            {
                _block.Text = _oldText;
                _canvas.Invalidate();
            }
        }
        public class ModifyArrowCommand : ICommand
        {
            private readonly BpmnArrow _arrow;
            private readonly BpmnBlock _originalStartBlock;
            private readonly PointF _originalStartPoint;
            private readonly BpmnBlock _originalEndBlock;
            private readonly PointF _originalEndPoint;
            private readonly BpmnBlock _newStartBlock;
            private readonly PointF _newStartPoint;
            private readonly BpmnBlock _newEndBlock;
            private readonly PointF _newEndPoint;
            private readonly InfiniteCanvas _canvas;

            public string Description => "Modify Arrow";

            public ModifyArrowCommand(BpmnArrow arrow,
                BpmnBlock originalStartBlock, PointF originalStartPoint,
                BpmnBlock originalEndBlock, PointF originalEndPoint,
                BpmnBlock newStartBlock, PointF newStartPoint,
                BpmnBlock newEndBlock, PointF newEndPoint,
                InfiniteCanvas canvas)
            {
                _arrow = arrow;
                _originalStartBlock = originalStartBlock;
                _originalStartPoint = originalStartPoint;
                _originalEndBlock = originalEndBlock;
                _originalEndPoint = originalEndPoint;
                _newStartBlock = newStartBlock;
                _newStartPoint = newStartPoint;
                _newEndBlock = newEndBlock;
                _newEndPoint = newEndPoint;
                _canvas = canvas;
            }

            public void Execute()
            {
                _arrow.StartBlock = _newStartBlock;
                _arrow.StartPoint = _newStartPoint;
                _arrow.EndBlock = _newEndBlock;
                _arrow.EndPoint = _newEndPoint;
                _canvas.Invalidate();
            }

            public void Undo()
            {
                _arrow.StartBlock = _originalStartBlock;
                _arrow.StartPoint = _originalStartPoint;
                _arrow.EndBlock = _originalEndBlock;
                _arrow.EndPoint = _originalEndPoint;
                _canvas.Invalidate();
            }
        }

        public class ResizeBlockCommand : ICommand
        {
            private readonly BpmnBlock _block;
            private readonly RectangleF _originalBounds;
            private readonly RectangleF _newBounds;
            private readonly Dictionary<BpmnArrow, (PointF startPoint, PointF endPoint)> _arrowStates;
            private readonly InfiniteCanvas _canvas;

            public string Description => $"Resize {_block.Type}";

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

            public void Execute()
            {
                _block.Bounds = _newBounds;
                UpdateArrowPositions();
                _canvas.Invalidate();
            }

            public void Undo()
            {
                _block.Bounds = _originalBounds;
                RestoreArrowPositions();
                _canvas.Invalidate();
            }

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

            private float Distance(PointF a, PointF b)
            {
                float dx = a.X - b.X;
                float dy = a.Y - b.Y;
                return (float)Math.Sqrt(dx * dx + dy * dy);
            }
        }
    }
}
