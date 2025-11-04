using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using TravelHub.Domain.Entities;

namespace TravelHub.Tests.TestUtilities;

public class FakeLogger<T> : ILogger<T>
{
    public record LogEntry(LogLevel Level, EventId EventId, string? Message, Exception? Exception);

    public IList<LogEntry> Entries { get; } = new List<LogEntry>();

    public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter?.Invoke(state, exception);
        Entries.Add(new LogEntry(logLevel, eventId, message, exception));
    }

    private class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new NullScope();
        public void Dispose() { }
    }
}

public class TestTempDataProvider : ITempDataProvider
{
    private const string Key = "__TestTempData__";

    public IDictionary<string, object> LoadTempData(HttpContext context)
    {
        if (context == null) return new Dictionary<string, object>();
        if (context.Items[Key] is IDictionary<string, object> dict) return dict;
        var newDict = new Dictionary<string, object>();
        context.Items[Key] = newDict;
        return newDict;
    }

    public void SaveTempData(HttpContext context, IDictionary<string, object> values)
    {
        if (context == null) return;
        context.Items[Key] = new Dictionary<string, object>(values ?? new Dictionary<string, object>());
    }
}

/// <summary>
/// Prosty normalizer używany w testach.
/// </summary>
public class TestLookupNormalizer : ILookupNormalizer
{
    public string NormalizeEmail(string? email) => email?.ToUpperInvariant() ?? string.Empty;
    public string NormalizeName(string? name) => name?.ToUpperInvariant() ?? string.Empty;
}

/// <summary>
/// Fabryka tworząca gotowy do użytku UserManager<Person> dla testów.
/// Jeśli nie podasz store, zostanie użyty FakeUserEmailStore (jeśli dostępny w TestUtilities).
/// </summary>
public static class TestUserManagerFactory
{
    public static UserManager<Person> Create(IUserStore<Person>? store = null)
    {
        // Jeśli nie podano store, spróbuj utworzyć FakeUserEmailStore z TestUtilities
        if (store == null)
        {
            // Jeśli FakeUserEmailStore nie istnieje w projekcie testowym, przekaż własny IUserStore<Person>
            store = new FakeUserEmailStore();
        }

        var options = Options.Create(new IdentityOptions());
        var hasher = new PasswordHasher<Person>();
        var userValidators = new List<IUserValidator<Person>>();
        var pwdValidators = new List<IPasswordValidator<Person>>();
        var lookupNormalizer = new TestLookupNormalizer();
        var errors = new IdentityErrorDescriber();
        IServiceProvider? services = null;
        var logger = NullLogger<UserManager<Person>>.Instance;

        return new UserManager<Person>(
            store,
            options,
            hasher,
            userValidators,
            pwdValidators,
            lookupNormalizer,
            errors,
            services,
            logger);
    }
}
