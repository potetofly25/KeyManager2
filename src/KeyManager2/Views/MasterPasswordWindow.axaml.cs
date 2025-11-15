using Avalonia.Controls;
using potetofly25.KeyManager2.ViewModels;

namespace potetofly25.KeyManager2.Views
{
    public partial class MasterPasswordWindow : Window
    {
        public MasterPasswordViewModel ViewModel => DataContext as MasterPasswordViewModel;

        public MasterPasswordWindow()
        {
            InitializeComponent();
            DataContext = new MasterPasswordViewModel(this);
        }
    }
}
