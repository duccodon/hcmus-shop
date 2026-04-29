using hcmus_shop.ViewModels;
using hcmus_shop.ViewModels.Products;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace hcmus_shop.Views.Dashboard.Components.Tables
{
    public sealed partial class RecentInvoicesTable : UserControl, INotifyPropertyChanged
    {
        // ──────────────────────────────────────────────
        // Constants
        // ──────────────────────────────────────────────

        private const int DefaultPageSize = 5;
        private const string DefaultSortColumn = "No";

        private static readonly string[] SupportedDateFormats =
            ["dd/MM/yyyy HH:mm", "dd/MM/yyyy", "MM/dd/yyyy", "M/d/yyyy"];

        // Segoe Fluent Icons glyphs
        private const string GlyphSortBoth = "\uE8CB";
        private const string GlyphSortAsc = "\uE70E";
        private const string GlyphSortDesc = "\uE70D";

        // Pagination symbols
        private const string LabelPrev = "\u2190 Previous";
        private const string LabelNext = "Next \u2192";
        private const string LabelEllipsis = "\u2026";

        // ──────────────────────────────────────────────
        // Dependency Property
        // ──────────────────────────────────────────────

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ItemsSource),
                typeof(IEnumerable<RecentInvoiceItem>),
                typeof(RecentInvoicesTable),
                new PropertyMetadata(defaultValue: null, OnItemsSourceChanged));

        // ──────────────────────────────────────────────
        // Private State
        // ──────────────────────────────────────────────

        private readonly List<RecentInvoiceItem> _allItems = [];
        private readonly List<RecentInvoiceItem> _filteredSortedItems = [];

        private string _sortColumn = DefaultSortColumn;
        private bool _sortAscending = true; // default: ascending for No
        private int _currentPage = 1;
        private int _selectedPageSize = DefaultPageSize;

        // ──────────────────────────────────────────────
        // Public Observable Collections
        // ──────────────────────────────────────────────

        public ObservableCollection<RecentInvoiceItem> PagedItems { get; } = [];
        public ObservableCollection<PageButtonItem> PageButtons { get; } = [];
        public ObservableCollection<int> PageSizeOptions { get; } = [5, 10, 20, 50];
        public IRelayCommand<int> PageButtonClickCommand { get; }

        // ──────────────────────────────────────────────
        // Computed Properties
        // ──────────────────────────────────────────────

        public int PageSize => _selectedPageSize;
        public int SelectedPageSize
        {
            get => _selectedPageSize;
            set
            {
                if (_selectedPageSize == value)
                {
                    return;
                }

                _selectedPageSize = value;
                _currentPage = 1;
                RebuildPagedItems();
                NotifyPagingChanged();
                OnPropertyChanged(nameof(SelectedPageSize));
            }
        }
        public string NoSortGlyph => GetSortGlyphFor("No");
        public string CustomerSortGlyph => GetSortGlyphFor("Customer");
        public string ProductSortGlyph => GetSortGlyphFor("Product");
        public string DateSortGlyph => GetSortGlyphFor("Date");
        public string StatusSortGlyph => GetSortGlyphFor("Status");
        public string PriceSortGlyph => GetSortGlyphFor("Price");
        public bool CanGoPrevious => _currentPage > 1;
        public bool CanGoNext => _currentPage < TotalPages;
        public string PagingText => $"Page {_currentPage}/{TotalPages} • {_filteredSortedItems.Count} items";
        public string FooterResultText
        {
            get
            {
                if (_filteredSortedItems.Count == 0)
                {
                    return "Result 0 of 0";
                }

                var start = ((_currentPage - 1) * PageSize) + 1;
                var end = Math.Min(_currentPage * PageSize, _filteredSortedItems.Count);
                return $"Result {start}-{end} of {_filteredSortedItems.Count}";
            }
        }

        private int TotalPages =>
            Math.Max(1, (int)Math.Ceiling((double)_filteredSortedItems.Count / PageSize));

        // ──────────────────────────────────────────────
        // Status filter state
        // ──────────────────────────────────────────────
        private string _activeStatusFilter = string.Empty;

        // ──────────────────────────────────────────────
        // Events
        // ──────────────────────────────────────────────

        public event PropertyChangedEventHandler? PropertyChanged;

        // ──────────────────────────────────────────────
        // Constructor
        // ──────────────────────────────────────────────

        public RecentInvoicesTable()
        {
            InitializeComponent();
            PageButtonClickCommand = new RelayCommand<int>(OnPageButtonCommandExecute);
        }

        // ──────────────────────────────────────────────
        // Dependency Property Accessor
        // ──────────────────────────────────────────────

        public IEnumerable<RecentInvoiceItem> ItemsSource
        {
            get => (IEnumerable<RecentInvoiceItem>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        // ──────────────────────────────────────────────
        // Dependency Property Callback
        // ──────────────────────────────────────────────

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RecentInvoicesTable table)
            {
                table.RebuildAllItems();
            }
        }

        // ──────────────────────────────────────────────
        // Event Handlers – Search
        // ──────────────────────────────────────────────

        private void SearchFieldCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SearchFieldCombo.SelectedItem is not ComboBoxItem selectedItem)
                return;

            // Guard: controls may not be initialised yet during startup
            if (SearchTextBox is null || DateSearchPanel is null)
                return;

            var isDateField = string.Equals(
                selectedItem.Tag?.ToString(), "Date", StringComparison.OrdinalIgnoreCase);

            SearchTextBox.Visibility = isDateField ? Visibility.Collapsed : Visibility.Visible;
            DateSearchPanel.Visibility = isDateField ? Visibility.Visible : Visibility.Collapsed;

            ApplyFilterAndSort(resetPage: true);
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilterAndSort(resetPage: true);
        }

        private void SearchDatePicker_DateChanged(
            CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            ApplyFilterAndSort(resetPage: true);
        }

        // ──────────────────────────────────────────────
        // Event Handlers – Status Flyout
        // ──────────────────────────────────────────────

        private void StatusHeader_Click(object sender, RoutedEventArgs e)
        {
            // Intentionally no sorting on Status header click.
            // This header is used only to open the status filter flyout.
        }

        private void StatusFilter_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuFlyoutItem item)
                return;

            _activeStatusFilter = item.Tag?.ToString() ?? string.Empty;

            // Update the header label to reflect current filter
            var label = string.IsNullOrEmpty(_activeStatusFilter)
                ? "Status: All"
                : $"Status: {_activeStatusFilter}";
            StatusHeaderLabel.Text = label;

            ApplyFilterAndSort(resetPage: true);
        }

        // ──────────────────────────────────────────────
        // Event Handlers – Sort
        // ──────────────────────────────────────────────

        private void SortHeader_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: string tag })
                return;

            if (string.Equals(_sortColumn, tag, StringComparison.Ordinal))
            {
                _sortAscending = !_sortAscending;
            }
            else
            {
                _sortColumn = tag;
                _sortAscending = !string.Equals(tag, "Date", StringComparison.OrdinalIgnoreCase);
            }

            NotifySortGlyphsChanged();
            ApplyFilterAndSort(resetPage: true);
        }

        // ──────────────────────────────────────────────
        // Event Handlers – Pagination
        // ──────────────────────────────────────────────

        private void PageButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: int targetPage })
                return;

            if (targetPage < 1 || targetPage > TotalPages || targetPage == _currentPage)
                return;

            _currentPage = targetPage;
            RebuildPagedItems();
            NotifyPagingChanged();
        }

        private void OnPageButtonCommandExecute(int targetPage)
        {
            if (targetPage < 1 || targetPage > TotalPages || targetPage == _currentPage)
                return;

            _currentPage = targetPage;
            RebuildPagedItems();
            NotifyPagingChanged();
        }

        // ──────────────────────────────────────────────
        // Core Data Pipeline
        // ──────────────────────────────────────────────

        private void RebuildAllItems()
        {
            _allItems.Clear();

            if (ItemsSource is not null)
            {
                _allItems.AddRange(ItemsSource);
            }

            ApplyFilterAndSort(resetPage: true);
        }

        private void ApplyFilterAndSort(bool resetPage)
        {
            if (SearchFieldCombo is null || SearchTextBox is null)
                return;

            IEnumerable<RecentInvoiceItem> query = _allItems;

            // ── Text / Date filter ──────────────────
            var selectedTag = (SearchFieldCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString()
                              ?? "Customer";

            if (string.Equals(selectedTag, "Date", StringComparison.OrdinalIgnoreCase))
            {
                query = ApplyDateRangeFilter(query);
            }
            else
            {
                query = ApplyKeywordFilter(query, selectedTag);
            }

            // ── Status header filter ────────────────
            if (!string.IsNullOrWhiteSpace(_activeStatusFilter))
            {
                query = query.Where(item =>
                    string.Equals(item.Status, _activeStatusFilter, StringComparison.OrdinalIgnoreCase));
            }

            // ── Sort ────────────────────────────────
            query = _sortColumn switch
            {
                "No" => ApplySort(query, x => x.No),
                "Customer" => ApplySort(query, x => x.Customer),
                "Product" => ApplySort(query, x => x.Product),
                "Status" => ApplySort(query, x => x.Status),
                "Price" => ApplySort(query, x => ParsePrice(x.Price)),
                _ => ApplySort(query, x => ParseDateOrMin(x.Date))
            };

            _filteredSortedItems.Clear();
            _filteredSortedItems.AddRange(query);

            if (resetPage)
                _currentPage = 1;

            RebuildPagedItems();
            NotifyPagingChanged();
        }

        private IEnumerable<RecentInvoiceItem> ApplyDateRangeFilter(IEnumerable<RecentInvoiceItem> source)
        {
            var from = SearchFromDatePicker.Date?.Date;
            var to = SearchToDatePicker.Date?.Date;

            if (from is null && to is null)
                return source;

            var fromDate = from ?? DateTimeOffset.MinValue.Date;
            var toDate = to ?? DateTimeOffset.MaxValue.Date;

            if (fromDate > toDate)
                (fromDate, toDate) = (toDate, fromDate);

            return source.Where(item =>
                TryParseDate(item.Date, out var parsed) &&
                parsed.Date >= fromDate &&
                parsed.Date <= toDate);
        }

        private IEnumerable<RecentInvoiceItem> ApplyKeywordFilter(
            IEnumerable<RecentInvoiceItem> source, string fieldTag)
        {
            var keyword = SearchTextBox.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(keyword))
                return source;

            return source.Where(item =>
                (GetFieldValue(item, fieldTag) ?? string.Empty)
                    .Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        private void RebuildPagedItems()
        {
            // Clamp page to valid range
            if (_currentPage > TotalPages)
                _currentPage = TotalPages;

            var pageItems = _filteredSortedItems
                .Skip((_currentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            PagedItems.Clear();
            foreach (var item in pageItems)
                PagedItems.Add(item);

            RebuildPageButtons();
        }

        // ──────────────────────────────────────────────
        // Pagination Button Builder
        // ──────────────────────────────────────────────

        private void RebuildPageButtons()
        {
            PageButtons.Clear();

            var totalPages = TotalPages;
            var currentPage = _currentPage;

            PageButtonItem NavBtn(string label, int target, bool enabled) => new()
            {
                Label = label,
                PageNumber = target,
                IsEnabled = enabled,
                IsCurrent = false,
            };

            PageButtonItem PageBtn(int page) => new()
            {
                Label = page.ToString(),
                PageNumber = page,
                IsEnabled = page != currentPage,
                IsCurrent = page == currentPage,
            };

            PageButtonItem Ellipsis() => new()
            {
                Label = LabelEllipsis,
                PageNumber = -1,
                IsEnabled = false,
                IsCurrent = false,
            };

            PageButtons.Add(NavBtn(LabelPrev, currentPage - 1, currentPage > 1));

            // Page number logic: always show first, last, current ±1 window
            var pagesToShow = BuildPageNumberSequence(currentPage, totalPages);

            int? lastAdded = null;
            foreach (var page in pagesToShow)
            {
                if (lastAdded.HasValue && page - lastAdded.Value > 1)
                    PageButtons.Add(Ellipsis());

                PageButtons.Add(PageBtn(page));
                lastAdded = page;
            }

            PageButtons.Add(NavBtn(LabelNext, currentPage + 1, currentPage < totalPages));
        }

        /// <summary>
        /// Returns a sorted, distinct set of page numbers to display:
        /// always includes page 1 and totalPages, plus a window around currentPage.
        /// </summary>
        private static IEnumerable<int> BuildPageNumberSequence(int currentPage, int totalPages)
        {
            var pages = new SortedSet<int> { 1, totalPages };

            for (var offset = -1; offset <= 1; offset++)
            {
                var p = currentPage + offset;
                if (p >= 1 && p <= totalPages)
                    pages.Add(p);
            }

            return pages;
        }

        // ──────────────────────────────────────────────
        // Sorting Helpers
        // ──────────────────────────────────────────────

        private IEnumerable<RecentInvoiceItem> ApplySort<TKey>(
            IEnumerable<RecentInvoiceItem> source,
            Func<RecentInvoiceItem, TKey> keySelector)
        {
            return _sortAscending
                ? source.OrderBy(keySelector)
                : source.OrderByDescending(keySelector);
        }

        // ──────────────────────────────────────────────
        // Field / Parse Helpers
        // ──────────────────────────────────────────────

        private static string? GetFieldValue(RecentInvoiceItem item, string tag) =>
            tag switch
            {
                "No" => item.No,
                "Customer" => item.Customer,
                "Product" => item.Product,
                "Date" => item.Date,
                "Status" => item.Status,
                "Price" => item.Price,
                _ => item.Customer,
            };

        private static bool TryParseDate(string raw, out DateTime result)
        {
            return DateTime.TryParseExact(
                       raw, SupportedDateFormats,
                       CultureInfo.InvariantCulture, DateTimeStyles.None, out result)
                || DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
        }

        private static DateTime ParseDateOrMin(string raw) =>
            TryParseDate(raw, out var parsed) ? parsed : DateTime.MinValue;

        private static decimal ParsePrice(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return 0m;

            var cleaned = raw
                .Replace("$", string.Empty, StringComparison.Ordinal)
                .Replace(",", string.Empty, StringComparison.Ordinal)
                .Trim();

            return decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
                ? value
                : 0m;
        }

        // ──────────────────────────────────────────────
        // Property Change Notifications
        // ──────────────────────────────────────────────

        private void NotifySortGlyphsChanged()
        {
            OnPropertyChanged(nameof(NoSortGlyph));
            OnPropertyChanged(nameof(CustomerSortGlyph));
            OnPropertyChanged(nameof(ProductSortGlyph));
            OnPropertyChanged(nameof(DateSortGlyph));
            OnPropertyChanged(nameof(StatusSortGlyph));
            OnPropertyChanged(nameof(PriceSortGlyph));
        }

        private void NotifyPagingChanged()
        {
            OnPropertyChanged(nameof(CanGoPrevious));
            OnPropertyChanged(nameof(CanGoNext));
            OnPropertyChanged(nameof(PagingText));
            OnPropertyChanged(nameof(FooterResultText));
        }

        private string GetSortGlyphFor(string column)
        {
            if (!string.Equals(_sortColumn, column, StringComparison.Ordinal))
                return GlyphSortBoth;

            return _sortAscending ? GlyphSortAsc : GlyphSortDesc;
        }

        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}