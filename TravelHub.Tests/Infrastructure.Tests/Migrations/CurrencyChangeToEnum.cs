using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using TravelHub.Infrastructure;
using Xunit;

namespace TravelHub.Tests.Infrastructure.Tests.Migrations;

public class CurrencyChangeToEnumMigrationTests
{
    private readonly Assembly _migrationsAssembly;

    public CurrencyChangeToEnumMigrationTests()
    {
        _migrationsAssembly = typeof(ApplicationDbContext).Assembly;
    }

    [Fact]
    public void Up_ShouldDropNameColumnAndChangeForeignKey()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeUpMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(3, migrationBuilder.Operations.Count);

        // Verify DropForeignKey
        var dropForeignKey = migrationBuilder.Operations[0] as DropForeignKeyOperation;
        Assert.NotNull(dropForeignKey);
        Assert.Equal("Expenses", dropForeignKey.Table);

        // Verify DropColumn
        var dropColumn = migrationBuilder.Operations[1] as DropColumnOperation;
        Assert.NotNull(dropColumn);
        Assert.Equal("Name", dropColumn.Name);
        Assert.Equal("Currencies", dropColumn.Table);

        // Verify AddForeignKey is part of the operations
        var addForeignKey = migrationBuilder.Operations.OfType<AddForeignKeyOperation>().FirstOrDefault();
        Assert.NotNull(addForeignKey);
        Assert.Equal("Expenses", addForeignKey.Table);
    }

    [Fact]
    public void Down_ShouldAddNameColumnAndRevertForeignKey()
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

        // Verify AddColumn
        var addColumn = migrationBuilder.Operations[1] as AddColumnOperation;
        Assert.NotNull(addColumn);
        Assert.Equal("Name", addColumn.Name);
        Assert.Equal("Currencies", addColumn.Table);
        Assert.Equal("nvarchar(100)", addColumn.ColumnType);
        Assert.False(addColumn.IsNullable);

        // Verify AddForeignKey
        var addForeignKey = migrationBuilder.Operations[2] as AddForeignKeyOperation;
        Assert.NotNull(addForeignKey);
        Assert.Equal("Expenses", addForeignKey.Table);
    }

    #region Helper Methods

    private object CreateMigrationInstance()
    {
        var migrationType = _migrationsAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "CurrencyChangeToEnum");

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