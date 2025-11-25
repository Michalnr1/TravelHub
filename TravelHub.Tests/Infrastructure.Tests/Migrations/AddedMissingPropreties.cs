using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using TravelHub.Infrastructure;
using Xunit;

namespace TravelHub.Tests.Infrastructure.Tests.Migrations;

public class AddedMissingPropretiesMigrationTests
{
    private readonly Assembly _migrationsAssembly;

    public AddedMissingPropretiesMigrationTests()
    {
        _migrationsAssembly = typeof(ApplicationDbContext).Assembly;
    }

    [Fact]
    public void Up_ShouldAddMissingProperties()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeUpMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(3, migrationBuilder.Operations.Count);

        var addColumnOperations = migrationBuilder.Operations.OfType<AddColumnOperation>().ToList();
        Assert.Equal(3, addColumnOperations.Count);

        var isPrivateOperation = addColumnOperations.First(op => op.Name == "IsPrivate");
        Assert.Equal("Trips", isPrivateOperation.Table);
        Assert.Equal(typeof(bool), isPrivateOperation.ClrType);
        Assert.False(isPrivateOperation.IsNullable);
        Assert.Equal(false, isPrivateOperation.DefaultValue);

        var costOperation = addColumnOperations.First(op => op.Name == "Cost");
        Assert.Equal("Transports", costOperation.Table);
        Assert.Equal("decimal(18,2)", costOperation.ColumnType);
        Assert.False(costOperation.IsNullable);
        Assert.Equal(0m, costOperation.DefaultValue);

        var ratingOperation = addColumnOperations.First(op => op.Name == "Rating");
        Assert.Equal("Activities", ratingOperation.Table);
        Assert.Equal(typeof(int), ratingOperation.ClrType);
        Assert.True(ratingOperation.IsNullable);
    }

    [Fact]
    public void Down_ShouldRemoveProperties()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeDownMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(3, migrationBuilder.Operations.Count);

        var dropColumnOperations = migrationBuilder.Operations.OfType<DropColumnOperation>().ToList();
        Assert.Equal(3, dropColumnOperations.Count);

        Assert.Contains(dropColumnOperations, op => op.Name == "IsPrivate" && op.Table == "Trips");
        Assert.Contains(dropColumnOperations, op => op.Name == "Cost" && op.Table == "Transports");
        Assert.Contains(dropColumnOperations, op => op.Name == "Rating" && op.Table == "Activities");
    }

    #region Helper Methods

    private object CreateMigrationInstance()
    {
        var migrationType = _migrationsAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "AddedMissingPropreties");

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