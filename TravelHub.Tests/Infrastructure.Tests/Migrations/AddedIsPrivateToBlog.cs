using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using TravelHub.Infrastructure;
using Xunit;

namespace TravelHub.Tests.Infrastructure.Tests.Migrations;

public class AddedIsPrivateToBlogMigrationTests
{
    private readonly Assembly _migrationsAssembly;

    public AddedIsPrivateToBlogMigrationTests()
    {
        _migrationsAssembly = typeof(ApplicationDbContext).Assembly;
    }

    [Fact]
    public void Up_ShouldAddIsPrivateColumn()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeUpMethod(migration, migrationBuilder);

        // Assert
        var operation = migrationBuilder.Operations[0] as AddColumnOperation;
        Assert.NotNull(operation);
        Assert.Equal("IsPrivate", operation.Name);
        Assert.Equal("Blogs", operation.Table);
        Assert.Equal(typeof(bool), operation.ClrType);
        Assert.Equal("bit", operation.ColumnType);
        Assert.False(operation.IsNullable);
        Assert.Equal(false, operation.DefaultValue);
    }

    [Fact]
    public void Down_ShouldRemoveIsPrivateColumn()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeDownMethod(migration, migrationBuilder);

        // Assert
        var operation = migrationBuilder.Operations[0] as DropColumnOperation;
        Assert.NotNull(operation);
        Assert.Equal("IsPrivate", operation.Name);
        Assert.Equal("Blogs", operation.Table);
    }

    [Fact]
    public void Up_ShouldHaveSingleOperation()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeUpMethod(migration, migrationBuilder);

        // Assert
        Assert.Single(migrationBuilder.Operations);
    }

    [Fact]
    public void Down_ShouldHaveSingleOperation()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeDownMethod(migration, migrationBuilder);

        // Assert
        Assert.Single(migrationBuilder.Operations);
    }

    #region Helper Methods

    private object CreateMigrationInstance()
    {
        var migrationType = _migrationsAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "AddedIsPrivateToBlog");

        if (migrationType == null)
        {
            throw new InvalidOperationException("AddedIsPrivateToBlog migration type not found");
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