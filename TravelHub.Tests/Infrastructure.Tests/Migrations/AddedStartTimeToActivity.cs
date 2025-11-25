using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using TravelHub.Infrastructure;
using Xunit;

namespace TravelHub.Tests.Infrastructure.Tests.Migrations;

public class AddedStartTimeToActivityMigrationTests
{
    private readonly Assembly _migrationsAssembly;

    public AddedStartTimeToActivityMigrationTests()
    {
        _migrationsAssembly = typeof(ApplicationDbContext).Assembly;
    }

    [Fact]
    public void Up_ShouldAddStartTimeColumn()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeUpMethod(migration, migrationBuilder);

        // Assert
        var operation = migrationBuilder.Operations[0] as AddColumnOperation;
        Assert.NotNull(operation);
        Assert.Equal("StartTime", operation.Name);
        Assert.Equal("Activities", operation.Table);
        Assert.Equal(typeof(decimal), operation.ClrType);
        Assert.Equal("decimal(4,2)", operation.ColumnType);
        Assert.Equal(4, operation.Precision);
        Assert.Equal(2, operation.Scale);
        Assert.False(operation.IsNullable);
        Assert.Equal(0m, operation.DefaultValue);
    }

    [Fact]
    public void Down_ShouldRemoveStartTimeColumn()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeDownMethod(migration, migrationBuilder);

        // Assert
        var operation = migrationBuilder.Operations[0] as DropColumnOperation;
        Assert.NotNull(operation);
        Assert.Equal("StartTime", operation.Name);
        Assert.Equal("Activities", operation.Table);
    }

    #region Helper Methods

    private object CreateMigrationInstance()
    {
        var migrationType = _migrationsAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "AddedStartTimeToActivity");

        if (migrationType == null)
        {
            throw new InvalidOperationException("AddedStartTimeToActivity migration type not found");
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