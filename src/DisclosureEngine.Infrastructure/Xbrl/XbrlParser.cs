using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using DisclosureEngine.Application.Common.Interfaces;

namespace DisclosureEngine.Infrastructure.Xbrl;

/// <summary>
/// Minimal XBRL 2.1 instance-document parser built on <see cref="XDocument"/>.
/// Deliberately scoped to a working subset (contexts with date or instant periods,
/// simple <c>&lt;measure&gt;</c> units, untyped facts with <c>contextRef</c>); see
/// <c>docs/DECISIONS.md</c> §12 for the rationale.
/// </summary>
public sealed class XbrlParser : IXbrlParser
{
    private static readonly XNamespace Xbrli = "http://www.xbrl.org/2003/instance";

    public async Task<XbrlParseResult> ParseAsync(Stream xmlStream, CancellationToken ct)
    {
        if (xmlStream is null) throw new ArgumentNullException(nameof(xmlStream));

        XDocument doc;
        try
        {
            doc = await XDocument.LoadAsync(xmlStream, LoadOptions.None, ct);
        }
        catch (XmlException ex)
        {
            return Empty(new[] { $"Invalid XML: {ex.Message}" });
        }

        var root = doc.Root;
        if (root is null) return Empty(Array.Empty<string>());

        var contexts = ParseContexts(root).ToList();
        var units    = ParseUnits(root).ToList();
        var facts    = ParseFacts(root).ToList();

        var contextIds = contexts.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);
        var unitIds    = units.Select(u => u.Id).ToHashSet(StringComparer.Ordinal);

        var validationErrors = new List<string>();
        foreach (var fact in facts)
        {
            if (!contextIds.Contains(fact.ContextRef))
                validationErrors.Add(
                    $"Fact '{fact.Concept}' references unknown contextRef '{fact.ContextRef}'.");

            if (fact.UnitRef is not null && !unitIds.Contains(fact.UnitRef))
                validationErrors.Add(
                    $"Fact '{fact.Concept}' references unknown unitRef '{fact.UnitRef}'.");
        }

        var (periodStart, periodEnd) = ComputePeriodBounds(contexts);

        return new XbrlParseResult(
            Facts:            facts,
            Contexts:         contexts,
            Units:            units,
            TotalFacts:       facts.Count,
            UniqueConcepts:   facts.Select(f => f.Concept).Distinct(StringComparer.Ordinal).Count(),
            PeriodStart:      periodStart,
            PeriodEnd:        periodEnd,
            ValidationErrors: validationErrors);
    }

    private static IEnumerable<ParsedXbrlContext> ParseContexts(XElement root)
    {
        foreach (var ctx in root.Elements(Xbrli + "context"))
        {
            var id       = ctx.Attribute("id")?.Value ?? string.Empty;
            var entityId = ctx.Element(Xbrli + "entity")?.Element(Xbrli + "identifier")?.Value ?? string.Empty;

            var period   = ctx.Element(Xbrli + "period");
            var start    = ParseDate(period?.Element(Xbrli + "startDate")?.Value);
            var end      = ParseDate(period?.Element(Xbrli + "endDate")?.Value);
            var instant  = ParseDate(period?.Element(Xbrli + "instant")?.Value);

            yield return new ParsedXbrlContext(id, entityId, start, end, instant);
        }
    }

    private static IEnumerable<ParsedXbrlUnit> ParseUnits(XElement root)
    {
        foreach (var unit in root.Elements(Xbrli + "unit"))
        {
            var id      = unit.Attribute("id")?.Value ?? string.Empty;
            // Take the first <measure> we see — covers simple units and the numerator
            // side of <divide>; full unit composition is out of scope for Day 2.
            var measure = unit.Descendants(Xbrli + "measure").FirstOrDefault()?.Value ?? string.Empty;
            yield return new ParsedXbrlUnit(id, measure);
        }
    }

    private static IEnumerable<ParsedXbrlFact> ParseFacts(XElement root)
    {
        foreach (var element in root.Elements())
        {
            var contextRef = element.Attribute("contextRef")?.Value;
            if (contextRef is null) continue; // not a fact

            var prefix  = element.GetPrefixOfNamespace(element.Name.Namespace);
            var concept = string.IsNullOrEmpty(prefix)
                ? element.Name.LocalName
                : $"{prefix}:{element.Name.LocalName}";

            var unitRef  = element.Attribute("unitRef")?.Value;
            var value    = element.Value;
            int? dec     = null;
            if (int.TryParse(
                    element.Attribute("decimals")?.Value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var parsed))
            {
                dec = parsed;
            }

            yield return new ParsedXbrlFact(concept, contextRef, unitRef, value, dec);
        }
    }

    private static (DateTime? Start, DateTime? End) ComputePeriodBounds(IReadOnlyList<ParsedXbrlContext> contexts)
    {
        var startCandidates = contexts
            .Select(c => c.StartDate ?? c.Instant)
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .ToList();

        var endCandidates = contexts
            .Select(c => c.EndDate ?? c.Instant)
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .ToList();

        DateTime? start = startCandidates.Count == 0 ? null : startCandidates.Min();
        DateTime? end   = endCandidates.Count == 0   ? null : endCandidates.Max();
        return (start, end);
    }

    private static DateTime? ParseDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        return DateTime.TryParse(
            raw,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var parsed)
            ? parsed
            : null;
    }

    private static XbrlParseResult Empty(IReadOnlyList<string> errors) => new(
        Array.Empty<ParsedXbrlFact>(),
        Array.Empty<ParsedXbrlContext>(),
        Array.Empty<ParsedXbrlUnit>(),
        TotalFacts:     0,
        UniqueConcepts: 0,
        PeriodStart:    null,
        PeriodEnd:      null,
        ValidationErrors: errors);
}
