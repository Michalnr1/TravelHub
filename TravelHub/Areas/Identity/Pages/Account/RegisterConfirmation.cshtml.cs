// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using TravelHub.Domain.Entities;

namespace TravelHub.Web.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterConfirmationModel : PageModel
    {
        private readonly UserManager<Person> _userManager;
        private readonly Domain.Interfaces.IEmailSender _sender;

        public RegisterConfirmationModel(UserManager<Person> userManager, Domain.Interfaces.IEmailSender sender)
        {
            _userManager = userManager;
            _sender = sender;
        }

        public string Email { get; set; }
        public bool DisplayConfirmAccountLink { get; set; }
        public string EmailConfirmationUrl { get; set; }
        public bool CanResendEmail { get; set; }

        public async Task<IActionResult> OnGetAsync(string email, string returnUrl = null)
        {
            if (email == null)
            {
                return RedirectToPage("/Index");
            }
            returnUrl = returnUrl ?? Url.Content("~/");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound($"Unable to load user with email '{email}'.");
            }

            Email = email;

            // Sprawdź czy użytkownik już potwierdził email
            if (user.EmailConfirmed)
            {
                // Email już potwierdzony - przekieruj do logowania
                return RedirectToPage("./Login");
            }

            // Umożliwij ponowne wysłanie emaila
            CanResendEmail = true;

            // Dla środowiska development - pokaż link bezpośredni
            // W produkcji to powinno być false
            DisplayConfirmAccountLink = false; // Zmień na true tylko w development

            // Wygeneruj URL dla linku potwierdzającego (tylko jeśli DisplayConfirmAccountLink = true)
            if (DisplayConfirmAccountLink)
            {
                var userId = await _userManager.GetUserIdAsync(user);
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                EmailConfirmationUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                    protocol: Request.Scheme);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostResendConfirmationAsync(string email, string returnUrl = null)
        {
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToPage("Trips/MyTrips");
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Nie pokazuj że użytkownik nie istnieje (ze względów bezpieczeństwa)
                return RedirectToPage("./RegisterConfirmation", new { email = email, returnUrl = returnUrl });
            }

            if (user.EmailConfirmed)
            {
                return RedirectToPage("./Login");
            }

            // Wygeneruj nowy token i wyślij email
            var userId = await _userManager.GetUserIdAsync(user);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                protocol: Request.Scheme);

            await _sender.SendEmailAsync(
                email,
                "Confirm your email",
                $"Please confirm your account by <a href='{callbackUrl}'>clicking here</a>.");

            // Dodaj komunikat o sukcesie
            TempData["ResendSuccess"] = true;
            return RedirectToPage("./RegisterConfirmation", new { email = email, returnUrl = returnUrl });
        }
    }
}