using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using TravelHub.Infrastructure;
using Xunit;

namespace TravelHub.Tests.Infrastructure.Tests.Migrations;

public class AddedHangfireJobIdToNotificationMigrationTests
{
    private readonly Assembly _migrationsAssembly;

    public AddedHangfireJobIdToNotificationMigrationTests()
    {
        _migrationsAssembly = typeof(ApplicationDbContext).Assembly;
    }

    [Fact]
    public void Up_ShouldAddHangfireJobIdColumn()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeUpMethod(migration, migrationBuilder);

        // Assert
        var operation = migrationBuilder.Operations[0] as AddColumnOperation;
        Assert.NotNull(operation);
        Assert.Equal("HangfireJobId", operation.Name);
        Assert.Equal("Notifications", operation.Table);
        Assert.Equal(typeof(string), operation.ClrType);
        Assert.Equal("nvarchar(max)", operation.ColumnType);
        Assert.True(operation.IsNullable);
    }

    [Fact]
    public void Down_ShouldRemoveHangfireJobIdColumn()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeDownMethod(migration, migrationBuilder);

        // Assert
        var operation = migrationBuilder.Operations[0] as DropColumnOperation;
        Assert.NotNull(operation);
        Assert.Equal("HangfireJobId", operation.Name);
        Assert.Equal("Notifications", operation.Table);
    }

    #region Helper Methods

    private object CreateMigrationInstance()
    {
        var migrationType = _migrationsAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "AddedHangfireJobIdToNotification");

        if (migrationType == null)
        {
            throw new InvalidOperationException("AddedHangfireJobIdToNotification migration type not found");
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