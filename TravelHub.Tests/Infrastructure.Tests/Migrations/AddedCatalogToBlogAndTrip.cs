using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using TravelHub.Infrastructure;
using Xunit;

namespace TravelHub.Tests.Infrastructure.Tests.Migrations;

public class AddedCatalogToBlogAndTripMigrationTests
{
    private readonly Assembly _migrationsAssembly;

    public AddedCatalogToBlogAndTripMigrationTests()
    {
        _migrationsAssembly = typeof(ApplicationDbContext).Assembly;
    }

    [Fact]
    public void Up_ShouldAddCatalogColumnsToBothTables()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeUpMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(2, migrationBuilder.Operations.Count);

        var tripOperation = migrationBuilder.Operations[0] as AddColumnOperation;
        var blogOperation = migrationBuilder.Operations[1] as AddColumnOperation;

        // Verify Trips table
        Assert.NotNull(tripOperation);
        Assert.Equal("Catalog", tripOperation.Name);
        Assert.Equal("Trips", tripOperation.Table);
        Assert.Equal(typeof(string), tripOperation.ClrType);
        Assert.Equal("nvarchar(100)", tripOperation.ColumnType);
        Assert.Equal(100, tripOperation.MaxLength);
        Assert.True(tripOperation.IsNullable);

        // Verify Blogs table
        Assert.NotNull(blogOperation);
        Assert.Equal("Catalog", blogOperation.Name);
        Assert.Equal("Blogs", blogOperation.Table);
        Assert.Equal(typeof(string), blogOperation.ClrType);
        Assert.Equal("nvarchar(100)", blogOperation.ColumnType);
        Assert.Equal(100, blogOperation.MaxLength);
        Assert.True(blogOperation.IsNullable);
    }

    [Fact]
    public void Down_ShouldRemoveCatalogColumnsFromBothTables()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeDownMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(2, migrationBuilder.Operations.Count);

        var tripOperation = migrationBuilder.Operations[0] as DropColumnOperation;
        var blogOperation = migrationBuilder.Operations[1] as DropColumnOperation;

        Assert.NotNull(tripOperation);
        Assert.Equal("Catalog", tripOperation.Name);
        Assert.Equal("Trips", tripOperation.Table);

        Assert.NotNull(blogOperation);
        Assert.Equal("Catalog", blogOperation.Name);
        Assert.Equal("Blogs", blogOperation.Table);
    }

    #region Helper Methods

    private object CreateMigrationInstance()
    {
        var migrationType = _migrationsAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "AddedCatalogToBlogAndTrip");

        if (migrationType == null)
        {
            throw new InvalidOperationException("AddedCatalogToBlogAndTrip migration type not found");
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