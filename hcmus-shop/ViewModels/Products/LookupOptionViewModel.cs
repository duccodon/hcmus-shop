namespace hcmus_shop.ViewModels.Products
{
    public class LookupOptionViewModel
    {
        public LookupOptionViewModel(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public int Id { get; }

        public string Name { get; }
    }
}
