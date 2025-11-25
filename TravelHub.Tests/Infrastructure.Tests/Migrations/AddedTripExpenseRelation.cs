using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using TravelHub.Infrastructure;
using Xunit;

namespace TravelHub.Tests.Infrastructure.Tests.Migrations;

public class AddedTripExpenseRelationMigrationTests
{
    private readonly Assembly _migrationsAssembly;

    public AddedTripExpenseRelationMigrationTests()
    {
        _migrationsAssembly = typeof(ApplicationDbContext).Assembly;
    }

    [Fact]
    public void Up_ShouldAddTripIdToExpenses()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeUpMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(3, migrationBuilder.Operations.Count);

        // Verify AddColumn
        var addColumn = migrationBuilder.Operations[0] as AddColumnOperation;
        Assert.NotNull(addColumn);
        Assert.Equal("TripId", addColumn.Name);
        Assert.Equal("Expenses", addColumn.Table);
        Assert.Equal(typeof(int), addColumn.ClrType);
        Assert.False(addColumn.IsNullable);
        Assert.Equal(0, addColumn.DefaultValue);

        // Verify CreateIndex
        var createIndex = migrationBuilder.Operations[1] as CreateIndexOperation;
        Assert.NotNull(createIndex);
        Assert.Equal("Expenses", createIndex.Table);
        Assert.Equal("TripId", createIndex.Columns[0]);

        // Verify AddForeignKey
        var addForeignKey = migrationBuilder.Operations[2] as AddForeignKeyOperation;
        Assert.NotNull(addForeignKey);
        Assert.Equal("Expenses", addForeignKey.Table);
        Assert.Equal("TripId", addForeignKey.Columns[0]);
        Assert.Equal("Trips", addForeignKey.PrincipalTable);
    }

    [Fact]
    public void Down_ShouldRemoveTripIdFromExpenses()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeDownMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(3, migrationBuilder.Operations.Count);

        // Verify DropForeignKey
        var dropForeignKey = migrationBuilder.Operations[0] as DropForeignKeyOperation;
        Assert.NotNull(dropForeignKey);
        Assert.Equal("Expenses", dropForeignKey.Table);

        // Verify DropIndex
        var dropIndex = migrationBuilder.Operations[1] as DropIndexOperation;
        Assert.NotNull(dropIndex);
        Assert.Equal("Expenses", dropIndex.Table);

        // Verify DropColumn
        var dropColumn = migrationBuilder.Operations[2] as DropColumnOperation;
        Assert.NotNull(dropColumn);
        Assert.Equal("TripId", dropColumn.Name);
        Assert.Equal("Expenses", dropColumn.Table);
    }

    #region Helper Methods

    private object CreateMigrationInstance()
    {
        var migrationType = _migrationsAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "AddedTripExpenseRelation");

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