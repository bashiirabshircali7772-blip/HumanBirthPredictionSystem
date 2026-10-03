using System.ComponentModel.DataAnnotations;

namespace HumanBirthPredictionSystem.ViewModels
{
    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "Username is required.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 100 characters.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(150, ErrorMessage = "Full Name cannot exceed 150 characters.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "System Role is required.")]
        public string Role { get; set; } = "User";

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 4, ErrorMessage = "Password must be at least 4 characters long.")]
        public string Password { get; set; } = string.Empty;
    }

    public class EditUserViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Username is required.")]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "System Role is required.")]
        public string Role { get; set; } = "User";

        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }
    }
}
