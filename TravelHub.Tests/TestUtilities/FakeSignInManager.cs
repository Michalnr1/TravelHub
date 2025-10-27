using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TravelHub.Domain.Entities;

namespace TravelHub.Tests.TestUtilities;

public class FakeSignInManager : SignInManager<Person>
{
    public SignInResult NextSignInResult { get; set; } = SignInResult.Failed;
    public bool SignInCalled { get; private set; }

    public FakeSignInManager(UserManager<Person> userManager)
        : base(userManager,
               new HttpContextAccessor { HttpContext = new DefaultHttpContext() },
               new FakeUserClaimsPrincipalFactory(),
               Microsoft.Extensions.Options.Options.Create(new IdentityOptions()),
               NullLogger<SignInManager<Person>>.Instance,
               new AuthenticationSchemeProvider(Microsoft.Extensions.Options.Options.Create(new AuthenticationOptions())),
               new FakeUserConfirmation())
    {
    }

    public override Task<SignInResult> PasswordSignInAsync(string userName, string password, bool isPersistent, bool lockoutOnFailure)
    {
        return Task.FromResult(NextSignInResult);
    }

    public override Task SignInAsync(Person user, bool isPersistent, string? authenticationMethod = null)
    {
        SignInCalled = true;
        return Task.CompletedTask;
    }

    public override Task<IEnumerable<AuthenticationScheme>> GetExternalAuthenticationSchemesAsync()
    {
        return Task.FromResult<IEnumerable<AuthenticationScheme>>(Array.Empty<AuthenticationScheme>());
    }

    // small helpers used by constructor
    private class FakeUserClaimsPrincipalFactory : IUserClaimsPrincipalFactory<Person>
    {
        public Task<ClaimsPrincipal> CreateAsync(Person user)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user?.Id ?? string.Empty),
                new Claim(ClaimTypes.Name, user?.UserName ?? user?.Email ?? string.Empty)
            }, "Test");
            return Task.FromResult(new ClaimsPrincipal(identity));
        }
    }

    private class FakeUserConfirmation : IUserConfirmation<Person>
    {
        // właściwy podpis metody zgodny z IUserConfirmation<TUser>
        public Task<bool> IsConfirmedAsync(UserManager<Person> manager, Person user)
        {
            return Task.FromResult(true);
        }
    }
}
