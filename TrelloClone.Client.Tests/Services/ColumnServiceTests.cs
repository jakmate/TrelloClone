using System.Net;
using System.Net.Http.Json;

using Moq;
using Moq.Protected;

using TrelloClone.Client.Services;
using TrelloClone.Shared.DTOs.Column;

using Xunit;

namespace TrelloClone.Client.Tests.Services;

public class ColumnServiceTests
{
    private readonly Mock<HttpMessageHandler> _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly ColumnService _service;

    public ColumnServiceTests()
    {
        _mockHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHandler.Object) { BaseAddress = new Uri("http://localhost") };
        _service = new ColumnService(_httpClient);
    }

    private void SetupHttpResponse(HttpStatusCode statusCode, object? content = null)
    {
        var response = new HttpResponseMessage(statusCode);
        if (content != null)
        {
            response.Content = JsonContent.Create(content);
        }
        else
        {
            response.Content = new StringContent("null", System.Text.Encoding.UTF8, "application/json");
        }

        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    [Fact]
    public async Task GetColumnsForBoardAsync_ReturnsColumnList()
    {
        var columns = new List<ColumnDto> { new ColumnDto { Id = Guid.NewGuid() } };
        SetupHttpResponse(HttpStatusCode.OK, columns);

        var result = await _service.GetColumnsForBoardAsync(Guid.NewGuid());

        Assert.Single(result);
    }

    [Fact]
    public async Task GetColumnsForBoardAsync_NullResponse_ReturnsEmptyList()
    {
        SetupHttpResponse(HttpStatusCode.OK, null);

        var result = await _service.GetColumnsForBoardAsync(Guid.NewGuid());

        Assert.Empty(result);
    }

    [Fact]
    public async Task CreateColumnAsync_ValidRequest_ReturnsColumn()
    {
        var column = new ColumnDto { Id = Guid.NewGuid() };
        SetupHttpResponse(HttpStatusCode.OK, column);

        var result = await _service.CreateColumnAsync(new CreateColumnRequest { BoardId = Guid.NewGuid() });

        Assert.Equal(column.Id, result.Id);
    }

    [Fact]
    public async Task CreateColumnAsync_NullResponse_ThrowsInvalidOperationException()
    {
        SetupHttpResponse(HttpStatusCode.OK, null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateColumnAsync(new CreateColumnRequest { BoardId = Guid.NewGuid() }));
    }

    [Fact]
    public async Task UpdateColumnAsync_ValidRequest_ReturnsColumn()
    {
        var column = new ColumnDto { Id = Guid.NewGuid() };
        SetupHttpResponse(HttpStatusCode.OK, column);

        var result = await _service.UpdateColumnAsync(Guid.NewGuid(), Guid.NewGuid(), new UpdateColumnRequest());

        Assert.Equal(column.Id, result.Id);
    }

    [Fact]
    public async Task UpdateColumnAsync_FailedResponse_ThrowsHttpRequestException()
    {
        SetupHttpResponse(HttpStatusCode.BadRequest);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            _service.UpdateColumnAsync(Guid.NewGuid(), Guid.NewGuid(), new UpdateColumnRequest()));
    }

    [Fact]
    public async Task DeleteColumnAsync_SuccessfulResponse_Completes()
    {
        SetupHttpResponse(HttpStatusCode.NoContent);

        await _service.DeleteColumnAsync(Guid.NewGuid(), Guid.NewGuid());

        _mockHandler.Protected().Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ReorderColumnsAsync_SuccessfulResponse_ReturnsTrue()
    {
        SetupHttpResponse(HttpStatusCode.OK);

        var result = await _service.ReorderColumnsAsync(Guid.NewGuid(), new List<ColumnPositionDto>());

        Assert.True(result);
    }

    [Fact]
    public async Task ReorderColumnsAsync_FailedResponse_ReturnsFalse()
    {
        SetupHttpResponse(HttpStatusCode.BadRequest);

        var result = await _service.ReorderColumnsAsync(Guid.NewGuid(), new List<ColumnPositionDto>());

        Assert.False(result);
    }
}
