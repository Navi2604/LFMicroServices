// ============================================================
// UserService.API / DTOs / UserDTOs.cs
// ============================================================

using System.ComponentModel.DataAnnotations;

namespace LifeTrack.UserService.DTOs
{
    public class UserDto
    {
        public long UserID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int RoleID { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class UpdateUserRequest
    {
        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role is required.")]
        [Range(1, 6, ErrorMessage = "Invalid role.")]
        public int RoleID { get; set; }
    }

    public class UserFilterDto
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public int? RoleID { get; set; }
        public bool? IsActive { get; set; }
    }
}