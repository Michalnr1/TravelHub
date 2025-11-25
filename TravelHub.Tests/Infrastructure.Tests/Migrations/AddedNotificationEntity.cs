using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using TravelHub.Infrastructure;
using Xunit;

namespace TravelHub.Tests.Infrastructure.Tests.Migrations;

public class AddedNotificationEntityMigrationTests
{
    private readonly Assembly _migrationsAssembly;

    public AddedNotificationEntityMigrationTests()
    {
        _migrationsAssembly = typeof(ApplicationDbContext).Assembly;
    }

    [Fact]
    public void Up_ShouldCreateNotificationsTable()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeUpMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(4, migrationBuilder.Operations.Count);

        // Verify CreateTable operation
        var createTableOperation = migrationBuilder.Operations[0] as CreateTableOperation;
        Assert.NotNull(createTableOperation);
        Assert.Equal("Notifications", createTableOperation.Name);
        Assert.Equal(8, createTableOperation.Columns.Count); // Id, UserId, Title, Content, ScheduledFor, SentAt, IsSent, CreatedAt

        // Verify CreateIndex operations
        var createIndexOperations = migrationBuilder.Operations.OfType<CreateIndexOperation>().ToList();
        Assert.Equal(3, createIndexOperations.Count);
        Assert.All(createIndexOperations, op => Assert.Equal("Notifications", op.Table));
        Assert.Contains(createIndexOperations, op => op.Columns.Contains("IsSent"));
        Assert.Contains(createIndexOperations, op => op.Columns.Contains("ScheduledFor"));
        Assert.Contains(createIndexOperations, op => op.Columns.Contains("UserId"));
    }

    [Fact]
    public void Down_ShouldDropNotificationsTable()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeDownMethod(migration, migrationBuilder);

        // Assert
        var operation = migrationBuilder.Operations[0] as DropTableOperation;
        Assert.NotNull(operation);
        Assert.Equal("Notifications", operation.Name);
    }

    #region Helper Methods

    private object CreateMigrationInstance()
    {
        var migrationType = _migrationsAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "AddedNotificationEntity");

        if (migrationType == null)
        {
            throw new InvalidOperationException("AddedNotificationEntity migration type not found");
        }

        return Activator.CreateInstance(migrationType, nonPublic: true);
    }

    private void InvokeUpMethod(object migrationInstance, MigrationBuilder migrationBuilder)
    {
        var upMethod = migrationInstance.GetType().GetMethod("Up",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        upMethod?.Invoke(migrationInstance, new object[] { migrationBuilder });
    }

    private void InvokeDownMethod(object migrationInstance, MigrationBuilder migrationBuilder)
    {
        var downMethod = migrationInstance.GetType().GetMethod("Down",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        downMethod?.Invoke(migrationInstance, new object[] { migrationBuilder });
    }

    #endregion
}