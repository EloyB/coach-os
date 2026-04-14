using CoachOS.Application.Students.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Students;

public interface IStudentLessonsService
{
    Task<Result<List<StudentLessonDto>>> GetMyLessonsAsync(string email, CancellationToken ct = default);
    Task<Result<StudentLessonDto>> GetMyLessonAsync(string email, Guid assignmentId, CancellationToken ct = default);
}
