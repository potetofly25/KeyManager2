using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace potetofly25.KeyManager2.ViewModels
{
    /// <summary>
    /// マスターパスワード設定ダイアログ用の ViewModel クラスです。
    /// ユーザーからマスターパスワードを入力・確認し、<c>AdvancedEncryptionService</c> を用いて
    /// ルートキーの生成または復元を行います。
    /// </summary>
    public partial class MasterPasswordViewModel : ObservableObject
    {
        /// <summary>
        /// この ViewModel に紐づくウィンドウインスタンスです。
        /// マスターパスワード設定完了またはキャンセル時にダイアログを閉じるために使用します。
        /// </summary>
        private readonly Window _window;

        /// <summary>
        /// 入力中のマスターパスワード文字列です。
        /// </summary>
        [ObservableProperty]
        private string masterPassword;

        /// <summary>
        /// マスターパスワード確認入力用の文字列です。
        /// <see cref="masterPassword"/> と一致しているかを検証します。
        /// </summary>
        [ObservableProperty]
        private string masterPasswordConfirm;

        /// <summary>
        /// 入力エラーや設定失敗時のメッセージを保持するプロパティです。
        /// 画面上にバインドしてエラー内容をユーザーに表示します。
        /// </summary>
        [ObservableProperty]
        private string errorMessage;

        /// <summary>
        /// マスターパスワードを検証および保存処理するコマンドです。
        /// 入力チェックのうえ、<c>AdvancedEncryptionService</c> を呼び出します。
        /// </summary>
        public IRelayCommand SetPasswordCommand { get; }

        /// <summary>
        /// マスターパスワード設定をキャンセルしてダイアログを閉じるコマンドです。
        /// </summary>
        public IRelayCommand CancelCommand { get; }

        /// <summary>
        /// <see cref="MasterPasswordViewModel"/> の新しいインスタンスを初期化します。
        /// コマンドのバインド設定を行います。
        /// </summary>
        /// <param name="window">この ViewModel に紐づけるウィンドウインスタンス。</param>
        public MasterPasswordViewModel(Window window)
        {
            _window = window;

            // コマンドとメソッドの関連付け
            SetPasswordCommand = new RelayCommand(SetPassword);
            CancelCommand = new RelayCommand(Cancel);
        }

        /// <summary>
        /// 入力されたマスターパスワードを検証し、<c>AdvancedEncryptionService</c> を通じて
        /// マスターパスワードおよびルートキーを設定します。
        /// 成功時にはダイアログを OK 結果として閉じます。
        /// </summary>
        private void SetPassword()
        {
            // 直前のエラーメッセージをクリア
            ErrorMessage = string.Empty;

            // 空文字・空白のみのパスワードは許可しない
            if (string.IsNullOrWhiteSpace(MasterPassword))
            {
                ErrorMessage = "Master password cannot be empty.";
                return;
            }

            // 確認入力との一致を検証
            if (MasterPassword != MasterPasswordConfirm)
            {
                ErrorMessage = "Passwords do not match.";
                return;
            }

            try
            {
                // マスターキー設定処理
                // すでにルートキーが保存済みかどうかで処理を分岐
                if (System.IO.File.Exists("KeyManager2_root.wrapped"))
                {
                    // 既存のラップ済みルートキーがある場合は、その復号のためのマスターパスワードとしてセット
                    Services.AdvancedEncryptionService.SetMasterPassword(MasterPassword);
                }
                else
                {
                    // まだルートキーがない場合は、新規にルートキーを生成しマスターパスワードでラップ
                    Services.AdvancedEncryptionService.InitializeMasterPassword(MasterPassword);
                }

                // 設定成功時はマスターパスワードをダイアログの戻り値としてウィンドウを閉じる
                _window.Close(MasterPassword);
            }
            catch (Exception ex)
            {
                // 失敗時のエラーメッセージを画面へ表示
                ErrorMessage = $"Failed to set master password: {ex.Message}";
            }
            finally
            {
                // メモリ上のパスワードをクリア（セキュリティ対策）
                MasterPassword = string.Empty;
                MasterPasswordConfirm = string.Empty;
            }
        }

        /// <summary>
        /// マスターパスワード設定処理をキャンセルし、ダイアログを閉じます。
        /// 戻り値として null を返却することで、呼び出し側にキャンセルを通知します。
        /// </summary>
        private void Cancel()
        {
            // キャンセルを表す null を持ってウィンドウを閉じる
            _window.Close(null);
        }
    }
}
