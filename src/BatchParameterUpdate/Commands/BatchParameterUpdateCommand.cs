using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BatchParameterUpdate.Models;
using BatchParameterUpdate.Services;

namespace BatchParameterUpdate.Commands;

[Transaction(TransactionMode.Manual)]
public class BatchParameterUpdateCommand : IExternalCommand
{
    public Result Execute(
        ExternalCommandData commandData,
        ref string message,
        ElementSet elements)
    {
        var uiDocument = commandData.Application.ActiveUIDocument;
        if (uiDocument is null)
        {
            message = "No active document.";
            return Result.Failed;
        }

        var document = uiDocument.Document;

        // The selection is captured once, when the command starts. Anything the
        // user selects afterwards is not part of this run.
        var selectedIds = uiDocument.Selection.GetElementIds().ToList();

        if (selectedIds.Count == 0)
        {
            TaskDialog.Show(
                "Batch Parameter Update",
                "Select one or more elements before running this command.");
            return Result.Cancelled;
        }

        // TODO: replace with values entered by the user in the input dialog.
        const string parameterName = "Comments";
        const string parameterValue = "Updated by add-in";

        BatchUpdateResult result;

        using (var transaction = new Transaction(document, "Batch Parameter Update"))
        {
            transaction.Start();

            try
            {
                var service = new ParameterUpdateService(document);
                result = service.UpdateElements(selectedIds, parameterName, parameterValue);
            }
            catch (Exception ex)
            {
                // The run could not proceed, so nothing is committed and the
                // model is left exactly as it was.
                transaction.RollBack();
                message = $"The operation failed and no changes were applied. {ex.Message}";
                return Result.Failed;
            }

            if (transaction.Commit() != TransactionStatus.Committed)
            {
                message = "The transaction could not be committed. No changes were applied.";
                return Result.Failed;
            }
        }

        ShowSummary(result);
        return Result.Succeeded;
    }

    private static void ShowSummary(BatchUpdateResult result)
    {
        var dialog = new TaskDialog("Batch Parameter Update")
        {
            MainInstruction = $"{result.UpdatedCount} updated, {result.SkippedCount} skipped."
        };

        if (result.SkippedCount > 0)
        {
            var reasons = result.Skipped
                .GroupBy(skipped => skipped.Reason)
                .Select(group => $"{Describe(group.Key)}: {group.Count()}");

            dialog.MainContent = string.Join(Environment.NewLine, reasons);

            var details = result.Skipped
                .Select(skipped => skipped.Details is null
                    ? $"{skipped.ElementName} - {Describe(skipped.Reason)}"
                    : $"{skipped.ElementName} - {Describe(skipped.Reason)} ({skipped.Details})");

            dialog.ExpandedContent = string.Join(Environment.NewLine, details);
        }

        dialog.Show();
    }

    private static string Describe(SkipReason reason) => reason switch
    {
        SkipReason.ParameterNotFound => "Parameter not found",
        SkipReason.AmbiguousParameterName => "Ambiguous parameter name",
        SkipReason.NotTextParameter => "Not a text parameter",
        SkipReason.ReadOnlyParameter => "Parameter is read-only",
        SkipReason.SetValueRejected => "Value was rejected",
        SkipReason.ApiException => "Revit API error",
        _ => "Unknown reason"
    };
}