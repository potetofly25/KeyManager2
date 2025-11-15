using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using potetofly25.KeyManager2.Models;
using potetofly25.KeyManager2.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private readonly CredentialService credentialService;

        /// <summary>
        /// 画面に表示する資格情報の一覧です。
        /// 検索やカテゴリフィルタによって内容が変化します。
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<Credential> credentials;

        /// <summary>
        /// 利用可能なカテゴリの一覧です。
        /// 既存の資格情報から一意なカテゴリを抽出して構築されます。
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<string> categories;

        /// <summary>
        /// 検索ボックスに入力された検索文字列です。
        /// LoginId、Description、Tags に対して部分一致検索を行います。
        /// </summary>
        [ObservableProperty]
        private string? searchText = string.Empty;

        /// <summary>
        /// 選択中のカテゴリ名です。
        /// このカテゴリに一致する資格情報のみを表示します。
        /// </summary>
        [ObservableProperty]
        private string? selectedCategory;

        /// <summary>
        /// 一覧の元データとして保持している全資格情報のリストです。
        /// フィルタの適用前データとして使用します。
        /// </summary>
        private List<Credential> allCredentials;

        /// <summary>
        /// <see cref="MainWindowViewModel"/> の新しいインスタンスを初期化します。
        /// コンストラクタ内で資格情報の読み込みとカテゴリ一覧の構築を行います。
        /// </summary>
        /// <param name="credentialService">
        /// 資格情報の取得・更新を行うための <see cref="CredentialService"/> インスタンスです。
        /// </param>
        public MainWindowViewModel(CredentialService credentialService)
        {
            // サービスの参照を保持
            this.credentialService = credentialService;

            // コレクションの初期化
            this.credentials = [];
            this.categories = [];
            this.allCredentials = [];

            // ★起動時に一度一覧を読み込む
            this.Refresh();
        }

        /// <summary>
        /// DB から資格情報を再取得し、カテゴリ一覧とフィルタ済み一覧を更新します。
        /// </summary>
        [RelayCommand]
        private void Refresh()
        {
            // ★DB から全資格情報を取得（復号した状態で欲しければ true / false を必要に応じて変更）
            this.allCredentials = this.credentialService.GetAll(tryDecrypt: true).ToList();

            // フィルタを適用して画面表示用コレクションを更新
            this.ApplyFilter();

            // カテゴリ一覧も更新
            this.UpdateCategories();
        }

        /// <summary>
        /// 現在の検索テキストおよびカテゴリを使って一覧をフィルタリングし、
        /// <see cref="Credentials"/> コレクションを更新します。
        /// </summary>
        [RelayCommand]
        public void ApplyFilter()
        {
            // 元データからクエリを開始
            IEnumerable<Credential> query = this.allCredentials;

            // カテゴリでフィルタ（SelectedCategory が空でない場合のみ）
            if (!string.IsNullOrWhiteSpace(this.SelectedCategory))
            {
                query = query.Where(c => string.Equals(c.Category, this.SelectedCategory, System.StringComparison.OrdinalIgnoreCase));
            }

            // 検索テキストでフィルタ
            if (!string.IsNullOrWhiteSpace(this.SearchText))
            {
                string keyword = this.SearchText.Trim();

                query = query.Where(c =>
                    (!string.IsNullOrEmpty(c.LoginId) && c.LoginId.Contains(keyword, System.StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(c.Description) && c.Description.Contains(keyword, System.StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(c.Tags) && c.Tags.Contains(keyword, System.StringComparison.OrdinalIgnoreCase)));
            }

            // 画面表示用コレクションを更新
            this.Credentials.Clear();
            foreach (Credential item in query)
            {
                this.Credentials.Add(item);
            }
        }

        /// <summary>
        /// 登録済み資格情報からカテゴリ一覧を作成し、<see cref="Categories"/> を更新します。
        /// </summary>
        private void UpdateCategories()
        {
            // 重複を除いたカテゴリ一覧を生成
            IEnumerable<string> categoryList = this.allCredentials
                .Where(c => !string.IsNullOrWhiteSpace(c.Category))
                .Select(c => c.Category!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c);

            this.Categories.Clear();
            foreach (string category in categoryList)
            {
                this.Categories.Add(category);
            }
        }

        /// <summary>
        /// 登録されている全てのタグ候補を返します。
        /// タグクラウド構築用に使用されます。
        /// </summary>
        /// <returns>タグ文字列のリストです。</returns>
        public List<string> GetAllTagCandidates()
        {
            List<string> result = [];

            foreach (Credential credential in this.allCredentials)
            {
                if (string.IsNullOrWhiteSpace(credential.Tags))
                {
                    continue;
                }

                string[] tokens = credential.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (string token in tokens)
                {
                    if (!result.Contains(token, StringComparer.OrdinalIgnoreCase))
                    {
                        result.Add(token);
                    }
                }
            }

            return result;
        }

    }
}
