using CommunityToolkit.Mvvm.ComponentModel;

namespace hcmus_shop.ViewModels.Products
{
    public partial class ProductSortCriterionViewModel : ObservableObject
    {
        public ProductSortCriterionViewModel(string field, string direction)
        {
            _field = field;
            _direction = direction;
        }

        [ObservableProperty]
        private string _field;

        [ObservableProperty]
        private string _direction;
    }
}
