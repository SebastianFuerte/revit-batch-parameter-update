using Autodesk.Revit.DB;

namespace BatchParameterUpdate.Models;

/// <summary>
/// An element that was not updated, and why.
/// </summary>
public sealed class SkippedElement
{
    public SkippedElement(ElementId elementId, string elementName, SkipReason reason, string? details = null)
    {
        ElementId = elementId;
        ElementName = elementName;
        Reason = reason;
        Details = details;
    }

    public ElementId ElementId { get; }

    public string ElementName { get; }

    public SkipReason Reason { get; }

    /// <summary>Extra context, such as an exception message. Optional.</summary>
    public string? Details { get; }
}