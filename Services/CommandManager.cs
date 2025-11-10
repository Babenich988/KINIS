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

    internal class CommandManager
    {
        private readonly Stack<ICommand> _undoStack = new Stack<ICommand>();
        private readonly Stack<ICommand> _redoStack = new Stack<ICommand>();
    }
}
