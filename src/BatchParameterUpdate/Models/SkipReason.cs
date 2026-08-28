namespace BatchParameterUpdate.Models;

/// <summary>
/// Reasons why an element could not be updated.
/// </summary>
public enum SkipReason
{
    /// <summary>The element does not have a parameter with the given name.</summary>
    ParameterNotFound,

    /// <summary>More than one parameter matches the given name on this element.</summary>
    AmbiguousParameterName,

    /// <summary>The parameter exists but does not store text.</summary>
    NotTextParameter,

    /// <summary>The parameter exists and stores text, but cannot be written to.</summary>
    ReadOnlyParameter,

    /// <summary>The Revit API rejected the write without raising an exception.</summary>
    SetValueRejected,

    /// <summary>The Revit API raised an exception while writing the value.</summary>
    ApiException
}