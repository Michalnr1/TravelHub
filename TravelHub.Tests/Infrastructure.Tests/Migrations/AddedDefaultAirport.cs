using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using TravelHub.Infrastructure;
using Xunit;

namespace TravelHub.Tests.Infrastructure.Tests.Migrations;

public class AddedDefaultAirportMigrationTests
{
    private readonly Assembly _migrationsAssembly;

    public AddedDefaultAirportMigrationTests()
    {
        _migrationsAssembly = typeof(ApplicationDbContext).Assembly;
    }

    [Fact]
    public void Up_ShouldAddDefaultAirportCodeColumn()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeUpMethod(migration, migrationBuilder);

        // Assert
        var operation = migrationBuilder.Operations[0] as AddColumnOperation;
        Assert.NotNull(operation);
        Assert.Equal("DefaultAirportCode", operation.Name);
        Assert.Equal("AspNetUsers", operation.Table);
        Assert.Equal(typeof(string), operation.ClrType);
        Assert.Equal("nvarchar(3)", operation.ColumnType);
        Assert.Equal(3, operation.MaxLength);
        Assert.True(operation.IsNullable);
    }

    [Fact]
    public void Down_ShouldRemoveDefaultAirportCodeColumn()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeDownMethod(migration, migrationBuilder);

        // Assert
        var operation = migrationBuilder.Operations[0] as DropColumnOperation;
        Assert.NotNull(operation);
        Assert.Equal("DefaultAirportCode", operation.Name);
        Assert.Equal("AspNetUsers", operation.Table);
    }

    #region Helper Methods

    private object CreateMigrationInstance()
    {
        var migrationType = _migrationsAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "AddedDefaultAirport");

        if (migrationType == null)
        {
            throw new InvalidOperationException("AddedDefaultAirport migration type not found");
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