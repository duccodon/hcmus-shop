using CommunityToolkit.Mvvm.ComponentModel;

namespace hcmus_shop.ViewModels.Products
{
    public partial class AdvancedFilterOptionViewModel : ObservableObject
    {
        public AdvancedFilterOptionViewModel(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public int Id { get; }

        public string Name { get; }

        [ObservableProperty]
        private bool _isSelected;
    }
}
