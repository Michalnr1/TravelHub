namespace TravelHub.Domain.Entities;

public class File
{
    public int Id { get; set; }
    public required string Name { get; set; }

    // Foreign Key for Spot
    public int? SpotId { get; set; }
    // Navigation Property back to the spot
    public Spot? Spot { get; set; }

    public bool IsEncrypted { get; set; } = false;
    public string? SaltBase64 { get; set; }    // salt used for PBKDF2
    public string? NonceBase64 { get; set; }   // nonce (IV) used for AES-GCM
    public string? DisplayName { get; set; }   // original file name shown to user
}
