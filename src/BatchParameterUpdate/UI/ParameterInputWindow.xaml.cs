using System.Windows;

namespace BatchParameterUpdate.UI;

/// <summary>
/// Collects the parameter name and the new value from the user.
/// </summary>
public partial class ParameterInputWindow : Window
{
    public ParameterInputWindow()
    {
        InitializeComponent();
        ParameterNameTextBox.Focus();
    }

    /// <summary>The parameter name entered by the user, trimmed.</summary>
    public string ParameterName { get; private set; } = string.Empty;

    /// <summary>The value entered by the user. Not trimmed: trailing spaces may be intentional.</summary>
    public string ParameterValue { get; private set; } = string.Empty;

    private void OnUpdateClick(object sender, RoutedEventArgs e)
    {
        var name = ParameterNameTextBox.Text.Trim();

        if (string.IsNullOrEmpty(name))
        {
            ShowValidationMessage("Enter a parameter name.");
            ParameterNameTextBox.Focus();
            return;
        }

        ParameterName = name;
        ParameterValue = ParameterValueTextBox.Text;

        DialogResult = true;
    }

    private void ShowValidationMessage(string text)
    {
        ValidationTextBlock.Text = text;
        ValidationTextBlock.Visibility = Visibility.Visible;
    }
}