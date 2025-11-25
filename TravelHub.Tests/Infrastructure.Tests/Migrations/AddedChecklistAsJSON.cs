using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using TravelHub.Infrastructure;
using Xunit;

namespace TravelHub.Tests.Infrastructure.Tests.Migrations;

public class AddedChecklistAsJSONMigrationTests
{
    private readonly Assembly _migrationsAssembly;

    public AddedChecklistAsJSONMigrationTests()
    {
        _migrationsAssembly = typeof(ApplicationDbContext).Assembly;
    }

    [Fact]
    public void Up_ShouldAddChecklistColumnsToBothTables()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeUpMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(2, migrationBuilder.Operations.Count);

        var tripOperation = migrationBuilder.Operations[0] as AddColumnOperation;
        var activityOperation = migrationBuilder.Operations[1] as AddColumnOperation;

        // Verify Trips table
        Assert.NotNull(tripOperation);
        Assert.Equal("Checklist", tripOperation.Name);
        Assert.Equal("Trips", tripOperation.Table);
        Assert.Equal(typeof(string), tripOperation.ClrType);
        Assert.Equal("nvarchar(max)", tripOperation.ColumnType);
        Assert.True(tripOperation.IsNullable);

        // Verify Activities table
        Assert.NotNull(activityOperation);
        Assert.Equal("Checklist", activityOperation.Name);
        Assert.Equal("Activities", activityOperation.Table);
        Assert.Equal(typeof(string), activityOperation.ClrType);
        Assert.Equal("nvarchar(max)", activityOperation.ColumnType);
        Assert.True(activityOperation.IsNullable);
    }

    [Fact]
    public void Down_ShouldRemoveChecklistColumnsFromBothTables()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeDownMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(2, migrationBuilder.Operations.Count);

        var tripOperation = migrationBuilder.Operations[0] as DropColumnOperation;
        var activityOperation = migrationBuilder.Operations[1] as DropColumnOperation;

        Assert.NotNull(tripOperation);
        Assert.Equal("Checklist", tripOperation.Name);
        Assert.Equal("Trips", tripOperation.Table);

        Assert.NotNull(activityOperation);
        Assert.Equal("Checklist", activityOperation.Name);
        Assert.Equal("Activities", activityOperation.Table);
    }

    #region Helper Methods

    private object CreateMigrationInstance()
    {
        var migrationType = _migrationsAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "AddedChecklistAsJSON");

        if (migrationType == null)
        {
            throw new InvalidOperationException("AddedChecklistAsJSON migration type not found");
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