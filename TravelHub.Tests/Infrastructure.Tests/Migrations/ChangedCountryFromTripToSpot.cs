using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using TravelHub.Infrastructure;
using Xunit;

namespace TravelHub.Tests.Infrastructure.Tests.Migrations;

public class ChangedCountryFromTripToSpotMigrationTests
{
    private readonly Assembly _migrationsAssembly;

    public ChangedCountryFromTripToSpotMigrationTests()
    {
        _migrationsAssembly = typeof(ApplicationDbContext).Assembly;
    }

    [Fact]
    public void Up_ShouldDropTripCountriesAndCreateSpotCountries()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeUpMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(3, migrationBuilder.Operations.Count);

        // Verify DropTable operation
        var dropTableOperation = migrationBuilder.Operations[0] as DropTableOperation;
        Assert.NotNull(dropTableOperation);
        Assert.Equal("TripCountries", dropTableOperation.Name);

        // Verify CreateTable operation
        var createTableOperation = migrationBuilder.Operations[1] as CreateTableOperation;
        Assert.NotNull(createTableOperation);
        Assert.Equal("SpotCountries", createTableOperation.Name);
        Assert.Equal(3, createTableOperation.Columns.Count); // SpotsId, CountriesCode, CountriesName

        // Verify CreateIndex operation is part of CreateTable
        var createIndexOperation = migrationBuilder.Operations.OfType<CreateIndexOperation>().First();
        Assert.NotNull(createIndexOperation);
        Assert.Equal("SpotCountries", createIndexOperation.Table);
    }

    [Fact]
    public void Down_ShouldDropSpotCountriesAndCreateTripCountries()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeDownMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(3, migrationBuilder.Operations.Count);

        // Verify DropTable operation
        var dropTableOperation = migrationBuilder.Operations[0] as DropTableOperation;
        Assert.NotNull(dropTableOperation);
        Assert.Equal("SpotCountries", dropTableOperation.Name);

        // Verify CreateTable operation
        var createTableOperation = migrationBuilder.Operations[1] as CreateTableOperation;
        Assert.NotNull(createTableOperation);
        Assert.Equal("TripCountries", createTableOperation.Name);
        Assert.Equal(3, createTableOperation.Columns.Count); // TripsId, CountriesCode, CountriesName
    }

    #region Helper Methods

    private object CreateMigrationInstance()
    {
        var migrationType = _migrationsAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "ChangedCountryFromTripToSpot");

        if (migrationType == null)
        {
            throw new InvalidOperationException("ChangedCountryFromTripToSpot migration type not found");
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