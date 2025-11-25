using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using TravelHub.Infrastructure;
using Xunit;

namespace TravelHub.Tests.Infrastructure.Tests.Migrations;

public class AddedSpotAndTransportConnectionToExpenseMigrationTests
{
    private readonly Assembly _migrationsAssembly;

    public AddedSpotAndTransportConnectionToExpenseMigrationTests()
    {
        _migrationsAssembly = typeof(ApplicationDbContext).Assembly;
    }

    [Fact]
    public void Up_ShouldAddConnectionsAndRemoveCostColumns()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeUpMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(9, migrationBuilder.Operations.Count);

        // Verify DropColumn operations
        var dropColumnOperations = migrationBuilder.Operations.OfType<DropColumnOperation>().ToList();
        Assert.Equal(2, dropColumnOperations.Count);
        Assert.Contains(dropColumnOperations, op => op.Name == "Cost" && op.Table == "Transports");
        Assert.Contains(dropColumnOperations, op => op.Name == "Cost" && op.Table == "Activities");

        // Verify AddColumn operations for Expenses
        var addColumnOperations = migrationBuilder.Operations.OfType<AddColumnOperation>().ToList();
        Assert.Equal(3, addColumnOperations.Count);

        var isEstimatedOperation = addColumnOperations.First(op => op.Name == "IsEstimated");
        Assert.Equal("Expenses", isEstimatedOperation.Table);
        Assert.Equal(typeof(bool), isEstimatedOperation.ClrType);
        Assert.False(isEstimatedOperation.IsNullable);
        Assert.Equal(false, isEstimatedOperation.DefaultValue);

        var spotIdOperation = addColumnOperations.First(op => op.Name == "SpotId");
        Assert.Equal("Expenses", spotIdOperation.Table);
        Assert.True(spotIdOperation.IsNullable);

        var transportIdOperation = addColumnOperations.First(op => op.Name == "TransportId");
        Assert.Equal("Expenses", transportIdOperation.Table);
        Assert.True(transportIdOperation.IsNullable);

        // Verify CreateIndex operations
        var createIndexOperations = migrationBuilder.Operations.OfType<CreateIndexOperation>().ToList();
        Assert.Equal(2, createIndexOperations.Count);
        Assert.Contains(createIndexOperations, op => op.Columns.Contains("SpotId") && op.Table == "Expenses");
        Assert.Contains(createIndexOperations, op => op.Columns.Contains("TransportId") && op.Table == "Expenses");

        // Verify AddForeignKey operations
        var addForeignKeyOperations = migrationBuilder.Operations.OfType<AddForeignKeyOperation>().ToList();
        Assert.Equal(2, addForeignKeyOperations.Count);
        Assert.Contains(addForeignKeyOperations, op => op.Columns.Contains("SpotId") && op.Table == "Expenses");
        Assert.Contains(addForeignKeyOperations, op => op.Columns.Contains("TransportId") && op.Table == "Expenses");
    }

    [Fact]
    public void Down_ShouldRemoveConnectionsAndAddCostColumns()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeDownMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(9, migrationBuilder.Operations.Count);

        // Verify DropForeignKey operations
        var dropForeignKeyOperations = migrationBuilder.Operations.OfType<DropForeignKeyOperation>().ToList();
        Assert.Equal(2, dropForeignKeyOperations.Count);
        Assert.All(dropForeignKeyOperations, op => Assert.Equal("Expenses", op.Table));

        // Verify DropIndex operations
        var dropIndexOperations = migrationBuilder.Operations.OfType<DropIndexOperation>().ToList();
        Assert.Equal(2, dropIndexOperations.Count);
        Assert.All(dropIndexOperations, op => Assert.Equal("Expenses", op.Table));

        // Verify DropColumn operations for Expenses
        var dropColumnOperations = migrationBuilder.Operations.OfType<DropColumnOperation>().ToList();
        Assert.Equal(3, dropColumnOperations.Count);
        Assert.All(dropColumnOperations, op => Assert.Equal("Expenses", op.Table));

        // Verify AddColumn operations
        var addColumnOperations = migrationBuilder.Operations.OfType<AddColumnOperation>().ToList();
        Assert.Equal(2, addColumnOperations.Count);

        var transportCostOperation = addColumnOperations.First(op => op.Table == "Transports");
        Assert.Equal("Cost", transportCostOperation.Name);
        Assert.Equal("decimal(18,2)", transportCostOperation.ColumnType);
        Assert.False(transportCostOperation.IsNullable);
        Assert.Equal(0m, transportCostOperation.DefaultValue);

        var activityCostOperation = addColumnOperations.First(op => op.Table == "Activities");
        Assert.Equal("Cost", activityCostOperation.Name);
        Assert.Equal("decimal(18,2)", activityCostOperation.ColumnType);
        Assert.True(activityCostOperation.IsNullable);
    }

    #region Helper Methods

    private object CreateMigrationInstance()
    {
        var migrationType = _migrationsAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "AddedSpotAndTransportConnectionToExpense");

        if (migrationType == null)
        {
            throw new InvalidOperationException("AddedSpotAndTransportConnectionToExpense migration type not found");
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
