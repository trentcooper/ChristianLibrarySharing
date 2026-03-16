namespace ChristianLibrary.UnitTests.Services;

/// <summary>
/// Exposes the Haversine calculation for unit testing
/// </summary>
public static class HaversineTestHelper
{
    public static double CalculateDistanceMiles(
        double lat1, double lon1,
        double lat2, double lon2)
    {
        const double earthRadiusMiles = 3958.8;

        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return earthRadiusMiles * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
}