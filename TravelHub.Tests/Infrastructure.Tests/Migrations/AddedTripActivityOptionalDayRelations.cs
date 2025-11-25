using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using TravelHub.Infrastructure;
using Xunit;

namespace TravelHub.Tests.Infrastructure.Tests.Migrations;

public class AddedTripActivityOptionalDayRelationsMigrationTests
{
    private readonly Assembly _migrationsAssembly;

    public AddedTripActivityOptionalDayRelationsMigrationTests()
    {
        _migrationsAssembly = typeof(ApplicationDbContext).Assembly;
    }

    [Fact]
    public void Up_ShouldMakeDayIdNullableAndAddTripRelations()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeUpMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(11, migrationBuilder.Operations.Count);

        // Verify DropForeignKey operations
        var dropForeignKeys = migrationBuilder.Operations.OfType<DropForeignKeyOperation>().ToList();
        Assert.Equal(2, dropForeignKeys.Count);

        // Verify AlterColumn
        var alterColumn = migrationBuilder.Operations.OfType<AlterColumnOperation>().First();
        Assert.NotNull(alterColumn);
        Assert.Equal("DayId", alterColumn.Name);
        Assert.Equal("Activities", alterColumn.Table);
        Assert.True(alterColumn.IsNullable);

        // Verify AddColumn operations
        var addColumns = migrationBuilder.Operations.OfType<AddColumnOperation>().ToList();
        Assert.Equal(2, addColumns.Count);

        var categoryIdColumn = addColumns.First(op => op.Name == "CategoryId");
        Assert.Equal("Activities", categoryIdColumn.Table);
        Assert.False(categoryIdColumn.IsNullable);
        Assert.Equal(0, categoryIdColumn.DefaultValue);

        var tripIdColumn = addColumns.First(op => op.Name == "TripId");
        Assert.Equal("Activities", tripIdColumn.Table);
        Assert.False(tripIdColumn.IsNullable);
        Assert.Equal(0, tripIdColumn.DefaultValue);

        // Verify CreateIndex operations
        var createIndexes = migrationBuilder.Operations.OfType<CreateIndexOperation>().ToList();
        Assert.Equal(2, createIndexes.Count);

        // Verify AddForeignKey operations
        var addForeignKeys = migrationBuilder.Operations.OfType<AddForeignKeyOperation>().ToList();
        Assert.Equal(4, addForeignKeys.Count);
    }

    [Fact]
    public void Down_ShouldMakeDayIdNotNullableAndRemoveTripRelations()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeDownMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(11, migrationBuilder.Operations.Count);

        // Verify DropForeignKey operations
        var dropForeignKeys = migrationBuilder.Operations.OfType<DropForeignKeyOperation>().ToList();
        Assert.Equal(4, dropForeignKeys.Count);

        // Verify DropIndex operations
        var dropIndexes = migrationBuilder.Operations.OfType<DropIndexOperation>().ToList();
        Assert.Equal(2, dropIndexes.Count);

        // Verify DropColumn operations
        var dropColumns = migrationBuilder.Operations.OfType<DropColumnOperation>().ToList();
        Assert.Equal(2, dropColumns.Count);

        // Verify AlterColumn
        var alterColumn = migrationBuilder.Operations.OfType<AlterColumnOperation>().First();
        Assert.NotNull(alterColumn);
        Assert.Equal("DayId", alterColumn.Name);
        Assert.Equal("Activities", alterColumn.Table);
        Assert.False(alterColumn.IsNullable);
        Assert.Equal(0, alterColumn.DefaultValue);

        // Verify AddForeignKey operations
        var addForeignKeys = migrationBuilder.Operations.OfType<AddForeignKeyOperation>().ToList();
        Assert.Equal(2, addForeignKeys.Count);
    }

    #region Helper Methods

    private object CreateMigrationInstance()
    {
        var migrationType = _migrationsAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "AddedTripActivityOptionalDayRelations");

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