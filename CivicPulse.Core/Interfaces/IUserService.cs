using CivicPulse.Core.DTOs;

namespace CivicPulse.Core.Interfaces;

public interface IUserService
{
    Task<UserDto> RegisterAsync(RegisterDto dto);
    Task<UserDto> LoginAsync(LoginDto dto);
    Task<UserDto> GetByIdAsync(int id);
    Task<UserDto> GetByEmailAsync(string email);
    Task<PagedResult<UserDto>> GetAllAsync(int page = 1, int pageSize = 20);
    Task<UserDto> UpdateProfileAsync(int id, string fullName, string phoneNumber);
    Task<bool> ChangePasswordAsync(int id, string currentPassword, string newPassword);
    Task<bool> ToggleUserStatusAsync(int id);
    Task<bool> UpdateRoleAsync(int id, string role);
    Task<bool> ResetPasswordAsync(int id, string newPassword);
    Task<bool> UpdateLanguageAsync(int id, string language);
    Task<PagedResult<UserDto>> GetFilteredAsync(string? role, bool? isActive, string? search, int page = 1, int pageSize = 20);
}
