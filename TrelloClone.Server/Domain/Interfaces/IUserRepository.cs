public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid userId);
    Task<User?> GetByIdWithBoardsAsync(Guid userId);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByUsernameAsync(string userName);
    Task<List<User>> GetAllAsync();
    void Add(User user);
    void Remove(User user);
}