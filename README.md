# Batch Parameter Update

A Revit add-in that performs a batch update of a text instance parameter on the
elements currently selected in the active document. Elements that cannot be
updated are skipped without interrupting the run, and a summary reports how many
elements were updated, how many were skipped, and why.

## Supported Revit versions

Revit 2026 (all point releases).

The add-in is built against the Revit 2026.4 API (Nice3point.Revit.Api.* version
2026.4.10), pinned for reproducible builds. It only uses API surface that has
been stable across Revit releases, so it runs on any Revit 2026 point release.
Revit assemblies are reference-only and are not deployed with the add-in; at
runtime the assemblies already loaded by the host process are used.

Revit 2027 is **not** supported: it runs on .NET 10 and changed how all-users
add-ins are discovered.

## Prerequisites

**To use the add-in:**

- Autodesk Revit 2026

No separate .NET runtime is required. Revit 2026 already runs on .NET 8, which
is the framework the add-in targets.

**To build from source:**

- Visual Studio 2026 (the solution uses the .slnx format)
- .NET 8 SDK

Revit does **not** need to be installed to build. The Revit API is referenced
through NuGet packages.

**To build the installer:**

- [Inno Setup 6](https://jrsoftware.org/isdl.php)

## Install

Download the installer from the [latest release](../../releases/latest) and run it.

The installer is **per-user** and does not require administrator rights. It
deploys to:

    %APPDATA%\Autodesk\Revit\Addins\2026\
    ├── BatchParameterUpdate.addin
    └── BatchParameterUpdate\
        └── BatchParameterUpdate.dll

If Revit 2026 is not detected on the machine, the installer warns but allows the
installation to continue.

Restart Revit after installing. Revit only reads add-in manifests at startup.

To uninstall, use Windows Settings > Apps, or the Start menu entry for
**Batch Parameter Update**.

## Usage

1. Select one or more elements in the active Revit document.
2. Go to **Add-Ins > External Tools > Batch Parameter Update**.
3. Enter the parameter name and the new value.
4. Click **Update**.
5. A summary reports how many elements were updated and how many were skipped,
   grouped by reason. Expand the details to see each skipped element.

The command operates only on the active document and only on the elements that
were selected when the command started.

## Build

    git clone https://github.com/SebastianFuerte/revit-batch-parameter-update.git
    cd revit-batch-parameter-update
    dotnet build -c Release

Or open revit-batch-parameter-update.slnx in Visual Studio 2026 and build.

In **Debug** configuration a post-build target deploys the add-in to your local
Revit 2026 add-ins folder, so you can press F5 and test immediately. This target
does not run in Release.

To debug against Revit, create a launch profile of type **Executable** pointing
to Revit.exe. The profile file is machine-specific and is not committed.

### Build the installer

1. Build the solution in **Release** configuration.
2. Open installer/BatchParameterUpdate.iss in Inno Setup and compile (F9).
3. The installer is written to installer/Output/.

## Design decisions

**Binaries live in a subfolder.** The manifest sits in the add-ins root and the
DLL in BatchParameterUpdate\. Revit loads all add-ins into the same AppDomain,
so third-party dependencies dropped flat in the add-ins root can collide with
other add-ins that ship a different version of the same library. Isolating
binaries avoids that class of failure.

**GetParameters instead of LookupParameter.** LookupParameter returns the first
match and gives no indication that others exist. An element can carry a project
parameter and a shared parameter with the same name. Rather than pick one
silently, the add-in detects the ambiguity and skips the element with a clear
reason. This was verified in a real model: the name Category matched two
parameters on a wall.

**Validation order.** Existence, then ambiguity, then storage type, then
writability, then the write itself. Each check returns early, so Parameter.Set
is never reached for a parameter that should not be written.

**Two levels of exception handling.** The service catches
Autodesk.Revit.Exceptions.ApplicationException per element, so one failure
becomes a skip rather than aborting the batch. The command catches at the
transaction level for failures that prevent the run from proceeding at all.

**A single transaction wraps the whole batch.** If the run cannot proceed, the
transaction is rolled back and the model is left exactly as it was. The result of
Commit() is checked rather than assumed.

**The dialog is owned by the Revit main window.** Without setting the owner via
WindowInteropHelper, a WPF dialog can be sent behind the Revit window, which
looks like a frozen application.

**Code-behind instead of MVVM.** The dialog has two fields and one button. MVVM
would add indirection without benefit at this scope.

**Per-user installation.** Installing to %APPDATA% requires no administrator
rights, which removes a failure mode for evaluators. A per-machine variant would
target %PROGRAMDATA% and require elevation.

**Inno Setup rather than WiX.** The script is a single readable text file that
lives in the repository, and the toolchain is free and widely used in the Revit
ecosystem. WiX produces MSIs and is a reasonable alternative, but its setup cost
was not justified at this scope.

## Assumptions and limitations

- Only **instance** parameters are considered. Type parameters are out of scope,
  matching the assignment.
- Only parameters with StorageType.String are updated. Numeric, ElementId, and
  integer parameters are reported as skipped.
- An **empty value is valid** and clears the parameter. Only the parameter name
  is required. Clearing a parameter is a legitimate operation, and the assignment
  lists an empty parameter name, not an empty value, as an invalid case.
- Parameter names are matched **case-sensitively**, which is how the Revit API
  behaves.
- The parameter name is trimmed; the value is not, since trailing whitespace may
  be intentional.
- Elements inside groups may reject instance parameter changes. Such failures are
  reported as skipped, not as crashes.
- In workshared models, elements owned or borrowed by another user cannot be
  edited. These are reported as skipped.
- The add-in is exposed under **External Tools**. A ribbon panel was considered
  but omitted to keep the scope aligned with the assignment.

## Verified behaviour

The following was exercised against a real Revit 2026 model:

| Case | How it was triggered |
|---|---|
| Successful update | Comments on walls |
| Parameter not found | Comments on model lines |
| Not a text parameter (Double) | Area on walls |
| Not a text parameter (Integer) | Structural Usage on walls |
| Not a text parameter (ElementId) | Base Constraint on walls |
| Ambiguous parameter name | Category matched two parameters |
| Read-only parameter | Family Name |
| Empty selection | running the command with nothing selected |
| Empty parameter name | submitting the dialog with a blank name |

Two skip reasons are implemented but were not reproduced in a model:
SetValueRejected (when Parameter.Set returns false without throwing) and
ApiException (when the Revit API throws while writing). Both are defensive paths
that are difficult to trigger deliberately.

## Repository layout

    ├── installer/
    │   └── BatchParameterUpdate.iss     Inno Setup script
    ├── src/
    │   └── BatchParameterUpdate/
    │       ├── Commands/                IExternalCommand entry point
    │       ├── Models/                  Result and skip reason types
    │       ├── Resources/               .addin manifest
    │       ├── Services/                Parameter update logic
    │       └── UI/                      WPF input dialog
    └── revit-batch-parameter-update.slnx