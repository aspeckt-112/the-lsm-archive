using Microsoft.AspNetCore.Components;

using MudBlazor;

using TheLsmArchive.ApiClient;

namespace TheLsmArchive.Web.Frontend.Pages.Abstractions;

/// <summary>
/// The base page component.
/// </summary>
public abstract class BasePage : ComponentBase
{
    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    /// <summary>
    /// Handles the specified result.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <param name="onSuccess">The success action.</param>
    /// <param name="onNoContent">The no content action.</param>
    /// <param name="onFailure">The failure action.</param>
    /// <typeparam name="T">The result data type.</typeparam>
    /// <exception cref="InvalidOperationException">Thrown when the result type is unknown.</exception>
    protected async Task HandleResult<T>(
        Result<T> result,
        Func<T, Task> onSuccess,
        Func<Task> onNoContent,
        Func<string, Task>? onFailure = null)
    {
        switch (result)
        {
            case Result<T>.Success success:
                await onSuccess(success.Data);
                break;
            case Result<T>.NoContent:
                await onNoContent();
                break;
            case Result<T>.Failure failure:
                if (onFailure is not null)
                {
                    await onFailure(failure.Message);
                }
                else
                {
                    ShowErrorMessage(failure.Message);
                }

                break;
            default:
                throw new InvalidOperationException("Unknown result type.");
        }

        await Task.CompletedTask;
    }

    private void ShowErrorMessage(string message) => Snackbar.Add(message, Severity.Error);
}
