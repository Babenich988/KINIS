using System;
using System.Collections.Generic;
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
                _arrows = arrows;
                _canvas = canvas;
            }
            public void Execute()
            {
                _arrows.Add(_arrow);
                _canvas.SetArrows(_arrows);
                _canvas.Invalidate();
            }
            public void Undo()
            {
                _arrows.Remove(_arrow);
                _canvas.SetArrows(_arrows);
                _canvas.Invalidate();
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
        }
    }
}
