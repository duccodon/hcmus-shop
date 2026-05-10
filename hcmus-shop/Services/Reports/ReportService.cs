using hcmus_shop.Contracts.Services;
using hcmus_shop.GraphQL.Operations;
using hcmus_shop.Models.Common;
using hcmus_shop.Models.DTOs;
using hcmus_shop.Services.Reports.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace hcmus_shop.Services.Reports
{
    public class ReportService : IReportService
    {
        private readonly IGraphQLClientService _graphQL;

        public ReportService(IGraphQLClientService graphQL)
        {
            _graphQL = graphQL;
        }

        public async Task<Result<List<SalesReportEntryDto>>> GetSalesReportAsync(SalesReportRequest request)
        {
            var result = await _graphQL.SafeExecuteAsync(() =>
                _graphQL.QueryAsync<SalesReportResponse>(
                    ReportQueries.SalesReport,
                    new
                    {
                        fromDate = request.FromDate,
                        toDate = request.ToDate,
                        groupBy = request.GroupBy
                    }));

            if (!result.IsSuccess)
            {
                return Result<List<SalesReportEntryDto>>.Failure(result.Error!);
            }

            return Result<List<SalesReportEntryDto>>.Success(result.Value!.SalesReport);
        }

        public async Task<Result<List<TopProductEntryDto>>> GetTopProductsAsync(TopProductsRequest request)
        {
            var result = await _graphQL.SafeExecuteAsync(() =>
                _graphQL.QueryAsync<TopProductsResponse>(
                    ReportQueries.TopProducts,
                    new
                    {
                        fromDate = request.FromDate,
                        toDate = request.ToDate,
                        limit = request.Limit
                    }));

            if (!result.IsSuccess)
            {
                return Result<List<TopProductEntryDto>>.Failure(result.Error!);
            }

            return Result<List<TopProductEntryDto>>.Success(result.Value!.TopProducts);
        }
    }
}
