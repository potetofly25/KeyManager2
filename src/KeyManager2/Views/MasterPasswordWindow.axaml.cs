using Avalonia.Controls;
using potetofly25.KeyManager2.ViewModels;

namespace potetofly25.KeyManager2.Views
{
    /// <summary>
    /// マスターパスワード入力・設定用のダイアログウィンドウです。
    /// <see cref="MasterPasswordViewModel"/> を DataContext としてバインドし、
    /// ユーザーからマスターパスワードを取得します。
    /// </summary>
    public partial class MasterPasswordWindow : Window
    {
        /// <summary>
        /// このウィンドウにバインドされている <see cref="MasterPasswordViewModel"/> を取得します。
        /// DataContext が <see cref="MasterPasswordViewModel"/> でない場合は null を返します。
        /// </summary>
        public MasterPasswordViewModel ViewModel => DataContext as MasterPasswordViewModel;

        /// <summary>
        /// <see cref="MasterPasswordWindow"/> の新しいインスタンスを初期化します。
        /// コンポーネント初期化後、このウィンドウ自身を引数として
        /// <see cref="MasterPasswordViewModel"/> を生成し、DataContext に設定します。
        /// </summary>
        public MasterPasswordWindow()
        {
            // XAML に定義された UI コンポーネントを初期化
            InitializeComponent();

            // このウィンドウを受け取る ViewModel を生成して DataContext に設定
            DataContext = new MasterPasswordViewModel(this);
        }
    }
}
