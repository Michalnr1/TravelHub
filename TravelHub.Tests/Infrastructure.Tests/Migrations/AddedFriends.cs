using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using TravelHub.Infrastructure;
using Xunit;

namespace TravelHub.Tests.Infrastructure.Tests.Migrations;

public class AddedFriendsMigrationTests
{
    private readonly Assembly _migrationsAssembly;

    public AddedFriendsMigrationTests()
    {
        _migrationsAssembly = typeof(ApplicationDbContext).Assembly;
    }

    [Fact]
    public void Up_ShouldAddCreatedAtAndCreateFriendRequestsTable()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeUpMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(9, migrationBuilder.Operations.Count);

        // Verify DropForeignKey operations
        var dropForeignKeyOperations = migrationBuilder.Operations.OfType<DropForeignKeyOperation>().ToList();
        Assert.Equal(2, dropForeignKeyOperations.Count);
        Assert.All(dropForeignKeyOperations, op => Assert.Equal("PersonFriends", op.Table));

        // Verify AddColumn operation
        var addColumnOperation = migrationBuilder.Operations[2] as AddColumnOperation;
        Assert.NotNull(addColumnOperation);
        Assert.Equal("CreatedAt", addColumnOperation.Name);
        Assert.Equal("PersonFriends", addColumnOperation.Table);
        Assert.Equal(typeof(DateTimeOffset), addColumnOperation.ClrType);
        Assert.False(addColumnOperation.IsNullable);

        // Verify CreateTable operation
        var createTableOperation = migrationBuilder.Operations[3] as CreateTableOperation;
        Assert.NotNull(createTableOperation);
        Assert.Equal("FriendRequests", createTableOperation.Name);
        Assert.Equal(7, createTableOperation.Columns.Count); // Id, RequesterId, AddresseeId, Status, RequestedAt, RespondedAt, Message

        // Verify CreateIndex operations
        var createIndexOperations = migrationBuilder.Operations.OfType<CreateIndexOperation>().ToList();
        Assert.Equal(3, createIndexOperations.Count);
        Assert.All(createIndexOperations, op => Assert.Equal("FriendRequests", op.Table));

        // Verify AddForeignKey operations
        var addForeignKeyOperations = migrationBuilder.Operations.OfType<AddForeignKeyOperation>().ToList();
        Assert.Equal(2, addForeignKeyOperations.Count);
        Assert.All(addForeignKeyOperations, op => Assert.Equal("PersonFriends", op.Table));
    }

    [Fact]
    public void Down_ShouldDropFriendRequestsTableAndRemoveCreatedAt()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeDownMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(6, migrationBuilder.Operations.Count);

        // Verify DropForeignKey operations
        var dropForeignKeyOperations = migrationBuilder.Operations.OfType<DropForeignKeyOperation>().ToList();
        Assert.Equal(2, dropForeignKeyOperations.Count);
        Assert.All(dropForeignKeyOperations, op => Assert.Equal("PersonFriends", op.Table));

        // Verify DropTable operation
        var dropTableOperation = migrationBuilder.Operations[2] as DropTableOperation;
        Assert.NotNull(dropTableOperation);
        Assert.Equal("FriendRequests", dropTableOperation.Name);

        // Verify DropColumn operation
        var dropColumnOperation = migrationBuilder.Operations[3] as DropColumnOperation;
        Assert.NotNull(dropColumnOperation);
        Assert.Equal("CreatedAt", dropColumnOperation.Name);
        Assert.Equal("PersonFriends", dropColumnOperation.Table);

        // Verify AddForeignKey operations
        var addForeignKeyOperations = migrationBuilder.Operations.OfType<AddForeignKeyOperation>().ToList();
        Assert.Equal(2, addForeignKeyOperations.Count);
        Assert.All(addForeignKeyOperations, op => Assert.Equal("PersonFriends", op.Table));
    }

    #region Helper Methods

    private object CreateMigrationInstance()
    {
        var migrationType = _migrationsAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "AddedFriends");

        if (migrationType == null)
        {
            throw new InvalidOperationException("AddedFriends migration type not found");
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