using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using TravelHub.Infrastructure;
using Xunit;

namespace TravelHub.Tests.Infrastructure.Tests.Migrations;

public class ChangeIdentityAndAddEmailConfirmationMigrationTests
{
    private readonly Assembly _migrationsAssembly;

    public ChangeIdentityAndAddEmailConfirmationMigrationTests()
    {
        _migrationsAssembly = typeof(ApplicationDbContext).Assembly;
    }

    [Fact]
    public void Up_ShouldCreateAllTables()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeUpMethod(migration, migrationBuilder);

        // Assert
        // Verify CreateTable operations count
        var createTableOperations = migrationBuilder.Operations.OfType<CreateTableOperation>().ToList();
        Assert.Equal(21, createTableOperations.Count);

        // Verify key tables exist
        Assert.Contains(createTableOperations, op => op.Name == "AspNetRoles");
        Assert.Contains(createTableOperations, op => op.Name == "AspNetUsers");
        Assert.Contains(createTableOperations, op => op.Name == "Categories");
        Assert.Contains(createTableOperations, op => op.Name == "Currencies");
        Assert.Contains(createTableOperations, op => op.Name == "AspNetRoleClaims");
        Assert.Contains(createTableOperations, op => op.Name == "AspNetUserClaims");
        Assert.Contains(createTableOperations, op => op.Name == "AspNetUserLogins");
        Assert.Contains(createTableOperations, op => op.Name == "AspNetUserRoles");
        Assert.Contains(createTableOperations, op => op.Name == "AspNetUserTokens");
        Assert.Contains(createTableOperations, op => op.Name == "PersonFriends");
        Assert.Contains(createTableOperations, op => op.Name == "Posts");
        Assert.Contains(createTableOperations, op => op.Name == "Trips");
        Assert.Contains(createTableOperations, op => op.Name == "Expenses");
        Assert.Contains(createTableOperations, op => op.Name == "Comments");
        Assert.Contains(createTableOperations, op => op.Name == "Days");
        Assert.Contains(createTableOperations, op => op.Name == "ExpenseParticipants");
        Assert.Contains(createTableOperations, op => op.Name == "Activities");
        Assert.Contains(createTableOperations, op => op.Name == "Spots");
        Assert.Contains(createTableOperations, op => op.Name == "Accommodations");
        Assert.Contains(createTableOperations, op => op.Name == "Photos");
        Assert.Contains(createTableOperations, op => op.Name == "Transports");

        // Verify CreateIndex operations
        var createIndexOperations = migrationBuilder.Operations.OfType<CreateIndexOperation>().ToList();
        Assert.True(createIndexOperations.Count > 0);

        // Verify AddForeignKey operations
        var addForeignKeyOperations = migrationBuilder.Operations.OfType<AddForeignKeyOperation>().ToList();
        Assert.True(addForeignKeyOperations.Count == 0);
    }

    [Fact]
    public void Down_ShouldDropAllTables()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeDownMethod(migration, migrationBuilder);

        // Assert
        // This migration drops all tables in one operation
        var dropTableOperations = migrationBuilder.Operations.OfType<DropTableOperation>().ToList();
        Assert.Equal(21, dropTableOperations.Count);

        // Verify key tables are dropped
        Assert.Contains(dropTableOperations, op => op.Name == "Accommodations");
        Assert.Contains(dropTableOperations, op => op.Name == "AspNetRoleClaims");
        Assert.Contains(dropTableOperations, op => op.Name == "AspNetUserClaims");
        Assert.Contains(dropTableOperations, op => op.Name == "AspNetUserLogins");
        Assert.Contains(dropTableOperations, op => op.Name == "AspNetUserRoles");
        Assert.Contains(dropTableOperations, op => op.Name == "AspNetUserTokens");
        Assert.Contains(dropTableOperations, op => op.Name == "Comments");
        Assert.Contains(dropTableOperations, op => op.Name == "ExpenseParticipants");
        Assert.Contains(dropTableOperations, op => op.Name == "PersonFriends");
        Assert.Contains(dropTableOperations, op => op.Name == "Photos");
        Assert.Contains(dropTableOperations, op => op.Name == "Transports");
        Assert.Contains(dropTableOperations, op => op.Name == "AspNetRoles");
        Assert.Contains(dropTableOperations, op => op.Name == "Posts");
        Assert.Contains(dropTableOperations, op => op.Name == "Expenses");
        Assert.Contains(dropTableOperations, op => op.Name == "Spots");
        Assert.Contains(dropTableOperations, op => op.Name == "Categories");
        Assert.Contains(dropTableOperations, op => op.Name == "Currencies");
        Assert.Contains(dropTableOperations, op => op.Name == "Activities");
        Assert.Contains(dropTableOperations, op => op.Name == "Days");
        Assert.Contains(dropTableOperations, op => op.Name == "Trips");
        Assert.Contains(dropTableOperations, op => op.Name == "AspNetUsers");
    }

    #region Helper Methods

    private object CreateMigrationInstance()
    {
        var migrationType = _migrationsAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "ChangeIdentityAndAddEmailConfirmation");

        return migrationType != null
            ? Activator.CreateInstance(migrationType, nonPublic: true)
            : null;
    }

    private void InvokeUpMethod(object migrationInstance, MigrationBuilder migrationBuilder)
    {
        if (migrationInstance == null) return;

        var upMethod = migrationInstance.GetType().GetMethod("Up",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        upMethod?.Invoke(migrationInstance, new object[] { migrationBuilder });
    }

    private void InvokeDownMethod(object migrationInstance, MigrationBuilder migrationBuilder)
    {
        if (migrationInstance == null) return;

        var downMethod = migrationInstance.GetType().GetMethod("Down",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        downMethod?.Invoke(migrationInstance, new object[] { migrationBuilder });
    }

    #endregion
}