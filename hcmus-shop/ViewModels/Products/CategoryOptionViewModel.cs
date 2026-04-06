using CommunityToolkit.Mvvm.ComponentModel;

namespace hcmus_shop.ViewModels.Products
{
    public class CategoryOptionViewModel : ObservableObject
    {
        private bool _isSelected;

        public CategoryOptionViewModel(int categoryId, string name)
        {
            CategoryId = categoryId;
            Name = name;
        }

        public int CategoryId { get; }

        public string Name { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }
}
