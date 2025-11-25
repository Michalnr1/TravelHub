using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using TravelHub.Infrastructure;
using Xunit;

namespace TravelHub.Tests.Infrastructure.Tests.Migrations;

public class AddedCountriesAndCurrencyToTripMigrationTests
{
    private readonly Assembly _migrationsAssembly;

    public AddedCountriesAndCurrencyToTripMigrationTests()
    {
        _migrationsAssembly = typeof(ApplicationDbContext).Assembly;
    }

    [Fact]
    public void Up_ShouldAddCurrencyCodeAndCreateCountriesTables()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeUpMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(4, migrationBuilder.Operations.Count);

        // Verify AddColumn operation
        var addColumnOperation = migrationBuilder.Operations[0] as AddColumnOperation;
        Assert.NotNull(addColumnOperation);
        Assert.Equal("CurrencyCode", addColumnOperation.Name);
        Assert.Equal("Trips", addColumnOperation.Table);
        Assert.Equal("nvarchar(3)", addColumnOperation.ColumnType);
        Assert.Equal(3, addColumnOperation.MaxLength);
        Assert.False(addColumnOperation.IsNullable);
        Assert.Equal("", addColumnOperation.DefaultValue);

        // Verify CreateTable operation for Countries
        var createCountriesTable = migrationBuilder.Operations[1] as CreateTableOperation;
        Assert.NotNull(createCountriesTable);
        Assert.Equal("Countries", createCountriesTable.Name);
        Assert.Equal(2, createCountriesTable.Columns.Count); // Code, Name

        // Verify CreateTable operation for TripCountries
        var createTripCountriesTable = migrationBuilder.Operations[2] as CreateTableOperation;
        Assert.NotNull(createTripCountriesTable);
        Assert.Equal("TripCountries", createTripCountriesTable.Name);
        Assert.Equal(3, createTripCountriesTable.Columns.Count); // TripsId, CountriesCode, CountriesName

        // Verify CreateIndex operation is part of CreateTable
        var createIndexOperation = migrationBuilder.Operations.OfType<CreateIndexOperation>().First();
        Assert.NotNull(createIndexOperation);
        Assert.Equal("TripCountries", createIndexOperation.Table);
    }

    [Fact]
    public void Down_ShouldDropTablesAndRemoveCurrencyCode()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeDownMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(3, migrationBuilder.Operations.Count);

        // Verify DropTable operations
        var dropTableOperations = migrationBuilder.Operations.OfType<DropTableOperation>().ToList();
        Assert.Equal(2, dropTableOperations.Count);
        Assert.Contains(dropTableOperations, op => op.Name == "TripCountries");
        Assert.Contains(dropTableOperations, op => op.Name == "Countries");

        // Verify DropColumn operation
        var dropColumnOperation = migrationBuilder.Operations[2] as DropColumnOperation;
        Assert.NotNull(dropColumnOperation);
        Assert.Equal("CurrencyCode", dropColumnOperation.Name);
        Assert.Equal("Trips", dropColumnOperation.Table);
    }

    #region Helper Methods

    private object CreateMigrationInstance()
    {
        var migrationType = _migrationsAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "AddedCountriesAndCurrencyToTrip");

        if (migrationType == null)
        {
            throw new InvalidOperationException("AddedCountriesAndCurrencyToTrip migration type not found");
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