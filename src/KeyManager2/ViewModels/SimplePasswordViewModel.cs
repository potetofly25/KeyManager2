using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace potetofly25.KeyManager2.ViewModels
{
    /// <summary>
    /// 単一のパスワード入力ダイアログ用の ViewModel クラスです。
    /// ユーザーからパスワードを入力させ、その値をダイアログの戻り値として返します。
    /// </summary>
    public partial class SimplePasswordViewModel : ObservableObject
    {
        /// <summary>
        /// この ViewModel に紐づくウィンドウインスタンスです。
        /// パスワード入力完了またはキャンセル時にダイアログを閉じるために使用します。
        /// </summary>
        private readonly Window _window;

        /// <summary>
        /// 入力されたパスワードを保持するプロパティです。
        /// 空白や未入力の場合はバリデーションエラーになります。
        /// </summary>
        [ObservableProperty]
        private string password;

        /// <summary>
        /// 入力エラーが発生した際のエラーメッセージを保持します。
        /// UI でユーザーに通知するために使用します。
        /// </summary>
        [ObservableProperty]
        private string errorMessage;

        /// <summary>
        /// パスワード入力を確定するコマンドです。
        /// 入力チェックを行い、問題なければダイアログを閉じます。
        /// </summary>
        public IRelayCommand OkCommand { get; }

        /// <summary>
        /// パスワード入力をキャンセルし、ダイアログを閉じるコマンドです。
        /// </summary>
        public IRelayCommand CancelCommand { get; }

        /// <summary>
        /// <see cref="SimplePasswordViewModel"/> の新しいインスタンスを初期化します。
        /// コマンドとウィンドウの関連付けを行います。
        /// </summary>
        /// <param name="window">紐づけるウィンドウインスタンス。</param>
        public SimplePasswordViewModel(Window window)
        {
            _window = window;

            // コマンドバインディング
            OkCommand = new RelayCommand(OnOk);
            CancelCommand = new RelayCommand(OnCancel);
        }

        /// <summary>
        /// OK 実行時の処理です。
        /// パスワードが未入力の場合はエラーメッセージを設定し、
        /// 正常な場合はダイアログをパスワードを返して閉じます。
        /// </summary>
        private void OnOk()
        {
            // 入力チェック
            if (string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Password cannot be empty.";
                return;
            }

            // 正常入力時はダイアログの戻り値としてパスワードを渡しつつ閉じる
            _window.Close(Password);

            // セキュリティのためメモリ上の値はクリアする
            Password = string.Empty;
        }

        /// <summary>
        /// キャンセル時の処理です。
        /// ダイアログを null を返して閉じます。
        /// </summary>
        private void OnCancel()
        {
            _window.Close(null);
        }
    }
}
