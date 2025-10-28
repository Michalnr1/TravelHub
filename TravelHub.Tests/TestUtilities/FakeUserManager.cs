using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TravelHub.Domain.Entities;

namespace TravelHub.Tests.TestUtilities;

public class FakeUserManager : UserManager<Person>
{
    public bool ForceCreateFailure { get; set; }

    public FakeUserManager(IUserEmailStore<Person> store, IdentityOptions? options = null)
        : base((IUserStore<Person>)store,
               Microsoft.Extensions.Options.Options.Create(options ?? new IdentityOptions()),
               new PasswordHasher<Person>(),
               Array.Empty<IUserValidator<Person>>(),
               Array.Empty<IPasswordValidator<Person>>(),
               new SimpleLookupNormalizer(),
               new IdentityErrorDescriber(),
               null,
               NullLogger<UserManager<Person>>.Instance)
    {
    }

    public override Task<IdentityResult> CreateAsync(Person user, string password)
    {
        if (ForceCreateFailure)
        {
            return Task.FromResult(IdentityResult.Failed(new IdentityError { Description = "Forced failure" }));
        }

        // Delegate to base (will hash password and call store.CreateAsync)
        return base.CreateAsync(user, password);
    }

    public override Task<string> GenerateEmailConfirmationTokenAsync(Person user)
    {
        // deterministyczny token for tests
        return Task.FromResult("test-email-token");
    }
}
