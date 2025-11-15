using Avalonia.Controls;
using Avalonia.Platform.Storage;
using potetofly25.KeyManager2.Models;
using potetofly25.KeyManager2.Services;
using potetofly25.KeyManager2.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace potetofly25.KeyManager2.Views
{
    /// <summary>
    /// アプリケーションのメインウィンドウを表すクラスです。
    /// 一覧表示、編集、新規作成、削除、コピー、マスターパスワード設定、
    /// エクスポート／インポート、およびタグクラウドの構築などの UI ロジックを担当します。
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// このウィンドウにバインドされている <see cref="MainWindowViewModel"/> を取得します。
        /// DataContext が <see cref="MainWindowViewModel"/> の場合にのみ有効な参照となります。
        /// </summary>
        private MainWindowViewModel? Vm => DataContext as MainWindowViewModel;

        /// <summary>
        /// 資格情報に対するデータ操作（取得・追加・更新・削除）を行うサービスです。
        /// </summary>
        private readonly CredentialService _svc = new();

        /// <summary>
        /// <see cref="MainWindow"/> の新しいインスタンスを初期化します。
        /// コンポーネント初期化後に各種ボタンのイベントハンドラおよびタグクラウドの構築を行います。
        /// </summary>
        public MainWindow()
        {
            // XAML で定義されたコンポーネントを初期化
            InitializeComponent();

            // コントロールの参照を取得
            var dg = this.FindControl<DataGrid>("DataGrid")!;
            var newBtn = this.FindControl<Button>("NewBtn")!;
            var editBtn = this.FindControl<Button>("EditBtn")!;
            var deleteBtn = this.FindControl<Button>("DeleteBtn")!;
            var copyBtn = this.FindControl<Button>("CopyBtn")!;
            var setMasterBtn = this.FindControl<Button>("SetMasterBtn")!;
            var exportBtn = this.FindControl<Button>("ExportBtn")!;
            var importBtn = this.FindControl<Button>("ImportBtn")!;
            var tagPanel = this.FindControl<WrapPanel>("TagCloudPanel")!;

            // 新規作成ボタンクリックイベント
            newBtn.Click += async (_, __) =>
            {
                // 新しい Credential オブジェクトを作成（デフォルト LoginId にタイムスタンプを付与）
                var cred = new Credential
                {
                    LoginId = $"user{System.DateTime.Now.Ticks}"
                };

                // 編集ダイアログを開く
                var win = new EditCredentialWindow(cred);
                var res = await win.ShowDialog<bool?>(this);

                if (res == true)
                {
                    // マスターパスワードが設定されている場合は暗号化を有効にする
                    var encrypt = AdvancedEncryptionService.IsMasterSet;

                    // 編集結果を DB に追加
                    _svc.Add(win.ViewModel.Credential, encryptPassword: encrypt);

                    // 一覧の更新
                    Vm?.RefreshCommand.Execute(null);
                }
            };

            // 編集ボタンクリックイベント
            editBtn.Click += async (_, __) =>
            {
                // DataGrid で選択されている行が Credential でなければ何もしない
                if (dg.SelectedItem is not Credential sel)
                {
                    return;
                }

                // 編集用にコピーを作成（直接バインドしているインスタンスを書き換えないため）
                var copy = new Credential
                {
                    Id = sel.Id,
                    LoginId = sel.LoginId,
                    Password = sel.Password,
                    Description = sel.Description,
                    Category = sel.Category,
                    Tags = sel.Tags,
                    IsEncrypted = sel.IsEncrypted
                };

                // 暗号化済みかつマスターパスワードが設定されている場合は、編集しやすいように復号しておく
                if (copy.IsEncrypted && AdvancedEncryptionService.IsMasterSet)
                {
                    try
                    {
                        copy.Password = AdvancedEncryptionService.DecryptString(copy.Password);
                    }
                    catch
                    {
                        // 復号に失敗した場合はそのまま暗号化文字列を表示
                    }
                }

                // 編集ダイアログを開く
                var win = new EditCredentialWindow(copy);
                var res = await win.ShowDialog<bool?>(this);

                if (res == true)
                {
                    // 現在のマスターパスワード状態に応じて保存時に暗号化するかどうかを決定
                    var encrypt = AdvancedEncryptionService.IsMasterSet;

                    // 更新処理（DB 反映）
                    _svc.Update(win.ViewModel.Credential, encryptPassword: encrypt);

                    // 一覧の更新
                    Vm?.RefreshCommand.Execute(null);
                }
            };

            // 削除ボタンクリックイベント
            deleteBtn.Click += (_, __) =>
            {
                if (dg.SelectedItem is Credential sel)
                {
                    // 選択された Credential を削除
                    _svc.Delete(sel);

                    // 一覧の更新
                    Vm?.RefreshCommand.Execute(null);
                }
            };

            // コピー（パスワードコピー）ボタンクリックイベント
            copyBtn.Click += async (_, __) =>
            {
                if (dg.SelectedItem is Credential sel)
                {
                    var pwd = sel.Password;

                    // 暗号化済みでマスターパスワードが設定されている場合は復号してからクリップボードへ
                    if (sel.IsEncrypted && AdvancedEncryptionService.IsMasterSet)
                    {
                        try
                        {
                            pwd = AdvancedEncryptionService.DecryptString(pwd);
                        }
                        catch
                        {
                            // 復号失敗時は暗号化文字列のままコピー
                        }
                    }

                    // Clipboard の取得（TopLevel から取得する方法を使用）
                    var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
                    if (clipboard != null)
                    {
                        await clipboard.SetTextAsync(pwd);
                    }
                }
            };

            // マスターパスワード設定ボタンクリックイベント
            setMasterBtn.Click += async (_, __) =>
            {
                // マスターパスワード入力ダイアログを表示
                var dlg = new MasterPasswordWindow();
                var pwd = await dlg.ShowDialog<string?>(this);

                if (!string.IsNullOrEmpty(pwd))
                {
                    try
                    {
                        // すでにルートキーが存在すると仮定してセットを試みる
                        AdvancedEncryptionService.SetMasterPassword(pwd);
                    }
                    catch
                    {
                        // 失敗した場合は新規にルートキーを生成して初期化
                        AdvancedEncryptionService.InitializeMasterPassword(pwd);
                    }

                    // マスターパスワード設定後に一覧を再取得（暗号化／復号状態が変わるため）
                    Vm?.RefreshCommand.Execute(null);
                }
            };

            // エクスポートボタンクリックイベント
            exportBtn.Click += async (_, __) =>
            {
                var fileType = new FilePickerFileType("KM2 Backup")
                {
                    Patterns = ["*.km2"]
                };

                // 保存ダイアログを表示
                var saveResult = await this.StorageProvider.SaveFilePickerAsync(
                    new FilePickerSaveOptions
                    {
                        Title = "エクスポート先を選択",
                        FileTypeChoices = [fileType],
                        SuggestedFileName = "backup.km2"
                    });

                if (saveResult != null)
                {
                    string? pwd = null;

                    // マスターパスワードが未設定の場合、バックアップを保護するためのパスワードを入力させる
                    if (!AdvancedEncryptionService.IsMasterSet)
                    {
                        var ip = new SimplePasswordWindow("Enter export password (will be used to wrap backup)");
                        pwd = await ip.ShowDialog<string?>(this);
                    }

                    // パスワード（必要なら）付きで全データをエクスポート
                    ExportImportService.ExportAll(saveResult.Path.LocalPath, pwd);
                }
            };

            // インポートボタンクリックイベント
            importBtn.Click += async (_, __) =>
            {
                var fileType = new FilePickerFileType("KM2 Backup")
                {
                    Patterns = ["*.km2"]
                };

                // インポート対象ファイルの選択ダイアログを表示
                var openResult = await this.StorageProvider.OpenFilePickerAsync(
                    new FilePickerOpenOptions
                    {
                        Title = "インポートするバックアップファイルを選択",
                        FileTypeFilter = [fileType],
                        AllowMultiple = false
                    });

                if (openResult != null && openResult.Count > 0)
                {
                    string? pwd = null;

                    // マスターパスワードが未設定の場合、バックアップをアンラップするためのパスワードを入力させる
                    if (!AdvancedEncryptionService.IsMasterSet)
                    {
                        var ip = new SimplePasswordWindow("Enter import password to unwrap backup");
                        pwd = await ip.ShowDialog<string?>(this);
                    }

                    // 指定ファイルから全データをインポート
                    ExportImportService.ImportAll(openResult[0].Path.LocalPath, pwd);

                    // インポート後に一覧を更新
                    Vm?.RefreshCommand.Execute(null);
                }
            };

            // タグクラウドの構築
            BuildTagCloud(tagPanel);
        }

        /// <summary>
        /// タグクラウド（タグの出現頻度に応じてフォントサイズを変えた一覧）を構築し、
        /// 指定された <see cref="WrapPanel"/> に配置します。
        /// </summary>
        /// <param name="panel">タグクラウドを表示する <see cref="WrapPanel"/>。</param>
        private void BuildTagCloud(WrapPanel panel)
        {
            // ViewModel が存在しない場合は何も行わない
            if (Vm == null)
            {
                return;
            }

            // 既存のタグ要素をクリア
            panel.Children.Clear();

            var vm = Vm;

            // 登録されているタグ候補を取得（頻度順）
            var candidates = vm.GetAllTagCandidates();

            if (candidates.Count == 0)
            {
                // タグが存在しない場合は何も表示しない
                return;
            }

            // タグの出現回数を数えるための辞書
            int max = 1;
            var svc = new CredentialService();
            var counts = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);

            // 全資格情報を取得（復号不要なので tryDecrypt: false）
            foreach (var c in svc.GetAll(false))
            {
                if (string.IsNullOrWhiteSpace(c.Tags))
                {
                    continue;
                }

                // カンマ区切りでタグを分解し、出現回数をカウント
                foreach (var t in c.Tags.Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries))
                {
                    counts[t] = counts.TryGetValue(t, out int value) ? value + 1 : 1;

                    // 最大出現回数を更新
                    if (counts[t] > max)
                    {
                        max = counts[t];
                    }
                }
            }

            // 出現回数の多い順にタグを並べて TextBlock を作成
            foreach (var kv in counts.OrderByDescending(kv => kv.Value))
            {
                // 出現頻度に応じてフォントサイズを調整（最低 12pt ベースにプラス）
                var size = 12 + (int)(12.0 * kv.Value / max);

                var tb = new TextBlock
                {
                    Text = kv.Key,
                    FontSize = size,
                    Margin = new Avalonia.Thickness(4)
                };

                // タグクリックで検索テキストに反映し、フィルタを適用
                tb.PointerPressed += (_, __) =>
                {
                    vm.SearchText = kv.Key;
                    vm.ApplyFilter();
                };

                // パネルに追加
                panel.Children.Add(tb);
            }
        }
    }
}
