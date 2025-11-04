using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using TravelHub.Tests.TestUtilities;
using TravelHub.Web.Areas.Identity.Pages.Account;
using Xunit;

namespace TravelHub.Tests.Web.Tests.Identity;

public class LoginModelTests
{
    private LoginModel CreateModel(FakeSignInManager signIn)
    {
        var m = new LoginModel(signIn, new FakeLogger<LoginModel>());
        m.PageContext = new PageContext { HttpContext = new DefaultHttpContext() };
        m.Url = new FakeUrlHelper();
        return m;
    }

    [Fact]
    public async Task OnPost_Redirects_WhenSuccess()
    {
        var userManager = new FakeUserManager(new FakeUserEmailStore());
        var signIn = new FakeSignInManager(userManager) { NextSignInResult = Microsoft.AspNetCore.Identity.SignInResult.Success };
        var model = CreateModel(signIn);
        model.Input = new LoginModel.InputModel { Email = "u@x", Password = "p", RememberMe = false };

        var result = await model.OnPostAsync("/home");
        var lr = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("/home", lr.Url);
    }

    [Fact]
    public async Task OnPost_RedirectsTo2fa_WhenRequiresTwoFactor()
    {
        var userManager = new FakeUserManager(new FakeUserEmailStore());
        var signIn = new FakeSignInManager(userManager) { NextSignInResult = Microsoft.AspNetCore.Identity.SignInResult.TwoFactorRequired };
        var model = CreateModel(signIn);
        model.Input = new LoginModel.InputModel { Email = "u@x", Password = "p", RememberMe = true };

        var result = await model.OnPostAsync("/return");
        var rr = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./LoginWith2fa", rr.PageName);
        Assert.Equal("/return", rr.RouteValues?["ReturnUrl"]);
    }

    [Fact]
    public async Task OnPost_RedirectsToLockout_WhenLockedOut()
    {
        var userManager = new FakeUserManager(new FakeUserEmailStore());
        var signIn = new FakeSignInManager(userManager) { NextSignInResult = Microsoft.AspNetCore.Identity.SignInResult.LockedOut };
        var model = CreateModel(signIn);
        model.Input = new LoginModel.InputModel { Email = "u@x", Password = "p", RememberMe = false };

        var result = await model.OnPostAsync("/r");
        var rr = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Lockout", rr.PageName);
    }

    [Fact]
    public async Task OnPost_ReturnsPage_WhenFailed()
    {
        var userManager = new FakeUserManager(new FakeUserEmailStore());
        var signIn = new FakeSignInManager(userManager) { NextSignInResult = Microsoft.AspNetCore.Identity.SignInResult.Failed };
        var model = CreateModel(signIn);
        model.Input = new LoginModel.InputModel { Email = "u@x", Password = "wrong", RememberMe = false };

        var result = await model.OnPostAsync("/x");
        var pr = Assert.IsType<PageResult>(result);
        Assert.True(model.ModelState.ErrorCount > 0);
    }
}
