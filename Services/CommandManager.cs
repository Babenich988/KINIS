using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
