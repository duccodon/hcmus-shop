using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;

namespace hcmus_shop.ViewModels
{
    public class ImagePreview
    {
        public BitmapImage? Bitmap { get; set; }
        public StorageFile? File { get; set; }
        public int DisplayOrder { get; set; }
    }
}