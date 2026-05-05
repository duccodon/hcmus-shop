using System;
using System.Collections.Generic;
using System.Text;

namespace hcmus_shop.GraphQL.Operations
{
    public static class SeriesQueries
    {
        public const string GetByBrand = @"
            query SeriesByBrand($brandId: Int!) {
                seriesByBrand(brandId: $brandId) {
                    seriesId
                    name
                    description
                    targetSegment
                }
            }";

        public const string GetById = @"
            query Series($seriesId: Int!) {
                series(seriesId: $seriesId) {
                    seriesId
                    brandId
                    name
                    description
                    targetSegment
                }
            }";

        public const string Create = @"
            mutation CreateSeries($brandId: Int!, $name: String!, $description: String, $targetSegment: String) {
                createSeries(brandId: $brandId, name: $name, description: $description, targetSegment: $targetSegment) {
                    seriesId
                    name
                }
            }";

        public const string Delete = @"
            mutation DeleteSeries($seriesId: Int!) {
                deleteSeries(seriesId: $seriesId) {
                    seriesId
                }
            }";
    }
}
