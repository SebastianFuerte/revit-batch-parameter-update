using Autodesk.Revit.DB;

namespace BatchParameterUpdate.Models;

/// <summary>
/// Outcome of a batch parameter update run.
/// </summary>
public sealed class BatchUpdateResult
{
    private readonly List<ElementId> _updated = new();
    private readonly List<SkippedElement> _skipped = new();

    public IReadOnlyList<ElementId> Updated => _updated;

    public IReadOnlyList<SkippedElement> Skipped => _skipped;

    public int UpdatedCount => _updated.Count;

    public int SkippedCount => _skipped.Count;

    public void AddUpdated(ElementId elementId) => _updated.Add(elementId);

    public void AddSkipped(SkippedElement skipped) => _skipped.Add(skipped);
}