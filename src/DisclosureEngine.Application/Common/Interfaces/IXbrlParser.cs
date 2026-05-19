namespace DisclosureEngine.Application.Common.Interfaces;

public interface IXbrlParser
{
    Task<XbrlParseResult> ParseAsync(Stream xmlStream, CancellationToken ct);
}

public sealed record XbrlParseResult(
    IReadOnlyList<ParsedXbrlFact> Facts,
    IReadOnlyList<ParsedXbrlContext> Contexts,
    IReadOnlyList<ParsedXbrlUnit> Units,
    int TotalFacts,
    int UniqueConcepts,
    DateTime? PeriodStart,
    DateTime? PeriodEnd,
    IReadOnlyList<string> ValidationErrors);

public sealed record ParsedXbrlFact(
    string Concept,
    string ContextRef,
    string? UnitRef,
    string Value,
    int? Decimals);

public sealed record ParsedXbrlContext(
    string Id,
    string EntityIdentifier,
    DateTime? StartDate,
    DateTime? EndDate,
    DateTime? Instant);

public sealed record ParsedXbrlUnit(string Id, string Measure);
