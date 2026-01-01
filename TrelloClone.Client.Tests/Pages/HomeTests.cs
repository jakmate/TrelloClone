using System.Security.Claims;

using Bunit;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

using Moq;

using TrelloClone.Client.Pages;

using Xunit;

namespace TrelloClone.Client.Tests.Pages;

public class HomeTests : BunitContext
{
    private readonly Mock<AuthenticationStateProvider> _mockAuthStateProvider;

    public HomeTests()
    {
        _mockAuthStateProvider = new Mock<AuthenticationStateProvider>();
        Services.AddSingleton(_mockAuthStateProvider.Object);
    }

    private void SetupAuthenticatedUser(bool isAuthenticated)
    {
        var identity = isAuthenticated
            ? new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "test") }, "test")
            : new ClaimsIdentity();

        var user = new ClaimsPrincipal(identity);
        var authState = Task.FromResult(new AuthenticationState(user));
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync()).Returns(authState);
    }

    [Fact]
    public void Home_UnauthenticatedUser_ShowsGetStartedAndSignIn()
    {
        // Arrange
        SetupAuthenticatedUser(false);

        // Act
        var cut = Render<Home>();

        // Assert
        Assert.Contains("Get Started", cut.Markup);
        Assert.Contains("Sign In", cut.Markup);
        Assert.DoesNotContain("Go to Your Boards", cut.Markup);
    }

    [Fact]
    public void Home_AuthenticatedUser_ShowsGoToBoards()
    {
        // Arrange
        SetupAuthenticatedUser(true);

        // Act
        var cut = Render<Home>();

        // Assert
        Assert.Contains("Go to Your Boards", cut.Markup);
        Assert.DoesNotContain("Get Started", cut.Markup);
        Assert.DoesNotContain("Sign In", cut.Markup);
    }

    [Fact]
    public void Home_RendersTitle()
    {
        // Arrange
        SetupAuthenticatedUser(false);

        // Act
        var cut = Render<Home>();

        // Assert
        Assert.Contains("Trello Clone", cut.Markup);
    }

    [Fact]
    public void Home_RendersFeatures()
    {
        // Arrange
        SetupAuthenticatedUser(false);

        // Act
        var cut = Render<Home>();

        // Assert
        Assert.Contains("Key Features", cut.Markup);
        Assert.Contains("Boards Management", cut.Markup);
        Assert.Contains("Drag & Drop Tasks", cut.Markup);
        Assert.Contains("Real-time Updates", cut.Markup);
    }

    [Fact]
    public void Home_RendersTechStack()
    {
        // Arrange
        SetupAuthenticatedUser(false);

        // Act
        var cut = Render<Home>();

        // Assert
        Assert.Contains("Technology Stack", cut.Markup);
        Assert.Contains("Blazor WebAssembly", cut.Markup);
        Assert.Contains("ASP.NET Core Web API", cut.Markup);
        Assert.Contains("Clean Architecture Pattern", cut.Markup);
    }

    [Fact]
    public void Home_RendersPreviewBoard()
    {
        // Arrange
        SetupAuthenticatedUser(false);

        // Act
        var cut = Render<Home>();

        // Assert
        Assert.Contains("To Do", cut.Markup);
        Assert.Contains("In Progress", cut.Markup);
        Assert.Contains("Done", cut.Markup);
    }
}
