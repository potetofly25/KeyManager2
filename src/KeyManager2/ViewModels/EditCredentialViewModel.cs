using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using potetofly25.KeyManager2.Models;
using potetofly25.KeyManager2.Services;
using Avalonia.Controls;

namespace potetofly25.KeyManager2.ViewModels
{
    public partial class EditCredentialViewModel : ObservableObject
    {
        [ObservableProperty]
        private Credential credential;

        [ObservableProperty] private int length = 12;
        [ObservableProperty] private bool useUpper = true;
        [ObservableProperty] private bool useLower = true;
        [ObservableProperty] private bool useDigits = true;
        [ObservableProperty] private bool useSymbols = true;

        private readonly PasswordGeneratorService _pg = new();

        public EditCredentialViewModel(Credential c)
        {
            Credential = c;
        }

        [RelayCommand]
        private void GeneratePassword()
        {
            Credential.Password = _pg.Generate(Length, UseUpper, UseLower, UseDigits, UseSymbols);
            OnPropertyChanged(nameof(Credential));
        }

        [RelayCommand]
        private void Save(Window window)
        {
            window?.Close(true);
        }

        [RelayCommand]
        private void Cancel(Window window)
        {
            window?.Close(false);
        }
    }
}
