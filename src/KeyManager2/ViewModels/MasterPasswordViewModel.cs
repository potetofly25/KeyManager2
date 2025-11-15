using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace potetofly25.KeyManager2.ViewModels
{
    public partial class MasterPasswordViewModel : ObservableObject
    {
        private readonly Window _window;

        [ObservableProperty]
        private string masterPassword;

        [ObservableProperty]
        private string masterPasswordConfirm;

        [ObservableProperty]
        private string errorMessage;

        public IRelayCommand SetPasswordCommand { get; }
        public IRelayCommand CancelCommand { get; }

        public MasterPasswordViewModel(Window window)
        {
            _window = window;

            SetPasswordCommand = new RelayCommand(SetPassword);
            CancelCommand = new RelayCommand(Cancel);
        }

        private void SetPassword()
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(MasterPassword))
            {
                ErrorMessage = "Master password cannot be empty.";
                return;
            }

            if (MasterPassword != MasterPasswordConfirm)
            {
                ErrorMessage = "Passwords do not match.";
                return;
            }

            try
            {
                // マスターキー設定
                // AdvancedEncryptionService を呼ぶ
                if (System.IO.File.Exists("KeyManager2_root.wrapped"))
                {
                    // 既存キーがある場合はセット
                    Services.AdvancedEncryptionService.SetMasterPassword(MasterPassword);
                }
                else
                {
                    // 新規作成
                    Services.AdvancedEncryptionService.InitializeMasterPassword(MasterPassword);
                }

                // OK で閉じる
                _window.Close(MasterPassword);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to set master password: {ex.Message}";
            }
            finally
            {
                // メモリクリア
                MasterPassword = null;
                MasterPasswordConfirm = null;
            }
        }

        private void Cancel()
        {
            _window.Close(null);
        }
    }

}
