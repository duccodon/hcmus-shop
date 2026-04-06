using CommunityToolkit.Mvvm.ComponentModel;

namespace hcmus_shop.ViewModels.Products
{
    public class CategoryOptionViewModel : ObservableObject
    {
        private bool _isSelected;

        public CategoryOptionViewModel(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }
}
