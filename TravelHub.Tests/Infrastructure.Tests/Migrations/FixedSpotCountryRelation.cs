using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using TravelHub.Infrastructure;
using Xunit;

namespace TravelHub.Tests.Infrastructure.Tests.Migrations;

public class FixedSpotCountryRelationMigrationTests
{
    private readonly Assembly _migrationsAssembly;

    public FixedSpotCountryRelationMigrationTests()
    {
        _migrationsAssembly = typeof(ApplicationDbContext).Assembly;
    }

    [Fact]
    public void Up_ShouldDropJoinTableAndAddDirectColumns()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeUpMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(5, migrationBuilder.Operations.Count);

        // Verify DropTable operation
        var dropTableOperation = migrationBuilder.Operations[0] as DropTableOperation;
        Assert.NotNull(dropTableOperation);
        Assert.Equal("SpotCountries", dropTableOperation.Name);

        // Verify AddColumn operations
        var addColumnOperations = migrationBuilder.Operations.OfType<AddColumnOperation>().ToList();
        Assert.Equal(2, addColumnOperations.Count);

        var countryCodeOperation = addColumnOperations.First(op => op.Name == "CountryCode");
        Assert.Equal("Activities", countryCodeOperation.Table);
        Assert.Equal("nvarchar(3)", countryCodeOperation.ColumnType);
        Assert.True(countryCodeOperation.IsNullable);

        var countryNameOperation = addColumnOperations.First(op => op.Name == "CountryName");
        Assert.Equal("Activities", countryNameOperation.Table);
        Assert.Equal("nvarchar(100)", countryNameOperation.ColumnType);
        Assert.True(countryNameOperation.IsNullable);

        // Verify CreateIndex operation
        var createIndexOperation = migrationBuilder.Operations.OfType<CreateIndexOperation>().First();
        Assert.NotNull(createIndexOperation);
        Assert.Equal("Activities", createIndexOperation.Table);
        Assert.Equal(2, createIndexOperation.Columns.Count());
        Assert.Contains("CountryCode", createIndexOperation.Columns);
        Assert.Contains("CountryName", createIndexOperation.Columns);

        // Verify AddForeignKey operation
        var addForeignKeyOperation = migrationBuilder.Operations.OfType<AddForeignKeyOperation>().First();
        Assert.NotNull(addForeignKeyOperation);
        Assert.Equal("Activities", addForeignKeyOperation.Table);
        Assert.Equal(2, addForeignKeyOperation.Columns.Count());
    }

    [Fact]
    public void Down_ShouldRecreateJoinTableAndRemoveColumns()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeDownMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(6, migrationBuilder.Operations.Count);

        // Verify DropForeignKey operation
        var dropForeignKeyOperation = migrationBuilder.Operations[0] as DropForeignKeyOperation;
        Assert.NotNull(dropForeignKeyOperation);
        Assert.Equal("Activities", dropForeignKeyOperation.Table);

        // Verify DropIndex operation
        var dropIndexOperation = migrationBuilder.Operations[1] as DropIndexOperation;
        Assert.NotNull(dropIndexOperation);
        Assert.Equal("Activities", dropIndexOperation.Table);

        // Verify DropColumn operations
        var dropColumnOperations = migrationBuilder.Operations.OfType<DropColumnOperation>().ToList();
        Assert.Equal(2, dropColumnOperations.Count);
        Assert.Contains(dropColumnOperations, op => op.Name == "CountryCode");
        Assert.Contains(dropColumnOperations, op => op.Name == "CountryName");

        // Verify CreateTable operation
        var createTableOperation = migrationBuilder.Operations.OfType<CreateTableOperation>().First();
        Assert.NotNull(createTableOperation);
        Assert.Equal("SpotCountries", createTableOperation.Name);

        // Verify CreateIndex operation
        var createIndexOperation = migrationBuilder.Operations.OfType<CreateIndexOperation>().First();
        Assert.NotNull(createIndexOperation);
        Assert.Equal("SpotCountries", createIndexOperation.Table);
    }

    #region Helper Methods

    private object CreateMigrationInstance()
    {
        var migrationType = _migrationsAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "FixedSpotCountryRelation");

        if (migrationType == null)
        {
            throw new InvalidOperationException("FixedSpotCountryRelation migration type not found");
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