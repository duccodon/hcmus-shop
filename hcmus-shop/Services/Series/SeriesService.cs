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

        public async Task<List<SeriesDto>> GetByBrandAsync(int brandId)
        {
            var query = @"
                query SeriesByBrand($brandId: Int!) {
                    seriesByBrand(brandId: $brandId) {
                        seriesId
                        name
                        description
                        targetSegment
                    }
                }";

            var result = await _graphQL.QueryAsync<SeriesByBrandResponse>(query, new { brandId });
            return result.SeriesByBrand;
        }

        public async Task<SeriesDto?> GetByIdAsync(int seriesId)
        {
            var query = @"
                query Series($seriesId: Int!) {
                    series(seriesId: $seriesId) {
                        seriesId
                        brandId
                        name
                        description
                        targetSegment
                    }
                }";

            var result = await _graphQL.QueryAsync<SeriesResponse>(query, new { seriesId });
            return result.Series;
        }

        public async Task<SeriesDto> CreateAsync(int brandId, string name, string? description = null, string? targetSegment = null)
        {
            var query = @"
                mutation CreateSeries($brandId: Int!, $name: String!, $description: String, $targetSegment: String) {
                    createSeries(brandId: $brandId, name: $name, description: $description, targetSegment: $targetSegment) {
                        seriesId
                        name
                    }
                }";

            var result = await _graphQL.MutateAsync<CreateSeriesResponse>(query, new { brandId, name, description, targetSegment });
            return result.CreateSeries;
        }

        public async Task<bool> DeleteAsync(int seriesId)
        {
            var query = @"
                mutation DeleteSeries($seriesId: Int!) {
                    deleteSeries(seriesId: $seriesId) {
                        seriesId
                    }
                }";

            await _graphQL.MutateAsync<DeleteSeriesResponse>(query, new { seriesId });
            return true;
        }

        private class SeriesByBrandResponse { public List<SeriesDto> SeriesByBrand { get; set; } = new(); }
        private class SeriesResponse { public SeriesDto? Series { get; set; } }
        private class CreateSeriesResponse { public SeriesDto CreateSeries { get; set; } = new(); }
        private class DeleteSeriesResponse { public SeriesDto DeleteSeries { get; set; } = new(); }
    }
}
