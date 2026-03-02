using MudBlazor;

namespace TheLsmArchive.Web.Frontend.Services;

/// <summary>
/// Provides breadcrumb management with navigation history.
/// </summary>
public interface IBreadcrumbService
{
    /// <summary>
    /// Occurs when the breadcrumbs change.
    /// </summary>
    public event EventHandler? OnBreadcrumbsChanged;

    /// <summary>
    /// Gets the current breadcrumbs.
    /// </summary>
    public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; }

    /// <summary>
    /// Pushes a new breadcrumb onto the trail.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="href">The href.</param>
    /// <param name="icon">The optional icon.</param>
    public void Push(string text, string href, string? icon = null);

    /// <summary>
    /// Replaces the last breadcrumb's text and icon.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="icon">The optional icon.</param>
    public void ReplaceLast(string text, string? icon = null);

    /// <summary>
    /// Resets the breadcrumbs to home.
    /// </summary>
    public void Reset();
}

/// <summary>
/// The breadcrumb service implementation.
/// </summary>
public class BreadcrumbService : IBreadcrumbService
{
    private readonly List<BreadcrumbItem> _items = [];

    /// <inheritdoc />
    public event EventHandler? OnBreadcrumbsChanged;

    /// <inheritdoc />
    public IReadOnlyList<BreadcrumbItem> Breadcrumbs => _items.AsReadOnly();

    public BreadcrumbService()
    {
        Reset();
    }

    /// <inheritdoc />
    public void Push(string text, string href, string? icon = null)
    {
        // If we're going home, reset everything
        if (href == "/" || href == "" || text.Equals("Home", StringComparison.OrdinalIgnoreCase))
        {
            Reset();
            return;
        }

        // Check if this href is already in our trail
        int existingIndex = _items.FindIndex(x => x.Href == href);
        if (existingIndex != -1)
        {
            // Truncate the trail to this point
            _items.RemoveRange(existingIndex + 1, _items.Count - (existingIndex + 1));
        }
        else
        {
            // Add new item
            _items.Add(new BreadcrumbItem(text, href, icon: icon));
        }

        NotifyChanged();
    }

    /// <inheritdoc />
    public void ReplaceLast(string text, string? icon = null)
    {
        if (_items.Count == 0) return;

        BreadcrumbItem last = _items[^1];
        _items[^1] = new BreadcrumbItem(text, last.Href, last.Disabled, icon: icon ?? last.Icon);
        NotifyChanged();
    }

    /// <inheritdoc />
    public void Reset()
    {
        _items.Clear();
        _items.Add(new BreadcrumbItem("Home", href: "/", icon: Icons.Material.Filled.Home));
        NotifyChanged();
    }

    private void NotifyChanged() => OnBreadcrumbsChanged?.Invoke(this, EventArgs.Empty);
}
