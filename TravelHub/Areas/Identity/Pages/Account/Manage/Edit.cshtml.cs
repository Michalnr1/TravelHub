// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TravelHub.Domain.Entities;
using TravelHub.Web.Validation;

namespace TravelHub.Web.Areas.Identity.Pages.Account.Manage
{
    public class EditModel : PageModel
    {
        private readonly UserManager<Person> _userManager;
        private readonly SignInManager<Person> _signInManager;

        public EditModel(
            UserManager<Person> userManager,
            SignInManager<Person> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>

            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
            [Display(Name = "Username")]
            public string UserName { get; set; }

            [Phone]
            [Display(Name = "Phone number")]
            public string PhoneNumber { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
            [Display(Name = "First Name")]
            public string FirstName { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
            [Display(Name = "Last Name")]
            public string LastName { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
            [Display(Name = "Nationality")]
            public string Nationality { get; set; }

            [StringLength(3, MinimumLength = 3, ErrorMessage = "The airport code must have exactly 3 characters")]
            [Display(Name = "Default Airport Code")]
            public string DefaultAirportCode { get; set; }

            [Required]
            [Display(Name = "Birthday")]
            [DataType(DataType.Date)]
            [MinimumAge(18, 150)]
            public DateTime Birthday { get; set; }

            [Required]
            [Display(Name = "Is account private?")]
            public bool IsPrivate { get; set; }
        }

        private async Task LoadAsync(Person user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var email = await _userManager.GetEmailAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

            Username = userName;

            Input = new InputModel
            {
                Email = email,
                UserName = userName,
                PhoneNumber = phoneNumber,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Nationality = user.Nationality,
                Birthday = user.Birthday,
                IsPrivate = user.IsPrivate,
                DefaultAirportCode = user.DefaultAirportCode
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var userName = await _userManager.GetUserNameAsync(user);
            if (Input.UserName != userName)
            {
                var existingUserByUserName = await _userManager.FindByNameAsync(Input.UserName);
                if (existingUserByUserName != null && existingUserByUserName.Id != user.Id)
                {
                    ModelState.AddModelError("Input.UserName", "This username is already taken.");
                    await LoadAsync(user);
                    return Page();
                }

                var setUserNameResult = await _userManager.SetUserNameAsync(user, Input.UserName);
                if (!setUserNameResult.Succeeded)
                {
                    foreach (var error in setUserNameResult.Errors)
                    {
                        ModelState.AddModelError("Input.UserName", error.Description);
                    }
                    await LoadAsync(user);
                    return Page();
                }
            }

            var email = await _userManager.GetEmailAsync(user);
            if (Input.Email != email)
            {
                var existingUserByEmail = await _userManager.FindByEmailAsync(Input.Email);
                if (existingUserByEmail != null && existingUserByEmail.Id != user.Id)
                {
                    ModelState.AddModelError("Input.Email", "This email is already taken.");
                    await LoadAsync(user);
                    return Page();
                }

                var setEmailResult = await _userManager.SetEmailAsync(user, Input.Email);
                if (!setEmailResult.Succeeded)
                {
                    foreach (var error in setEmailResult.Errors)
                    {
                        ModelState.AddModelError("Input.Email", error.Description);
                    }
                    await LoadAsync(user);
                    return Page();
                }
            }

            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Unexpected error when trying to set phone number.";
                    return RedirectToPage();
                }
            }

            if (Input.FirstName != user.FirstName)
                user.FirstName = Input.FirstName;

            if (Input.LastName != user.LastName)
                user.LastName = Input.LastName;

            if (Input.Nationality != user.Nationality)
                user.Nationality = Input.Nationality;

            if (Input.DefaultAirportCode != user.DefaultAirportCode)
                user.DefaultAirportCode = Input.DefaultAirportCode;

            if (Input.Birthday != user.Birthday)
                user.Birthday = Input.Birthday;

            if (Input.IsPrivate != user.IsPrivate)
                user.IsPrivate = Input.IsPrivate;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                await LoadAsync(user);
                return Page();
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated";
            return RedirectToPage();
        }
    }
}
