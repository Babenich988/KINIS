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
                _canvas.RaiseElementAdded();
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
                _canvas.RaiseElementAdded();
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

        public class CreateCurvedArrowCommand : ICommand
        {
            private readonly BpmnCurvedArrow _curvedArrow;
            private readonly List<BpmnCurvedArrow> _curvedArrows;
            private readonly InfiniteCanvas _canvas;

            public string Description => "Create Curved Arrow";

            public CreateCurvedArrowCommand(BpmnCurvedArrow curvedArrow, List<BpmnCurvedArrow> curvedArrows, InfiniteCanvas canvas)
            {
                _curvedArrow = curvedArrow;
                _curvedArrows = curvedArrows;
                _canvas = canvas;
            }

            public void Execute()
            {
                _curvedArrows.Add(_curvedArrow);
                _canvas.SetCurvedArrows(_curvedArrows);
                _canvas.Invalidate();
                Console.WriteLine($"Curved arrow added via command, total curved arrows: {_curvedArrows.Count}");
            }

            public void Undo()
            {
                _curvedArrows.Remove(_curvedArrow);
                _canvas.SetCurvedArrows(_curvedArrows);
                _canvas.Invalidate();
                Console.WriteLine($"Curved arrow removed via undo, total curved arrows: {_curvedArrows.Count}");
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
                private readonly bool _isStartModified; // ДОБАВИЛИ

                public string Description => "Modify Arrow";

                public ModifyArrowCommand(BpmnArrow arrow,
                    BpmnBlock originalStartBlock, PointF originalStartPoint, int originalStartIndex,
                    BpmnBlock originalEndBlock, PointF originalEndPoint, int originalEndIndex,
                    BpmnBlock newStartBlock, PointF newStartPoint, int newStartIndex,
                    BpmnBlock newEndBlock, PointF newEndPoint, int newEndIndex,
                    bool isStartModified, // ДОБАВИЛИ
                    InfiniteCanvas canvas)
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
                    _isStartModified = isStartModified; // ДОБАВИЛИ
                    _canvas = canvas;
                }

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

                public string Description => "Modify Curved Arrow";

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

        public class AddLaneCommand : ICommand
        {
            private readonly BpmnBlock _poolBlock;
            private readonly PoolLine _lane;
            private readonly List<BpmnBlock> _blocks;
            private readonly InfiniteCanvas _canvas;
            private readonly bool _isNested;
            private readonly PoolLine _parentLane;

            public string Description => "Add Lane";

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

        public class RemoveLaneCommand : ICommand
        {
            private readonly BpmnBlock _poolBlock;
            private readonly PoolLine _lane;
            private readonly List<BpmnBlock> _blocks;
            private readonly InfiniteCanvas _canvas;
            private readonly bool _isNested;
            private readonly PoolLine _parentLane;

            public string Description => "Remove Lane";

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

        public class MacroCommand : ICommand
        {
            private readonly List<ICommand> _commands;
            private readonly string _description;

            public string Description => _description;

            public MacroCommand(List<ICommand> commands, string description)
            {
                _commands = commands;
                _description = description;
            }

            public void Execute()
            {
                // Выполняем все команды в списке
                foreach (var cmd in _commands)
                {
                    cmd.Execute();
                }
            }

            public void Undo()
            {
                // Отменяем в ОБРАТНОМ порядке
                for (int i = _commands.Count - 1; i >= 0; i--)
                {
                    _commands[i].Undo();
                }
            }
        }

        public class DeleteCurvedArrowCommand : ICommand
        {
            private readonly BpmnCurvedArrow _curvedArrow;
            private readonly List<BpmnCurvedArrow> _curvedArrows;
            private readonly InfiniteCanvas _canvas;

            public string Description => "Delete Curved Arrow";

            public DeleteCurvedArrowCommand(BpmnCurvedArrow curvedArrow,
                                           List<BpmnCurvedArrow> curvedArrows,
                                           InfiniteCanvas canvas)
            {
                _curvedArrow = curvedArrow;
                _curvedArrows = curvedArrows;
                _canvas = canvas;
            }

            public void Execute()
            {
                _curvedArrows.Remove(_curvedArrow);
                _canvas.SetCurvedArrows(_curvedArrows);
                _canvas.Invalidate();
            }

            public void Undo()
            {
                _curvedArrows.Add(_curvedArrow);
                _canvas.SetCurvedArrows(_curvedArrows);
                _canvas.Invalidate();
            }
        }

        public class MoveArrowCommand : ICommand
        {
            private readonly BpmnArrow _arrow;
            private readonly PointF _originalStartPoint;
            private readonly PointF _originalEndPoint;
            private readonly PointF _newStartPoint;
            private readonly PointF _newEndPoint;
            private readonly InfiniteCanvas _canvas;

            public string Description => "Move Arrow";

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

            public void Execute()
            {
                _arrow.StartPoint = _newStartPoint;
                _arrow.EndPoint = _newEndPoint;
                _arrow.CalculateOrthogonalPath();
                _canvas.Invalidate();
            }

            public void Undo()
            {
                _arrow.StartPoint = _originalStartPoint;
                _arrow.EndPoint = _originalEndPoint;
                _arrow.CalculateOrthogonalPath();
                _canvas.Invalidate();
            }
        }
    }
}