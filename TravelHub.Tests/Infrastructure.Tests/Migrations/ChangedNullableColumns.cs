using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using TravelHub.Infrastructure;
using Xunit;

namespace TravelHub.Tests.Infrastructure.Tests.Migrations;

public class ChangedNullableColumnsMigrationTests
{
    private readonly Assembly _migrationsAssembly;

    public ChangedNullableColumnsMigrationTests()
    {
        _migrationsAssembly = typeof(ApplicationDbContext).Assembly;
    }

    [Fact]
    public void Up_ShouldMakeColumnsNullable()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeUpMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(6, migrationBuilder.Operations.Count);

        // Verify DropForeignKey
        var dropForeignKey = migrationBuilder.Operations[0] as DropForeignKeyOperation;
        Assert.NotNull(dropForeignKey);
        Assert.Equal("Expenses", dropForeignKey.Table);

        // Verify AlterColumn operations
        var alterColumns = migrationBuilder.Operations.OfType<AlterColumnOperation>().ToList();
        Assert.Equal(4, alterColumns.Count);

        var spotIdColumn = alterColumns.First(op => op.Name == "SpotId");
        Assert.Equal("Photos", spotIdColumn.Table);
        Assert.True(spotIdColumn.IsNullable);

        var altColumn = alterColumns.First(op => op.Name == "Alt");
        Assert.Equal("Photos", altColumn.Table);
        Assert.True(altColumn.IsNullable);

        var categoryIdInExpenses = alterColumns.First(op => op.Name == "CategoryId" && op.Table == "Expenses");
        Assert.True(categoryIdInExpenses.IsNullable);

        var categoryIdInActivities = alterColumns.First(op => op.Name == "CategoryId" && op.Table == "Activities");
        Assert.True(categoryIdInActivities.IsNullable);

        // Verify AddForeignKey
        var addForeignKey = migrationBuilder.Operations.OfType<AddForeignKeyOperation>().First(o => o.Table == "Expenses");
        Assert.NotNull(addForeignKey);
        Assert.Equal("Expenses", addForeignKey.Table);

    }

    [Fact]
    public void Down_ShouldMakeColumnsNotNullable()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeDownMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(6, migrationBuilder.Operations.Count);

        // Verify DropForeignKey
        var dropForeignKey = migrationBuilder.Operations[0] as DropForeignKeyOperation;
        Assert.NotNull(dropForeignKey);
        Assert.Equal("Expenses", dropForeignKey.Table);

        // Verify AlterColumn operations
        var alterColumns = migrationBuilder.Operations.OfType<AlterColumnOperation>().ToList();
        Assert.Equal(4, alterColumns.Count);

        var spotIdColumn = alterColumns.First(op => op.Name == "SpotId");
        Assert.Equal("Photos", spotIdColumn.Table);
        Assert.False(spotIdColumn.IsNullable);

        var altColumn = alterColumns.First(op => op.Name == "Alt");
        Assert.Equal("Photos", altColumn.Table);
        Assert.False(altColumn.IsNullable);
        Assert.Equal("", altColumn.DefaultValue);

        var categoryIdInExpenses = alterColumns.First(op => op.Name == "CategoryId" && op.Table == "Expenses");
        Assert.False(categoryIdInExpenses.IsNullable);

        var categoryIdInActivities = alterColumns.First(op => op.Name == "CategoryId" && op.Table == "Activities");
        Assert.False(categoryIdInActivities.IsNullable);

        // Verify AddForeignKey
        var addForeignKey = migrationBuilder.Operations.OfType<AddForeignKeyOperation>().First(o => o.Table == "Expenses");
        Assert.NotNull(addForeignKey);
        Assert.Equal("Expenses", addForeignKey.Table);
    }

    #region Helper Methods

    private object CreateMigrationInstance()
    {
        var migrationType = _migrationsAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "ChangedNullableColumns");

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