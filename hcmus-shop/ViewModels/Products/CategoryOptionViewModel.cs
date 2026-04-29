using CommunityToolkit.Mvvm.ComponentModel;

namespace hcmus_shop.ViewModels.Products
{
    public partial class CategoryOptionViewModel : ObservableObject
    {
        public CategoryOptionViewModel(int categoryId, string name)
        {
            CategoryId = categoryId;
            Name = name;
        }

        public int CategoryId { get; }

        public string Name { get; }

        [ObservableProperty]
        private bool _isSelected;
    }
}
