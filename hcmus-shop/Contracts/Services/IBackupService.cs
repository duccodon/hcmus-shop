using hcmus_shop.Models.Common;
using System.Threading.Tasks;

namespace hcmus_shop.Contracts.Services
{
    public interface IBackupService
    {
        /// <summary>
        /// Downloads a SQL backup from the server to a user-chosen path.
        /// Returns the saved path or failure.
        /// </summary>
        Task<Result<string>> DownloadBackupAsync(string targetPath);

        /// <summary>
        /// Uploads a SQL file to the server's /restore endpoint.
        /// </summary>
        Task<Result<bool>> RestoreAsync(string sqlFilePath);
    }
}
