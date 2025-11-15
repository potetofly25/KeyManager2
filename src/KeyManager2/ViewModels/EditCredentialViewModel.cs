using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using potetofly25.KeyManager2.Models;
using potetofly25.KeyManager2.Services;

namespace potetofly25.KeyManager2.ViewModels
{
    /// <summary>
    /// 資格情報（<see cref="Credential"/>）の編集画面用 ViewModel クラスです。
    /// パスワードの編集・自動生成およびダイアログの OK / Cancel 操作を制御します。
    /// </summary>
    public partial class EditCredentialViewModel : ObservableObject
    {
        /// <summary>
        /// 編集対象となる資格情報オブジェクトです。
        /// 画面の各入力項目とバインドされます。
        /// </summary>
        [ObservableProperty]
        private Credential credential;

        /// <summary>
        /// 自動生成するパスワードの長さです。
        /// </summary>
        [ObservableProperty]
        private int length = 12;

        /// <summary>
        /// 自動生成パスワードに英大文字を含めるかどうかを表すフラグです。
        /// </summary>
        [ObservableProperty]
        private bool useUpper = true;

        /// <summary>
        /// 自動生成パスワードに英小文字を含めるかどうかを表すフラグです。
        /// </summary>
        [ObservableProperty]
        private bool useLower = true;

        /// <summary>
        /// 自動生成パスワードに数字を含めるかどうかを表すフラグです。
        /// </summary>
        [ObservableProperty]
        private bool useDigits = true;

        /// <summary>
        /// 自動生成パスワードに記号を含めるかどうかを表すフラグです。
        /// </summary>
        [ObservableProperty]
        private bool useSymbols = true;

        /// <summary>
        /// パスワード生成ロジックを提供するサービスです。
        /// </summary>
        private readonly PasswordGeneratorService _pg = new();

        /// <summary>
        /// <see cref="EditCredentialViewModel"/> の新しいインスタンスを初期化します。
        /// 編集対象の <see cref="Credential"/> を受け取り、画面へ反映するためのプロパティに設定します。
        /// </summary>
        /// <param name="c">編集対象となる資格情報オブジェクト。</param>
        public EditCredentialViewModel(Credential c)
        {
            // 画面で編集する Credential をセット
            Credential = c;
        }

        /// <summary>
        /// パスワード自動生成コマンドです。
        /// 現在の設定（<see cref="Length"/>、<see cref="UseUpper"/>、<see cref="UseLower"/>、
        /// <see cref="UseDigits"/>、<see cref="UseSymbols"/>）に基づいてパスワードを生成し、
        /// <see cref="Credential.Password"/> に反映します。
        /// </summary>
        [RelayCommand]
        private void GeneratePassword()
        {
            // パスワードを生成し、Credential に適用
            Credential.Password = _pg.Generate(Length, UseUpper, UseLower, UseDigits, UseSymbols);

            // Credential オブジェクトの変更を UI に伝える
            OnPropertyChanged(nameof(Credential));
        }

        /// <summary>
        /// 編集結果を保存（確定）するコマンドです。
        /// 呼び出し元にはダイアログの戻り値として true を返します。
        /// </summary>
        /// <param name="window">この ViewModel に対応するウィンドウインスタンス。</param>
        [RelayCommand]
        private static void Save(Window window)
        {
            // true を返してウィンドウを閉じる（保存確定を意味する）
            window?.Close(true);
        }

        /// <summary>
        /// 編集をキャンセルするコマンドです。
        /// 呼び出し元にはダイアログの戻り値として false を返します。
        /// </summary>
        /// <param name="window">この ViewModel に対応するウィンドウインスタンス。</param>
        [RelayCommand]
        private static void Cancel(Window window)
        {
            // false を返してウィンドウを閉じる（キャンセルを意味する）
            window?.Close(false);
        }
    }
}
