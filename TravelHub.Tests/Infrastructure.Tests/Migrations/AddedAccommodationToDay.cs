using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using TravelHub.Infrastructure;
using Xunit;

namespace TravelHub.Tests.Infrastructure.Tests.Migrations;

public class AddedAccommodationToDayMigrationTests
{
    private readonly Assembly _migrationsAssembly;

    public AddedAccommodationToDayMigrationTests()
    {
        _migrationsAssembly = typeof(ApplicationDbContext).Assembly;
    }

    [Fact]
    public void Up_ShouldAddAccommodationColumnAndForeignKey()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeUpMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(3, migrationBuilder.Operations.Count);

        // Verify AddColumn operation
        var addColumnOperation = migrationBuilder.Operations[0] as AddColumnOperation;
        Assert.NotNull(addColumnOperation);
        Assert.Equal("AccommodationId", addColumnOperation.Name);
        Assert.Equal("Days", addColumnOperation.Table);
        Assert.Equal(typeof(int), addColumnOperation.ClrType);
        Assert.True(addColumnOperation.IsNullable);

        // Verify CreateIndex operation
        var createIndexOperation = migrationBuilder.Operations[1] as CreateIndexOperation;
        Assert.NotNull(createIndexOperation);
        Assert.Equal("Days", createIndexOperation.Table);
        Assert.Equal("AccommodationId", createIndexOperation.Columns[0]);

        // Verify AddForeignKey operation
        var addForeignKeyOperation = migrationBuilder.Operations[2] as AddForeignKeyOperation;
        Assert.NotNull(addForeignKeyOperation);
        Assert.Equal("Days", addForeignKeyOperation.Table);
        Assert.Equal("AccommodationId", addForeignKeyOperation.Columns[0]);
        Assert.Equal("Activities", addForeignKeyOperation.PrincipalTable);
    }

    [Fact]
    public void Down_ShouldRemoveForeignKeyAndColumn()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeDownMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(3, migrationBuilder.Operations.Count);

        // Verify DropForeignKey operation
        var dropForeignKeyOperation = migrationBuilder.Operations[0] as DropForeignKeyOperation;
        Assert.NotNull(dropForeignKeyOperation);
        Assert.Equal("Days", dropForeignKeyOperation.Table);

        // Verify DropIndex operation
        var dropIndexOperation = migrationBuilder.Operations[1] as DropIndexOperation;
        Assert.NotNull(dropIndexOperation);
        Assert.Equal("Days", dropIndexOperation.Table);

        // Verify DropColumn operation
        var dropColumnOperation = migrationBuilder.Operations[2] as DropColumnOperation;
        Assert.NotNull(dropColumnOperation);
        Assert.Equal("AccommodationId", dropColumnOperation.Name);
        Assert.Equal("Days", dropColumnOperation.Table);
    }

    #region Helper Methods

    private object CreateMigrationInstance()
    {
        var migrationType = _migrationsAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "AddedAccommodationToDay");

        if (migrationType == null)
        {
            throw new InvalidOperationException("AddedAccommodationToDay migration type not found");
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