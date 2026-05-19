using DisclosureEngine.Domain.Entities;
using DisclosureEngine.Domain.Enums;
using FluentAssertions;

namespace DisclosureEngine.UnitTests.Domain;

public sealed class ReportTests
{
    private static Report NewDraft() => new(
        title: "FY2025 Disclosure",
        fiscalYear: 2025,
        tenantId: Guid.NewGuid(),
        createdByUserId: Guid.NewGuid());

    [Fact]
    public void Submit_FromDraft_TransitionsToSubmitted()
    {
        var report = NewDraft();

        report.Submit();

        report.Status.Should().Be(ReportStatus.Submitted);
        report.SubmittedAt.Should().NotBeNull();
        report.SubmittedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Submit_FromSubmitted_ThrowsInvalidOperationException()
    {
        var report = NewDraft();
        report.Submit();

        Action act = () => report.Submit();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Submitted*");
    }

    [Fact]
    public void Submit_FromPublished_ThrowsInvalidOperationException()
    {
        var report = NewDraft();
        report.Submit();
        report.Publish();

        Action act = () => report.Submit();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Published*");
    }

    [Fact]
    public void Publish_FromSubmitted_TransitionsToPublished()
    {
        var report = NewDraft();
        report.Submit();

        report.Publish();

        report.Status.Should().Be(ReportStatus.Published);
        report.PublishedAt.Should().NotBeNull();
        report.PublishedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Publish_FromDraft_ThrowsInvalidOperationException()
    {
        var report = NewDraft();

        Action act = () => report.Publish();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Draft*");
    }

    [Fact]
    public void EnsureEditable_OnSubmitted_Throws()
    {
        var report = NewDraft();
        report.Submit();

        Action act = () => report.EnsureEditable();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Submitted*");
    }
}
