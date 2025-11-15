using Avalonia.Controls;
using potetofly25.KeyManager2.ViewModels;
using potetofly25.KeyManager2.Models;

namespace potetofly25.KeyManager2.Views
{
    /// <summary>
    /// 資格情報編集用のダイアログウィンドウです。
    /// 指定された <see cref="Credential"/> をもとに <see cref="EditCredentialViewModel"/> を生成し、
    /// DataContext にバインドします。
    /// </summary>
    public partial class EditCredentialWindow : Window
    {
        /// <summary>
        /// このウィンドウにバインドされている <see cref="EditCredentialViewModel"/> を取得します。
        /// DataContext が <see cref="EditCredentialViewModel"/> の場合のみ参照できます。
        /// </summary>
        public EditCredentialViewModel ViewModel => DataContext as EditCredentialViewModel;

        /// <summary>
        /// <see cref="EditCredentialWindow"/> の新しいインスタンスを初期化します。
        /// 受け取った <see cref="Credential"/> をもとに ViewModel を生成し、DataContext へ設定します。
        /// </summary>
        /// <param name="c">編集対象となる資格情報オブジェクト。</param>
        public EditCredentialWindow(Credential c)
        {
            // ウィンドウコンポーネントを初期化
            InitializeComponent();

            // ViewModel を生成し、DataContext に設定
            DataContext = new EditCredentialViewModel(c);
        }
    }
}
