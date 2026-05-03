using hcmus_shop.Contracts.Services;
using hcmus_shop.GraphQL.Operations;
using hcmus_shop.Models.Common;
using hcmus_shop.Models.DTOs;
using hcmus_shop.Services.GraphQL;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace hcmus_shop.Services.Series
{
    public class SeriesService : ISeriesService
    {
        private readonly IGraphQLClientService _graphQL;

        public SeriesService(IGraphQLClientService graphQL)
        {
            _graphQL = graphQL;
        }

        public async Task<Result<List<SeriesDto>>> GetByBrandAsync(int brandId)
        {
            var result = await (_graphQL as GraphQLClientService)!
                .SafeExecuteAsync(() =>
                    _graphQL.QueryAsync<SeriesByBrandResponse>(
                        SeriesQueries.GetByBrand,
                        new { brandId }
                    )
                );

            if (!result.IsSuccess)
                return Result<List<SeriesDto>>.Failure(result.Error!);

            return Result<List<SeriesDto>>.Success(result.Value!.SeriesByBrand);
        }

        public async Task<Result<SeriesDto?>> GetByIdAsync(int seriesId)
        {
            var result = await (_graphQL as GraphQLClientService)!
                .SafeExecuteAsync(() =>
                    _graphQL.QueryAsync<SeriesResponse>(
                        SeriesQueries.GetById,
                        new { seriesId }
                    )
                );

            if (!result.IsSuccess)
                return Result<SeriesDto?>.Failure(result.Error!);

            return Result<SeriesDto?>.Success(result.Value!.Series);
        }

        public async Task<Result<SeriesDto>> CreateAsync(int brandId, string name, string? description = null, string? targetSegment = null)
        {
            var result = await (_graphQL as GraphQLClientService)!
                .SafeExecuteAsync(() =>
                    _graphQL.MutateAsync<CreateSeriesResponse>(
                        SeriesQueries.Create,
                        new { brandId, name, description, targetSegment }
                    )
                );

            if (!result.IsSuccess)
                return Result<SeriesDto>.Failure(result.Error!);

            return Result<SeriesDto>.Success(result.Value!.CreateSeries);
        }

        public async Task<Result<bool>> DeleteAsync(int seriesId)
        {
            var result = await (_graphQL as GraphQLClientService)!
                .SafeExecuteAsync(() =>
                    _graphQL.MutateAsync<DeleteSeriesResponse>(
                        SeriesQueries.Delete,
                        new { seriesId }
                    )
                );

            if (!result.IsSuccess)
                return Result<bool>.Failure(result.Error!);

            return Result<bool>.Success(true);
        }

        private class SeriesByBrandResponse { public List<SeriesDto> SeriesByBrand { get; set; } = new(); }
        private class SeriesResponse { public SeriesDto? Series { get; set; } }
        private class CreateSeriesResponse { public SeriesDto CreateSeries { get; set; } = new(); }
        private class DeleteSeriesResponse { public SeriesDto DeleteSeries { get; set; } = new(); }
    }
}