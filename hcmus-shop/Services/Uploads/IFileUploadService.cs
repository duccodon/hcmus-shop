using System.Threading.Tasks;
using Windows.Storage;

namespace hcmus_shop.Services.Uploads
{
    public interface IFileUploadService
    {
        Task<string> UploadImageAsync(StorageFile file);
    }
}
