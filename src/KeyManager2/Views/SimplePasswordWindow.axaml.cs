using Avalonia.Controls;
using potetofly25.KeyManager2.ViewModels;

namespace potetofly25.KeyManager2.Views
{
    public partial class SimplePasswordWindow : Window
    {
        public SimplePasswordViewModel ViewModel => DataContext as SimplePasswordViewModel;

        public SimplePasswordWindow(string title = "Enter Password")
        {
            InitializeComponent();
            Title = title;
            DataContext = new SimplePasswordViewModel(this);
        }
    }
}
