using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Xplore.Infrastructure.Map;

/// <summary>
/// Service that interacts with Wikipedia and Wikidata APIs to enrich POIs with
/// comprehensive descriptions and high-quality images.
/// Respects Wikimedia's rate limits (max 200 req/s, but we use a lower concurrency limit)
/// and caches responses to avoid redownloading data for the same POI.
/// </summary>
public class WikipediaApiService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<WikipediaApiService> _logger;
    
    // Concurrency limit to respect Wikimedia API policies
    private static readonly SemaphoreSlim _semaphore = new(10, 10);

    public WikipediaApiService(HttpClient httpClient, IMemoryCache cache, ILogger<WikipediaApiService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        
        // Ensure a User-Agent is set as required by Wikimedia policies
        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "XploreApp/1.0 (https://github.com/your-repo/Xplore; contact@yourdomain.com)");
        }
    }

    /// <summary>
    /// Attempts to enrich an Overpass POI with Wikipedia description and image.
    /// Returns a new instance of OverpassPoi with updated fields if successful.
    /// </summary>
    public async Task<OverpassPoi> EnrichPoiAsync(OverpassPoi poi, string language = "it")
    {
        string? wikiTitle = null;
        string? wikiLang = language;

        // 2. Resolve Title
        if (!string.IsNullOrEmpty(poi.WikipediaTag))
        {
            // Wikipedia tag format: "lang:Title" or "Title"
            var parts = poi.WikipediaTag.Split(':', 2);
            if (parts.Length == 2)
            {
                wikiLang = parts[0];
                wikiTitle = parts[1];
            }
            else
            {
                wikiTitle = parts[0];
            }
        }
        else if (!string.IsNullOrEmpty(poi.WikidataTag))
        {
            // We only have Wikidata ID (e.g. Q12345). We need to resolve it to a Wikipedia title.
            wikiTitle = await ResolveWikidataToWikipediaAsync(poi.WikidataTag, language);
        }
        else if (!string.IsNullOrEmpty(poi.Name) && poi.Name != "Unknown")
        {
            // Fallback: Search Wikipedia by Name
            wikiTitle = await SearchWikipediaByNameAsync(poi.Name, language);
        }

        if (string.IsNullOrEmpty(wikiTitle))
        {
            // No Wikipedia title found — still try Wikidata image as last resort
            if (!string.IsNullOrEmpty(poi.WikidataTag) && string.IsNullOrEmpty(poi.ImageUrl))
            {
                var wikidataImage = await FetchWikidataImageAsync(poi.WikidataTag);
                if (wikidataImage != null)
                    return poi with { ImageUrl = wikidataImage };
            }
            return poi;
        }

        // 3. Fetch Wikipedia Summary
        var (description, imageUrl) = await FetchWikipediaSummaryAsync(wikiTitle, wikiLang);

        // 4. Fallback to Wikidata P18 image if Wikipedia didn't provide one
        if (imageUrl == null && !string.IsNullOrEmpty(poi.WikidataTag))
        {
            imageUrl = await FetchWikidataImageAsync(poi.WikidataTag);
        }

        if (description != null || imageUrl != null)
        {
            return poi with 
            { 
                Description = description ?? poi.Description,
                ImageUrl = imageUrl ?? poi.ImageUrl
            };
        }

        return poi;
    }

    private async Task<string?> ResolveWikidataToWikipediaAsync(string wikidataId, string language)
    {
        var cacheKey = $"wikidata_{wikidataId}_{language}";
        if (_cache.TryGetValue(cacheKey, out string? cachedTitle))
        {
            return cachedTitle;
        }

        await _semaphore.WaitAsync();
        try
        {
            var url = $"https://www.wikidata.org/w/api.php?action=wbgetentities&ids={wikidataId}&props=sitelinks&sitefilter={language}wiki&format=json";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var entities = doc.RootElement.GetProperty("entities");
            if (entities.TryGetProperty(wikidataId, out var entity))
            {
                if (entity.TryGetProperty("sitelinks", out var sitelinks))
                {
                    if (sitelinks.TryGetProperty($"{language}wiki", out var site))
                    {
                        var title = site.GetProperty("title").GetString();
                        
                        _cache.Set(cacheKey, title, TimeSpan.FromDays(7));
                        return title;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve Wikidata ID {WikidataId} to Wikipedia article.", wikidataId);
        }
        finally
        {
            _semaphore.Release();
        }

        // Cache negative result briefly to avoid bad requests spam
        _cache.Set(cacheKey, (string?)null, TimeSpan.FromHours(1));
        return null;
    }

    private async Task<(string? Description, string? ImageUrl)> FetchWikipediaSummaryAsync(string title, string language)
    {
        var encodedTitle = Uri.EscapeDataString(title.Replace(' ', '_'));
        var cacheKey = $"wikipedia_summary_{language}_{encodedTitle}";

        if (_cache.TryGetValue(cacheKey, out (string?, string?) cachedResult))
        {
            return cachedResult;
        }

        await _semaphore.WaitAsync();
        try
        {
            var url = $"https://{language}.wikipedia.org/api/rest_v1/page/summary/{encodedTitle}";
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                string? extract = root.TryGetProperty("extract", out var extractProp) ? extractProp.GetString() : null;
                string? imageUrl = null;

                if (root.TryGetProperty("thumbnail", out var thumbnail))
                {
                    imageUrl = thumbnail.TryGetProperty("source", out var source) ? source.GetString() : null;
                    
                    // The thumbnail returned by the summary API is sometimes small (320px).
                    // We can attempt to request a slightly larger version (e.g., 640px)
                    if (imageUrl != null && imageUrl.Contains("/320px-"))
                    {
                        imageUrl = imageUrl.Replace("/320px-", "/640px-");
                    }
                }
                else if (root.TryGetProperty("originalimage", out var originalImage))
                {
                    imageUrl = originalImage.TryGetProperty("source", out var source) ? source.GetString() : null;
                }

                var result = (extract, imageUrl);
                _cache.Set(cacheKey, result, TimeSpan.FromDays(7));
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch Wikipedia summary for {Title} ({Lang}).", title, language);
        }
        finally
        {
            _semaphore.Release();
        }

        var emptyResult = ((string?)null, (string?)null);
        _cache.Set(cacheKey, emptyResult, TimeSpan.FromHours(1));
        return emptyResult;
    }

    private async Task<string?> SearchWikipediaByNameAsync(string name, string language)
    {
        var encodedName = Uri.EscapeDataString(name);
        var cacheKey = $"wikipedia_search_{language}_{encodedName}";

        if (_cache.TryGetValue(cacheKey, out string? cachedTitle))
        {
            return cachedTitle;
        }

        await _semaphore.WaitAsync();
        try
        {
            var url = $"https://{language}.wikipedia.org/w/api.php?action=query&list=search&srsearch={encodedName}&utf8=&format=json&srlimit=1";
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("query", out var query) && query.TryGetProperty("search", out var searchArray))
                {
                    if (searchArray.GetArrayLength() > 0)
                    {
                        var title = searchArray[0].GetProperty("title").GetString();
                        _cache.Set(cacheKey, title, TimeSpan.FromDays(7));
                        return title;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to search Wikipedia for name {Name} ({Lang}).", name, language);
        }
        finally
        {
            _semaphore.Release();
        }

        _cache.Set(cacheKey, (string?)null, TimeSpan.FromHours(1));
        return null;
    }

    /// <summary>
    /// Fetches a high-quality image URL from Wikidata using the P18 (image) property.
    /// Builds a Wikimedia Commons URL from the filename.
    /// </summary>
    private async Task<string?> FetchWikidataImageAsync(string wikidataId)
    {
        var cacheKey = $"wikidata_image_{wikidataId}";
        if (_cache.TryGetValue(cacheKey, out string? cachedUrl))
        {
            return cachedUrl;
        }

        await _semaphore.WaitAsync();
        try
        {
            var url = $"https://www.wikidata.org/w/api.php?action=wbgetclaims&entity={wikidataId}&property=P18&format=json";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("claims", out var claims) &&
                claims.TryGetProperty("P18", out var p18Array) &&
                p18Array.GetArrayLength() > 0)
            {
                var mainSnak = p18Array[0].GetProperty("mainsnak");
                if (mainSnak.TryGetProperty("datavalue", out var datavalue))
                {
                    var filename = datavalue.GetProperty("value").GetString();
                    if (!string.IsNullOrEmpty(filename))
                    {
                        // Build Wikimedia Commons URL from filename
                        var encodedFilename = Uri.EscapeDataString(filename.Replace(' ', '_'));
                        var imageUrl = $"https://commons.wikimedia.org/wiki/Special:FilePath/{encodedFilename}";

                        _logger.LogDebug("🖼️ [Wikidata] P18 image for {Id}: {Url}", wikidataId, imageUrl);
                        _cache.Set(cacheKey, imageUrl, TimeSpan.FromDays(7));
                        return imageUrl;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch Wikidata P18 image for {WikidataId}.", wikidataId);
        }
        finally
        {
            _semaphore.Release();
        }

        _cache.Set(cacheKey, (string?)null, TimeSpan.FromHours(1));
        return null;
    }
}
