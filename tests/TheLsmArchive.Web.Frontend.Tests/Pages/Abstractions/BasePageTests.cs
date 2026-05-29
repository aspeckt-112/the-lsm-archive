using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

using MudBlazor;

using TheLsmArchive.ApiClient;
using TheLsmArchive.Web.Frontend.Pages.Abstractions;
using TheLsmArchive.Web.Frontend.Tests.TestSupport;

namespace TheLsmArchive.Web.Frontend.Tests.Pages.Abstractions;

public sealed class BasePageTests : FrontendComponentTestContext
{
    [Fact]
    public void HandleResult_WhenSuccess_ExecutesSuccessCallback()
    {
        // Arrange
        RenderProviders();

        IRenderedComponent<TestBasePage> component = RenderComponent<TestBasePage>(parameters =>
            parameters.Add(x => x.Result, Result<string>.Ok("Loaded")));

        // Act
        component.Find("button.run-handle-result").Click();

        // Assert
        Equal("Loaded", component.Instance.SuccessValue);
        False(component.Instance.NoContentCalled);
        Null(component.Instance.FailureValue);
    }

    [Fact]
    public void HandleResult_WhenNoContent_ExecutesNoContentCallback()
    {
        // Arrange
        RenderProviders();

        IRenderedComponent<TestBasePage> component = RenderComponent<TestBasePage>(parameters =>
            parameters.Add(x => x.Result, Result<string>.None()));

        // Act
        component.Find("button.run-handle-result").Click();

        // Assert
        True(component.Instance.NoContentCalled);
        Null(component.Instance.SuccessValue);
        Null(component.Instance.FailureValue);
    }

    [Fact]
    public void HandleResult_WhenFailureWithoutHandler_ShowsSnackbarError()
    {
        // Arrange
        RenderProviders();

        IRenderedComponent<TestBasePage> component = RenderComponent<TestBasePage>(parameters =>
            parameters
                .Add(x => x.Result, Result<string>.Fail("Search failed."))
                .Add(x => x.UseFailureHandler, false));

        // Act
        component.Find("button.run-handle-result").Click();

        // Assert
        component.WaitForAssertion(() => Equal("Search failed.", Snackbar.ShownSnackbars.Single().Message));
        Null(component.Instance.SuccessValue);
        False(component.Instance.NoContentCalled);
        Null(component.Instance.FailureValue);
    }

    [Fact]
    public void HandleResult_WhenFailureWithHandler_ExecutesFailureCallback()
    {
        // Arrange
        RenderProviders();

        IRenderedComponent<TestBasePage> component = RenderComponent<TestBasePage>(parameters =>
            parameters
                .Add(x => x.Result, Result<string>.Fail("Patreon unavailable."))
                .Add(x => x.UseFailureHandler, true));

        // Act
        component.Find("button.run-handle-result").Click();

        // Assert
        Equal("Patreon unavailable.", component.Instance.FailureValue);
        False(component.Instance.NoContentCalled);
        Null(component.Instance.SuccessValue);
    }

    private sealed class TestBasePage : BasePage
    {
        [Parameter]
        public Result<string> Result { get; set; } = Result<string>.None();

        [Parameter]
        public bool UseFailureHandler { get; set; }

        public string? SuccessValue { get; private set; }

        public bool NoContentCalled { get; private set; }

        public string? FailureValue { get; private set; }

        private async Task ExecuteAsync()
        {
            await HandleResult(
                Result,
                onSuccess: value =>
                {
                    SuccessValue = value;
                    return Task.CompletedTask;
                },
                onNoContent: () =>
                {
                    NoContentCalled = true;
                    return Task.CompletedTask;
                },
                onFailure: UseFailureHandler
                    ? message =>
                    {
                        FailureValue = message;
                        return Task.CompletedTask;
                    }
                    : null);
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "class", "run-handle-result");
            builder.AddAttribute(2, "onclick", EventCallback.Factory.Create(this, ExecuteAsync));
            builder.AddContent(3, "Run");
            builder.CloseElement();
        }
    }
}





