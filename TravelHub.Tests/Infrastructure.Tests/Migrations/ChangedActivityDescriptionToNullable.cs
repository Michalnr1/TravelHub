using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using TravelHub.Infrastructure;
using Xunit;

namespace TravelHub.Tests.Infrastructure.Tests.Migrations;

public class ChangedActivityDescriptionToNullableMigrationTests
{
    private readonly Assembly _migrationsAssembly;

    public ChangedActivityDescriptionToNullableMigrationTests()
    {
        _migrationsAssembly = typeof(ApplicationDbContext).Assembly;
    }

    [Fact]
    public void Up_ShouldMakeActivityDescriptionNullable()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeUpMethod(migration, migrationBuilder);

        // Assert
        var operation = migrationBuilder.Operations[0] as AlterColumnOperation;
        Assert.NotNull(operation);
        Assert.Equal("Description", operation.Name);
        Assert.Equal("Activities", operation.Table);
        Assert.Equal("nvarchar(1000)", operation.ColumnType);
        Assert.Equal(1000, operation.MaxLength);
        Assert.True(operation.IsNullable);
    }

    [Fact]
    public void Down_ShouldMakeActivityDescriptionNotNullable()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeDownMethod(migration, migrationBuilder);

        // Assert
        var operation = migrationBuilder.Operations[0] as AlterColumnOperation;
        Assert.NotNull(operation);
        Assert.Equal("Description", operation.Name);
        Assert.Equal("Activities", operation.Table);
        Assert.Equal("nvarchar(1000)", operation.ColumnType);
        Assert.Equal(1000, operation.MaxLength);
        Assert.False(operation.IsNullable);
        Assert.Equal("", operation.DefaultValue);
    }

    #region Helper Methods

    private object CreateMigrationInstance()
    {
        var migrationType = _migrationsAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "ChangedActivityDescriptionToNullable");

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