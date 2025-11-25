using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using TravelHub.Infrastructure;
using Xunit;

namespace TravelHub.Tests.Infrastructure.Tests.Migrations;

public class AddedTripParticipantsMigrationTests
{
    private readonly Assembly _migrationsAssembly;

    public AddedTripParticipantsMigrationTests()
    {
        _migrationsAssembly = typeof(ApplicationDbContext).Assembly;
    }

    [Fact]
    public void Up_ShouldCreateTripParticipantsTableAndAlterTripStatus()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeUpMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(12, migrationBuilder.Operations.Count);

        // Verify DropForeignKey operation
        var dropForeignKeyOperation = migrationBuilder.Operations[0] as DropForeignKeyOperation;
        Assert.NotNull(dropForeignKeyOperation);
        Assert.Equal("Trips", dropForeignKeyOperation.Table);

        // Verify AlterColumn operation
        var alterColumnOperation = migrationBuilder.Operations[1] as AlterColumnOperation;
        Assert.NotNull(alterColumnOperation);
        Assert.Equal("Status", alterColumnOperation.Name);
        Assert.Equal("Trips", alterColumnOperation.Table);
        Assert.Equal("nvarchar(20)", alterColumnOperation.ColumnType);
        Assert.Equal(20, alterColumnOperation.MaxLength);

        // Verify CreateTable operation
        var createTableOperation = migrationBuilder.Operations[2] as CreateTableOperation;
        Assert.NotNull(createTableOperation);
        Assert.Equal("TripParticipants", createTableOperation.Name);
        Assert.Equal(5, createTableOperation.Columns.Count); // Id, TripId, PersonId, JoinedAt, Status

        // Verify CreateIndex operations for Trips
        var createIndexOperations = migrationBuilder.Operations.OfType<CreateIndexOperation>().ToList();
        Assert.Equal(8, createIndexOperations.Count);

        var tripIndexes = createIndexOperations.Where(op => op.Table == "Trips").ToList();
        Assert.Equal(4, tripIndexes.Count);
        Assert.Contains(tripIndexes, op => op.Columns.Contains("EndDate"));
        Assert.Contains(tripIndexes, op => op.Columns.Contains("StartDate"));
        Assert.Contains(tripIndexes, op => op.Columns.Contains("Status"));
        Assert.Contains(tripIndexes, op => op.Columns.Contains("IsPrivate"));

        // Verify CreateIndex operations for TripParticipants
        var participantIndexes = createIndexOperations.Where(op => op.Table == "TripParticipants").ToList();
        Assert.Equal(4, participantIndexes.Count);
        Assert.Contains(participantIndexes, op => op.Columns.Contains("JoinedAt"));
        Assert.Contains(participantIndexes, op => op.Columns.Contains("PersonId"));
        Assert.Contains(participantIndexes, op => op.Columns.Contains("Status"));
        Assert.Contains(participantIndexes, op => op.Columns.SequenceEqual(new[] { "TripId", "PersonId" }));

        // Verify AddForeignKey operation
        var addForeignKeyOperation = migrationBuilder.Operations[^1] as AddForeignKeyOperation;
        Assert.NotNull(addForeignKeyOperation);
        Assert.Equal("Trips", addForeignKeyOperation.Table);
    }

    [Fact]
    public void Down_ShouldDropTripParticipantsTableAndRevertChanges()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeDownMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(8, migrationBuilder.Operations.Count);

        // Verify DropForeignKey operation
        var dropForeignKeyOperation = migrationBuilder.Operations[0] as DropForeignKeyOperation;
        Assert.NotNull(dropForeignKeyOperation);
        Assert.Equal("Trips", dropForeignKeyOperation.Table);

        // Verify DropTable operation
        var dropTableOperation = migrationBuilder.Operations[1] as DropTableOperation;
        Assert.NotNull(dropTableOperation);
        Assert.Equal("TripParticipants", dropTableOperation.Name);

        // Verify DropIndex operations for Trips
        var dropIndexOperations = migrationBuilder.Operations.OfType<DropIndexOperation>().ToList();
        Assert.Equal(4, dropIndexOperations.Count);
        Assert.All(dropIndexOperations, op => Assert.Equal("Trips", op.Table));

        // Verify AlterColumn operation
        var alterColumnOperation = migrationBuilder.Operations.OfType<AlterColumnOperation>().Single();
        Assert.Equal("Status", alterColumnOperation.Name);
        Assert.Equal("Trips", alterColumnOperation.Table);
        Assert.Equal("int", alterColumnOperation.ColumnType);


        // Verify AddForeignKey operation
        var addForeignKeyOperation = migrationBuilder.Operations.OfType<AddForeignKeyOperation>().First(o => o.Table == "Trips");
        Assert.NotNull(addForeignKeyOperation);
        Assert.Equal("Trips", addForeignKeyOperation.Table);

    }

    #region Helper Methods

    private object CreateMigrationInstance()
    {
        var migrationType = _migrationsAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "AddedTripParticipants");

        if (migrationType == null)
        {
            throw new InvalidOperationException("AddedTripParticipants migration type not found");
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