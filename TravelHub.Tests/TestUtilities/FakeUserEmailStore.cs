#nullable disable
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using TravelHub.Domain.Entities;

namespace TravelHub.Tests.TestUtilities;

public class FakeUserEmailStore : IUserEmailStore<Person>, IUserPasswordStore<Person>
{
    private readonly ConcurrentDictionary<string, Person> _users = new();

    // IUserStore - Create/Update/Delete powinny zwracać IdentityResult
    public Task<IdentityResult> CreateAsync(Person user, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(user.Id)) user.Id = Guid.NewGuid().ToString();
        _users[user.Id] = user;
        return Task.FromResult(IdentityResult.Success);
    }

    public Task<IdentityResult> DeleteAsync(Person user, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(user?.Id)) _users.TryRemove(user.Id, out _);
        return Task.FromResult(IdentityResult.Success);
    }

    public Task<IdentityResult> UpdateAsync(Person user, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(user?.Id)) _users[user.Id] = user;
        return Task.FromResult(IdentityResult.Success);
    }

    public Task<Person> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        _users.TryGetValue(userId, out var u);
        return Task.FromResult(u);
    }

    public Task<Person> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        var u = _users.Values.FirstOrDefault(x => string.Equals(x.NormalizedUserName, normalizedUserName, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(u);
    }

    public Task SetUserNameAsync(Person user, string userName, CancellationToken cancellationToken)
    {
        if (user != null) user.UserName = userName;
        return Task.CompletedTask;
    }

    public Task<string> GetUserIdAsync(Person user, CancellationToken cancellationToken)
        => Task.FromResult(user?.Id ?? string.Empty);

    public Task<string> GetUserNameAsync(Person user, CancellationToken cancellationToken)
        => Task.FromResult(user?.UserName ?? string.Empty);

    public Task SetNormalizedUserNameAsync(Person user, string normalizedName, CancellationToken cancellationToken)
    {
        if (user != null) user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    public Task<string> GetNormalizedUserNameAsync(Person user, CancellationToken cancellationToken)
        => Task.FromResult(user?.NormalizedUserName ?? string.Empty);

    public void Dispose() { /* no-op */ }

    // IUserEmailStore
    public Task SetEmailAsync(Person user, string email, CancellationToken cancellationToken)
    {
        if (user != null) user.Email = email;
        return Task.CompletedTask;
    }

    public Task<string> GetEmailAsync(Person user, CancellationToken cancellationToken)
        => Task.FromResult(user?.Email ?? string.Empty);

    public Task<bool> GetEmailConfirmedAsync(Person user, CancellationToken cancellationToken)
        => Task.FromResult(user?.EmailConfirmed ?? false);

    public Task SetEmailConfirmedAsync(Person user, bool confirmed, CancellationToken cancellationToken)
    {
        if (user != null) user.EmailConfirmed = confirmed;
        return Task.CompletedTask;
    }

    public Task<Person> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        var u = _users.Values.FirstOrDefault(x => string.Equals(x.NormalizedEmail, normalizedEmail, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(u);
    }

    public Task<string> GetNormalizedEmailAsync(Person user, CancellationToken cancellationToken)
        => Task.FromResult(user?.NormalizedEmail ?? string.Empty);

    public Task SetNormalizedEmailAsync(Person user, string normalizedEmail, CancellationToken cancellationToken)
    {
        if (user != null) user.NormalizedEmail = normalizedEmail;
        return Task.CompletedTask;
    }

    // IUserPasswordStore
    public Task SetPasswordHashAsync(Person user, string passwordHash, CancellationToken cancellationToken)
    {
        if (user != null) user.PasswordHash = passwordHash;
        return Task.CompletedTask;
    }

    public Task<string> GetPasswordHashAsync(Person user, CancellationToken cancellationToken)
        => Task.FromResult(user?.PasswordHash ?? string.Empty);

    public Task<bool> HasPasswordAsync(Person user, CancellationToken cancellationToken)
        => Task.FromResult(!string.IsNullOrEmpty(user?.PasswordHash));
}