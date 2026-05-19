using DisclosureEngine.Infrastructure.Xbrl;
using FluentAssertions;

namespace DisclosureEngine.IntegrationTests.Xbrl;

public sealed class XbrlParserTests
{
    private static readonly string FixturesDir =
        Path.Combine(AppContext.BaseDirectory, "fixtures", "xbrl");

    private static FileStream Open(string fixtureFileName) =>
        File.OpenRead(Path.Combine(FixturesDir, fixtureFileName));

    [Fact]
    public async Task ParseAsync_ValidMinimalSample_ReturnsExpectedFacts()
    {
        var sut = new XbrlParser();
        await using var stream = Open("sample-minimal.xml");

        var result = await sut.ParseAsync(stream, CancellationToken.None);

        result.TotalFacts.Should().Be(3);
        result.UniqueConcepts.Should().Be(3);
        result.Contexts.Should().HaveCount(2);
        result.Units.Should().HaveCount(1);
        result.Units[0].Measure.Should().Be("iso4217:EUR");
        result.ValidationErrors.Should().BeEmpty();

        result.Facts.Select(f => f.Concept).Should().BeEquivalentTo(
            new[] { "us-gaap:Revenues", "us-gaap:NetIncomeLoss", "us-gaap:Assets" });

        result.PeriodStart.Should().Be(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        result.PeriodEnd  .Should().Be(new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task ParseAsync_InvalidContextRef_CollectsValidationError()
    {
        var sut = new XbrlParser();
        await using var stream = Open("sample-invalid-context-ref.xml");

        var result = await sut.ParseAsync(stream, CancellationToken.None);

        // Both facts still parse — parser doesn't throw.
        result.TotalFacts.Should().Be(2);
        result.ValidationErrors.Should().ContainSingle()
            .Which.Should().Contain("ctx-DOES-NOT-EXIST");
    }

    [Fact]
    public async Task ParseAsync_EmptyDocument_ReturnsEmptyResult()
    {
        var sut = new XbrlParser();
        var emptyXml = "<?xml version=\"1.0\"?><xbrl xmlns=\"http://www.xbrl.org/2003/instance\"/>";
        await using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(emptyXml));

        var result = await sut.ParseAsync(stream, CancellationToken.None);

        result.TotalFacts.Should().Be(0);
        result.UniqueConcepts.Should().Be(0);
        result.Contexts.Should().BeEmpty();
        result.Units.Should().BeEmpty();
        result.Facts.Should().BeEmpty();
        result.ValidationErrors.Should().BeEmpty();
        result.PeriodStart.Should().BeNull();
        result.PeriodEnd.Should().BeNull();
    }
}
