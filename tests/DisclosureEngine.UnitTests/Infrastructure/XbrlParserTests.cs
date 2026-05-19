using DisclosureEngine.Infrastructure.Xbrl;
using FluentAssertions;

namespace DisclosureEngine.UnitTests.Infrastructure;

public sealed class XbrlParserTests
{
    [Fact]
    public async Task ParseAsync_NullStream_ThrowsArgumentNullException()
    {
        var sut = new XbrlParser();

        Func<Task> act = () => sut.ParseAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
