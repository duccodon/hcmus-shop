namespace hcmus_shop.ViewModels.Products
{
    public class FilterOptionViewModel
    {
        public int? Id { get; }
        public string Name { get; }

        public FilterOptionViewModel(int? id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
