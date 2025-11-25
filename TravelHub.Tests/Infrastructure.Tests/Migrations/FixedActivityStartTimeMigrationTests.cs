using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using TravelHub.Infrastructure;

namespace TravelHub.Tests.Infrastructure.Tests.Migrations;

public class FixedActivityStartTimeMigrationTests
{
    private readonly Assembly _migrationsAssembly;

    public FixedActivityStartTimeMigrationTests()
    {
        // Pobierz assembly, w którym znajdują się migracje
        _migrationsAssembly = typeof(ApplicationDbContext).Assembly;
    }

    [Fact]
    public void Up_ShouldMakeStartTimeNullable()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeUpMethod(migration, migrationBuilder);

        // Assert
        var operation = migrationBuilder.Operations[0] as AlterColumnOperation;
        Assert.NotNull(operation);
        Assert.Equal("StartTime", operation.Name);
        Assert.Equal("Activities", operation.Table);
        Assert.Equal(typeof(decimal), operation.ClrType);
        Assert.Equal("decimal(4,2)", operation.ColumnType);
        Assert.Equal(4, operation.Precision);
        Assert.Equal(2, operation.Scale);
        Assert.True(operation.IsNullable);
    }

    [Fact]
    public void Down_ShouldMakeStartTimeNotNullable()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeDownMethod(migration, migrationBuilder);

        // Assert
        var operation = migrationBuilder.Operations[0] as AlterColumnOperation;
        Assert.NotNull(operation);
        Assert.Equal("StartTime", operation.Name);
        Assert.Equal("Activities", operation.Table);
        Assert.Equal(typeof(decimal), operation.ClrType);
        Assert.Equal("decimal(4,2)", operation.ColumnType);
        Assert.Equal(4, operation.Precision);
        Assert.Equal(2, operation.Scale);
        Assert.False(operation.IsNullable);

        // Verify default value
        var defaultValue = operation.DefaultValue as decimal?;
        Assert.Equal(0m, defaultValue);
    }

    [Fact]
    public void Up_ShouldHaveCorrectOperationsCount()
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
    public void Down_ShouldHaveCorrectOperationsCount()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeDownMethod(migration, migrationBuilder);

        // Assert
        Assert.Single(migrationBuilder.Operations);
    }

    #region Helper Methods using Reflection

    private object CreateMigrationInstance()
    {
        var migrationType = _migrationsAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "FixedActivityStartTime");

        if (migrationType == null)
        {
            // Spróbuj znaleźć po pełnej nazwie z namespace
            migrationType = _migrationsAssembly.GetTypes()
                .FirstOrDefault(t => t.FullName != null && t.FullName.Contains("FixedActivityStartTime"));
        }

        if (migrationType == null)
        {
            throw new InvalidOperationException($"FixedActivityStartTime migration type not found in assembly {_migrationsAssembly.FullName}. Available types: {string.Join(", ", _migrationsAssembly.GetTypes().Select(t => t.Name))}");
        }

        return Activator.CreateInstance(migrationType, nonPublic: true);
    }

    private void InvokeUpMethod(object migrationInstance, MigrationBuilder migrationBuilder)
    {
        var upMethod = migrationInstance.GetType().GetMethod("Up",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        if (upMethod == null)
        {
            throw new InvalidOperationException("Up method not found");
        }

        upMethod.Invoke(migrationInstance, new object[] { migrationBuilder });
    }

    private void InvokeDownMethod(object migrationInstance, MigrationBuilder migrationBuilder)
    {
        var downMethod = migrationInstance.GetType().GetMethod("Down",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        if (downMethod == null)
        {
            throw new InvalidOperationException("Down method not found");
        }

        downMethod.Invoke(migrationInstance, new object[] { migrationBuilder });
    }

    #endregion
}