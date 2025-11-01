namespace TravelHub.Domain.DTOs;

public class ParticipantShareDto
{
    public required string PersonId { get; set; }

    // Wartość wpisana przez użytkownika, może być procentem lub kwotą
    public decimal InputValue { get; set; } = 0m;

    // Flaga do określenia, co wpisał użytkownik: 0: Równo, 1: Kwota, 2: Procent
    public int ShareType { get; set; } = 0;
}
