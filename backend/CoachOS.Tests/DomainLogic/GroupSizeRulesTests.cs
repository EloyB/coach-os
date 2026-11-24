using AwesomeAssertions;
using CoachOS.Domain.Common;
using CoachOS.Domain.Models;
using NUnit.Framework;

namespace CoachOS.Tests.DomainLogic;

[TestFixture]
public class GroupSizeRulesTests
{
    [TestCase(new[] { 4 }, 4)]
    [TestCase(new[] { 2, 3 }, 3)]
    [TestCase(new[] { 1, 1 }, 1)]
    [TestCase(new[] { 10, 2 }, 4)]
    [TestCase(new int[0], 4)]
    [TestCase(new[] { 0 }, 4)]
    public void SeriesMax_IsTheLargestSlotCapacityCappedAtFour(int[] capacities, int expected)
        => GroupSizeRules.SeriesMax(capacities).Should().Be(expected);

    [TestCase(1, 1, 4)]
    [TestCase(2, 1, 4)]
    [TestCase(4, 1, 4)]
    [TestCase(3, 3, 4)]
    [TestCase(2, 2, 2)]
    [TestCase(3, 3, 2)] // een bestaande groep wordt niet geblokkeerd door een kleiner slot
    public void Validate_AcceptsChoicesBetweenOwnSizeAndSeriesMax(int max, int own, int seriesMax)
        => GroupSizeRules.Validate(max, own, seriesMax).Should().BeNull();

    [TestCase(0, 1, 4, "tussen 1 en 4")]
    [TestCase(5, 1, 4, "tussen 1 en 4")]
    [TestCase(1, 2, 4, "eigen groepsgrootte (2)")]
    [TestCase(2, 3, 4, "eigen groepsgrootte (3)")]
    [TestCase(3, 1, 2, "toelaat (2)")]
    public void Validate_RejectsChoicesOutsideTheBounds(int max, int own, int seriesMax, string messagePart)
    {
        Error? error = GroupSizeRules.Validate(max, own, seriesMax);

        error.Should().NotBeNull();
        error!.Code.Should().Be(ErrorCodes.Validation);
        error.Message.Should().Contain(messagePart);
    }
}
