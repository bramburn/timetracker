using System;
using System.Windows.Forms;

namespace TimeTracker.DesktopApp;

/// <summary>
/// Simple test form to verify Windows Forms functionality
/// </summary>
public partial class TestForm : Form
{
    public TestForm()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "TimeTracker Test Form";
        Size = new Size(400, 300);
        StartPosition = FormStartPosition.CenterScreen;

        var label = new Label
        {
            Text = "TimeTracker Phase 1 Test",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill
        };

        Controls.Add(label);
    }
}

/// <summary>
/// Simple test program to verify basic functionality
/// </summary>
public static class TestProgram
{
    [STAThread]
    public static void TestMain()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        
        using var form = new TestForm();
        Application.Run(form);
    }
}
