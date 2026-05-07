using hcmus_shop.Contracts.Services;
using hcmus_shop.Models.Common;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace hcmus_shop.Services.Backup
{
    public class BackupService : IBackupService
    {
        private readonly IGraphQLClientService _graphQL;
        private readonly IAuthService _auth;

        public BackupService(IGraphQLClientService graphQL, IAuthService auth)
        {
            _graphQL = graphQL;
            _auth = auth;
        }

        private string ServerBaseUrl()
        {
            // ServerUrl includes /graphql; strip it for REST endpoints.
            var url = _graphQL.ServerUrl;
            const string suffix = "/graphql";
            if (url.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return url.Substring(0, url.Length - suffix.Length);
            return url;
        }

        private HttpClient CreateAuthorizedClient(TimeSpan timeout)
        {
            var http = new HttpClient { Timeout = timeout };
            if (!string.IsNullOrEmpty(_auth.Token))
            {
                http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _auth.Token);
            }
            return http;
        }

        public async Task<Result<string>> DownloadBackupAsync(string targetPath)
        {
            try
            {
                using var http = CreateAuthorizedClient(TimeSpan.FromMinutes(2));
                using var resp = await http.GetAsync($"{ServerBaseUrl()}/backup");
                if (!resp.IsSuccessStatusCode)
                    return Result<string>.Failure($"Server responded with {(int)resp.StatusCode}");

                await using var fileStream = File.Create(targetPath);
                await resp.Content.CopyToAsync(fileStream);
                return Result<string>.Success(targetPath);
            }
            catch (Exception ex)
            {
                return Result<string>.Failure(ex.Message);
            }
        }

        public async Task<Result<bool>> RestoreAsync(string sqlFilePath)
        {
            try
            {
                using var http = CreateAuthorizedClient(TimeSpan.FromMinutes(5));
                using var content = new MultipartFormDataContent();
                await using var fileStream = File.OpenRead(sqlFilePath);
                content.Add(new StreamContent(fileStream), "file", Path.GetFileName(sqlFilePath));

                using var resp = await http.PostAsync($"{ServerBaseUrl()}/restore", content);
                if (!resp.IsSuccessStatusCode)
                {
                    var body = await resp.Content.ReadAsStringAsync();
                    return Result<bool>.Failure($"Restore failed: {body}");
                }
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(ex.Message);
            }
        }
    }
}
