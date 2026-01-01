using Bunit;
using Bunit.TestDoubles;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using Moq;

using TrelloClone.Client.Components;
using TrelloClone.Client.Layouts;
using TrelloClone.Client.Services;

using Xunit;

namespace TrelloClone.Client.Tests.Layout;

public class MainLayoutTests : BunitContext
{
    private readonly Mock<IAuthService> _mockAuthService;

    public MainLayoutTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        Services.AddSingleton(_mockAuthService.Object);

        ComponentFactories.AddStub<Notifications>();
        ComponentFactories.AddStub<NavMenu>();
    }

    [Fact]
    public void MainLayout_AuthenticatedUser_ShowsUserMenu()
    {
        // Arrange
        var authContext = this.AddAuthorization();
        authContext.SetAuthorized("John Doe");

        // Act
        var cut = Render<MainLayout>();

        // Assert
        Assert.Contains("John Doe", cut.Markup);
        Assert.Contains("Sign out", cut.Markup);
    }

    [Fact]
    public void MainLayout_UnauthenticatedUser_ShowsAuthButtons()
    {
        // Arrange
        var authContext = this.AddAuthorization();
        authContext.SetNotAuthorized();

        // Act
        var cut = Render<MainLayout>();

        // Assert
        Assert.Contains("Log in", cut.Markup);
        Assert.Contains("Sign up", cut.Markup);
    }

    [Fact]
    public void ToggleSidebar_TogglesCollapsedClass()
    {
        // Arrange
        this.AddAuthorization().SetNotAuthorized();
        var cut = Render<MainLayout>();
        var toggleButton = cut.Find(".sidebar-toggle");

        // Act
        Assert.Contains("collapsed", cut.Markup);
        toggleButton.Click();
        Assert.DoesNotContain("collapsed", cut.Markup);
        toggleButton.Click();
        Assert.Contains("collapsed", cut.Markup);
    }

    [Fact]
    public async Task Logout_CallsAuthServiceAndNavigates()
    {
        // Arrange
        this.AddAuthorization().SetAuthorized("testuser");
        _mockAuthService.Setup(x => x.LogoutAsync()).Returns(Task.CompletedTask);

        var cut = Render<MainLayout>();
        var logoutButton = cut.Find(".logout-btn");

        // Act
        await logoutButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        _mockAuthService.Verify(x => x.LogoutAsync(), Times.Once);
        var navMan = Services.GetRequiredService<NavigationManager>();
        Assert.EndsWith("/", navMan.Uri);
    }

    [Fact]
    public void MainLayout_RendersBrandLogo()
    {
        // Arrange
        this.AddAuthorization().SetNotAuthorized();

        // Act
        var cut = Render<MainLayout>();

        // Assert
        Assert.Contains("Trello", cut.Markup);
        Assert.Contains("bi-kanban", cut.Markup);
    }
}
