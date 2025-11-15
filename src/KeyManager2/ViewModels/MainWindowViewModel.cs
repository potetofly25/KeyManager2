using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using potetofly25.KeyManager2.Models;
using potetofly25.KeyManager2.Services;
using System.Collections.Generic;
using System.Linq;

namespace potetofly25.KeyManager2.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly CredentialService _svc = new();

        [ObservableProperty]
        private List<Credential> credentials = [];

        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        private string? selectedCategory;

        [ObservableProperty]
        private List<string> categories = [];

        public MainWindowViewModel()
        {
            Load();
        }

        public void Load()
        {
            Credentials = _svc.GetAll(tryDecrypt: AdvancedEncryptionService.IsMasterSet);
            BuildCategories();
        }

        private void BuildCategories()
        {
            var cats = Credentials.Select(c => c.Category ?? "Uncategorized").Distinct().OrderBy(x => x).ToList();
            Categories = cats;
        }

        partial void OnSearchTextChanged(string value)
        {
            ApplyFilter();
        }

        partial void OnSelectedCategoryChanged(string? value)
        {
            ApplyFilter();
        }

        [RelayCommand]
        private void Refresh()
        {
            Load();
        }

        public void ApplyFilter()
        {
            var q = _svc.GetAll(tryDecrypt: AdvancedEncryptionService.IsMasterSet).AsEnumerable();
            if (!string.IsNullOrWhiteSpace(SelectedCategory))
            {
                q = q.Where(c => (c.Category ?? "Uncategorized") == SelectedCategory);
            }
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var s = SearchText.ToLowerInvariant();
                q = q.Where(c => (c.LoginId ?? string.Empty).ToLowerInvariant().Contains(s)
                    || (c.Description ?? string.Empty).Contains(s, System.StringComparison.InvariantCultureIgnoreCase)
                    || (c.Tags ?? string.Empty).Contains(s, System.StringComparison.InvariantCultureIgnoreCase));
            }
            Credentials = [.. q];
        }

        public List<string> GetAllTagCandidates()
        {
            var all = _svc.GetAll(tryDecrypt: false);
            var map = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var c in all)
            {
                if (string.IsNullOrWhiteSpace(c.Tags)) continue;
                foreach (var t in c.Tags.Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries))
                {
                    if (map.TryGetValue(t, out int value)) map[t] = ++value; else map[t] = 1;
                }
            }
            return [.. map.OrderByDescending(kv => kv.Value).Select(kv => kv.Key)];
        }
    }
}
