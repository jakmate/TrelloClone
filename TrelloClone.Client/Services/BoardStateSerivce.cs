namespace TrelloClone.Client.Services;

public interface IBoardStateService
{
    event Action? OnBoardsChanged;
    void NotifyBoardsChanged();
}

public class BoardStateService : IBoardStateService
{
    public event Action? OnBoardsChanged;

    public void NotifyBoardsChanged()
    {
        OnBoardsChanged?.Invoke();
    }
}
