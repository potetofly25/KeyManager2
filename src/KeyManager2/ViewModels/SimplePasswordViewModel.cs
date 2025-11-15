using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace potetofly25.KeyManager2.ViewModels
{
    public partial class SimplePasswordViewModel : ObservableObject
    {
        private readonly Window _window;

        [ObservableProperty]
        private string password;

        [ObservableProperty]
        private string errorMessage;

        public IRelayCommand OkCommand { get; }
        public IRelayCommand CancelCommand { get; }

        public SimplePasswordViewModel(Window window)
        {
            _window = window;

            OkCommand = new RelayCommand(OnOk);
            CancelCommand = new RelayCommand(OnCancel);
        }

        private void OnOk()
        {
            if (string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Password cannot be empty.";
                return;
            }

            _window.Close(Password);

            // メモリクリア
            Password = null;
        }

        private void OnCancel()
        {
            _window.Close(null);
        }
    }
}
