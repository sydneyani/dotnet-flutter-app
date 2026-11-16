using Meridian.Domain;
using Meridian.Domain.Assets;
using Meridian.Domain.Common;
using Meridian.Domain.Inspections;

namespace Meridian.Domain.Tests;

public class AssetAndInspectionTests
{
    private static readonly DateTimeOffset Now = new(2026, 4, 2, 15, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 4, 2);

    [Theory]
    [InlineData(" tr-1044 ", "TR-1044")]
    [InlineData("PMP-CW-3", "PMP-CW-3")]
    public void Asset_tags_are_normalized(string input, string expected)
    {
        Assert.Equal(expected, AssetTag.Normalize(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("1ABC")]
    [InlineData("A")]
    [InlineData("HAS SPACE")]
    [InlineData("A--B")]
    [InlineData("-AB")]
    [InlineData("AB-")]
    public void Asset_tags_reject_malformed_values(string input)
    {
        Assert.Throws<DomainException>(() => AssetTag.Normalize(input));
    }

    [Fact]
    public void Retired_assets_cannot_be_inspected_and_failures_need_findings()
    {
        var asset = Asset.Register(
            Guid.NewGuid(),
            "MTR-YARD",
            "Yard revenue meter",
            AssetCategory.Instrument,
            AssetStatus.Retired,
            AssetCriticality.Low,
            "Kestrel Protection",
            "KM-200",
            "KM-00912",
            new DateOnly(2008, 2, 11),
            "Retired when the yard was re-metered.",
            Today,
            Now);

        Assert.Throws<DomainException>(() => asset.EnsureInspectable());

        var failure = Assert.Throws<DomainException>(() => Inspection.Record(
            Guid.NewGuid(),
            null,
            Guid.NewGuid(),
            Now.AddHours(-1),
            InspectionResult.Fail,
            "Gland is wet.",
            "  ",
            null,
            Now));
        Assert.Contains("findings", failure.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Passing_inspection_moves_the_asset_clock_forward_only()
    {
        var asset = Asset.Register(
            Guid.NewGuid(),
            "CB-220-14",
            "230 kV line breaker",
            AssetCategory.CircuitBreaker,
            AssetStatus.InService,
            AssetCriticality.High,
            "Helios Electric",
            "HE-SF6-245",
            "HE-CB-22014",
            new DateOnly(2016, 3, 18),
            "SF6 breaker on the tie line.",
            Today,
            Now);

        asset.RecordInspection(Now.AddDays(-1));
        asset.RecordInspection(Now.AddDays(-10));

        Assert.Equal(Now.AddDays(-1), asset.LastInspectedAt);
    }
}
