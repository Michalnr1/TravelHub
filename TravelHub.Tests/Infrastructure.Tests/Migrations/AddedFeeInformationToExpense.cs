using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using TravelHub.Infrastructure;
using Xunit;

namespace TravelHub.Tests.Infrastructure.Tests.Migrations;

public class AddedFeeInformationToExpenseMigrationTests
{
    private readonly Assembly _migrationsAssembly;

    public AddedFeeInformationToExpenseMigrationTests()
    {
        _migrationsAssembly = typeof(ApplicationDbContext).Assembly;
    }

    [Fact]
    public void Up_ShouldAddAdditionalFeeAndPercentageFeeColumns()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeUpMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(2, migrationBuilder.Operations.Count);

        var additionalFeeOperation = migrationBuilder.Operations[0] as AddColumnOperation;
        var percentageFeeOperation = migrationBuilder.Operations[1] as AddColumnOperation;

        // Verify AdditionalFee column
        Assert.NotNull(additionalFeeOperation);
        Assert.Equal("AdditionalFee", additionalFeeOperation.Name);
        Assert.Equal("Expenses", additionalFeeOperation.Table);
        Assert.Equal(typeof(decimal), additionalFeeOperation.ClrType);
        Assert.Equal("decimal(18,2)", additionalFeeOperation.ColumnType);
        Assert.Equal(18, additionalFeeOperation.Precision);
        Assert.Equal(2, additionalFeeOperation.Scale);
        Assert.False(additionalFeeOperation.IsNullable);
        Assert.Equal(0m, additionalFeeOperation.DefaultValue);

        // Verify PercentageFee column
        Assert.NotNull(percentageFeeOperation);
        Assert.Equal("PercentageFee", percentageFeeOperation.Name);
        Assert.Equal("Expenses", percentageFeeOperation.Table);
        Assert.Equal(typeof(decimal), percentageFeeOperation.ClrType);
        Assert.Equal("decimal(5,2)", percentageFeeOperation.ColumnType);
        Assert.Equal(5, percentageFeeOperation.Precision);
        Assert.Equal(2, percentageFeeOperation.Scale);
        Assert.False(percentageFeeOperation.IsNullable);
        Assert.Equal(0m, percentageFeeOperation.DefaultValue);
    }

    [Fact]
    public void Down_ShouldRemoveBothColumns()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeDownMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(2, migrationBuilder.Operations.Count);

        var dropAdditionalFeeOperation = migrationBuilder.Operations[0] as DropColumnOperation;
        var dropPercentageFeeOperation = migrationBuilder.Operations[1] as DropColumnOperation;

        Assert.NotNull(dropAdditionalFeeOperation);
        Assert.Equal("AdditionalFee", dropAdditionalFeeOperation.Name);
        Assert.Equal("Expenses", dropAdditionalFeeOperation.Table);

        Assert.NotNull(dropPercentageFeeOperation);
        Assert.Equal("PercentageFee", dropPercentageFeeOperation.Name);
        Assert.Equal("Expenses", dropPercentageFeeOperation.Table);
    }

    #region Helper Methods

    private object CreateMigrationInstance()
    {
        var migrationType = _migrationsAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "AddedFeeInformationToExpense");

        if (migrationType == null)
        {
            throw new InvalidOperationException("AddedFeeInformationToExpense migration type not found");
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