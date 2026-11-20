using NyxMonitor.Core.Models;

namespace NyxMonitor.App.ViewModels;

/// <summary>Base for every sidebar page. <see cref="Update"/> is always called on the UI thread.</summary>
public abstract class PageViewModel : ViewModelBase
{
    public abstract string Title { get; }

    public virtual void Update(SystemSnapshot snapshot)
    {
    }
}
