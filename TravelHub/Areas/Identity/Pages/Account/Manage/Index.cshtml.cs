// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Services;

namespace TravelHub.Web.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<Person> _userManager;
        private readonly ITripService _tripService;
        private readonly ITripParticipantService _tripParticipantService;
        private readonly ISpotService _spotService;

        public IndexModel(
            UserManager<Person> userManager,
            ITripService tripService,
            ITripParticipantService tripParticipantService,
            ISpotService spotService)
        {
            _userManager = userManager;
            _tripService = tripService;
            _tripParticipantService = tripParticipantService;
            _spotService = spotService;
        }

        public string Username { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Nationality { get; set; }
        public string DefaultAirportCode { get; set; }
        public DateTime Birthday { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsPrivate { get; set; }

        // Statystyki
        public int TotalTrips { get; set; }
        public int TotalDays { get; set; }
        public int VisitedCountries { get; set; }
        public List<CountryStat> TopCountries { get; set; } = new();
        public List<TripStat> RecentTrips { get; set; } = new();

        public class CountryStat
        {
            public string Name { get; set; }
            public string Code { get; set; }
            public int VisitCount { get; set; }
        }

        public class TripStat
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public int DaysCount { get; set; }
            public int CountriesCount { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadUserDataAsync(user);
            await LoadStatisticsAsync(user.Id);

            return Page();
        }

        private async Task LoadUserDataAsync(Person user)
        {
            Username = await _userManager.GetUserNameAsync(user);
            Email = await _userManager.GetEmailAsync(user);
            PhoneNumber = await _userManager.GetPhoneNumberAsync(user);
            FirstName = user.FirstName;
            LastName = user.LastName;
            Nationality = user.Nationality;
            DefaultAirportCode = user.DefaultAirportCode;
            Birthday = user.Birthday;
            IsPrivate = user.IsPrivate;
        }

        private async Task LoadStatisticsAsync(string userId)
        {
            // Pobierz wszystkie podró¿e u¿ytkownika
            var userTrips = await _tripParticipantService.GetUserParticipatingTripsAsync(userId);

            // Filtruj tylko ukoñczone podró¿e
            var finishedTrips = userTrips
                .Where(ut => ut.Trip.Status == Status.Finished)
                .ToList();

            var tripIds = finishedTrips.Select(ut => ut.TripId).ToList();

            TotalTrips = tripIds.Count;

            // Zbierz dane sekwencyjnie
            var tripsStats = new List<TripStat>();
            var allCountries = new List<Country>();

            foreach (var tripId in tripIds)
            {
                var trip = await _tripService.GetTripWithDetailsAsync(tripId);
                var countries = await _spotService.GetCountriesByTripAsync(tripId);

                // Oblicz liczbê dni w podró¿y (ró¿nica miêdzy StartDate a EndDate)
                var daysInTrip = (trip.EndDate - trip.StartDate).Days + 1; // +1 bo wliczamy dzieñ startowy

                var tripStat = new TripStat
                {
                    Id = tripId,
                    Name = trip.Name,
                    StartDate = trip.StartDate,
                    EndDate = trip.EndDate,
                    DaysCount = daysInTrip,
                    CountriesCount = countries.Count()
                };

                tripsStats.Add(tripStat);
                allCountries.AddRange(countries);
            }

            // Posortuj i weŸ ostatnie 5 podró¿y
            RecentTrips = tripsStats
                .OrderByDescending(t => t.StartDate)
                .Take(5)
                .ToList();

            // Oblicz ca³kowit¹ liczbê dni (suma dni ze wszystkich ukoñczonych podró¿y)
            TotalDays = tripsStats.Sum(t => t.DaysCount);

            // Oblicz odwiedzone kraje (unikalne kody krajów)
            VisitedCountries = allCountries
                .Select(c => c.Code)
                .Distinct()
                .Count();

            // Top kraje - grupujemy po kodzie i nazwie
            TopCountries = allCountries
                .GroupBy(c => new { c.Code, c.Name })
                .Select(g => new CountryStat
                {
                    Code = g.Key.Code,
                    Name = g.Key.Name,
                    VisitCount = g.Count()
                })
                .OrderByDescending(c => c.VisitCount)
                .Take(5)
                .ToList();
        }
    }
}