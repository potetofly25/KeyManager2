using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using potetofly25.KeyManager2.Services;
using potetofly25.KeyManager2.ViewModels;
using potetofly25.KeyManager2.Views;
using System.Linq;

namespace potetofly25.KeyManager2
{
    /// <summary>
    /// アプリケーションのエントリポイントとなるクラスです。
    /// Avalonia アプリケーションのライフサイクルを管理します。
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// XAML で定義されたアプリケーションリソースを読み込みます。
        /// </summary>
        public override void Initialize()
        {
            // アプリケーション XAML の初期化
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// フレームワークの初期化完了後に呼び出され、メインウィンドウの生成と表示を行います。
        /// </summary>
        public override void OnFrameworkInitializationCompleted()
        {
            // デスクトップアプリケーションとして実行されている場合のみメインウィンドウを生成
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                //// Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
                //// More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                //DisableAvaloniaDataAnnotationValidation();

                // 資格情報用サービスの生成
                var credentialService = new CredentialService();

                // メインウィンドウ用 ViewModel の生成
                var mainWindowViewModel = new MainWindowViewModel(credentialService);

                // メインウィンドウの生成と DataContext の設定
                desktop.MainWindow = new MainWindow
                {
                    DataContext = mainWindowViewModel
                };
            }

            // 基底クラスの処理を呼び出し
            base.OnFrameworkInitializationCompleted();
        }

        //private void DisableAvaloniaDataAnnotationValidation()
        //{
        //    // Get an array of plugins to remove
        //    var dataValidationPluginsToRemove =
        //        BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        //    // remove each entry found
        //    foreach (var plugin in dataValidationPluginsToRemove)
        //    {
        //        BindingPlugins.DataValidators.Remove(plugin);
        //    }
        //}

    }
}