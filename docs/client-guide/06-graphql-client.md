# 06 — GraphQL Client Layer

How a service call becomes an HTTP request.

## The service interface

```csharp
public interface IGraphQLClientService
{
    string ServerUrl { get; }
    void SetServerUrl(string url);
    void SetAuthToken(string? token);
    Task<T> QueryAsync<T>(string query, object? variables = null);
    Task<T> MutateAsync<T>(string query, object? variables = null);
}
```

It's just a thin wrapper around `HttpClient` that knows how to talk GraphQL.

## The implementation: GraphQLClientService

### Constructor

```csharp
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
```

- One `HttpClient` per app (singleton — registered as such in DI)
- `CamelCase` policy means C# `BrandId` ↔ JSON `brandId` (matches our GraphQL schema)
- `WhenWritingNull` skips null fields when sending to server (cleaner payloads)

### SetAuthToken

```csharp
public void SetAuthToken(string? token)
{
    _httpClient.DefaultRequestHeaders.Authorization = string.IsNullOrEmpty(token)
        ? null
        : new AuthenticationHeaderValue("Bearer", token);
}
```

Sets `Authorization: Bearer <token>` on every subsequent request.

### SendAsync (the core)

```csharp
private async Task<T> SendAsync<T>(string query, object? variables)
{
    var requestBody = new { query, variables };
    var json = JsonSerializer.Serialize(requestBody, _jsonOptions);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var response = await _httpClient.PostAsync(_serverUrl, content);
    var responseBody = await response.Content.ReadAsStringAsync();
    var result = JsonSerializer.Deserialize<GraphQLResponse<T>>(responseBody, _jsonOptions);

    if (result?.Errors?.Length > 0)
        throw new GraphQLException(result.Errors[0].Message);
    if (result?.Data == null)
        throw new GraphQLException("No data");

    return result.Data;
}
```

What happens:
1. Wrap the query string and variables in `{ query, variables }` (the standard GraphQL HTTP shape)
2. Serialize to JSON
3. POST to the server URL
4. Server responds with `{ data, errors }`
5. If errors: throw
6. If no data: throw
7. Return the `data` cast to `T`

### Where `T` comes from

When you call:
```csharp
var result = await _graphQL.QueryAsync<DashboardStatsResponse>(DashboardQueries.GetStats);
```

`T` = `DashboardStatsResponse`, which looks like:
```csharp
private class DashboardStatsResponse
{
    public DashboardStatsDto DashboardStats { get; set; } = new();
}
```

So `result.DashboardStats` is the actual data we want. The wrapper class is needed because GraphQL responses always have the field name as the top-level key.

### SafeExecuteAsync

```csharp
public async Task<Result<T>> SafeExecuteAsync<T>(Func<Task<T>> action)
{
    try
    {
        var data = await action();
        return Result<T>.Success(data);
    }
    catch (GraphQLException ex) { return Result<T>.Failure($"GraphQL: {ex.Message}"); }
    catch (HttpRequestException ex) { return Result<T>.Failure($"Network: {ex.Message}"); }
    catch (Exception ex) { return Result<T>.Failure($"Unexpected: {ex.Message}"); }
}
```

Wraps any throwing call into a `Result<T>` so callers can write:
```csharp
if (result.IsSuccess) { /* use result.Value */ }
else { /* show result.Error */ }
```

Cleaner than try/catch in every service method.

## How a service uses this

Example from `DashboardService`:

```csharp
public async Task<Result<DashboardStatsDto>> GetStatsAsync()
{
    var result = await (_graphQL as GraphQLClientService)!
        .SafeExecuteAsync(() =>
            _graphQL.QueryAsync<DashboardStatsResponse>(DashboardQueries.GetStats));

    if (!result.IsSuccess)
        return Result<DashboardStatsDto>.Failure(result.Error!);

    return Result<DashboardStatsDto>.Success(result.Value!.DashboardStats);
}
```

Steps:
1. Call `QueryAsync` with the query string and expected response type
2. Wrap in `SafeExecuteAsync` to convert exceptions into `Result<T>`
3. Unwrap from the GraphQL response wrapper (`result.Value.DashboardStats`)
4. Return the inner DTO

## The query strings

We keep all query strings in `GraphQL/Operations/*.cs`:

```csharp
public static class DashboardQueries
{
    public const string GetStats = @"
        query DashboardStats {
            dashboardStats {
                totalProducts
                totalOrdersToday
                ...
            }
        }";
}
```

Why constants?
- Easy to find all queries in one place
- Reusable across services
- Easier to test/preview in Apollo Sandbox by copy-pasting

## Mental model

Think of `GraphQLClientService` as the **postal service**:
- Services hand it sealed envelopes (queries + variables)
- It delivers them, brings back replies
- It knows nothing about what's inside

Services know what they're sending and what they expect back, but don't worry about HTTP, JSON, or auth headers.
