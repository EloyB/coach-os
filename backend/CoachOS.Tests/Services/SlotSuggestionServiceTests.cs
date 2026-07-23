using CoachOS.Application.Planning;
using CoachOS.Application.Planning.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

[TestFixture]
public class SlotSuggestionServiceTests
{
    private Mock<ITrainerAvailabilityRepository> _repo = null!;
    private Mock<ITennisClubRepository> _clubRepo = null!;
    private Mock<IUserLookupService> _userLookup = null!;
    private SlotSuggestionService _sut = null!;

    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _clubId = Guid.NewGuid();
    private readonly Guid _trainerA = Guid.NewGuid();
    private readonly Guid _trainerB = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<ITrainerAvailabilityRepository>();
        _clubRepo = new Mock<ITennisClubRepository>();
        _userLookup = new Mock<IUserLookupService>();

        _clubRepo.Setup(r => r.ExistsAsync(_clubId, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _userLookup.Setup(r => r.GetUserNamesByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, string>
            {
                [_trainerA] = "Anna Aerts",
                [_trainerB] = "Bram Bosmans",
            });

        _sut = new SlotSuggestionService(_repo.Object, _clubRepo.Object, _userLookup.Object);
    }

    private TrainerAvailability Availability(
        Guid trainerId,
        int dayOfWeek,
        string start,
        string end,
        Guid? clubId = null,
        Guid? orgId = null,
        bool isActive = true) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId ?? _orgId,
            TrainerId = trainerId,
            TennisClubId = clubId,
            DayOfWeek = dayOfWeek,
            StartTime = TimeOnly.ParseExact(start, "HH:mm"),
            EndTime = TimeOnly.ParseExact(end, "HH:mm"),
            IsActive = isActive,
        };

