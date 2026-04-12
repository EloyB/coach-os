# Plan: CreateLessonSeries with inline Lessons

## Context
The FE will send a single request to create a lesson series **with all its lessons** in one call. Currently, lessons are added one-by-one via a separate `POST /lessonseries/{id}/lessons` endpoint. We need to embed a required list of lessons inside `CreateLessonSeriesRequest` and handle creation atomically.

## Files to modify

| # | File | Change |
|---|------|--------|
| 1 | `CoachOS.Application/LessonSeries/DTOs/CreateLessonSeriesRequest.cs` | Add `List<CreateLessonRequest> Lessons` property |
| 2 | `CoachOS.Application/LessonSeries/Validators/CreateLessonSeriesRequestValidator.cs` | Add rule: `Lessons` not empty (min 1), plus `ForEach` child validator using existing `CreateLessonRequestValidator` |
| 3 | `CoachOS.Application/LessonSeries/LessonSeriesService.cs` → `CreateAsync` | After creating the series, iterate `request.Lessons`, map each to a `Lesson` entity (reusing existing `ToLesson` mapper), persist all via `ILessonRepository.AddAsync`, single `SaveChangesAsync` |
| 4 | `CoachOS.Application/LessonSeries/ILessonSeriesService.cs` | No signature change needed — `CreateAsync` already takes `CreateLessonSeriesRequest` and returns `Result<Guid>` |

## Detail

### 1. DTO — `CreateLessonSeriesRequest`
Add one field:
```csharp
List<CreateLessonRequest> Lessons
```

### 2. Validator — `CreateLessonSeriesRequestValidator`
```csharp
RuleFor(x => x.Lessons)
    .NotEmpty().WithMessage("Minstens één les is verplicht");

RuleForEach(x => x.Lessons)
    .SetValidator(new CreateLessonRequestValidator());
```

### 3. Service — `LessonSeriesService.CreateAsync`
After the existing series save, loop through `request.Lessons`, call `mapper.ToLesson(lesson, series)` (already exists), then `lessonRepo.AddAsync(...)` for each, then a single `SaveChangesAsync`. This keeps it in one DB transaction.

**No new mapper methods needed** — `ToLesson(CreateLessonRequest, LessonSeries)` already exists and calculates `EndTime` from the series `DurationMinutes`.

### 4. No endpoint changes
`CreateLessonSeriesEndpoint` already accepts `CreateLessonSeriesRequest` — the new `Lessons` property is automatically deserialized.

## Verification
1. `dotnet build CoachOS.slnx` — compiles
2. `dotnet test CoachOS.slnx` — existing tests pass
3. POST `/lessonseries` with a body containing `"lessons": [...]` — series + lessons created atomically
