using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

using Moq;

using TrelloClone.Client.Pages;
using TrelloClone.Client.Services;
using TrelloClone.Shared.DTOs.Auth;

using Xunit;

namespace TrelloClone.Client.Tests.Pages;

public class LoginTests : BunitContext
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly Mock<IJSRuntime> _mockJSRuntime;

    public LoginTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        _mockJSRuntime = new Mock<IJSRuntime>();

        Services.AddSingleton(_mockAuthService.Object);
        Services.AddSingleton<IJSRuntime>(_mockJSRuntime.Object);
    }

    [Fact]
    public void Login_RendersCorrectly()
    {
        // Arrange
        _mockAuthService.Setup(x => x.IsAuthenticatedAsync()).ReturnsAsync(false);

        // Act
        var cut = Render<Login>();

        // Assert
        Assert.Contains("Log in to your account", cut.Markup);
        Assert.Contains("Email", cut.Markup);
        Assert.Contains("Password", cut.Markup);
    }

    [Fact]
    public async Task Login_AlreadyAuthenticated_RedirectsToBoards()
    {
        // Arrange
        _mockAuthService.Setup(x => x.IsAuthenticatedAsync()).ReturnsAsync(true);

        // Act
        Render<Login>();
        await Task.Delay(100);

        // Assert
        var navMan = Services.GetRequiredService<NavigationManager>();
        Assert.EndsWith("/boards", navMan.Uri);
    }

    [Fact]
    public async Task HandleLogin_ValidCredentials_RedirectsToBoards()
    {
        // Arrange
        _mockAuthService.Setup(x => x.IsAuthenticatedAsync()).ReturnsAsync(false);
        _mockAuthService.Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
            .ReturnsAsync(new AuthResponse { Token = "token" });

        var cut = Render<Login>();
        var emailInput = cut.Find("#email");
        var passwordInput = cut.Find("#password");
        var form = cut.Find("form");

        // Act
        await emailInput.ChangeAsync("test@test.com");
        await passwordInput.ChangeAsync("password123");
        await form.SubmitAsync();

        // Assert
        var navMan = Services.GetRequiredService<NavigationManager>();
        Assert.EndsWith("/boards", navMan.Uri);
    }

    [Fact]
    public async Task HandleLogin_InvalidCredentials_ShowsError()
    {
        // Arrange
        _mockAuthService.Setup(x => x.IsAuthenticatedAsync()).ReturnsAsync(false);
        _mockAuthService.Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
            .ThrowsAsync(new UnauthorizedAccessException());

        var cut = Render<Login>();
        var emailInput = cut.Find("#email");
        var passwordInput = cut.Find("#password");
        var form = cut.Find("form");

        // Act
        await emailInput.ChangeAsync("test@test.com");
        await passwordInput.ChangeAsync("password123");
        await form.SubmitAsync();
        await cut.WaitForStateAsync(() => cut.Markup.Contains("Invalid email or password"));

        // Assert
        Assert.Contains("Invalid email or password", cut.Markup);
    }

    [Fact]
    public async Task HandleLogin_GeneralException_ShowsErrorMessage()
    {
        // Arrange
        _mockAuthService.Setup(x => x.IsAuthenticatedAsync()).ReturnsAsync(false);
        _mockAuthService.Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
            .ThrowsAsync(new Exception("Network error"));

        var cut = Render<Login>();
        var emailInput = cut.Find("#email");
        var passwordInput = cut.Find("#password");
        var form = cut.Find("form");

        // Act
        await emailInput.ChangeAsync("test@test.com");
        await passwordInput.ChangeAsync("password123");
        await form.SubmitAsync();
        await cut.WaitForStateAsync(() => cut.Markup.Contains("Network error"));

        // Assert
        Assert.Contains("An error occurred: Network error", cut.Markup);
    }

    [Fact]
    public async Task HandleLogin_WhileLoading_ShowsSpinner()
    {
        // Arrange
        var tcs = new TaskCompletionSource<AuthResponse>();
        _mockAuthService.Setup(x => x.IsAuthenticatedAsync()).ReturnsAsync(false);
        _mockAuthService.Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
            .Returns(tcs.Task);

        var cut = Render<Login>();
        var emailInput = cut.Find("#email");
        var passwordInput = cut.Find("#password");
        var form = cut.Find("form");

        // Act
        await emailInput.ChangeAsync("test@test.com");
        await passwordInput.ChangeAsync("password123");
        var submitTask = form.SubmitAsync();

        // Assert - spinner should be visible while loading
        await cut.WaitForStateAsync(() => cut.Markup.Contains("spinner"));
        Assert.Contains("spinner", cut.Markup);

        // Complete the task
        tcs.SetResult(new AuthResponse { Token = "token" });
        await submitTask;
    }

    [Fact]
    public void Login_ShowsSignUpLink()
    {
        // Arrange
        _mockAuthService.Setup(x => x.IsAuthenticatedAsync()).ReturnsAsync(false);

        // Act
        var cut = Render<Login>();

        // Assert
        Assert.Contains("Don't have an account?", cut.Markup);
        Assert.Contains("/register", cut.Markup);
    }
}
