using System.Text.Json;
using ChristianLibrary.Services.DTOs.Books;
using ChristianLibrary.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace ChristianLibrary.Services;

/// <summary>
/// Looks up book details by Isbn using the Open Library API
/// </summary>
public class IsbnLookupService : IIsbnLookupService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<IsbnLookupService> _logger;

    private const string OpenLibraryBaseUrl =
        "https://openlibrary.org/api/books?bibkeys=Isbn:{0}&format=json&jscmd=data";

    public IsbnLookupService(HttpClient httpClient, ILogger<IsbnLookupService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IsbnLookupResponse> LookupByIsbnAsync(string isbn)
    {
        // Sanitize Isbn - strip hyphens and spaces
        var cleanIsbn = isbn.Replace("-", "").Replace(" ", "").Trim();

        _logger.LogInformation("Looking up Isbn {Isbn} via Open Library", cleanIsbn);

        try
        {
            var url = string.Format(OpenLibraryBaseUrl, cleanIsbn);
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Open Library returned {StatusCode} for Isbn {Isbn}",
                    response.StatusCode, cleanIsbn);
                return IsbnLookupResponse.CreateError(
                    "Unable to reach the book lookup service. Please try again later.");
            }

            var json = await response.Content.ReadAsStringAsync();
            var key = $"Isbn:{cleanIsbn}";

            using var document = JsonDocument.Parse(json);

            if (!document.RootElement.TryGetProperty(key, out var bookElement))
            {
                _logger.LogInformation("No book found for Isbn {Isbn}", cleanIsbn);
                return IsbnLookupResponse.CreateNotFound(cleanIsbn);
            }

            // Title
            var title = bookElement.TryGetProperty("title", out var titleEl)
                ? titleEl.GetString()
                : null;

            if (string.IsNullOrEmpty(title))
                return IsbnLookupResponse.CreateNotFound(cleanIsbn);

            // Authors - take the first one
            string? author = null;
            if (bookElement.TryGetProperty("authors", out var authorsEl)
                && authorsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var a in authorsEl.EnumerateArray())
                {
                    if (a.TryGetProperty("name", out var nameEl))
                    {
                        author = nameEl.GetString();
                        break;
                    }
                }
            }

            // Publishers - take the first one
            string? publisher = null;
            if (bookElement.TryGetProperty("publishers", out var publishersEl)
                && publishersEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var p in publishersEl.EnumerateArray())
                {
                    if (p.TryGetProperty("name", out var nameEl))
                    {
                        publisher = nameEl.GetString();
                        break;
                    }
                }
            }

            // Publication year - parse from publish_date string
            int? publicationYear = null;
            if (bookElement.TryGetProperty("publish_date", out var dateEl))
            {
                var dateStr = dateEl.GetString() ?? "";
                // Open Library returns dates like "1952", "June 1952", "June 16, 1952"
                var yearMatch = System.Text.RegularExpressions.Regex.Match(dateStr, @"\d{4}");
                if (yearMatch.Success && int.TryParse(yearMatch.Value, out var year))
                    publicationYear = year;
            }

            // Page count
            int? pageCount = null;
            if (bookElement.TryGetProperty("number_of_pages", out var pagesEl))
                pageCount = pagesEl.GetInt32();

            // Description
            string? description = null;
            if (bookElement.TryGetProperty("description", out var descEl))
            {
                // Description can be a string or an object with a "value" property
                description = descEl.ValueKind == JsonValueKind.String
                    ? descEl.GetString()
                    : descEl.TryGetProperty("value", out var valueEl)
                        ? valueEl.GetString()
                        : null;
            }

            // Cover image
            string? coverImageUrl = null;
            if (bookElement.TryGetProperty("cover", out var coverEl))
            {
                if (coverEl.TryGetProperty("large", out var largeEl))
                    coverImageUrl = largeEl.GetString();
                else if (coverEl.TryGetProperty("medium", out var mediumEl))
                    coverImageUrl = mediumEl.GetString();
            }

            _logger.LogInformation(
                "Isbn {Isbn} resolved to: {Title} by {Author}",
                cleanIsbn, title, author);

            return IsbnLookupResponse.CreateFound(
                title,
                author,
                publisher,
                publicationYear,
                pageCount,
                description,
                coverImageUrl,
                cleanIsbn,
                "English" // Open Library doesn't reliably return language
            );
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error during Isbn lookup for {Isbn}", cleanIsbn);
            return IsbnLookupResponse.CreateError(
                "Unable to reach the book lookup service. Please check your connection.");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Open Library response for Isbn {Isbn}", cleanIsbn);
            return IsbnLookupResponse.CreateError(
                "Received an unexpected response from the book lookup service.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Isbn lookup for {Isbn}", cleanIsbn);
            return IsbnLookupResponse.CreateError(
                "An unexpected error occurred during Isbn lookup.");
        }
    }
}