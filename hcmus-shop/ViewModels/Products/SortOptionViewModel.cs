namespace hcmus_shop.ViewModels.Products
{
    public class SortOptionViewModel
    {
        public string Value { get; }
        public string Name { get; }

        public SortOptionViewModel(string value, string name)
        {
            Value = value;
            Name = name;
        }
    }
}
