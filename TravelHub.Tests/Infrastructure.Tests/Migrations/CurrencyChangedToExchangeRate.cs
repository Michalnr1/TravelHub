using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using TravelHub.Infrastructure;
using Xunit;

namespace TravelHub.Tests.Infrastructure.Tests.Migrations;

public class CurrencyChangedToExchangeRateMigrationTests
{
    private readonly Assembly _migrationsAssembly;

    public CurrencyChangedToExchangeRateMigrationTests()
    {
        _migrationsAssembly = typeof(ApplicationDbContext).Assembly;
    }

    [Fact]
    public void Up_ShouldChangeCurrencyStructure()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeUpMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(14, migrationBuilder.Operations.Count);

        // Verify DropForeignKey
        var dropForeignKey = migrationBuilder.Operations[0] as DropForeignKeyOperation;
        Assert.NotNull(dropForeignKey);
        Assert.Equal("Expenses", dropForeignKey.Table);

        // Verify DropIndex
        var dropIndex = migrationBuilder.Operations[1] as DropIndexOperation;
        Assert.NotNull(dropIndex);
        Assert.Equal("Expenses", dropIndex.Table);

        // Verify DropPrimaryKey
        var dropPrimaryKey = migrationBuilder.Operations[2] as DropPrimaryKeyOperation;
        Assert.NotNull(dropPrimaryKey);
        Assert.Equal("Currencies", dropPrimaryKey.Table);

        // Verify RenameColumn operations
        var renameColumns = migrationBuilder.Operations.OfType<RenameColumnOperation>().ToList();
        Assert.Equal(2, renameColumns.Count);

        // Verify AddColumn operations
        var addColumns = migrationBuilder.Operations.OfType<AddColumnOperation>().ToList();
        Assert.Equal(3, addColumns.Count);

        var exchangeRateIdColumn = addColumns.First(op => op.Name == "ExchangeRateId");
        Assert.Equal("Expenses", exchangeRateIdColumn.Table);
        Assert.False(exchangeRateIdColumn.IsNullable);

        var idColumn = addColumns.First(op => op.Name == "Id");
        Assert.Equal("Currencies", idColumn.Table);
        Assert.False(idColumn.IsNullable);

        // Verify AddPrimaryKey
        var addPrimaryKey = migrationBuilder.Operations.OfType<AddPrimaryKeyOperation>().First();
        Assert.NotNull(addPrimaryKey);
        Assert.Equal("Currencies", addPrimaryKey.Table);

        // Verify CreateIndex operations
        var createIndexes = migrationBuilder.Operations.OfType<CreateIndexOperation>().ToList();
        Assert.Equal(2, createIndexes.Count);

        // Verify AddForeignKey operations
        var addForeignKeys = migrationBuilder.Operations.OfType<AddForeignKeyOperation>().ToList();
        Assert.Equal(2, addForeignKeys.Count);
    }

    [Fact]
    public void Down_ShouldRevertCurrencyStructure()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeDownMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(14, migrationBuilder.Operations.Count);

        // Verify Drop operations
        var dropForeignKeys = migrationBuilder.Operations.OfType<DropForeignKeyOperation>().ToList();
        Assert.Equal(2, dropForeignKeys.Count);

        var dropIndexes = migrationBuilder.Operations.OfType<DropIndexOperation>().ToList();
        Assert.Equal(2, dropIndexes.Count);

        var dropPrimaryKey = migrationBuilder.Operations.OfType<DropPrimaryKeyOperation>().First();
        Assert.NotNull(dropPrimaryKey);
        Assert.Equal("Currencies", dropPrimaryKey.Table);

        // Verify RenameColumn operations
        var renameColumns = migrationBuilder.Operations.OfType<RenameColumnOperation>().ToList();
        Assert.Equal(2, renameColumns.Count);

        // Verify AddColumn
        var addColumn = migrationBuilder.Operations.OfType<AddColumnOperation>().First();
        Assert.NotNull(addColumn);
        Assert.Equal("CurrencyKey", addColumn.Name);
        Assert.Equal("Expenses", addColumn.Table);

        // Verify AddPrimaryKey
        var addPrimaryKey = migrationBuilder.Operations.OfType<AddPrimaryKeyOperation>().First();
        Assert.NotNull(addPrimaryKey);
        Assert.Equal("Currencies", addPrimaryKey.Table);

        // Verify CreateIndex
        var createIndex = migrationBuilder.Operations.OfType<CreateIndexOperation>().First();
        Assert.NotNull(createIndex);
        Assert.Equal("Expenses", createIndex.Table);

        // Verify AddForeignKey
        var addForeignKey = migrationBuilder.Operations.OfType<AddForeignKeyOperation>().First();
        Assert.NotNull(addForeignKey);
        Assert.Equal("Expenses", addForeignKey.Table);
    }

    #region Helper Methods

    private object CreateMigrationInstance()
    {
        var migrationType = _migrationsAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "CurrencyChangedToExchangeRate");

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