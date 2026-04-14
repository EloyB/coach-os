namespace CoachOS.Application.StudentAuth.DTOs;

public class StudentAuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Student";
}
