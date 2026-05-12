using hcmus_shop.Contracts.Services;
using hcmus_shop.GraphQL.Operations;
using hcmus_shop.Models.Common;
using hcmus_shop.Models.DTOs;
using hcmus_shop.Services.GraphQL;
using hcmus_shop.Services.Users.Dto;
using System.Threading.Tasks;

namespace hcmus_shop.Services.Users
{
    public class UserService : IUserService
    {
        private readonly IGraphQLClientService _graphQL;

        public UserService(IGraphQLClientService graphQL)
        {
            _graphQL = graphQL;
        }

        public async Task<Result<UserPageDto>> GetAllAsync(UserFilterDto filter)
        {
            var result = await (_graphQL as GraphQLClientService)!.SafeExecuteAsync(() =>
                _graphQL.QueryAsync<UsersResponse>(UserQueries.GetUsers, new
                {
                    search = filter.Search,
                    role = filter.Role,
                    page = filter.Page,
                    pageSize = filter.PageSize
                }));

            if (!result.IsSuccess)
            {
                return Result<UserPageDto>.Failure(result.Error!);
            }

            return Result<UserPageDto>.Success(result.Value!.Users);
        }

        public async Task<Result<UserDto?>> GetByIdAsync(string userId)
        {
            var result = await (_graphQL as GraphQLClientService)!.SafeExecuteAsync(() =>
                _graphQL.QueryAsync<UserResponse>(UserQueries.GetUserById, new { userId }));

            if (!result.IsSuccess)
            {
                return Result<UserDto?>.Failure(result.Error!);
            }

            return Result<UserDto?>.Success(result.Value!.User);
        }

        public async Task<Result<UserDto>> CreateAsync(CreateUserInput input)
        {
            var result = await (_graphQL as GraphQLClientService)!.SafeExecuteAsync(() =>
                _graphQL.MutateAsync<CreateUserResponse>(UserQueries.CreateUser, new { input }));

            if (!result.IsSuccess)
            {
                return Result<UserDto>.Failure(result.Error!);
            }

            return Result<UserDto>.Success(result.Value!.CreateUser);
        }

        public async Task<Result<UserDto>> UpdateAsync(string userId, UpdateUserInput input)
        {
            var result = await (_graphQL as GraphQLClientService)!.SafeExecuteAsync(() =>
                _graphQL.MutateAsync<UpdateUserResponse>(UserQueries.UpdateUser, new { userId, input }));

            if (!result.IsSuccess)
            {
                return Result<UserDto>.Failure(result.Error!);
            }

            return Result<UserDto>.Success(result.Value!.UpdateUser);
        }

        public async Task<Result<bool>> DeleteAsync(string userId)
        {
            var result = await (_graphQL as GraphQLClientService)!.SafeExecuteAsync(() =>
                _graphQL.MutateAsync<DeleteUserResponse>(UserQueries.DeleteUser, new { userId }));

            if (!result.IsSuccess)
            {
                return Result<bool>.Failure(result.Error!);
            }

            return Result<bool>.Success(result.Value!.DeleteUser);
        }
    }
}
