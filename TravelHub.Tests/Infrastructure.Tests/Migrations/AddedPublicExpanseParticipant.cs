using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using TravelHub.Infrastructure;
using Xunit;

namespace TravelHub.Tests.Infrastructure.Tests.Migrations;

public class AddedPublicExpanseParticipantMigrationTests
{
    private readonly Assembly _migrationsAssembly;

    public AddedPublicExpanseParticipantMigrationTests()
    {
        _migrationsAssembly = typeof(ApplicationDbContext).Assembly;
    }

    [Fact]
    public void Up_ShouldRenameColumnsAndAddNewColumns()
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
        Assert.All(dropForeignKeyOperations, op => Assert.Equal("ExpenseParticipants", op.Table));

        // Verify RenameColumn operations
        var renameColumnOperations = migrationBuilder.Operations.OfType<RenameColumnOperation>().ToList();
        Assert.Equal(2, renameColumnOperations.Count);
        Assert.Contains(renameColumnOperations, op => op.Name == "ParticipantsId" && op.NewName == "PersonId");
        Assert.Contains(renameColumnOperations, op => op.Name == "ExpensesToCoverId" && op.NewName == "ExpenseId");

        // Verify RenameIndex operation
        var renameIndexOperation = migrationBuilder.Operations.OfType<RenameIndexOperation>().First();
        Assert.NotNull(renameIndexOperation);
        Assert.Equal("ExpenseParticipants", renameIndexOperation.Table);
        Assert.Equal("IX_ExpenseParticipants_ParticipantsId", renameIndexOperation.Name);
        Assert.Equal("IX_ExpenseParticipants_PersonId", renameIndexOperation.NewName);

        // Verify AddColumn operations
        var addColumnOperations = migrationBuilder.Operations.OfType<AddColumnOperation>().ToList();
        Assert.Equal(2, addColumnOperations.Count);

        var actualShareValueOperation = addColumnOperations.First(op => op.Name == "ActualShareValue");
        Assert.Equal("ExpenseParticipants", actualShareValueOperation.Table);
        Assert.Equal("decimal(18,2)", actualShareValueOperation.ColumnType);
        Assert.False(actualShareValueOperation.IsNullable);
        Assert.Equal(0m, actualShareValueOperation.DefaultValue);

        var shareOperation = addColumnOperations.First(op => op.Name == "Share");
        Assert.Equal("ExpenseParticipants", shareOperation.Table);
        Assert.Equal("decimal(18,3)", shareOperation.ColumnType);
        Assert.False(shareOperation.IsNullable);
        Assert.Equal(0m, shareOperation.DefaultValue);

        // Verify AddForeignKey operations
        var addForeignKeyOperations = migrationBuilder.Operations.OfType<AddForeignKeyOperation>().ToList();
        Assert.Equal(2, addForeignKeyOperations.Count);
        Assert.All(addForeignKeyOperations, op => Assert.Equal("ExpenseParticipants", op.Table));
    }

    [Fact]
    public void Down_ShouldRevertColumnNamesAndRemoveColumns()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeDownMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(9, migrationBuilder.Operations.Count);

        // Verify DropForeignKey operations
        var dropForeignKeyOperations = migrationBuilder.Operations.OfType<DropForeignKeyOperation>().ToList();
        Assert.Equal(2, dropForeignKeyOperations.Count);
        Assert.All(dropForeignKeyOperations, op => Assert.Equal("ExpenseParticipants", op.Table));

        // Verify DropColumn operations
        var dropColumnOperations = migrationBuilder.Operations.OfType<DropColumnOperation>().ToList();
        Assert.Equal(2, dropColumnOperations.Count);
        Assert.All(dropColumnOperations, op => Assert.Equal("ExpenseParticipants", op.Table));

        // Verify RenameColumn operations
        var renameColumnOperations = migrationBuilder.Operations.OfType<RenameColumnOperation>().ToList();
        Assert.Equal(2, renameColumnOperations.Count);
        Assert.Contains(renameColumnOperations, op => op.Name == "PersonId" && op.NewName == "ParticipantsId");
        Assert.Contains(renameColumnOperations, op => op.Name == "ExpenseId" && op.NewName == "ExpensesToCoverId");

        // Verify RenameIndex operation
        var renameIndexOperation = migrationBuilder.Operations.OfType<RenameIndexOperation>().First();
        Assert.NotNull(renameIndexOperation);
        Assert.Equal("ExpenseParticipants", renameIndexOperation.Table);
        Assert.Equal("IX_ExpenseParticipants_PersonId", renameIndexOperation.Name);
        Assert.Equal("IX_ExpenseParticipants_ParticipantsId", renameIndexOperation.NewName);

        // Verify AddForeignKey operations
        var addForeignKeyOperations = migrationBuilder.Operations.OfType<AddForeignKeyOperation>().ToList();
        Assert.Equal(2, addForeignKeyOperations.Count);
        Assert.All(addForeignKeyOperations, op => Assert.Equal("ExpenseParticipants", op.Table));
    }

    #region Helper Methods

    private object CreateMigrationInstance()
    {
        var migrationType = _migrationsAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "AddedPublicExpanseParticipant");

        if (migrationType == null)
        {
            throw new InvalidOperationException("AddedPublicExpanseParticipant migration type not found");
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