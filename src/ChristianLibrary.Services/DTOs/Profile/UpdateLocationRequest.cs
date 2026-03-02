using System.ComponentModel.DataAnnotations;

namespace ChristianLibrary.Services.DTOs.Profile;

/// <summary>
/// Request to update user location/address
/// </summary>
public class UpdateLocationRequest
{
    /// <summary>
    /// Street address
    /// </summary>
    [StringLength(200)]
    public string? Street { get; set; }

    /// <summary>
    /// City
    /// </summary>
    [StringLength(100)]
    public string? City { get; set; }

    /// <summary>
    /// State or province
    /// </summary>
    [StringLength(100)]
    public string? State { get; set; }

    /// <summary>
    /// ZIP or postal code
    /// </summary>
    [StringLength(20)]
    public string? ZipCode { get; set; }

    /// <summary>
    /// Country
    /// </summary>
    [StringLength(100)]
    public string? Country { get; set; }

    /// <summary>
    /// Optional: Manually provided latitude
    /// </summary>
    public decimal? Latitude { get; set; }

    /// <summary>
    /// Optional: Manually provided longitude
    /// </summary>
    public decimal? Longitude { get; set; }
}