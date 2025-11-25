using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using TravelHub.Domain.Entities;
using TravelHub.Infrastructure;
using Xunit;

namespace TravelHub.Tests.Infrastructure.Tests.Migrations;

public class AddedPhotoToPostAndCommentMigrationTests
{
    private readonly Assembly _migrationsAssembly;

    public AddedPhotoToPostAndCommentMigrationTests()
    {
        _migrationsAssembly = typeof(ApplicationDbContext).Assembly;
    }

    [Fact]
    public void Up_ShouldAddPhotoConnections()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeUpMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(9, migrationBuilder.Operations.Count);

        // Verify DropForeignKey
        var dropForeignKey = migrationBuilder.Operations[0] as DropForeignKeyOperation;
        Assert.NotNull(dropForeignKey);
        Assert.Equal("Comments", dropForeignKey.Table);

        // Verify DropColumn
        var dropColumn = migrationBuilder.Operations[1] as DropColumnOperation;
        Assert.NotNull(dropColumn);
        Assert.Equal("Rating", dropColumn.Name);
        Assert.Equal("Photos", dropColumn.Table);

        // Verify AddColumn operations
        var addColumns = migrationBuilder.Operations.OfType<AddColumnOperation>().ToList();
        Assert.Equal(2, addColumns.Count);

        var commentIdColumn = addColumns.First(op => op.Name == "CommentId");
        Assert.Equal("Photos", commentIdColumn.Table);
        Assert.True(commentIdColumn.IsNullable);

        var postIdColumn = addColumns.First(op => op.Name == "PostId");
        Assert.Equal("Photos", postIdColumn.Table);
        Assert.True(postIdColumn.IsNullable);

        // Verify CreateIndex operations
        var createIndexes = migrationBuilder.Operations.OfType<CreateIndexOperation>().ToList();
        Assert.Equal(2, createIndexes.Count);

        // Verify AddForeignKey operations
        var addForeignKeys = migrationBuilder.Operations.OfType<AddForeignKeyOperation>().ToList();
        Assert.Equal(3, addForeignKeys.Count);
    }

    [Fact]
    public void Down_ShouldRemovePhotoConnections()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeDownMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(9, migrationBuilder.Operations.Count);

        // Verify DropForeignKey operations
        var dropForeignKeys = migrationBuilder.Operations.OfType<DropForeignKeyOperation>().ToList();
        Assert.Equal(3, dropForeignKeys.Count);

        // Verify DropIndex operations
        var dropIndexes = migrationBuilder.Operations.OfType<DropIndexOperation>().ToList();
        Assert.Equal(2, dropIndexes.Count);

        // Verify DropColumn operations
        var dropColumns = migrationBuilder.Operations.OfType<DropColumnOperation>().ToList();
        Assert.Equal(2, dropColumns.Count);

        // Verify AddColumn
        var addColumn = migrationBuilder.Operations.OfType<AddColumnOperation>().First();
        Assert.NotNull(addColumn);
        Assert.Equal("Rating", addColumn.Name);
        Assert.Equal("Photos", addColumn.Table);

        // Verify AddForeignKey
        var addForeignKey = migrationBuilder.Operations.OfType<AddForeignKeyOperation>().First();
        Assert.NotNull(addForeignKey);
        Assert.Equal("Comments", addForeignKey.Table);
    }

    #region Helper Methods

    private object CreateMigrationInstance()
    {
        var migrationType = _migrationsAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "AddedPhotoToPostAndComment");

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