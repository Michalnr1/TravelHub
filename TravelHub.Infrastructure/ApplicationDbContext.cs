using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
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

            // M:N self-referencing relationship for Friends
            entity.HasMany(p => p.Friends)
                  .WithMany(p => p.FriendOf)
                  .UsingEntity<Dictionary<string, object>>(
                          "PersonFriends",
                          j => j.HasOne<Person>().WithMany().HasForeignKey("FriendId"),
                          j => j.HasOne<Person>().WithMany().HasForeignKey("UserId"),
                          j =>
                          {
                              j.HasKey("UserId", "FriendId");
                              j.ToTable("PersonFriends");
                          });

            // M:N relationship with Expense
            entity.HasMany(p => p.ExpensesToCover)
                  .WithMany(e => e.Participants)
                  .UsingEntity(j => j.ToTable("ExpenseParticipants"));
        });

        // --- Trip Configuration ---
        builder.Entity<Trip>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Name).IsRequired().HasMaxLength(200);
            entity.Property(t => t.StartDate).HasColumnType("date");
            entity.Property(t => t.EndDate).HasColumnType("date");
            entity.Property(t => t.IsPrivate).IsRequired();

            var currencyCodeConverter = new EnumToStringConverter<CurrencyCode>();
            entity.Property(t => t.CurrencyCode)
                  .HasConversion(currencyCodeConverter)
                  .HasMaxLength(3);

            // 1:N relationship with Person (Trip organizer)
            entity.HasOne(t => t.Person)
                  .WithMany(p => p.Trips)
                  .HasForeignKey(t => t.PersonId)
                  .OnDelete(DeleteBehavior.Cascade); // If a person is deleted, their trips are deleted.

            entity.HasMany(t => t.Countries)
                  .WithMany(c => c.Trips)
                  .UsingEntity(j => j.ToTable("TripCountries"));
        });

        // --- Day Configuration ---
        builder.Entity<Day>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Date).HasColumnType("date");

            // 1:N relationship with Trip
            entity.HasOne(d => d.Trip)
                  .WithMany(t => t.Days)
                  .HasForeignKey(d => d.TripId)
                  .OnDelete(DeleteBehavior.Restrict); // If a trip is deleted, its days are deleted.
        });

        // --- Activity Configuration ---
        builder.Entity<Activity>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Name).IsRequired().HasMaxLength(200);
            entity.Property(a => a.Description).HasMaxLength(1000);
            entity.Property(a => a.Duration).HasPrecision(18, 2);

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
            entity.Property(s => s.Cost).HasPrecision(18, 2);
            entity.Property(s => s.Rating);
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
            entity.Property(s => s.Cost).HasPrecision(18, 2);

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

            // 1:N relationship for the person who paid
            entity.HasOne(e => e.PaidBy)
                  .WithMany(p => p.PaidExpenses)
                  .HasForeignKey(e => e.PaidById)
                  .OnDelete(DeleteBehavior.Restrict); // Don't delete a person if they paid for an expense

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

            // 1:N relationship with Person (Author)
            entity.HasOne(p => p.Author)
                  .WithMany(person => person.Posts)
                  .HasForeignKey(p => p.AuthorId)
                  .OnDelete(DeleteBehavior.Restrict); // Don't delete an author if they have posts
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

        // --- Country Configuration ---
        builder.Entity<Country>(entity =>
        {
            // Composite key configuration
            entity.HasKey(c => new { c.Code, c.Name });

            entity.Property(c => c.Code).IsRequired().HasMaxLength(3);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
        });
    }
}
