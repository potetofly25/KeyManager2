using Avalonia.Controls;
using potetofly25.KeyManager2.ViewModels;
using potetofly25.KeyManager2.Models;

namespace potetofly25.KeyManager2.Views
{
    public partial class EditCredentialWindow : Window
    {
        public EditCredentialViewModel ViewModel => DataContext as EditCredentialViewModel;
        public EditCredentialWindow(Credential c)
        {
            InitializeComponent();
            DataContext = new EditCredentialViewModel(c);
        }
    }
}
