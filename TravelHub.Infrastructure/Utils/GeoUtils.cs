namespace TravelHub.Web.Utils;

public static class GeoUtils
{

    public static double GetSmallestDistance(List<(double, double)> points, (double, double) newPoint)
    {
        if (points.Count == 0) return 0;

        (double newLat, double newLng) = newPoint;

        double smallest = double.MaxValue;
        foreach ((double lat, double lng) in points) 
        { 
            smallest = Math.Min(smallest, HaversineDistance(lat, lng, newLat, newLng));
        }
        return smallest;
    }

    public static double HaversineDistance(double lat1, double lon1,
                                           double lat2, double lon2)
    {
        const double R = 6371;
        double dLat = ToRadians(lat2 - lat1);
        double dLon = ToRadians(lon2 - lon1);
        double a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRadians(double angle)
    {
        return Math.PI * angle / 180.0;
    }
}
