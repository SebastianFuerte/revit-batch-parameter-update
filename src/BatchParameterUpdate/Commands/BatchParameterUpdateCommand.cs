using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace BatchParameterUpdate.Commands;

[Transaction(TransactionMode.Manual)]
public class BatchParameterUpdateCommand : IExternalCommand
{
    public Result Execute(
        ExternalCommandData commandData,
        ref string message,
        ElementSet elements)
    {
        TaskDialog.Show("Batch Parameter Update", "The add-in loaded correctly.");
        return Result.Succeeded;
    }
}