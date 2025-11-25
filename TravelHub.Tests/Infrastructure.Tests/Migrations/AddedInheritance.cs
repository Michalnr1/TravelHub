using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using TravelHub.Infrastructure;
using Xunit;

namespace TravelHub.Tests.Infrastructure.Tests.Migrations;

public class AddedInheritanceMigrationTests
{
    private readonly Assembly _migrationsAssembly;

    public AddedInheritanceMigrationTests()
    {
        _migrationsAssembly = typeof(ApplicationDbContext).Assembly;
    }

    [Fact]
    public void Up_ShouldDropTablesAndAddColumnsToActivities()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeUpMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(16, migrationBuilder.Operations.Count);

        // Verify DropForeignKey operations
        var dropForeignKeys = migrationBuilder.Operations.OfType<DropForeignKeyOperation>().ToList();
        Assert.Equal(3, dropForeignKeys.Count);

        // Verify DropTable operations
        var dropTables = migrationBuilder.Operations.OfType<DropTableOperation>().ToList();
        Assert.Equal(2, dropTables.Count);
        Assert.Contains(dropTables, op => op.Name == "Accommodations");
        Assert.Contains(dropTables, op => op.Name == "Spots");

        // Verify AddColumn operations
        var addColumns = migrationBuilder.Operations.OfType<AddColumnOperation>().ToList();
        Assert.Equal(8, addColumns.Count);

        var checkInColumn = addColumns.First(op => op.Name == "CheckIn");
        Assert.Equal("Activities", checkInColumn.Table);
        Assert.True(checkInColumn.IsNullable);

        var checkInTimeColumn = addColumns.First(op => op.Name == "CheckInTime");
        Assert.Equal("Activities", checkInTimeColumn.Table);
        Assert.True(checkInTimeColumn.IsNullable);

        var checkOutColumn = addColumns.First(op => op.Name == "CheckOut");
        Assert.Equal("Activities", checkOutColumn.Table);
        Assert.True(checkOutColumn.IsNullable);

        var checkOutTimeColumn = addColumns.First(op => op.Name == "CheckOutTime");
        Assert.Equal("Activities", checkOutTimeColumn.Table);
        Assert.True(checkOutTimeColumn.IsNullable);

        var costColumn = addColumns.First(op => op.Name == "Cost");
        Assert.Equal("Activities", costColumn.Table);
        Assert.True(costColumn.IsNullable);

        var discriminatorColumn = addColumns.First(op => op.Name == "Discriminator");
        Assert.Equal("Activities", discriminatorColumn.Table);
        Assert.False(discriminatorColumn.IsNullable);
        Assert.Equal("", discriminatorColumn.DefaultValue);

        var latitudeColumn = addColumns.First(op => op.Name == "Latitude");
        Assert.Equal("Activities", latitudeColumn.Table);
        Assert.True(latitudeColumn.IsNullable);

        var longitudeColumn = addColumns.First(op => op.Name == "Longitude");
        Assert.Equal("Activities", longitudeColumn.Table);
        Assert.True(longitudeColumn.IsNullable);

        // Verify AddForeignKey operations
        var addForeignKeys = migrationBuilder.Operations.OfType<AddForeignKeyOperation>().ToList();
        Assert.Equal(3, addForeignKeys.Count);
    }

    [Fact]
    public void Down_ShouldRecreateTablesAndRemoveColumnsFromActivities()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeDownMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(17, migrationBuilder.Operations.Count);

        // Verify DropForeignKey operations
        var dropForeignKeys = migrationBuilder.Operations.OfType<DropForeignKeyOperation>().ToList();
        Assert.Equal(3, dropForeignKeys.Count);

        // Verify DropColumn operations
        var dropColumns = migrationBuilder.Operations.OfType<DropColumnOperation>().ToList();
        Assert.Equal(8, dropColumns.Count);

        // Verify CreateTable operations
        var createTables = migrationBuilder.Operations.OfType<CreateTableOperation>().ToList();
        Assert.Equal(2, createTables.Count);
        Assert.Contains(createTables, op => op.Name == "Spots");
        Assert.Contains(createTables, op => op.Name == "Accommodations");

        // Verify CreateIndex operation
        var createIndex = migrationBuilder.Operations.OfType<CreateIndexOperation>().First();
        Assert.NotNull(createIndex);
        Assert.Equal("Spots", createIndex.Table);

        // Verify AddForeignKey operations
        var addForeignKeys = migrationBuilder.Operations.OfType<AddForeignKeyOperation>().ToList();
        Assert.Equal(3, addForeignKeys.Count);
    }

    #region Helper Methods

    private object CreateMigrationInstance()
    {
        var migrationType = _migrationsAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "AddedInheritance");

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