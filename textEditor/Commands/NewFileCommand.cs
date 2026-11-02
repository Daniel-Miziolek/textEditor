using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextEditor.Commands
{
    public class NewFileCommand : CommandBase
    {
        private readonly Action _onNewFile;

        public NewFileCommand(Action onNewFile)
        {
            _onNewFile = onNewFile;
        }

        public override void Execute(object? parameter)
        {
            _onNewFile();
        }
    }
}
