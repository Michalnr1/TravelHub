using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using TravelHub.Infrastructure;
using Xunit;

namespace TravelHub.Tests.Infrastructure.Tests.Migrations;

public class AddedDayNameMigrationTests
{
    private readonly Assembly _migrationsAssembly;

    public AddedDayNameMigrationTests()
    {
        _migrationsAssembly = typeof(ApplicationDbContext).Assembly;
    }

    [Fact]
    public void Up_ShouldAddNameAndMakeNumberNullable()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeUpMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(2, migrationBuilder.Operations.Count);

        // Verify AlterColumn
        var alterColumn = migrationBuilder.Operations[0] as AlterColumnOperation;
        Assert.NotNull(alterColumn);
        Assert.Equal("Number", alterColumn.Name);
        Assert.Equal("Days", alterColumn.Table);
        Assert.True(alterColumn.IsNullable);

        // Verify AddColumn
        var addColumn = migrationBuilder.Operations[1] as AddColumnOperation;
        Assert.NotNull(addColumn);
        Assert.Equal("Name", addColumn.Name);
        Assert.Equal("Days", addColumn.Table);
        Assert.Equal("nvarchar(max)", addColumn.ColumnType);
        Assert.False(addColumn.IsNullable);
        Assert.Equal("", addColumn.DefaultValue);
    }

    [Fact]
    public void Down_ShouldRemoveNameAndMakeNumberNotNullable()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeDownMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(2, migrationBuilder.Operations.Count);

        // Verify DropColumn
        var dropColumn = migrationBuilder.Operations[0] as DropColumnOperation;
        Assert.NotNull(dropColumn);
        Assert.Equal("Name", dropColumn.Name);
        Assert.Equal("Days", dropColumn.Table);

        // Verify AlterColumn
        var alterColumn = migrationBuilder.Operations[1] as AlterColumnOperation;
        Assert.NotNull(alterColumn);
        Assert.Equal("Number", alterColumn.Name);
        Assert.Equal("Days", alterColumn.Table);
        Assert.False(alterColumn.IsNullable);
        Assert.Equal(0, alterColumn.DefaultValue);
    }

    #region Helper Methods

    private object CreateMigrationInstance()
    {
        var migrationType = _migrationsAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "AddedDayName");

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