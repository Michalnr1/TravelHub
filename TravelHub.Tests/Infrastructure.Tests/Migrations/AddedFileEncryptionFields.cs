using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using TravelHub.Infrastructure;
using Xunit;

namespace TravelHub.Tests.Infrastructure.Tests.Migrations;

public class AddedFileEncryptionFieldsMigrationTests
{
    private readonly Assembly _migrationsAssembly;

    public AddedFileEncryptionFieldsMigrationTests()
    {
        _migrationsAssembly = typeof(ApplicationDbContext).Assembly;
    }

    [Fact]
    public void Up_ShouldAddAllEncryptionColumns()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeUpMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(4, migrationBuilder.Operations.Count);

        var displayNameOperation = migrationBuilder.Operations[0] as AddColumnOperation;
        var isEncryptedOperation = migrationBuilder.Operations[1] as AddColumnOperation;
        var nonceOperation = migrationBuilder.Operations[2] as AddColumnOperation;
        var saltOperation = migrationBuilder.Operations[3] as AddColumnOperation;

        // Verify DisplayName
        Assert.NotNull(displayNameOperation);
        Assert.Equal("DisplayName", displayNameOperation.Name);
        Assert.Equal("File", displayNameOperation.Table);
        Assert.Equal(typeof(string), displayNameOperation.ClrType);
        Assert.Equal("nvarchar(max)", displayNameOperation.ColumnType);
        Assert.True(displayNameOperation.IsNullable);

        // Verify IsEncrypted
        Assert.NotNull(isEncryptedOperation);
        Assert.Equal("IsEncrypted", isEncryptedOperation.Name);
        Assert.Equal("File", isEncryptedOperation.Table);
        Assert.Equal(typeof(bool), isEncryptedOperation.ClrType);
        Assert.Equal("bit", isEncryptedOperation.ColumnType);
        Assert.False(isEncryptedOperation.IsNullable);
        Assert.Equal(false, isEncryptedOperation.DefaultValue);

        // Verify NonceBase64
        Assert.NotNull(nonceOperation);
        Assert.Equal("NonceBase64", nonceOperation.Name);
        Assert.Equal("File", nonceOperation.Table);
        Assert.Equal(typeof(string), nonceOperation.ClrType);
        Assert.Equal("nvarchar(max)", nonceOperation.ColumnType);
        Assert.True(nonceOperation.IsNullable);

        // Verify SaltBase64
        Assert.NotNull(saltOperation);
        Assert.Equal("SaltBase64", saltOperation.Name);
        Assert.Equal("File", saltOperation.Table);
        Assert.Equal(typeof(string), saltOperation.ClrType);
        Assert.Equal("nvarchar(max)", saltOperation.ColumnType);
        Assert.True(saltOperation.IsNullable);
    }

    [Fact]
    public void Down_ShouldRemoveAllEncryptionColumns()
    {
        // Arrange
        var migration = CreateMigrationInstance();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act
        InvokeDownMethod(migration, migrationBuilder);

        // Assert
        Assert.Equal(4, migrationBuilder.Operations.Count);

        var operations = migrationBuilder.Operations.Cast<DropColumnOperation>().ToList();

        Assert.All(operations, op => Assert.Equal("File", op.Table));
        Assert.Contains(operations, op => op.Name == "DisplayName");
        Assert.Contains(operations, op => op.Name == "IsEncrypted");
        Assert.Contains(operations, op => op.Name == "NonceBase64");
        Assert.Contains(operations, op => op.Name == "SaltBase64");
    }

    #region Helper Methods

    private object CreateMigrationInstance()
    {
        var migrationType = _migrationsAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "AddedFileEncryptionFields");

        if (migrationType == null)
        {
            throw new InvalidOperationException("AddedFileEncryptionFields migration type not found");
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