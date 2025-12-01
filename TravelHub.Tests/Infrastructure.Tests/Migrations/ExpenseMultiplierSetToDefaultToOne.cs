using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using TravelHub.Infrastructure;
using Xunit;

namespace TravelHub.Tests.Infrastructure.Tests.Migrations;

public class ExpenseMultiplierSetToDefaultToOneMigrationTests
{
    private readonly Assembly _migrationsAssembly;

    public ExpenseMultiplierSetToDefaultToOneMigrationTests()
    {
        _migrationsAssembly = typeof(ApplicationDbContext).Assembly;
    }

    [Fact]
    public void Up_ShouldSetMultiplierDefaultValueToOne()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeUpMethod(migration, migrationBuilder);

        // Assert
        var operation = migrationBuilder.Operations[0] as AlterColumnOperation;
        Assert.NotNull(operation);
        Assert.Equal("Multiplier", operation.Name);
        Assert.Equal("Expenses", operation.Table);
        Assert.Equal(typeof(int), operation.ClrType);
        Assert.Equal("int", operation.ColumnType);
        Assert.False(operation.IsNullable);
        Assert.Equal(1, operation.DefaultValue);
    }

    [Fact]
    public void Down_ShouldRemoveDefaultValue()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeDownMethod(migration, migrationBuilder);

        // Assert
        var operation = migrationBuilder.Operations[0] as AlterColumnOperation;
        Assert.NotNull(operation);
        Assert.Equal("Multiplier", operation.Name);
        Assert.Equal("Expenses", operation.Table);
        Assert.Equal(typeof(int), operation.ClrType);
        Assert.Equal("int", operation.ColumnType);
        Assert.False(operation.IsNullable);
        Assert.Null(operation.DefaultValue);
    }

    #region Helper Methods

    private object CreateMigrationInstance()
    {
        var migrationType = _migrationsAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "ExpenseMultiplierSetToDefaultToOne");

        if (migrationType == null)
        {
            throw new InvalidOperationException("ExpenseMultiplierSetToDefaultToOne migration type not found");
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