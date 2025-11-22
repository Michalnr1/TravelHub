using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;
using TravelHub.Domain.Entities;

// Ensure you have a 'using' statement for the namespace where your entities are located.
// using YourProject.Entities; 

namespace TravelHub.Infrastructure;

public class ApplicationDbContext : IdentityDbContext<Person>
{
    // Add DbSet for each of your entities
    public DbSet<Trip> Trips { get; set; }
    public DbSet<Day> Days { get; set; }
    public DbSet<Activity> Activities { get; set; }
    //public DbSet<Spot> Spots { get; set; }
    //public DbSet<Accommodation> Accommodations { get; set; }
    public DbSet<Transport> Transports { get; set; }
    public DbSet<Photo> Photos { get; set; }
    public DbSet<Expense> Expenses { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<ExchangeRate> ExchangeRates { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Country> Countries { get; set; }
    public DbSet<ExpenseParticipant> ExpenseParticipants { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<PersonFriends> PersonFriends { get; set; }
    public DbSet<FriendRequest> FriendRequests { get; set; }
    public DbSet<TripParticipant> TripParticipants { get; set; } 
    public DbSet<Blog> Blogs { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // This is crucial for IdentityDbContext to configure its own tables.
        base.OnModelCreating(builder);

        // --- Person Configuration ---
        builder.Entity<Person>(entity =>
        {
            entity.Property(p => p.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(p => p.LastName).IsRequired().HasMaxLength(100);
            entity.Property(p => p.Nationality).HasMaxLength(100);
            entity.Property(p => p.Birthday).HasColumnType("date");
            entity.Property(p => p.DefaultAirportCode).HasMaxLength(3);

            // ZMIANA: Konfiguracja relacji M:N przez encję PersonFriends
            entity.HasMany(p => p.Friends)
                  .WithOne(pf => pf.User)
                  .HasForeignKey(pf => pf.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(p => p.FriendOf)
                  .WithOne(pf => pf.Friend)
                  .HasForeignKey(pf => pf.FriendId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // --- PersonFriends Configuration ---
        builder.Entity<PersonFriends>(entity =>
        {
            entity.HasKey(pf => new { pf.UserId, pf.FriendId });

            entity.Property(pf => pf.CreatedAt)
                  .IsRequired();

            // Relacja do User
            entity.HasOne(pf => pf.User)
                  .WithMany(p => p.Friends)
                  .HasForeignKey(pf => pf.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Relacja do Friend
            entity.HasOne(pf => pf.Friend)
                  .WithMany(p => p.FriendOf)
                  .HasForeignKey(pf => pf.FriendId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // --- FriendRequest Configuration ---
        builder.Entity<FriendRequest>(entity =>
        {
            entity.HasKey(fr => fr.Id);

            entity.Property(fr => fr.Id)
                .ValueGeneratedOnAdd();

            entity.Property(fr => fr.RequesterId)
                .IsRequired()
                .HasMaxLength(450);

            entity.Property(fr => fr.AddresseeId)
                .IsRequired()
                .HasMaxLength(450);

            entity.Property(fr => fr.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(fr => fr.RequestedAt)
                .IsRequired();

            entity.Property(fr => fr.RespondedAt)
                .IsRequired(false);

            entity.Property(fr => fr.Message)
                .HasMaxLength(500)
                .IsRequired(false);

            // Relacja do Requester (Person)
            entity.HasOne(fr => fr.Requester)
                .WithMany()
                .HasForeignKey(fr => fr.RequesterId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relacja do Addressee (Person)
            entity.HasOne(fr => fr.Addressee)
                .WithMany()
                .HasForeignKey(fr => fr.AddresseeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Index dla lepszej wydajności zapytań
            entity.HasIndex(fr => new { fr.AddresseeId, fr.Status });
            entity.HasIndex(fr => new { fr.RequesterId, fr.Status });
            entity.HasIndex(fr => fr.RequestedAt);
        });

        // --- Trip Configuration ---
        builder.Entity<Trip>(entity =>
        {
            entity.HasKey(t => t.Id);

            entity.Property(t => t.Id)
                .ValueGeneratedOnAdd();

            entity.Property(t => t.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(t => t.StartDate)
                .HasColumnType("date");

            entity.Property(t => t.EndDate)
                .HasColumnType("date");

            entity.Property(t => t.IsPrivate)
                .IsRequired();

            entity.Property(t => t.PersonId)
                .IsRequired()
                .HasMaxLength(450);

            entity.Property(t => t.Catalog).HasMaxLength(100);

            entity.Property(t => t.Checklist)
              .HasConversion(
                  v => JsonSerializer.Serialize(v, new JsonSerializerOptions
                  {
                      PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                  }),
                  v => JsonSerializer.Deserialize<Checklist>(v, new JsonSerializerOptions
                  {
                      PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                  }))
              .HasColumnType("nvarchar(max)");

            // Konwersja enum Status
            var statusConverter = new EnumToStringConverter<Status>();
            entity.Property(t => t.Status)
                .HasConversion(statusConverter)
                .HasMaxLength(20);

            // Konwersja enum CurrencyCode
            var currencyCodeConverter = new EnumToStringConverter<CurrencyCode>();
            entity.Property(t => t.CurrencyCode)
                .HasConversion(currencyCodeConverter)
                .HasMaxLength(3);

            // 1:1 relationship with Blog
            entity.HasOne(t => t.Blog)
                .WithOne(b => b.Trip)
                .HasForeignKey<Blog>(b => b.TripId)
                .OnDelete(DeleteBehavior.Cascade);

            // 1:N relationship with Person (Trip organizer)
            entity.HasOne(t => t.Person)
                .WithMany(p => p.Trips)
                .HasForeignKey(t => t.PersonId)
                .OnDelete(DeleteBehavior.Restrict);

            // 1:N relationship with Days
            entity.HasMany(t => t.Days)
                .WithOne(d => d.Trip)
                .HasForeignKey(d => d.TripId)
                .OnDelete(DeleteBehavior.Cascade);

            // 1:N relationship with Activities
            entity.HasMany(t => t.Activities)
                .WithOne(a => a.Trip)
                .HasForeignKey(a => a.TripId)
                .OnDelete(DeleteBehavior.Cascade);

            // 1:N relationship with Transports
            entity.HasMany(t => t.Transports)
                .WithOne(tr => tr.Trip)
                .HasForeignKey(tr => tr.TripId)
                .OnDelete(DeleteBehavior.Cascade);

            // 1:N relationship with Expenses
            entity.HasMany(t => t.Expenses)
                .WithOne(e => e.Trip)
                .HasForeignKey(e => e.TripId)
                .OnDelete(DeleteBehavior.Cascade);

            // 1:N relationship with ExchangeRates
            entity.HasMany(t => t.ExchangeRates)
                .WithOne(er => er.Trip)
                .HasForeignKey(er => er.TripId)
                .OnDelete(DeleteBehavior.Cascade);

            // 1:N relationship with TripParticipants
            entity.HasMany(t => t.Participants)
                .WithOne(tp => tp.Trip)
                .HasForeignKey(tp => tp.TripId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexy dla lepszej wydajności
            entity.HasIndex(t => t.PersonId);
            entity.HasIndex(t => t.StartDate);
            entity.HasIndex(t => t.EndDate);
            entity.HasIndex(t => t.Status);
            entity.HasIndex(t => t.IsPrivate);
        });

        // --- TripParticipant Configuration ---
        builder.Entity<TripParticipant>(entity =>
        {
            entity.HasKey(tp => tp.Id);

            entity.Property(tp => tp.Id)
                .ValueGeneratedOnAdd();

            entity.Property(tp => tp.TripId)
                .IsRequired();

            entity.Property(tp => tp.PersonId)
                .IsRequired()
                .HasMaxLength(450);

            entity.Property(tp => tp.JoinedAt)
                .IsRequired();

            entity.Property(tp => tp.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            // Relacja do Trip
            entity.HasOne(tp => tp.Trip)
                .WithMany(t => t.Participants)
                .HasForeignKey(tp => tp.TripId)
                .OnDelete(DeleteBehavior.Cascade); // Jeśli wycieczka jest usuwana, uczestnicy też są usuwani

            // Relacja do Person
            entity.HasOne(tp => tp.Person)
                .WithMany() // Person nie ma bezpośredniej kolekcji TripParticipant
                .HasForeignKey(tp => tp.PersonId)
                .OnDelete(DeleteBehavior.Cascade); // Jeśli użytkownik jest usuwany, jego uczestnictwo też jest usuwane

            // Unikalna para TripId + PersonId (jeden użytkownik może być uczestnikiem danej wycieczki tylko raz)
            entity.HasIndex(tp => new { tp.TripId, tp.PersonId })
                .IsUnique();

            // Indexy dla lepszej wydajności zapytań
            entity.HasIndex(tp => tp.PersonId);
            entity.HasIndex(tp => tp.Status);
            entity.HasIndex(tp => tp.JoinedAt);
        });

        // --- Day Configuration ---
        builder.Entity<Day>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Date).HasColumnType("date");
            entity.Property(d => d.Number).IsRequired(false);
            entity.Property(d => d.Name).HasMaxLength(100);

            // 1:N relationship with Trip
            entity.HasOne(d => d.Trip)
                  .WithMany(t => t.Days)
                  .HasForeignKey(d => d.TripId)
                  .OnDelete(DeleteBehavior.Restrict);

            // 1:N relationship with Accommodation
            entity.HasOne(d => d.Accommodation)
                  .WithMany(t => t.Days)
                  .HasForeignKey(d => d.AccommodationId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.SetNull);

            // 1:N relationship with Activities
            entity.HasMany(d => d.Activities)
                .WithOne(a => a.Day)
                .HasForeignKey(a => a.DayId)
                .OnDelete(DeleteBehavior.Restrict);

            // 1:N relationship with Posts
            entity.HasMany(d => d.Posts)
                .WithOne(p => p.Day)
                .HasForeignKey(p => p.DayId)
                .OnDelete(DeleteBehavior.SetNull);

            // Indexes for better performance
            entity.HasIndex(d => d.TripId);
            entity.HasIndex(d => d.AccommodationId);
            entity.HasIndex(d => d.Date);
        });

        // --- Activity Configuration ---
        builder.Entity<Activity>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Name).IsRequired().HasMaxLength(200);
            entity.Property(a => a.Description).HasMaxLength(1000);
            entity.Property(a => a.Duration).HasPrecision(18, 2);
            entity.Property(a => a.StartTime).HasPrecision(4, 2);

            entity.Property(a => a.Checklist)
              .HasConversion(
                  v => JsonSerializer.Serialize(v, new JsonSerializerOptions
                  {
                      PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                  }),
                  v => JsonSerializer.Deserialize<Checklist>(v, new JsonSerializerOptions
                  {
                      PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                  }))
              .HasColumnType("nvarchar(max)");

            // 1:N relationship with Trip
            entity.HasOne(a => a.Trip)
                    .WithMany(t => t.Activities)
                    .HasForeignKey(a => a.TripId)
                    .OnDelete(DeleteBehavior.Cascade); // If a trip is deleted, its activities are deleted.

            // 1:N relationship with Day
            entity.HasOne(a => a.Day)
                  .WithMany(d => d.Activities)
                  .HasForeignKey(a => a.DayId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.SetNull); // If a day is deleted, set DayId to NULL.

            // 1:N relationship with Category
            entity.HasOne(a => a.Category)
                    .WithMany(c => c.Activities)
                    .HasForeignKey(a => a.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
        });

        // --- Spot Configuration ---
        builder.Entity<Spot>(entity =>
        {
            // entity.HasKey(s => s.Id);
            // entity.Property(s => s.Cost).HasPrecision(18, 2);
            entity.Property(s => s.Rating);

            // 1:N relationship from Spot to Country
            entity.HasOne(s => s.Country)
                .WithMany(c => c.Spots)
                .HasForeignKey(s => new { s.CountryCode, s.CountryName })
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- Accommodation Configuration ---
        builder.Entity<Accommodation>(entity =>
        {
            entity.Property(a => a.CheckInTime).HasPrecision(4, 2);
            entity.Property(a => a.CheckOutTime).HasPrecision(4, 2);
        });

        // --- Transport Configuration ---
        builder.Entity<Transport>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Name).HasMaxLength(150);
            entity.Property(t => t.Duration).HasPrecision(18, 2);
            // entity.Property(s => s.Cost).HasPrecision(18, 2);

            // Configure the two 1:N relationships from Transport to Spot
            entity.HasOne(t => t.FromSpot)
                  .WithMany(s => s.TransportsFrom)
                  .HasForeignKey(t => t.FromSpotId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.ToSpot)
                  .WithMany(s => s.TransportsTo)
                  .HasForeignKey(t => t.ToSpotId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // --- Photo Configuration ---
        builder.Entity<Photo>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(255);
            entity.Property(p => p.Alt).HasMaxLength(500);
            entity.Property(p => p.FilePath).HasMaxLength(1000);

            // 1:N relationship with Spot
            entity.HasOne(p => p.Spot)
                  .WithMany(s => s.Photos)
                  .HasForeignKey(p => p.SpotId)
                  .OnDelete(DeleteBehavior.Cascade);

            // 1:N relationship with Post
            entity.HasOne(p => p.Post)
                    .WithMany(post => post.Photos)
                    .HasForeignKey(p => p.PostId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull); // If a Post is deleted, set PostId to NULL.

            // 1:N relationship with Comment
            entity.HasOne(p => p.Comment)
                    .WithMany(c => c.Photos)
                    .HasForeignKey(p => p.CommentId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull); // If a Comment is deleted, set CommentId to NULL.
        });

        // --- Expense Configuration ---
        builder.Entity<Expense>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Value).HasPrecision(18, 2);
            entity.Property(e => e.EstimatedValue).HasPrecision(18, 2);
            entity.Property(e => e.IsEstimated).IsRequired();
            entity.Property(e => e.Multiplier).IsRequired().HasDefaultValue(1);
            entity.Property(e => e.AdditionalFee).IsRequired().HasDefaultValue(0).HasPrecision(18, 2);
            entity.Property(e => e.PercentageFee).IsRequired().HasDefaultValue(0).HasPrecision(5, 2);

            // 1:N relationship for the person who paid
            entity.HasOne(e => e.PaidBy)
                  .WithMany(p => p.PaidExpenses)
                  .HasForeignKey(e => e.PaidById)
                  .OnDelete(DeleteBehavior.Restrict); // Don't delete a person if they paid for an expense

            // 1:N relationship for the person who recived a transfer
            entity.HasOne(e => e.TransferredTo)
                  .WithMany(p => p.RecivedTransfers)
                  .HasForeignKey(e => e.TransferredToId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.Restrict); // Don't delete a person if they recived a transfer

            // 1:N relationship with Trip
            entity.HasOne(e => e.Trip)
                  .WithMany(t => t.Expenses)
                  .HasForeignKey(e => e.TripId)
                  .OnDelete(DeleteBehavior.Restrict);

            // 1:N relationship with ExchangeRate
            entity.HasOne(e => e.ExchangeRate)
                  .WithMany(c => c.Expenses)
                  .HasForeignKey(e => e.ExchangeRateId)
                  .OnDelete(DeleteBehavior.Restrict);

            // 1:1 relationship with Spot
            entity.HasOne(e => e.Spot)
                  .WithOne(s => s.Expense!)
                  .HasForeignKey<Expense>(e => e.SpotId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.NoAction);

            // 1:1 relationship with Transport
            entity.HasOne(e => e.Transport)
                  .WithOne(t => t.Expense!)
                  .HasForeignKey<Expense>(e => e.TransportId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // --- Category Configuration ---
        builder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
            entity.Property(c => c.Color).HasMaxLength(7); // e.g., #RRGGBB

            // 1:N relationship with Activity
            entity.HasMany(c => c.Activities)
                  .WithOne(a => a.Category)
                  .HasForeignKey(a => a.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // --- ExchangeRate Configuration ---
        builder.Entity<ExchangeRate>(entity =>
        {
            entity.HasKey(er => er.Id);

            var currencyCodeConverter = new EnumToStringConverter<CurrencyCode>();
            entity.Property(er => er.CurrencyCodeKey).HasConversion(currencyCodeConverter).HasMaxLength(3);

            entity.Property(er => er.ExchangeRateValue).HasPrecision(18, 6);

            // 1:N relationship with Trip
            entity.HasOne(er => er.Trip)
                .WithMany(t => t.ExchangeRates)
                .HasForeignKey(er => er.TripId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(c => new { c.TripId, c.CurrencyCodeKey, c.ExchangeRateValue })
                 .IsUnique();
        });

        // --- Post Configuration ---
        builder.Entity<Post>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Content).IsRequired();
            entity.Property(p => p.Title).IsRequired().HasMaxLength(200);
            entity.Property(p => p.CreationDate).IsRequired();
            entity.Property(p => p.EditDate).IsRequired(false);

            // 1:N relationship with Person (Author)
            entity.HasOne(p => p.Author)
                  .WithMany(person => person.Posts)
                  .HasForeignKey(p => p.AuthorId)
                  .OnDelete(DeleteBehavior.ClientNoAction); // Don't delete an author if they have posts

            // 1:N relationship with Blog
            entity.HasOne(p => p.Blog)
                .WithMany(b => b.Posts)
                .HasForeignKey(p => p.BlogId)
                .OnDelete(DeleteBehavior.Cascade);

            // 1:N relationship with Day
            entity.HasOne(p => p.Day)
                .WithMany(d => d.Posts)
                .HasForeignKey(p => p.DayId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // 1:N relationship with Comments
            entity.HasMany(p => p.Comments)
                .WithOne(c => c.Post)
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes for better performance
            entity.HasIndex(p => p.AuthorId);
            entity.HasIndex(p => p.BlogId);
            entity.HasIndex(p => p.DayId);
            entity.HasIndex(p => p.CreationDate);
        });

        // --- Comment Configuration ---
        builder.Entity<Comment>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Content).IsRequired().HasMaxLength(2000);

            // 1:N relationship with Person (Author)
            entity.HasOne(c => c.Author)
                  .WithMany(person => person.Comments)
                  .HasForeignKey(c => c.AuthorId)
                  .OnDelete(DeleteBehavior.Restrict); // Don't delete an author if they have comments

            // 1:N relationship with Post
            entity.HasOne(c => c.Post)
                  .WithMany(p => p.Comments)
                  .HasForeignKey(c => c.PostId)
                  .OnDelete(DeleteBehavior.Restrict); // Don't delete a post if it has comments.
        });

        // --- Notification configuration ---
        builder.Entity<Notification>(entity =>
        {
            entity.HasKey(n => n.Id);

            entity.Property(n => n.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(n => n.Content)
                .IsRequired();

            entity.Property(n => n.ScheduledFor)
                .IsRequired();

            entity.Property(n => n.CreatedAt)
                .IsRequired();

            // 1:N relationship with Person (User)
            entity.HasOne(n => n.User)
                .WithMany(person => person.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade); // If a person is deleted, their notifications are deleted.

            // Index for better performance
            entity.HasIndex(n => n.UserId);
            entity.HasIndex(n => n.ScheduledFor);
            entity.HasIndex(n => n.IsSent);
        });

        // --- Country Configuration ---
        builder.Entity<Country>(entity =>
        {
            // Composite key configuration
            entity.HasKey(c => new { c.Code, c.Name });

            entity.Property(c => c.Code)
                  .IsRequired()
                  .HasMaxLength(3);

            entity.Property(c => c.Name)
                  .IsRequired()
                  .HasMaxLength(100);
        });

        // --- ExpenseParticipant Configuration ---
        builder.Entity<ExpenseParticipant>(entity =>
        {
            entity.HasKey(ep => new { ep.ExpenseId, ep.PersonId });

            entity.Property(ep => ep.Share)
                  .IsRequired()
                  .HasPrecision(18, 3);

            entity.Property(ep => ep.ActualShareValue)
                  .IsRequired()
                  .HasPrecision(18, 2);

            entity.HasOne(ep => ep.Expense)
                  .WithMany(e => e.Participants)
                  .HasForeignKey(ep => ep.ExpenseId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ep => ep.Person)
                  .WithMany(p => p.ExpensesToCover)
                  .HasForeignKey(ep => ep.PersonId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable("ExpenseParticipants");
        });

        // --- ChatMessage Configuration ---
        builder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(cm => cm.Id);

            entity.Property(cm => cm.Message).IsRequired().HasMaxLength(500);

            // 1:N relationship with Person (Author)
            entity.HasOne(cm => cm.Person)
                  .WithMany(person => person.ChatMessages)
                  .HasForeignKey(cm => cm.PersonId)
                  .OnDelete(DeleteBehavior.Cascade);

            // 1:N relationship with Trip
            entity.HasOne(cm => cm.Trip)
                .WithMany(t => t.ChatMessages)
                .HasForeignKey(cm => cm.TripId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- File Configuration ---
        builder.Entity<Domain.Entities.File>(entity =>
        {
            entity.HasKey(f => f.Id);
            entity.Property(f => f.Name).IsRequired(false).HasMaxLength(200);

            // 1:N relationship with Trip
            entity.HasOne(f => f.Spot)
                .WithMany(t => t.Files)
                .HasForeignKey(f => f.SpotId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- Blog Configuration ---
        builder.Entity<Blog>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.Property(b => b.Name).IsRequired().HasMaxLength(200);
            entity.Property(b => b.Description).HasMaxLength(1000);
            entity.Property(t => t.Catalog).HasMaxLength(100);

            // 1:N relationship with Person (Owner)
            entity.HasOne(b => b.Owner)
                .WithMany(p => p.Blogs)
                .HasForeignKey(b => b.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            // 1:1 relationship with Trip
            entity.HasOne(b => b.Trip)
                .WithOne(t => t.Blog)
                .HasForeignKey<Blog>(b => b.TripId)
                .OnDelete(DeleteBehavior.Restrict);

            // 1:N relationship with Posts
            entity.HasMany(b => b.Posts)
                .WithOne(p => p.Blog)
                .HasForeignKey(p => p.BlogId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes for better performance
            entity.HasIndex(b => b.OwnerId);
            entity.HasIndex(b => b.TripId).IsUnique();
        });
    }
}
