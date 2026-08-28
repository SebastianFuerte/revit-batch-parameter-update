using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BatchParameterUpdate.Models;
using BatchParameterUpdate.Services;
using BatchParameterUpdate.UI;
using System.Windows.Interop;

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
            var emptyDialog = new TaskDialog("Batch Parameter Update")
            {
                TitleAutoPrefix = false,
                MainInstruction = "Select one or more elements before running this command."
            };
            emptyDialog.Show();
            return Result.Cancelled;
        }

        var inputWindow = new ParameterInputWindow();

        // Setting Revit's main window as the owner keeps the dialog modal to Revit
        // and prevents it from being sent behind the main window.
        new WindowInteropHelper(inputWindow).Owner = commandData.Application.MainWindowHandle;

        if (inputWindow.ShowDialog() != true)
        {
            return Result.Cancelled;
        }

        var parameterName = inputWindow.ParameterName;
        var parameterValue = inputWindow.ParameterValue;

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
            TitleAutoPrefix = false,
            MainInstruction = $"{result.UpdatedCount} updated, {result.SkippedCount} skipped."
        };

        if (result.SkippedCount > 0)
        {
            var reasonGroups = result.Skipped
                .GroupBy(skipped => skipped.Reason)
                .OrderByDescending(group => group.Count())
                .ToList();

            dialog.MainContent = reasonGroups.Count == 1
                ? $"All skipped elements: {Describe(reasonGroups[0].Key).ToLowerInvariant()}."
                : string.Join(
                    Environment.NewLine,
                    reasonGroups.Select(group => $"{Describe(group.Key)}: {group.Count()}"));
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