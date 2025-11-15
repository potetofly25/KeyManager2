using Avalonia.Controls;
using Avalonia.Platform.Storage;
using potetofly25.KeyManager2.Models;
using potetofly25.KeyManager2.Services;
using potetofly25.KeyManager2.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace potetofly25.KeyManager2.Views
{
    public partial class MainWindow : Window
    {
        private MainWindowViewModel? Vm => DataContext as MainWindowViewModel;
        private readonly CredentialService _svc = new();

        public MainWindow()
        {
            InitializeComponent();

            var dg = this.FindControl<DataGrid>("DataGrid")!;
            var newBtn = this.FindControl<Button>("NewBtn")!;
            var editBtn = this.FindControl<Button>("EditBtn")!;
            var deleteBtn = this.FindControl<Button>("DeleteBtn")!;
            var copyBtn = this.FindControl<Button>("CopyBtn")!;
            var setMasterBtn = this.FindControl<Button>("SetMasterBtn")!;
            var exportBtn = this.FindControl<Button>("ExportBtn")!;
            var importBtn = this.FindControl<Button>("ImportBtn")!;
            var tagPanel = this.FindControl<WrapPanel>("TagCloudPanel")!;

            newBtn.Click += async (_, __) =>
            {
                var cred = new Credential { LoginId = $"user{System.DateTime.Now.Ticks}" };
                var win = new EditCredentialWindow(cred);
                var res = await win.ShowDialog<bool?>(this);
                if (res == true)
                {
                    var encrypt = AdvancedEncryptionService.IsMasterSet;
                    _svc.Add(win.ViewModel.Credential, encryptPassword: encrypt);
                    Vm?.RefreshCommand.Execute(null);
                }
            };

            editBtn.Click += async (_, __) =>
            {
                if (dg.SelectedItem is not Credential sel) return;
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
                if (copy.IsEncrypted && AdvancedEncryptionService.IsMasterSet)
                {
                    try { copy.Password = AdvancedEncryptionService.DecryptString(copy.Password); } catch { }
                }
                var win = new EditCredentialWindow(copy);
                var res = await win.ShowDialog<bool?>(this);
                if (res == true)
                {
                    var encrypt = AdvancedEncryptionService.IsMasterSet;
                    _svc.Update(win.ViewModel.Credential, encryptPassword: encrypt);
                    Vm?.RefreshCommand.Execute(null);
                }
            };

            deleteBtn.Click += (_, __) =>
            {
                if (dg.SelectedItem is Credential sel)
                {
                    _svc.Delete(sel);
                    Vm?.RefreshCommand.Execute(null);
                }
            };

            copyBtn.Click += async (_, __) =>
            {
                if (dg.SelectedItem is Credential sel)
                {
                    var pwd = sel.Password;
                    if (sel.IsEncrypted && AdvancedEncryptionService.IsMasterSet)
                    {
                        try { pwd = AdvancedEncryptionService.DecryptString(pwd); } catch { }
                    }
                    // Clipboardの取得方法を修正
                    var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
                    if (clipboard != null)
                    {
                        await clipboard.SetTextAsync(pwd);
                    }
                }
            };

            setMasterBtn.Click += async (_, __) =>
            {
                var dlg = new MasterPasswordWindow();
                var pwd = await dlg.ShowDialog<string?>(this);
                if (!string.IsNullOrEmpty(pwd))
                {
                    // If no root stored, initialize; else set
                    try
                    {
                        AdvancedEncryptionService.SetMasterPassword(pwd);
                    }
                    catch
                    {
                        // try initialize
                        AdvancedEncryptionService.InitializeMasterPassword(pwd);
                    }
                    Vm?.RefreshCommand.Execute(null);
                }
            };

            exportBtn.Click += async (_, __) =>
            {
                var fileType = new FilePickerFileType("KM2 Backup")
                {
                    Patterns = new[] { "*.km2" }
                };
                var saveResult = await this.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "エクスポート先を選択",
                    FileTypeChoices = new List<FilePickerFileType> { fileType },
                    SuggestedFileName = "backup.km2"
                });
                if (saveResult != null)
                {
                    string? pwd = null;
                    if (!AdvancedEncryptionService.IsMasterSet)
                    {
                        var ip = new SimplePasswordWindow("Enter export password (will be used to wrap backup)");
                        pwd = await ip.ShowDialog<string?>(this);
                    }
                    ExportImportService.ExportAll(saveResult.Path.LocalPath, pwd);
                }
            };

            importBtn.Click += async (_, __) =>
            {
                var fileType = new FilePickerFileType("KM2 Backup")
                {
                    Patterns = new[] { "*.km2" }
                };
                var openResult = await this.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "インポートするバックアップファイルを選択",
                    FileTypeFilter = new List<FilePickerFileType> { fileType },
                    AllowMultiple = false
                });
                if (openResult != null && openResult.Count > 0)
                {
                    string? pwd = null;
                    if (!AdvancedEncryptionService.IsMasterSet)
                    {
                        var ip = new SimplePasswordWindow("Enter import password to unwrap backup");
                        pwd = await ip.ShowDialog<string?>(this);
                    }
                    ExportImportService.ImportAll(openResult[0].Path.LocalPath, pwd);
                    Vm?.RefreshCommand.Execute(null);
                }
            };

            // Build tag cloud
            BuildTagCloud(tagPanel);
        }

        private void BuildTagCloud(WrapPanel panel)
        {
            if (Vm == null) return;

            panel.Children.Clear();
            var vm = Vm;
            var candidates = vm.GetAllTagCandidates();
            if (candidates.Count == 0) return;
            int max = 1;
            var svc = new CredentialService();
            var counts = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var c in svc.GetAll(false))
            {
                if (string.IsNullOrWhiteSpace(c.Tags)) continue;
                foreach (var t in c.Tags.Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries))
                {
                    counts[t] = counts.TryGetValue(t, out int value) ? value + 1 : 1;
                    if (counts[t] > max) max = counts[t];
                }
            }
            foreach (var kv in counts.OrderByDescending(kv => kv.Value))
            {
                var size = 12 + (int)(12.0 * kv.Value / max);
                var tb = new TextBlock { Text = kv.Key, FontSize = size, Margin = new Avalonia.Thickness(4) };
                tb.PointerPressed += (_, __) =>
                {
                    vm.SearchText = kv.Key;
                    vm.ApplyFilter();
                };
                panel.Children.Add(tb);
            }
        }
    }
}
