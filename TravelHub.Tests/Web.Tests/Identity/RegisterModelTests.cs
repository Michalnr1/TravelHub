using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TravelHub.Domain.Entities;
using TravelHub.Tests.TestUtilities;
using TravelHub.Web.Areas.Identity.Pages.Account;
using Xunit;

namespace TravelHub.Tests.Web.Tests.Identity;

public class RegisterModelTests
{
    // Teraz przekazujemy jawnie store (FakeUserEmailStore) do CreateModel
    private RegisterModel CreateModel(FakeUserManager userManager, FakeSignInManager signInManager, FakeEmailSender emailSender, IUserStore<Person> store)
    {
        var model = new RegisterModel(userManager, store, signInManager, new FakeLogger<RegisterModel>(), emailSender);
        model.Url = new FakeUrlHelper();
        model.PageContext = new PageContext { HttpContext = new DefaultHttpContext() };
        model.PageContext.HttpContext.Request.Scheme = "https";
        return model;
    }

    [Fact]
    public async Task OnGet_PopulatesExternalLogins()
    {
        var store = new FakeUserEmailStore();
        var userManager = new FakeUserManager(store);
        var signIn = new FakeSignInManager(userManager);
        var emailSender = new FakeEmailSender();

        var model = CreateModel(userManager, signIn, emailSender, store);
        await model.OnGetAsync(null);

        Assert.NotNull(model.ExternalLogins);
        Assert.Empty(model.ExternalLogins);
    }

    [Fact]
    public async Task OnPost_ReturnsPage_WhenModelInvalid()
    {
        var store = new FakeUserEmailStore();
        var userManager = new FakeUserManager(store);
        var signIn = new FakeSignInManager(userManager);
        var emailSender = new FakeEmailSender();
        var model = CreateModel(userManager, signIn, emailSender, store);

        model.ModelState.AddModelError("Email", "required");

        var result = await model.OnPostAsync("/return");

        Assert.IsType<PageResult>(result);
    }

    [Fact]
    public async Task OnPost_Redirects_RegisterConfirmation_WhenRequireConfirmedAccount()
    {
        var store = new FakeUserEmailStore();
        var options = new IdentityOptions();
        options.SignIn.RequireConfirmedAccount = true;
        var userManager = new FakeUserManager(store, options);
        var signIn = new FakeSignInManager(userManager);
        var emailSender = new FakeEmailSender();

        var model = CreateModel(userManager, signIn, emailSender, store);
        model.Input = new RegisterModel.InputModel
        {
            Email = "test@example.com",
            Password = "Password1!",
            ConfirmPassword = "Password1!",
            FirstName = "Jan",
            LastName = "Kowalski",
            Nationality = "PL",
            Birthday = DateTime.Today.AddYears(-30)
        };

        var result = await model.OnPostAsync("/home");

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("RegisterConfirmation", redirect.PageName);
        Assert.Single(emailSender.Sent);
        Assert.Equal("test@example.com", redirect.RouteValues?["email"]);
    }

    [Fact]
    public async Task OnPost_SignsIn_WhenRequireConfirmedAccountFalse()
    {
        var store = new FakeUserEmailStore();
        var options = new IdentityOptions();
        options.SignIn.RequireConfirmedAccount = false;
        var userManager = new FakeUserManager(store, options);
        var signIn = new FakeSignInManager(userManager);
        var emailSender = new FakeEmailSender();

        var model = CreateModel(userManager, signIn, emailSender, store);
        model.Input = new RegisterModel.InputModel
        {
            Email = "a@b.com",
            Password = "Password1!",
            ConfirmPassword = "Password1!",
            FirstName = "Anna",
            LastName = "Nowak",
            Nationality = "PL",
            Birthday = DateTime.Today.AddYears(-25)
        };

        var result = await model.OnPostAsync("/dashboard");

        var lr = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("/dashboard", lr.Url);
        Assert.True(signIn.SignInCalled);
        Assert.Single(emailSender.Sent);
    }
}
