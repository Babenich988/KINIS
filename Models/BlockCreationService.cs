using Kinis.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Kinis.Services
{
    public class BlockCreationService
    {
        private readonly InfiniteCanvas _canvas;
        private readonly List<BpmnBlock> _blocks;

        public BlockCreationService(InfiniteCanvas canvas, List<BpmnBlock> blocks)
        {
            _canvas = canvas;
            _blocks = blocks;
        }
    }
}