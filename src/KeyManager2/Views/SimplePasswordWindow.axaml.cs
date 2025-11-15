using Avalonia.Controls;
using potetofly25.KeyManager2.ViewModels;

namespace potetofly25.KeyManager2.Views
{
    /// <summary>
    /// 単一パスワード入力用のダイアログウィンドウです。
    /// <see cref="SimplePasswordViewModel"/> を DataContext として使用し、
    /// ユーザーからパスワードを入力してもらう目的で利用されます。
    /// </summary>
    public partial class SimplePasswordWindow : Window
    {
        /// <summary>
        /// このウィンドウの DataContext に設定されている
        /// <see cref="SimplePasswordViewModel"/> を取得します。
        /// </summary>
        public SimplePasswordViewModel ViewModel => DataContext as SimplePasswordViewModel;

        /// <summary>
        /// <see cref="SimplePasswordWindow"/> の新しいインスタンスを初期化します。
        /// ウィンドウタイトルを指定でき、ViewModel と紐付けて UI を構築します。
        /// </summary>
        /// <param name="title">ダイアログのタイトル。未指定の場合は "Enter Password"。</param>
        public SimplePasswordWindow(string title = "Enter Password")
        {
            // XAML に定義されたコンポーネントを初期化
            InitializeComponent();

            // ウィンドウタイトルを設定
            Title = title;

            // このウィンドウを受け取る ViewModel を DataContext として設定
            DataContext = new SimplePasswordViewModel(this);
        }
    }
}
