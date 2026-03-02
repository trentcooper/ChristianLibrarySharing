namespace ChristianLibrary.Services.DTOs.Profile;

/// <summary>
/// Response containing user location information
/// </summary>
public class LocationResponse
{
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? Country { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    /// <summary>
    /// Computed: Full address as a single string
    /// </summary>
    public string? FullAddress
    {
        get
        {
            var parts = new List<string>();
            
            if (!string.IsNullOrEmpty(Street)) parts.Add(Street);
            if (!string.IsNullOrEmpty(City)) parts.Add(City);
            if (!string.IsNullOrEmpty(State)) parts.Add(State);
            if (!string.IsNullOrEmpty(ZipCode)) parts.Add(ZipCode);
            if (!string.IsNullOrEmpty(Country)) parts.Add(Country);

            return parts.Count > 0 ? string.Join(", ", parts) : null;
        }
    }

    /// <summary>
    /// Computed: City and state only (for privacy)
    /// </summary>
    public string? CityState
    {
        get
        {
            if (!string.IsNullOrEmpty(City) && !string.IsNullOrEmpty(State))
                return $"{City}, {State}";
            if (!string.IsNullOrEmpty(City))
                return City;
            if (!string.IsNullOrEmpty(State))
                return State;
            return null;
        }
    }

    /// <summary>
    /// Whether coordinates are available for proximity search
    /// </summary>
    public bool HasCoordinates => Latitude.HasValue && Longitude.HasValue;
}