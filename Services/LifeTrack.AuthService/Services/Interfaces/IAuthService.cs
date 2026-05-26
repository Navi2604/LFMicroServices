// ============================================================
// AuthService.API / Services / Interfaces / IAuthService.cs
// NO CHANGES — Service delegates to cached repository
// ============================================================

using LifeTrack.AuthService.DTOs;
using LifeTrack.Shared.Wrappers;

namespace LifeTrack.AuthService.Services.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request);
        Task<ApiResponse<LoginResponse>> LoginPatientAsync(LoginRequest request);
        Task<ApiResponse<bool>> RegisterPatientAsync(RegisterPatientRequest request);
        Task<ApiResponse<bool>> CreateStaffAsync(CreateStaffRequest request);
    }
}