    private void GivenAvailabilities(params TrainerAvailability[] availabilities) =>
        _repo.Setup(r => r.GetByOrganizationAsync(_orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(availabilities.ToList());

    private async Task<List<SlotSuggestionDto>> Suggest()
    {
        Result<List<SlotSuggestionDto>> result = await _sut.SuggestSlotsAsync(_orgId, _clubId, CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        return result.Value!;
    }

    [Test]
    public async Task SuggestSlotsAsync_TwoOverlappingTrainers_ReturnsWindowWithTwoParallelSlots()
    {
        GivenAvailabilities(
            Availability(_trainerA, 1, "17:00", "20:00", _clubId),
            Availability(_trainerB, 1, "18:00", "21:00", _clubId));

        List<SlotSuggestionDto> suggestions = await Suggest();

        SlotSuggestionDto? overlap = suggestions.SingleOrDefault(s => s.StartTime == "18:00" && s.EndTime == "20:00");
        overlap.Should().NotBeNull();
        overlap!.DayOfWeek.Should().Be(1);
        overlap.AvailableTrainerCount.Should().Be(2);
        overlap.SuggestedParallelSlots.Should().Be(2);
        overlap.Trainers.Select(t => t.Id).Should().BeEquivalentTo(new[] { _trainerA, _trainerB });
        overlap.Trainers.Select(t => t.Name).Should().Contain("Anna Aerts");
    }

    [Test]
    public async Task SuggestSlotsAsync_NonOverlappingWindows_ReturnsSeparateSuggestions()
    {
        GivenAvailabilities(
            Availability(_trainerA, 2, "09:00", "11:00", _clubId),
            Availability(_trainerB, 2, "14:00", "16:00", _clubId));

        List<SlotSuggestionDto> suggestions = await Suggest();

        suggestions.Should().HaveCount(2);
        suggestions.Should().ContainSingle(s => s.StartTime == "09:00" && s.EndTime == "11:00" && s.SuggestedParallelSlots == 1);
        suggestions.Should().ContainSingle(s => s.StartTime == "14:00" && s.EndTime == "16:00" && s.SuggestedParallelSlots == 1);
    }

    [Test]
    public async Task SuggestSlotsAsync_WindowShorterThanOneHour_IsFilteredOut()
    {
        GivenAvailabilities(Availability(_trainerA, 3, "18:00", "18:45", _clubId));

        List<SlotSuggestionDto> suggestions = await Suggest();

        suggestions.Should().BeEmpty();
    }

    [Test]
    public async Task SuggestSlotsAsync_PartialOverlapShorterThanOneHour_OnlyLongerWindowsReturned()
    {
        // Overlap 18:30-19:00 duurt 30 min -> valt weg. De randen blijven wel over.
        GivenAvailabilities(
            Availability(_trainerA, 4, "17:00", "19:00", _clubId),
            Availability(_trainerB, 4, "18:30", "21:00", _clubId));

        List<SlotSuggestionDto> suggestions = await Suggest();

        suggestions.Should().NotContain(s => s.SuggestedParallelSlots == 2);
        suggestions.Should().ContainSingle(s => s.StartTime == "17:00" && s.EndTime == "18:30");
        suggestions.Should().ContainSingle(s => s.StartTime == "19:00" && s.EndTime == "21:00");
    }

    [Test]
    public async Task SuggestSlotsAsync_AdjacentWindowsSameTrainerSet_AreMerged()
    {
        GivenAvailabilities(
            Availability(_trainerA, 0, "17:00", "19:00", _clubId),
            Availability(_trainerA, 0, "19:00", "21:00", _clubId));

        List<SlotSuggestionDto> suggestions = await Suggest();

        suggestions.Should().ContainSingle();
        suggestions[0].StartTime.Should().Be("17:00");
        suggestions[0].EndTime.Should().Be("21:00");
    }

    [Test]
    public async Task SuggestSlotsAsync_ClubNullAvailability_CountsForThisClub()
    {
        GivenAvailabilities(
            Availability(_trainerA, 1, "18:00", "20:00", _clubId),
            Availability(_trainerB, 1, "18:00", "20:00", clubId: null));

        List<SlotSuggestionDto> suggestions = await Suggest();

        suggestions.Should().ContainSingle();
        suggestions[0].AvailableTrainerCount.Should().Be(2);
        suggestions[0].Trainers.Select(t => t.Id).Should().Contain(_trainerB);
    }

    [Test]
    public async Task SuggestSlotsAsync_AvailabilityOfOtherClub_IsIgnored()
    {
        GivenAvailabilities(
            Availability(_trainerA, 1, "18:00", "20:00", _clubId),
            Availability(_trainerB, 1, "18:00", "20:00", clubId: Guid.NewGuid()));

        List<SlotSuggestionDto> suggestions = await Suggest();

        suggestions.Should().ContainSingle();
        suggestions[0].AvailableTrainerCount.Should().Be(1);
        suggestions[0].Trainers.Single().Id.Should().Be(_trainerA);
    }

    [Test]
    public async Task SuggestSlotsAsync_AvailabilityOfOtherOrganization_IsIgnored()
    {
        GivenAvailabilities(
            Availability(_trainerA, 1, "18:00", "20:00", _clubId),
            Availability(_trainerB, 1, "18:00", "20:00", _clubId, orgId: Guid.NewGuid()));

        List<SlotSuggestionDto> suggestions = await Suggest();

        suggestions.Should().ContainSingle();
        suggestions[0].AvailableTrainerCount.Should().Be(1);
        suggestions[0].Trainers.Single().Id.Should().Be(_trainerA);
    }

    [Test]
    public async Task SuggestSlotsAsync_InactiveAvailability_IsIgnored()
    {
        GivenAvailabilities(
            Availability(_trainerA, 1, "18:00", "20:00", _clubId),
            Availability(_trainerB, 1, "18:00", "20:00", _clubId, isActive: false));

        List<SlotSuggestionDto> suggestions = await Suggest();

        suggestions.Should().ContainSingle();
        suggestions[0].AvailableTrainerCount.Should().Be(1);
        suggestions[0].Trainers.Single().Id.Should().Be(_trainerA);
    }

    [Test]
    public async Task SuggestSlotsAsync_ClubNotInOrganization_ReturnsNotFound()
    {
        _clubRepo.Setup(r => r.ExistsAsync(_clubId, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        Result<List<SlotSuggestionDto>> result = await _sut.SuggestSlotsAsync(_orgId, _clubId, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.NotFound);
        _repo.Verify(r => r.GetByOrganizationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task SuggestSlotsAsync_DifferentDays_AreGroupedSeparately()
    {
        GivenAvailabilities(
            Availability(_trainerA, 1, "18:00", "20:00", _clubId),
            Availability(_trainerB, 5, "18:00", "20:00", _clubId));

        List<SlotSuggestionDto> suggestions = await Suggest();

        suggestions.Should().HaveCount(2);
        suggestions.Select(s => s.DayOfWeek).Should().BeEquivalentTo(new[] { 1, 5 });
        suggestions.Should().OnlyContain(s => s.SuggestedParallelSlots == 1);
    }
}
