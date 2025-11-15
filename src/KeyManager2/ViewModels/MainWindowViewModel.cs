using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using potetofly25.KeyManager2.Models;
using potetofly25.KeyManager2.Services;
using System.Collections.Generic;
using System.Linq;

namespace potetofly25.KeyManager2.ViewModels
{
    /// <summary>
    /// アプリケーションのメインウィンドウに対応する ViewModel クラスです。
    /// 資格情報一覧の取得、検索、カテゴリフィルタリング、タグ候補取得などのプレゼンテーションロジックを担当します。
    /// </summary>
    public partial class MainWindowViewModel : ObservableObject
    {
        /// <summary>
        /// 資格情報データにアクセスするためのサービスです。
        /// DB からの取得、追加、更新、削除をカプセル化しています。
        /// </summary>
        private readonly CredentialService _svc = new();

        /// <summary>
        /// 画面に表示する資格情報の一覧です。
        /// 検索やカテゴリフィルタによって内容が変化します。
        /// </summary>
        [ObservableProperty]
        private List<Credential> credentials = [];

        /// <summary>
        /// 検索ボックスに入力された検索文字列です。
        /// LoginId、Description、Tags に対して部分一致検索を行います。
        /// </summary>
        [ObservableProperty]
        private string searchText = string.Empty;

        /// <summary>
        /// 選択中のカテゴリ名です。
        /// このカテゴリに一致する資格情報のみを表示します。
        /// </summary>
        [ObservableProperty]
        private string? selectedCategory;

        /// <summary>
        /// 利用可能なカテゴリの一覧です。
        /// 既存の資格情報から一意なカテゴリを抽出して構築されます。
        /// </summary>
        [ObservableProperty]
        private List<string> categories = [];

        /// <summary>
        /// <see cref="MainWindowViewModel"/> の新しいインスタンスを初期化します。
        /// コンストラクタ内で資格情報の読み込みとカテゴリ一覧の構築を行います。
        /// </summary>
        public MainWindowViewModel()
        {
            // 初期ロードを実行
            Load();
        }

        /// <summary>
        /// 資格情報をサービスから全件取得し、画面用プロパティへ設定します。
        /// 取得時には、マスターパスワードが設定されている場合に限り復号を試行します。
        /// 併せてカテゴリ一覧も再構築します。
        /// </summary>
        public void Load()
        {
            // 現在のマスターパスワード状態に応じて復号を試みつつ、全件取得
            Credentials = _svc.GetAll(tryDecrypt: AdvancedEncryptionService.IsMasterSet);

            // 資格情報からカテゴリ一覧を再構築
            BuildCategories();
        }

        /// <summary>
        /// 現在の資格情報一覧からカテゴリ一覧を構築します。
        /// カテゴリが null または空の場合は "Uncategorized" として扱います。
        /// </summary>
        private void BuildCategories()
        {
            // カテゴリを取り出し、null の場合は "Uncategorized" として扱った上で一意にソート
            var cats = Credentials
                .Select(c => c.Category ?? "Uncategorized")
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            Categories = cats;
        }

        /// <summary>
        /// <see cref="SearchText"/> プロパティ変更時に呼び出される部分メソッドです。
        /// 検索文字列が変更されるたびにフィルタ処理を再適用します。
        /// </summary>
        /// <param name="value">新しい検索文字列。</param>
        partial void OnSearchTextChanged(string value)
        {
            // 検索文字列が変更された場合はフィルタを再適用
            ApplyFilter();
        }

        /// <summary>
        /// <see cref="SelectedCategory"/> プロパティ変更時に呼び出される部分メソッドです。
        /// 選択カテゴリが変更されるたびにフィルタ処理を再適用します。
        /// </summary>
        /// <param name="value">新しく選択されたカテゴリ名。</param>
        partial void OnSelectedCategoryChanged(string? value)
        {
            // 選択カテゴリが変更された場合はフィルタを再適用
            ApplyFilter();
        }

        /// <summary>
        /// 資格情報一覧を再読み込みするコマンドです。
        /// DB から再取得し、カテゴリ一覧も更新されます。
        /// </summary>
        [RelayCommand]
        private void Refresh()
        {
            // 最新の状態で再読み込み
            Load();
        }

        /// <summary>
        /// 現在の検索条件（<see cref="SearchText"/> と <see cref="SelectedCategory"/>）に基づき、
        /// 表示用の <see cref="Credentials"/> をフィルタリングします。
        /// </summary>
        public void ApplyFilter()
        {
            // 毎回 DB から新たに取得（復号の可否は現在のマスター状態による）
            var q = _svc.GetAll(tryDecrypt: AdvancedEncryptionService.IsMasterSet).AsEnumerable();

            // カテゴリが指定されている場合、そのカテゴリに一致するものだけに絞り込み
            if (!string.IsNullOrWhiteSpace(SelectedCategory))
            {
                q = q.Where(c => (c.Category ?? "Uncategorized") == SelectedCategory);
            }

            // 検索文字列が指定されている場合、LoginId / Description / Tags に対して部分一致検索
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var s = SearchText.ToLowerInvariant();

                q = q.Where(c =>
                    (c.LoginId ?? string.Empty).Contains(s, System.StringComparison.InvariantCultureIgnoreCase)
                    || (c.Description ?? string.Empty).Contains(s, System.StringComparison.InvariantCultureIgnoreCase)
                    || (c.Tags ?? string.Empty).Contains(s, System.StringComparison.InvariantCultureIgnoreCase));
            }

            // 結果を List にして Credentials を更新（C# 12 のコレクション式を使用）
            Credentials = [.. q];
        }

        /// <summary>
        /// 登録されているすべての <see cref="Credential.Tags"/> からタグ候補一覧を取得します。
        /// タグの使用頻度が高い順にソートされた候補リストを返します。
        /// </summary>
        /// <returns>頻度降順に並んだタグ文字列の一覧。</returns>
        public List<string> GetAllTagCandidates()
        {
            // パスワード復号は不要なので tryDecrypt: false で全件取得
            var all = _svc.GetAll(tryDecrypt: false);

            // タグとその出現回数を保持するマップ（大文字小文字を区別しない）
            var map = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);

            foreach (var c in all)
            {
                // Tags が空または null の場合はスキップ
                if (string.IsNullOrWhiteSpace(c.Tags))
                {
                    continue;
                }

                // カンマ区切りでタグを分割・トリムして集計
                foreach (var t in c.Tags.Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries))
                {
                    if (map.TryGetValue(t, out int value))
                    {
                        map[t] = ++value;
                    }
                    else
                    {
                        map[t] = 1;
                    }
                }
            }

            // 出現回数の多い順にタグ名だけを取り出してリスト化
            return [.. map.OrderByDescending(kv => kv.Value).Select(kv => kv.Key)];
        }
    }
}
