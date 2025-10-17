namespace TravelHub.Domain.Entities;

public enum Status
{
    Planning,
    Finished
}

public enum TransportationType
{
    Car,
    Motorcycle,
    Plane,
    Ship,
    Ferry,
    Taxi,
    Bus,
    Walk
}

// The diagram shows values like "1/5", "2/5", etc.
// These have been mapped to corresponding integer values.
public enum Rating
{
    One = 1,
    Two = 2,
    Three = 3,
    Four = 4,
    Five = 5
}
