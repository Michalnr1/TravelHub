using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace TravelHub.Infrastructure.Tests.Migrations;

public class AddedBlogModuleMigrationTests
{
    private readonly Assembly _migrationsAssembly;

    public AddedBlogModuleMigrationTests()
    {
        _migrationsAssembly = typeof(ApplicationDbContext).Assembly;
    }

    [Fact]
    public void Up_ShouldPerformAllOperations()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeUpMethod(migration, migrationBuilder);

        // Assert - Verify operation count
        Assert.Equal(27, migrationBuilder.Operations.Count); // All operations in Up method

        // Verify table rename
        var renameTableOperation = migrationBuilder.Operations.OfType<RenameTableOperation>().First();
        Assert.NotNull(renameTableOperation);
        Assert.Equal("ChatMessage", renameTableOperation.Name);
        Assert.Equal("ChatMessages", renameTableOperation.NewName);

        // Verify column additions
        var addColumnOperations = migrationBuilder.Operations.OfType<AddColumnOperation>().ToList();
        Assert.Equal(4, addColumnOperations.Count);

        var blogIdInTrips = addColumnOperations.First(op => op.Table == "Trips" && op.Name == "BlogId");
        Assert.True(blogIdInTrips.IsNullable);

        var blogIdInPosts = addColumnOperations.First(op => op.Table == "Posts" && op.Name == "BlogId");
        Assert.False(blogIdInPosts.IsNullable);
        Assert.Equal(0, blogIdInPosts.DefaultValue);

        var titleInPosts = addColumnOperations.First(op => op.Table == "Posts" && op.Name == "Title");
        Assert.Equal("nvarchar(200)", titleInPosts.ColumnType);
        Assert.Equal(200, titleInPosts.MaxLength);

        // Verify alter column
        var alterColumnOperation = migrationBuilder.Operations.OfType<AlterColumnOperation>().First();
        Assert.NotNull(alterColumnOperation);
        Assert.Equal("Name", alterColumnOperation.Name);
        Assert.Equal("Days", alterColumnOperation.Table);
        Assert.Equal("nvarchar(100)", alterColumnOperation.ColumnType);

        // Verify create table
        var createTableOperation = migrationBuilder.Operations.OfType<CreateTableOperation>().First();
        Assert.NotNull(createTableOperation);
        Assert.Equal("Blogs", createTableOperation.Name);
        Assert.Equal(5, createTableOperation.Columns.Count); // Id, Name, Description, OwnerId, TripId
    }

    [Fact]
    public void Down_ShouldRevertAllChanges()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeDownMethod(migration, migrationBuilder);

        // Assert - Verify operation count
        Assert.Equal(25, migrationBuilder.Operations.Count); // All operations in Down method

        // Verify table rename back
        var renameTableOperation = migrationBuilder.Operations.OfType<RenameTableOperation>().First();
        Assert.NotNull(renameTableOperation);
        Assert.Equal("ChatMessages", renameTableOperation.Name);
        Assert.Equal("ChatMessage", renameTableOperation.NewName);

        // Verify column drops
        var dropColumnOperations = migrationBuilder.Operations.OfType<DropColumnOperation>().ToList();
        Assert.Equal(4, dropColumnOperations.Count);

        // Verify drop table
        var dropTableOperation = migrationBuilder.Operations.OfType<DropTableOperation>().First();
        Assert.NotNull(dropTableOperation);
        Assert.Equal("Blogs", dropTableOperation.Name);

        // Verify alter column back
        var alterColumnOperation = migrationBuilder.Operations.OfType<AlterColumnOperation>().First();
        Assert.NotNull(alterColumnOperation);
        Assert.Equal("Name", alterColumnOperation.Name);
        Assert.Equal("Days", alterColumnOperation.Table);
        Assert.Equal("nvarchar(max)", alterColumnOperation.ColumnType);
    }

    #region Helper Methods

    private object CreateMigrationInstance()
    {
        var migrationType = _migrationsAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "AddedBlogModule");

        if (migrationType == null)
        {
            throw new InvalidOperationException("AddedBlogModule migration type not found");
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