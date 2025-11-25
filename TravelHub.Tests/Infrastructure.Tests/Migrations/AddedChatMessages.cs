using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using TravelHub.Infrastructure;
using Xunit;

namespace TravelHub.Tests.Infrastructure.Tests.Migrations;

public class AddedChatMessagesMigrationTests
{
    private readonly Assembly _migrationsAssembly;

    public AddedChatMessagesMigrationTests()
    {
        _migrationsAssembly = typeof(ApplicationDbContext).Assembly;
    }

    [Fact]
    public void Up_ShouldAddColumnsAndCreateChatMessageTable()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeUpMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(5, migrationBuilder.Operations.Count);

        // Verify AddColumn operations
        var addColumnOperations = migrationBuilder.Operations.OfType<AddColumnOperation>().ToList();
        Assert.Equal(2, addColumnOperations.Count);

        var multiplierOperation = addColumnOperations.First(op => op.Name == "Multiplier");
        Assert.Equal("Expenses", multiplierOperation.Table);
        Assert.Equal(typeof(int), multiplierOperation.ClrType);
        Assert.False(multiplierOperation.IsNullable);
        Assert.Equal(0, multiplierOperation.DefaultValue);

        var fileNameOperation = addColumnOperations.First(op => op.Name == "FileName");
        Assert.Equal("Activities", fileNameOperation.Table);
        Assert.Equal("nvarchar(200)", fileNameOperation.ColumnType);
        Assert.Equal(200, fileNameOperation.MaxLength);

        // Verify CreateTable operation
        var createTableOperation = migrationBuilder.Operations.OfType<CreateTableOperation>().First();
        Assert.NotNull(createTableOperation);
        Assert.Equal("ChatMessage", createTableOperation.Name);
        Assert.Equal(4, createTableOperation.Columns.Count); // Id, Message, PersonId, TripId

        // Verify CreateIndex operations
        var createIndexOperations = migrationBuilder.Operations.OfType<CreateIndexOperation>().ToList();
        Assert.Equal(2, createIndexOperations.Count);
        Assert.All(createIndexOperations, op => Assert.Equal("ChatMessage", op.Table));
    }

    [Fact]
    public void Down_ShouldDropTableAndRemoveColumns()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeDownMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(3, migrationBuilder.Operations.Count);

        // Verify DropTable operation
        var dropTableOperation = migrationBuilder.Operations[0] as DropTableOperation;
        Assert.NotNull(dropTableOperation);
        Assert.Equal("ChatMessage", dropTableOperation.Name);

        // Verify DropColumn operations
        var dropColumnOperations = migrationBuilder.Operations.OfType<DropColumnOperation>().ToList();
        Assert.Equal(2, dropColumnOperations.Count);
        Assert.Contains(dropColumnOperations, op => op.Name == "Multiplier" && op.Table == "Expenses");
        Assert.Contains(dropColumnOperations, op => op.Name == "FileName" && op.Table == "Activities");
    }

    #region Helper Methods

    private object CreateMigrationInstance()
    {
        var migrationType = _migrationsAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "AddedChatMessages");

        if (migrationType == null)
        {
            throw new InvalidOperationException("AddedChatMessages migration type not found");
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
