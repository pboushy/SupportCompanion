using Avalonia;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using SupportCompanion.ViewModels;

namespace SupportCompanion.Views;

public partial class MdmStatusWidget : UserControl
{
    public MdmStatusWidget()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        DataContext = ((App)Application.Current).ServiceProvider.GetRequiredService<MdmStatusViewModel>();

        var window = VisualRoot as Window;
        if (window != null) window.Closed += Window_Closed;
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        if (DataContext is MdmStatusViewModel viewModel) viewModel.Dispose();
        DataContext = null;

        var window = sender as Window;
        if (window != null) window.Closed -= Window_Closed;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        var window = VisualRoot as Window;
        if (window != null) window.Closed -= Window_Closed;
    }
}