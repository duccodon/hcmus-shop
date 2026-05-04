using hcmus_shop.Contracts.Services;
using hcmus_shop.GraphQL.Operations;
using hcmus_shop.Models.Common;
using hcmus_shop.Models.DTOs;
using hcmus_shop.Services.GraphQL;
using System.Threading.Tasks;

namespace hcmus_shop.Services.Dashboard
{
    public class DashboardService : IDashboardService
    {
        private readonly IGraphQLClientService _graphQL;

        public DashboardService(IGraphQLClientService graphQL)
        {
            _graphQL = graphQL;
        }

        public async Task<Result<DashboardStatsDto>> GetStatsAsync()
        {
            var result = await (_graphQL as GraphQLClientService)!
                .SafeExecuteAsync(() =>
                    _graphQL.QueryAsync<DashboardStatsResponse>(DashboardQueries.GetStats));

            if (!result.IsSuccess)
                return Result<DashboardStatsDto>.Failure(result.Error!);

            return Result<DashboardStatsDto>.Success(result.Value!.DashboardStats);
        }

        private class DashboardStatsResponse
        {
            public DashboardStatsDto DashboardStats { get; set; } = new();
        }
    }
}
