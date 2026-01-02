using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;

using TrelloClone.Server.Domain.Entities;
using TrelloClone.Server.Infrastructure.Persistence;
using TrelloClone.Shared.DTOs.Column;
using TrelloClone.Shared.Enums;

using Xunit;

namespace TrelloClone.Server.Tests.Infrastructure.Persistence;

public sealed class ColumnRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ColumnRepository _repository;
    private bool _disposed; // Track disposal state

    public ColumnRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _repository = new ColumnRepository(_context);
    }

    [Fact]
    public async Task TitleExistsAsync_TitleExists_ReturnsTrue()
    {
        // Arrange
        var board = new Board { Name = "TestBoard" };
        _context.Boards.Add(board);
        var column = new Column
        {
            Id = Guid.NewGuid(),
            Title = "TestColumn"
        };
        board.Columns.Add(column);
        await _context.SaveChangesAsync();

        // Act
        var exists = await _repository.TitleExistsAsync(board.Id, "TestColumn");

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task TitleExistsAsync_TitleDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var board = new Board { Name = "TestBoard" };
        _context.Boards.Add(board);
        await _context.SaveChangesAsync();

        // Act
        var exists = await _repository.TitleExistsAsync(board.Id, "newTitle");

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task GetByIdAsync_ColumnExists_ReturnsColumn()
    {
        // Arrange
        var board = new Board { Name = "TestBoard" };
        _context.Boards.Add(board);
        var columnId = Guid.NewGuid();
        var column = new Column
        {
            Id = columnId,
            Title = "TestColumn"
        };
        board.Columns.Add(column);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(columnId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(columnId, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ColumnDoesNotExist_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ListByBoardAsync_ReturnsColumnsForBoard()
    {
        // Arrange
        var board = new Board { Name = "TestBoard" };
        _context.Boards.Add(board);
        var column1 = new Column { Id = Guid.NewGuid(), Title = "Column1", Board = board, Position = 1 };
        var column2 = new Column { Id = Guid.NewGuid(), Title = "Column2", Board = board, Position = 2 };
        _context.Columns.AddRange(column1, column2);
        await _context.SaveChangesAsync();

        // Act
        var columns = await _repository.ListByBoardAsync(board.Id);

        // Assert
        Assert.NotNull(columns);
        Assert.Equal(2, columns.Count);
        Assert.Equal("Column1", columns[0].Title);
        Assert.Equal("Column2", columns[1].Title);
    }

    [Fact]
    public async Task ListByBoardAsync_ReturnsEmptyListForNonExistentBoard()
    {
        // Act
        var columns = await _repository.ListByBoardAsync(Guid.NewGuid());

        // Assert
        Assert.Empty(columns);
    }

    [Fact]
    public async Task ListByBoardAsync_ReturnsColumnsOrderedByPosition()
    {
        // Arrange
        var board = new Board { Name = "TestBoard" };
        _context.Boards.Add(board);
        var column1 = new Column { Id = Guid.NewGuid(), Title = "Column1", Board = board, Position = 2 };
        var column2 = new Column { Id = Guid.NewGuid(), Title = "Column2", Board = board, Position = 1 };
        _context.Columns.AddRange(column1, column2);
        await _context.SaveChangesAsync();

        // Act
        var columns = await _repository.ListByBoardAsync(board.Id);

        // Assert
        Assert.Equal("Column2", columns[0].Title);
        Assert.Equal("Column1", columns[1].Title);
    }

    [Fact]
    public async Task UpdatePositionsAsync_UpdatesColumnPositions()
    {
        // Arrange
        var board = new Board { Name = "TestBoard" };
        _context.Boards.Add(board);
        var column1 = new Column { Id = Guid.NewGuid(), Title = "Column1", Board = board, Position = 1 };
        var column2 = new Column { Id = Guid.NewGuid(), Title = "Column2", Board = board, Position = 2 };
        _context.Columns.AddRange(column1, column2);
        await _context.SaveChangesAsync();

        var positions = new List<ColumnPositionDto>
        {
            new ColumnPositionDto { Id = column1.Id, Position = 2 },
            new ColumnPositionDto { Id = column2.Id, Position = 1 }
        };

        // Act
        await _repository.UpdatePositionsAsync(positions);

        // Assert
        var updatedColumn1 = await _context.Columns.FindAsync(column1.Id);
        var updatedColumn2 = await _context.Columns.FindAsync(column2.Id);
        Assert.NotNull(updatedColumn1);
        Assert.NotNull(updatedColumn2);
        Assert.Equal(2, updatedColumn1.Position);
        Assert.Equal(1, updatedColumn2.Position);
    }

    [Fact]
    public async Task UpdatePositionsAsync_OnlyUpdatesSpecifiedColumns()
    {
        // Arrange
        var board = new Board { Name = "TestBoard" };
        _context.Boards.Add(board);
        var column1 = new Column { Id = Guid.NewGuid(), Title = "Column1", Board = board, Position = 1 };
        var column2 = new Column { Id = Guid.NewGuid(), Title = "Column2", Board = board, Position = 2 };
        _context.Columns.AddRange(column1, column2);
        await _context.SaveChangesAsync();

        var positions = new List<ColumnPositionDto>
        {
            new ColumnPositionDto { Id = column1.Id, Position = 3 }
        };

        // Act
        await _repository.UpdatePositionsAsync(positions);

        // Assert
        var updatedColumn1 = await _context.Columns.FindAsync(column1.Id);
        var updatedColumn2 = await _context.Columns.FindAsync(column2.Id);
        Assert.NotNull(updatedColumn1);
        Assert.NotNull(updatedColumn2);
        Assert.Equal(3, updatedColumn1.Position);
        Assert.Equal(2, updatedColumn2.Position);
    }

    [Fact]
    public async Task Add_AddsColumnToDatabase()
    {
        // Arrange
        var column = new Column
        {
            Id = Guid.NewGuid(),
            Title = "TestColumn"
        };

        // Act
        _repository.Add(column);
        await _context.SaveChangesAsync();

        // Assert
        var savedColumn = await _context.Columns.FindAsync(column.Id);
        Assert.NotNull(savedColumn);
        Assert.Equal(column.Id, savedColumn.Id);
    }

    [Fact]
    public async Task Update_UpdatesColumnInDatabase()
    {
        // Arrange
        var board = new Board { Name = "TestBoard" };
        _context.Boards.Add(board);
        var column = new Column
        {
            Id = Guid.NewGuid(),
            Title = "OriginalTitle",
            Board = board
        };
        _context.Columns.Add(column);
        await _context.SaveChangesAsync();

        // Act
        column.Title = "UpdatedTitle";
        _repository.Update(column);
        await _context.SaveChangesAsync();

        // Assert
        var updatedColumn = await _context.Columns.FindAsync(column.Id);
        Assert.NotNull(updatedColumn);
        Assert.Equal("UpdatedTitle", updatedColumn.Title);
    }

    [Fact]
    public async Task Remove_RemovesColumnFromDatabase()
    {
        // Arrange
        var column = new Column
        {
            Id = Guid.NewGuid(),
            Title = "TestColumn"
        };
        _context.Columns.Add(column);
        await _context.SaveChangesAsync();
        var columnId = column.Id;

        // Act
        _repository.Remove(column);
        await _context.SaveChangesAsync();

        // Assert
        var deletedColumn = await _context.Columns.FindAsync(columnId);
        Assert.Null(deletedColumn);
    }

    // IDisposable implementation for sealed class - stupid sonarqube
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed resources
                _context?.Dispose();
            }

            _disposed = true;
        }
    }
}
