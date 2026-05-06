using hcmus_shop.Contracts.Services;
using hcmus_shop.GraphQL.Operations;
using hcmus_shop.Models.Common;
using hcmus_shop.Models.DTOs;
using hcmus_shop.Services.Promotions.Dto;
using System.Threading.Tasks;

namespace hcmus_shop.Services.Promotions
{
    public class PromotionService : IPromotionService
    {
        private readonly IGraphQLClientService _graphQL;

        public PromotionService(IGraphQLClientService graphQL)
        {
            _graphQL = graphQL;
        }

        public async Task<Result<PromotionPageDto>> GetAllAsync(PromotionFilterDto filter)
        {
            var result = await _graphQL.SafeExecuteAsync(() =>
                _graphQL.QueryAsync<PromotionsResponse>(
                    PromotionQueries.GetPromotions,
                    new
                    {
                        search = string.IsNullOrWhiteSpace(filter.Search) ? null : filter.Search.Trim(),
                        page = filter.Page,
                        pageSize = filter.PageSize
                    }));

            if (!result.IsSuccess)
            {
                return Result<PromotionPageDto>.Failure(result.Error!);
            }

            return Result<PromotionPageDto>.Success(result.Value!.Promotions);
        }

        public async Task<Result<PromotionDto?>> GetByIdAsync(int promotionId)
        {
            var result = await _graphQL.SafeExecuteAsync(() =>
                _graphQL.QueryAsync<PromotionResponse>(
                    PromotionQueries.GetPromotion,
                    new { promotionId }));

            if (!result.IsSuccess)
            {
                return Result<PromotionDto?>.Failure(result.Error!);
            }

            return Result<PromotionDto?>.Success(result.Value!.Promotion);
        }

        public async Task<Result<PromotionDto>> CreateAsync(CreatePromotionInput input)
        {
            var result = await _graphQL.SafeExecuteAsync(() =>
                _graphQL.MutateAsync<CreatePromotionResponse>(
                    PromotionQueries.CreatePromotion,
                    new { input }));

            if (!result.IsSuccess)
            {
                return Result<PromotionDto>.Failure(result.Error!);
            }

            return Result<PromotionDto>.Success(result.Value!.CreatePromotion);
        }

        public async Task<Result<PromotionDto>> UpdateAsync(int promotionId, UpdatePromotionInput input)
        {
            var result = await _graphQL.SafeExecuteAsync(() =>
                _graphQL.MutateAsync<UpdatePromotionResponse>(
                    PromotionQueries.UpdatePromotion,
                    new { promotionId, input }));

            if (!result.IsSuccess)
            {
                return Result<PromotionDto>.Failure(result.Error!);
            }

            return Result<PromotionDto>.Success(result.Value!.UpdatePromotion);
        }

        public async Task<Result<bool>> DeleteAsync(int promotionId)
        {
            var result = await _graphQL.SafeExecuteAsync(() =>
                _graphQL.MutateAsync<DeletePromotionResponse>(
                    PromotionQueries.DeletePromotion,
                    new { promotionId }));

            if (!result.IsSuccess)
            {
                return Result<bool>.Failure(result.Error!);
            }

            return Result<bool>.Success(result.Value!.DeletePromotion.PromotionId > 0);
        }

        public async Task<Result<PromotionValidationDto>> ValidateAsync(string code)
        {
            var result = await _graphQL.SafeExecuteAsync(() =>
                _graphQL.QueryAsync<ValidatePromotionResponse>(
                    PromotionQueries.ValidatePromotion,
                    new { code }));

            if (!result.IsSuccess)
            {
                return Result<PromotionValidationDto>.Failure(result.Error!);
            }

            return Result<PromotionValidationDto>.Success(result.Value!.ValidatePromotion);
        }
    }
}
