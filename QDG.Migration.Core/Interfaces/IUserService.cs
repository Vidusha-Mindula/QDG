using QDG.Migration.Core.Models;

namespace QDG.Migration.Core.Interfaces;

public interface IUserService
{
    Task<User?> RegisterAsync(string username, string password);
    Task<string?> LoginAsync(string username, string password);
}
