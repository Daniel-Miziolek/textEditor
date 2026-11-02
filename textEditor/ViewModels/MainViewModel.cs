using System.ComponentModel;
using System.Windows.Input;
using TextEditor.Commands;

namespace TextEditor.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private TextDocument _document = new TextDocument();

        public TextDocument Document
        {
            get => _document;
            private set
            {
                if (ReferenceEquals(_document, value))
                    return;

                _document = value;
                OnPropertyChanged(nameof(Document));
            }
        }

        public ICommand NewFile { get; }
        public ICommand? Save { get; }
        public ICommand? SaveAs { get; }
        public ICommand? Exit { get; }

        public MainViewModel()
        {
            NewFile = new NewFileCommand(CreateDocument);
        }

        private void CreateDocument()
        {
            Document = new TextDocument();
        }
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
   
}
