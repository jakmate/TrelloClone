namespace TrelloClone.Client.Services;

public class BoardStateService
{
    public event Action? OnBoardsChanged;

    public void NotifyBoardsChanged()
    {
        OnBoardsChanged?.Invoke();
    }
}