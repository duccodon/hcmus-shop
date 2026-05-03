using hcmus_shop.Models.Common;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace hcmus_shop.Services.GraphQL
{
    public class GraphQLClientService : Contracts.Services.IGraphQLClientService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;
        private string _serverUrl;

        public string ServerUrl => _serverUrl;

        public GraphQLClientService(string serverUrl)
        {
            _serverUrl = serverUrl;

            _httpClient = new HttpClient();

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            };
        }

        // ========================
        // AUTH
        // ========================
        public void SetAuthToken(string? token)
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                string.IsNullOrEmpty(token)
                    ? null
                    : new AuthenticationHeaderValue("Bearer", token);
        }

        public void SetServerUrl(string url)
        {
            _serverUrl = url;
        }

        // ========================
        // PUBLIC API
        // ========================
        public async Task<T> QueryAsync<T>(string query, object? variables = null)
        {
            return await SendAsync<T>(query, variables);
        }

        public async Task<T> MutateAsync<T>(string query, object? variables = null)
        {
            return await SendAsync<T>(query, variables);
        }

        // SAFE WRAPPER 
        public async Task<Result<T>> SafeExecuteAsync<T>(Func<Task<T>> action)
        {
            try
            {
                var data = await action();
                return Result<T>.Success(data);
            }
            catch (GraphQLException ex)
            {
                return Result<T>.Failure($"GraphQL: {ex.Message}");
            }
            catch (HttpRequestException ex)
            {
                return Result<T>.Failure($"Network: {ex.Message}");
            }
            catch (Exception ex)
            {
                return Result<T>.Failure($"Unexpected: {ex.Message}");
            }
        }

        // ========================
        // CORE EXECUTION
        // ========================
        private async Task<T> SendAsync<T>(string query, object? variables)
        {
            var requestBody = new
            {
                query,
                variables
            };

            var json = JsonSerializer.Serialize(requestBody, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response;

            try
            {
                response = await _httpClient.PostAsync(_serverUrl, content);
            }
            catch (HttpRequestException ex)
            {
                throw new GraphQLException($"Cannot connect to server: {ex.Message}");
            }

            var responseBody = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<GraphQLResponse<T>>(responseBody, _jsonOptions);

            if (result == null)
                throw new GraphQLException("Empty response from server");

            if (result.Errors != null && result.Errors.Length > 0)
                throw new GraphQLException(result.Errors[0].Message);

            if (result.Data == null)
                throw new GraphQLException("No data returned from server");

            return result.Data;
        }
    }

    // ========================
    // RESPONSE WRAPPERS
    // ========================
    public class GraphQLResponse<T>
    {
        public T? Data { get; set; }
        public GraphQLError[]? Errors { get; set; }
    }

    public class GraphQLError
    {
        public string Message { get; set; } = string.Empty;
    }

    public class GraphQLException : Exception
    {
        public GraphQLException(string message) : base(message) { }
    }
}