using hcmus_shop.Services.GraphQL;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage;
using Windows.Storage.Streams;

namespace hcmus_shop.Services.Uploads
{
    public class FileUploadService : IFileUploadService
    {
        private readonly IGraphQLClientService _graphQLClientService;
        private readonly HttpClient _httpClient;

        public FileUploadService(IGraphQLClientService graphQLClientService)
        {
            _graphQLClientService = graphQLClientService;
            _httpClient = new HttpClient();
        }

        public async Task<string> UploadImageAsync(StorageFile file)
        {
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            var uploadUrl = BuildUploadEndpoint(_graphQLClientService.ServerUrl);

            using IRandomAccessStream stream = await file.OpenReadAsync();
            using var inputStream = stream.AsStreamForRead();
            using var content = new MultipartFormDataContent();
            using var fileContent = new StreamContent(inputStream);

            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(GetMimeType(file.FileType));
            content.Add(fileContent, "file", file.Name);

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.PostAsync(uploadUrl, content);
            }
            catch (Exception ex)
            {
                LogUploadError(uploadUrl, "HTTP upload failed", ex.Message);
                throw new Exception($"Cannot upload image to {uploadUrl}: {ex.Message}", ex);
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                LogUploadError(uploadUrl, $"Upload failed with status {(int)response.StatusCode}", responseBody);
                var serverMessage = ExtractMessage(responseBody);
                throw new Exception(string.IsNullOrWhiteSpace(serverMessage)
                    ? $"Upload failed with status {(int)response.StatusCode}."
                    : serverMessage);
            }

            var payload = JsonSerializer.Deserialize<UploadResponse>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (payload is null || string.IsNullOrWhiteSpace(payload.Url))
            {
                LogUploadError(uploadUrl, "Upload response is missing url", responseBody);
                throw new Exception("Upload response is missing url.");
            }

            return payload.Url;
        }

        private static void LogUploadError(string uploadUrl, string title, string details)
        {
            var message = $"[FileUploadService] {title}{Environment.NewLine}Endpoint: {uploadUrl}{Environment.NewLine}{details}";
            Debug.WriteLine(message);
            Console.Error.WriteLine(message);
        }

        private static string BuildUploadEndpoint(string graphQLEndpoint)
        {
            var graphqlUri = new Uri(graphQLEndpoint, UriKind.Absolute);
            var builder = new UriBuilder(graphqlUri.Scheme, graphqlUri.Host, graphqlUri.Port, "/uploads");
            return builder.Uri.ToString();
        }

        private static string GetMimeType(string extension)
        {
            var ext = extension?.Trim().ToLowerInvariant();
            return ext switch
            {
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
        }

        private static string ExtractMessage(string responseBody)
        {
            try
            {
                var error = JsonSerializer.Deserialize<UploadErrorResponse>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return error?.Message ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private class UploadResponse
        {
            public string Url { get; set; } = string.Empty;
        }

        private class UploadErrorResponse
        {
            public string Message { get; set; } = string.Empty;
        }
    }
}
