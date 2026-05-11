using LifeTrack.AuthService.DTOs;
using LifeTrack.Shared.Wrappers;

namespace LifeTrack.AuthService.Services.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResponse<LoginResponse>> LoginAsync(
            LoginRequest req);

        Task<ApiResponse<UserDto>> RegisterAsync(
            RegisterRequest req);

        Task<ApiResponse<UserDto>> CreateStaffAsync(
            CreateStaffRequest req);
    }
}