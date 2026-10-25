using System.Windows.Controls;
using System.Windows.Input;

namespace TextEditor.ViewModels
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public class MainViewModel
    {
        public ICommand NewFile { get; }
        public ICommand Save { get; }
        public ICommand SaveAs { get; }
        public ICommand Exit { get; }

        public MainViewModel()
        {
            
        }
    }
   
}
