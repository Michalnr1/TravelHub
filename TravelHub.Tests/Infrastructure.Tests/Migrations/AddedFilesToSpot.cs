using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using TravelHub.Infrastructure;
using Xunit;

namespace TravelHub.Tests.Infrastructure.Tests.Migrations;

public class AddedFilesToSpotMigrationTests
{
    private readonly Assembly _migrationsAssembly;

    public AddedFilesToSpotMigrationTests()
    {
        _migrationsAssembly = typeof(ApplicationDbContext).Assembly;
    }

    [Fact]
    public void Up_ShouldCreateFileTableAndDropFileName()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeUpMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(3, migrationBuilder.Operations.Count);

        // Verify DropColumn operation
        var dropColumnOperation = migrationBuilder.Operations[0] as DropColumnOperation;
        Assert.NotNull(dropColumnOperation);
        Assert.Equal("FileName", dropColumnOperation.Name);
        Assert.Equal("Activities", dropColumnOperation.Table);

        // Verify CreateTable operation
        var createTableOperation = migrationBuilder.Operations[1] as CreateTableOperation;
        Assert.NotNull(createTableOperation);
        Assert.Equal("File", createTableOperation.Name);
        Assert.Equal(3, createTableOperation.Columns.Count); // Id, Name, SpotId

        // Verify CreateIndex operation
        var createIndexOperation = migrationBuilder.Operations[2] as CreateIndexOperation;
        Assert.NotNull(createIndexOperation);
        Assert.Equal("File", createIndexOperation.Table);
        Assert.Equal("SpotId", createIndexOperation.Columns[0]);
    }

    [Fact]
    public void Down_ShouldDropFileTableAndAddFileName()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeDownMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(2, migrationBuilder.Operations.Count);

        // Verify DropTable operation
        var dropTableOperation = migrationBuilder.Operations[0] as DropTableOperation;
        Assert.NotNull(dropTableOperation);
        Assert.Equal("File", dropTableOperation.Name);

        // Verify AddColumn operation
        var addColumnOperation = migrationBuilder.Operations[1] as AddColumnOperation;
        Assert.NotNull(addColumnOperation);
        Assert.Equal("FileName", addColumnOperation.Name);
        Assert.Equal("Activities", addColumnOperation.Table);
        Assert.Equal("nvarchar(200)", addColumnOperation.ColumnType);
        Assert.Equal(200, addColumnOperation.MaxLength);
        Assert.True(addColumnOperation.IsNullable);
    }

    #region Helper Methods

    private object CreateMigrationInstance()
    {
        var migrationType = _migrationsAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "AddedFilesToSpot");

        if (migrationType == null)
        {
            throw new InvalidOperationException("AddedFilesToSpot migration type not found");
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