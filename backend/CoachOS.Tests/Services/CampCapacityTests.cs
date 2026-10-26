using CoachOS.Application.Camps;
using CoachOS.Domain.Entities;
using FluentAssertions;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

[TestFixture]
public class CampCapacityTests
{
    private static readonly DateOnly Day1 = new(2026, 7, 14);

    private static Camp CampWith(int? max, out CampPackage morning, out CampPackage afternoon, out CampPackage both)
    {
        Camp camp = new() { Id = Guid.NewGuid(), MaxParticipants = max };
        CampDay day = new() { Id = Guid.NewGuid(), Date = Day1 };
        day.Parts.Add(new CampDayPart { Order = 0, Name = "Ochtend" });
        day.Parts.Add(new CampDayPart { Order = 1, Name = "Namiddag" });
        camp.Days.Add(day);

        morning = Package("Ochtend", 0);
        afternoon = Package("Namiddag", 1);
        both = Package("Hele dag", 0, 1);
        camp.Packages.Add(morning);
        camp.Packages.Add(afternoon);
        camp.Packages.Add(both);
        return camp;
    }

    private static CampPackage Package(string name, params int[] partOrders)
    {
        CampPackage p = new() { Id = Guid.NewGuid(), Name = name };
        foreach (int order in partOrders)
            p.Slots.Add(new CampPackageSlot { Date = Day1, PartOrder = order });
        return p;
    }

    [Test]
    public void SlotsOf_Package_ReturnsItsSlots()
    {
        Camp camp = CampWith(2, out CampPackage morning, out _, out CampPackage both);

        CampCapacity.SlotsOf(camp, morning.Id).Should().BeEquivalentTo(new[] { (Day1, 0) });
        CampCapacity.SlotsOf(camp, both.Id).Should().BeEquivalentTo(new[] { (Day1, 0), (Day1, 1) });
    }

    [Test]
    public void SlotsOf_NoOrUnknownPackage_ReturnsEveryPartOfTheCamp()
    {
        Camp camp = CampWith(2, out _, out _, out _);

        CampCapacity.SlotsOf(camp, null).Should().BeEquivalentTo(new[] { (Day1, 0), (Day1, 1) });
        CampCapacity.SlotsOf(camp, Guid.NewGuid()).Should().BeEquivalentTo(new[] { (Day1, 0), (Day1, 1) });
    }

    [Test]
    public void IsFull_OtherPartHasRoom_IsFalse()
    {
        Camp camp = CampWith(2, out CampPackage morning, out CampPackage afternoon, out _);

        CampCapacity.IsFull(camp, [morning.Id, morning.Id], [afternoon.Id]).Should().BeFalse();
    }

    [Test]
    public void IsFull_NewParticipantCoversAFullPart_IsTrue()
    {
        Camp camp = CampWith(2, out CampPackage morning, out _, out CampPackage both);

        CampCapacity.IsFull(camp, [morning.Id, morning.Id], [both.Id]).Should().BeTrue();
    }

    [Test]
    public void IsFull_GroupOfThreeOnSamePartExceedsTheLimit_IsTrue()
    {
        Camp camp = CampWith(2, out CampPackage morning, out _, out _);

        CampCapacity.IsFull(camp, [], [morning.Id, morning.Id, morning.Id]).Should().BeTrue();
    }

    [Test]
    public void IsFull_ExistingWithoutPackageCountInEveryPart()
    {
        Camp camp = CampWith(2, out _, out CampPackage afternoon, out _);

        CampCapacity.IsFull(camp, [null, null], [afternoon.Id]).Should().BeTrue();
    }

    [Test]
    public void IsFull_NoMaximum_IsNeverFull()
    {
        Camp camp = CampWith(null, out CampPackage morning, out _, out _);

        CampCapacity.IsFull(camp, Enumerable.Repeat<Guid?>(morning.Id, 50), [morning.Id]).Should().BeFalse();
    }

    [Test]
    public void IsFull_CampWithoutPackages_IsNotJudgedPerPart()
    {
        Camp camp = new() { Id = Guid.NewGuid(), MaxParticipants = 1 };

        CampCapacity.IsFull(camp, [null, null], [null]).Should().BeFalse();
    }

    [Test]
    public void IsPackageFull_TrueWhenAnyOfItsPartsIsAtTheLimit()
    {
        Camp camp = CampWith(2, out CampPackage morning, out _, out CampPackage both);

        CampCapacity.IsPackageFull(camp, both, [morning.Id, morning.Id]).Should().BeTrue();
        CampCapacity.IsPackageFull(camp, both, [morning.Id]).Should().BeFalse();
    }
}
