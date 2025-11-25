using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using TravelHub.Infrastructure;
using Xunit;

namespace TravelHub.Tests.Infrastructure.Tests.Migrations;

public class AppDbContextChangeMigrationTests
{
    private readonly Assembly _migrationsAssembly;

    public AppDbContextChangeMigrationTests()
    {
        _migrationsAssembly = typeof(ApplicationDbContext).Assembly;
    }

    [Fact]
    public void Up_ShouldRenameCurrenciesToExchangeRates()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeUpMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(8, migrationBuilder.Operations.Count);

        // Verify DropForeignKey operations
        var dropForeignKeyOperations = migrationBuilder.Operations.OfType<DropForeignKeyOperation>().ToList();
        Assert.Equal(2, dropForeignKeyOperations.Count);
        Assert.Contains(dropForeignKeyOperations, op => op.Table == "Currencies");
        Assert.Contains(dropForeignKeyOperations, op => op.Table == "Expenses");

        // Verify DropPrimaryKey operation
        var dropPrimaryKeyOperation = migrationBuilder.Operations[2] as DropPrimaryKeyOperation;
        Assert.NotNull(dropPrimaryKeyOperation);
        Assert.Equal("Currencies", dropPrimaryKeyOperation.Table);

        // Verify RenameTable operation
        var renameTableOperation = migrationBuilder.Operations[3] as RenameTableOperation;
        Assert.NotNull(renameTableOperation);
        Assert.Equal("Currencies", renameTableOperation.Name);
        Assert.Equal("ExchangeRates", renameTableOperation.NewName);

        // Verify RenameIndex operation
        var renameIndexOperation = migrationBuilder.Operations[4] as RenameIndexOperation;
        Assert.NotNull(renameIndexOperation);
        Assert.Equal("ExchangeRates", renameIndexOperation.Table);

        // Verify AddPrimaryKey operation is part of RenameTable
        var addPrimaryKeyOperation = migrationBuilder.Operations.OfType<AddPrimaryKeyOperation>().First();
        Assert.NotNull(addPrimaryKeyOperation);
        Assert.Equal("ExchangeRates", addPrimaryKeyOperation.Table);

        // Verify AddForeignKey operations
        var addForeignKeyOperations = migrationBuilder.Operations.OfType<AddForeignKeyOperation>().ToList();
        Assert.Equal(2, addForeignKeyOperations.Count);
        Assert.Contains(addForeignKeyOperations, op => op.Table == "ExchangeRates");
        Assert.Contains(addForeignKeyOperations, op => op.Table == "Expenses");
    }

    [Fact]
    public void Down_ShouldRenameExchangeRatesToCurrencies()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeDownMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(8, migrationBuilder.Operations.Count);

        // Verify DropForeignKey operations
        var dropForeignKeyOperations = migrationBuilder.Operations.OfType<DropForeignKeyOperation>().ToList();
        Assert.Equal(2, dropForeignKeyOperations.Count);
        Assert.Contains(dropForeignKeyOperations, op => op.Table == "ExchangeRates");
        Assert.Contains(dropForeignKeyOperations, op => op.Table == "Expenses");

        // Verify DropPrimaryKey operation
        var dropPrimaryKeyOperation = migrationBuilder.Operations[2] as DropPrimaryKeyOperation;
        Assert.NotNull(dropPrimaryKeyOperation);
        Assert.Equal("ExchangeRates", dropPrimaryKeyOperation.Table);

        // Verify RenameTable operation
        var renameTableOperation = migrationBuilder.Operations[3] as RenameTableOperation;
        Assert.NotNull(renameTableOperation);
        Assert.Equal("ExchangeRates", renameTableOperation.Name);
        Assert.Equal("Currencies", renameTableOperation.NewName);

        // Verify RenameIndex operation
        var renameIndexOperation = migrationBuilder.Operations[4] as RenameIndexOperation;
        Assert.NotNull(renameIndexOperation);
        Assert.Equal("Currencies", renameIndexOperation.Table);

        // Verify AddForeignKey operations
        var addForeignKeyOperations = migrationBuilder.Operations.OfType<AddForeignKeyOperation>().ToList();
        Assert.Equal(2, addForeignKeyOperations.Count);
        Assert.Contains(addForeignKeyOperations, op => op.Table == "Currencies");
        Assert.Contains(addForeignKeyOperations, op => op.Table == "Expenses");
    }

    #region Helper Methods

    private object CreateMigrationInstance()
    {
        var migrationType = _migrationsAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "AppDbContextChange");

        if (migrationType == null)
        {
            throw new InvalidOperationException("AppDbContextChange migration type not found");
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