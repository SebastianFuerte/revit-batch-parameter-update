using Autodesk.Revit.DB;
using BatchParameterUpdate.Models;

namespace BatchParameterUpdate.Services;

/// <summary>
/// Applies a text value to a named instance parameter across a set of elements.
/// This service does not manage transactions; the caller is responsible for
/// opening and committing one.
/// </summary>
public sealed class ParameterUpdateService
{
    private readonly Document _document;

    public ParameterUpdateService(Document document)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
    }

    /// <summary>
    /// Attempts to set <paramref name="value"/> on the parameter named
    /// <paramref name="parameterName"/> for each element in <paramref name="elementIds"/>.
    /// Elements that cannot be updated are skipped and reported.
    /// </summary>
    public BatchUpdateResult UpdateElements(
        IEnumerable<ElementId> elementIds,
        string parameterName,
        string value)
    {
        var result = new BatchUpdateResult();

        foreach (var elementId in elementIds)
        {
            var element = _document.GetElement(elementId);
            if (element is null)
            {
                continue;
            }

            UpdateSingleElement(element, parameterName, value, result);
        }

        return result;
    }

    private static void UpdateSingleElement(
        Element element,
        string parameterName,
        string value,
        BatchUpdateResult result)
    {
        var elementName = DescribeElement(element);

        // GetParameters returns every match, unlike LookupParameter which
        // silently returns the first one. A project parameter and a shared
        // parameter can share a name on the same element.
        var matches = element.GetParameters(parameterName);

        if (matches.Count == 0)
        {
            result.AddSkipped(new SkippedElement(
                element.Id, elementName, SkipReason.ParameterNotFound));
            return;
        }

        if (matches.Count > 1)
        {
            result.AddSkipped(new SkippedElement(
                element.Id, elementName, SkipReason.AmbiguousParameterName,
                $"{matches.Count} parameters share this name."));
            return;
        }

        var parameter = matches[0];

        if (parameter.StorageType != StorageType.String)
        {
            result.AddSkipped(new SkippedElement(
                element.Id, elementName, SkipReason.NotTextParameter,
                $"Storage type is {parameter.StorageType}."));
            return;
        }

        if (parameter.IsReadOnly)
        {
            result.AddSkipped(new SkippedElement(
                element.Id, elementName, SkipReason.ReadOnlyParameter));
            return;
        }

        try
        {
            if (parameter.Set(value))
            {
                result.AddUpdated(element.Id);
            }
            else
            {
                result.AddSkipped(new SkippedElement(
                    element.Id, elementName, SkipReason.SetValueRejected));
            }
        }
        catch (Autodesk.Revit.Exceptions.ApplicationException ex)
        {
            // Catching per element keeps one failure from aborting the whole run,
            // as required by the assignment.
            result.AddSkipped(new SkippedElement(
                element.Id, elementName, SkipReason.ApiException, ex.Message));
        }
    }

    private static string DescribeElement(Element element)
    {
        var categoryName = element.Category?.Name ?? "No category";
        return $"{categoryName}: {element.Name} [{element.Id}]";
    }
}