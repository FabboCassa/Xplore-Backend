using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Xplore.Infrastructure.Map;

/// <summary>
/// Service that queries OpenStreetMap's Overpass API to fetch Points of Interest.
/// Stateless proxy — no data persisted. ODbL license, free, no API key.
///
/// Queries ALL tourist-relevant POI types:
/// tourism=*, historic=*, amenity=place_of_worship, leisure=park/garden.
/// </summary>
public class OverpassApiService
{
    private readonly HttpClient _httpClient;
    private readonly WikipediaApiService _wikipediaApiService;
    private readonly ILogger<OverpassApiService> _logger;
    private const string OverpassUrl = "https://overpass-api.de/api/interpreter";

    public OverpassApiService(HttpClient httpClient, WikipediaApiService wikipediaApiService, ILogger<OverpassApiService> logger)
    {
        _httpClient = httpClient;
        _wikipediaApiService = wikipediaApiService;
        _logger = logger;
    }

    public async Task<List<OverpassPoi>> GetPoisAsync(double lat, double lon, double radiusKm)
    {
        var radiusMeters = (int)(radiusKm * 1000);
        var latStr = lat.ToString(CultureInfo.InvariantCulture);
        var lonStr = lon.ToString(CultureInfo.InvariantCulture);

        // Comprehensive Overpass QL query for ALL tourist-relevant POI types
        var query = $"""
            [out:json][timeout:15];
            (
              // Tourism: museums, galleries, artwork, attractions, viewpoints, info centers
              nwr["tourism"~"museum|gallery|artwork|attraction|viewpoint|information|zoo|aquarium|theme_park"](around:{radiusMeters},{latStr},{lonStr});

              // Historic: castles, monuments, memorials, ruins, archaeological sites, churches
              nwr["historic"~"castle|monument|memorial|ruins|archaeological_site|fort|manor|palace|city_gate|tower|church"](around:{radiusMeters},{latStr},{lonStr});

              // Religious: cathedrals, churches, temples, mosques, synagogues
              nwr["amenity"="place_of_worship"](around:{radiusMeters},{latStr},{lonStr});

              // Nature & parks
              nwr["leisure"~"park|garden|nature_reserve"](around:{radiusMeters},{latStr},{lonStr});
              nwr["boundary"="national_park"](around:{radiusMeters},{latStr},{lonStr});

              // Culture: theatres, cinemas, arts centres, libraries
              nwr["amenity"~"theatre|cinema|arts_centre|library"](around:{radiusMeters},{latStr},{lonStr});
            );
            out center body;
            """;

        _logger.LogInformation("🌍 [Overpass] Querying ALL POI types: center=({Lat}, {Lon}), radius={Radius}m", latStr, lonStr, radiusMeters);

        var sw = Stopwatch.StartNew();

        try
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("data", query)
            });

            var response = await _httpClient.PostAsync(OverpassUrl, content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            sw.Stop();

            _logger.LogInformation("🌍 [Overpass] Response in {Elapsed}ms, size={Size} bytes", sw.ElapsedMilliseconds, json.Length);

            var pois = ParseOverpassResponse(json);

            // Filter out POIs without a name
            var named = pois.Where(p => p.Name != "Unknown" && !string.IsNullOrWhiteSpace(p.Name)).ToList();

            // Enrich POIs using Wikipedia/Wikidata in parallel
            var enrichedPois = new List<OverpassPoi>();
            var enrichTasks = named.Select(poi => _wikipediaApiService.EnrichPoiAsync(poi));
            var enrichedResults = await Task.WhenAll(enrichTasks);
            enrichedPois.AddRange(enrichedResults);

            _logger.LogInformation("🌍 [Overpass] {Total} total → {Named} with names (filtered {Removed} unnamed), {Enriched} enriched",
                pois.Count, named.Count, pois.Count - named.Count, enrichedPois.Count);

            return enrichedPois;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "🌍 [Overpass] FAILED after {Elapsed}ms for lat={Lat}, lon={Lon}", sw.ElapsedMilliseconds, lat, lon);
            return [];
        }
    }

    /// <summary>
    /// Search POIs by name within a given radius using Overpass QL regex filter.
    /// Only returns named POIs whose name matches the query (case-insensitive).
    /// </summary>
    public async Task<List<OverpassPoi>> SearchPoisAsync(string query, double lat, double lon, double radiusKm)
    {
        var radiusMeters = (int)(radiusKm * 1000);
        var latStr = lat.ToString(CultureInfo.InvariantCulture);
        var lonStr = lon.ToString(CultureInfo.InvariantCulture);

        // Escape special regex characters in the user's query
        var escapedQuery = System.Text.RegularExpressions.Regex.Escape(query);

        var overpassQuery = $"""
            [out:json][timeout:15];
            (
              nwr["name"~"{escapedQuery}",i]["tourism"](around:{radiusMeters},{latStr},{lonStr});
              nwr["name"~"{escapedQuery}",i]["historic"](around:{radiusMeters},{latStr},{lonStr});
              nwr["name"~"{escapedQuery}",i]["amenity"~"place_of_worship|theatre|cinema|arts_centre|library"](around:{radiusMeters},{latStr},{lonStr});
              nwr["name"~"{escapedQuery}",i]["leisure"~"park|garden|nature_reserve"](around:{radiusMeters},{latStr},{lonStr});
            );
            out center body;
            """;

        _logger.LogInformation("🔎 [Overpass] Search '{Query}': center=({Lat}, {Lon}), radius={Radius}m",
            query, latStr, lonStr, radiusMeters);

        var sw = Stopwatch.StartNew();

        try
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("data", overpassQuery)
            });

            var response = await _httpClient.PostAsync(OverpassUrl, content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            sw.Stop();

            _logger.LogInformation("🔎 [Overpass] Search response in {Elapsed}ms, size={Size} bytes",
                sw.ElapsedMilliseconds, json.Length);

            var pois = ParseOverpassResponse(json);
            var named = pois.Where(p => p.Name != "Unknown" && !string.IsNullOrWhiteSpace(p.Name)).ToList();

            // Enrich with Wikipedia data
            var enrichTasks = named.Select(poi => _wikipediaApiService.EnrichPoiAsync(poi));
            var enrichedPois = (await Task.WhenAll(enrichTasks)).ToList();

            _logger.LogInformation("🔎 [Overpass] Search '{Query}' → {Count} results", query, enrichedPois.Count);
            return enrichedPois;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "🔎 [Overpass] Search FAILED after {Elapsed}ms for query='{Query}'",
                sw.ElapsedMilliseconds, query);
            return [];
        }
    }

    private static List<OverpassPoi> ParseOverpassResponse(string json)
    {
        var result = new List<OverpassPoi>();
        var seen = new HashSet<string>(); // Deduplicate by OSM ID

        using var doc = JsonDocument.Parse(json);
        var elements = doc.RootElement.GetProperty("elements");

        foreach (var element in elements.EnumerateArray())
        {
            double poiLat, poiLon;

            // Ways/relations use "center", nodes use direct lat/lon
            if (element.TryGetProperty("center", out var center))
            {
                poiLat = center.GetProperty("lat").GetDouble();
                poiLon = center.GetProperty("lon").GetDouble();
            }
            else if (element.TryGetProperty("lat", out _))
            {
                poiLat = element.GetProperty("lat").GetDouble();
                poiLon = element.GetProperty("lon").GetDouble();
            }
            else
            {
                continue; // Skip elements without coordinates
            }

            var tags = element.TryGetProperty("tags", out var tagsElement) ? tagsElement : default;
            var name = GetTag(tags, "name") ?? "Unknown";

            var osmId = element.GetProperty("id").GetInt64();
            var osmType = element.GetProperty("type").GetString() ?? "node";
            var id = $"osm_{osmType}_{osmId}";

            // Deduplicate (same element can appear in multiple query groups)
            if (!seen.Add(id)) continue;

            // ── Extract rich metadata ──
            var description = GetTag(tags, "description")
                           ?? GetTag(tags, "note")
                           ?? GetTag(tags, "description:en");

            var category = ClassifyCategory(tags);

            // Image: try direct image tag, then build Wikipedia thumbnail URL
            var imageUrl = GetTag(tags, "image")
                        ?? GetWikipediaThumbnailUrl(GetTag(tags, "wikipedia"))
                        ?? GetWikidataThumbnailUrl(GetTag(tags, "wikidata"));

            var type = ClassifyType(tags);
            var wikipediaTag = GetTag(tags, "wikipedia");
            var wikidataTag = GetTag(tags, "wikidata");

            result.Add(new OverpassPoi(
                Id: id,
                Name: name,
                Latitude: poiLat,
                Longitude: poiLon,
                Type: type,
                Description: description,
                Category: category,
                ImageUrl: imageUrl,
                WikipediaTag: wikipediaTag,
                WikidataTag: wikidataTag
            ));
        }

        return result;
    }

    /// <summary>
    /// Classify POI into a broad type based on OSM tags.
    /// </summary>
    private static string ClassifyType(JsonElement tags)
    {
        var tourism = GetTag(tags, "tourism");
        var historic = GetTag(tags, "historic");
        var amenity = GetTag(tags, "amenity");
        var leisure = GetTag(tags, "leisure");

        if (tourism is "museum" or "gallery") return "MUSEUM";
        if (tourism == "artwork") return "ARTWORK";
        if (historic != null) return "HISTORIC";
        if (amenity == "place_of_worship") return "RELIGIOUS";
        if (leisure is "park" or "garden" or "nature_reserve") return "NATURE";
        if (amenity is "theatre" or "cinema" or "arts_centre") return "CULTURE";
        if (amenity == "library") return "CULTURE";
        if (tourism == "attraction") return "ATTRACTION";
        if (tourism is "viewpoint") return "VIEWPOINT";
        if (tourism is "zoo" or "aquarium" or "theme_park") return "ATTRACTION";
        return "OTHER";
    }

    /// <summary>
    /// Classify into a human-readable category label.
    /// </summary>
    private static string ClassifyCategory(JsonElement tags)
    {
        var tourism = GetTag(tags, "tourism");
        var historic = GetTag(tags, "historic");
        var amenity = GetTag(tags, "amenity");
        var leisure = GetTag(tags, "leisure");
        var religion = GetTag(tags, "religion");

        if (tourism == "museum") return "Museo";
        if (tourism == "gallery") return "Galleria d'Arte";
        if (tourism == "artwork") return "Opera d'Arte";
        if (tourism == "attraction") return "Attrazione";
        if (tourism == "viewpoint") return "Punto Panoramico";
        if (tourism == "information") return "Info Turistiche";
        if (tourism == "zoo") return "Zoo";
        if (tourism == "aquarium") return "Acquario";
        if (tourism == "theme_park") return "Parco Tematico";

        if (historic == "castle") return "Castello";
        if (historic == "monument") return "Monumento";
        if (historic == "memorial") return "Memoriale";
        if (historic == "ruins") return "Rovine";
        if (historic == "archaeological_site") return "Sito Archeologico";
        if (historic is "fort" or "city_gate" or "tower") return "Fortificazione";
        if (historic is "manor" or "palace") return "Palazzo Storico";
        if (historic == "church") return "Chiesa Storica";

        if (amenity == "place_of_worship")
        {
            return religion switch
            {
                "christian" => "Chiesa",
                "muslim" => "Moschea",
                "jewish" => "Sinagoga",
                "buddhist" => "Tempio Buddhista",
                "hindu" => "Tempio Induista",
                _ => "Luogo di Culto"
            };
        }

        if (leisure == "park") return "Parco";
        if (leisure == "garden") return "Giardino";
        if (leisure == "nature_reserve") return "Riserva Naturale";

        if (amenity == "theatre") return "Teatro";
        if (amenity == "cinema") return "Cinema";
        if (amenity == "arts_centre") return "Centro d'Arte";
        if (amenity == "library") return "Biblioteca";

        return "Punto di Interesse";
    }

    /// <summary>
    /// Build a Wikipedia thumbnail URL from a "wikipedia" OSM tag like "it:Duomo di Modena".
    /// </summary>
    private static string? GetWikipediaThumbnailUrl(string? wikiTag)
    {
        if (string.IsNullOrEmpty(wikiTag)) return null;

        // Format: "lang:Article_Title" or just "Article_Title"
        var parts = wikiTag.Split(':', 2);
        var lang = parts.Length == 2 ? parts[0] : "en";
        var title = parts.Length == 2 ? parts[1] : parts[0];
        var encoded = Uri.EscapeDataString(title.Replace(' ', '_'));

        // Wikipedia's Special:FilePath/PageImages API
        return $"https://{lang}.wikipedia.org/wiki/Special:FilePath/{encoded}";
    }

    /// <summary>
    /// Placeholder for Wikidata image lookup. Returns null for now;
    /// actual implementation would query the Wikidata API for P18 (image property).
    /// </summary>
    private static string? GetWikidataThumbnailUrl(string? wikidataId)
    {
        // Wikidata image lookup requires an API call, skipped for now
        return null;
    }

    private static string? GetTag(JsonElement tags, string key)
    {
        if (tags.ValueKind == JsonValueKind.Undefined) return null;
        return tags.TryGetProperty(key, out var value) ? value.GetString() : null;
    }
}

public record OverpassPoi(
    string Id,
    string Name,
    double Latitude,
    double Longitude,
    string Type,
    string? Description,
    string? Category,
    string? ImageUrl,
    string? WikipediaTag = null,
    string? WikidataTag = null
);
