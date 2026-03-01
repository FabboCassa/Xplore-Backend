using System.Text.Json;

namespace Xplore.Infrastructure.Map;

/// <summary>
/// Static helper that classifies Overpass POI elements into broad types
/// and human-readable category labels based on their OSM tags.
/// </summary>
public static class PoiClassifier
{
    // ── Broad type (matches mobile PinType enum) ─────────────────

    /// <summary>
    /// Classify POI into a broad type based on OSM tags.
    /// </summary>
    public static string ClassifyType(JsonElement tags)
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

    // ── Human-readable category label ────────────────────────────

    /// <summary>
    /// Classify into a human-readable Italian category label.
    /// </summary>
    public static string ClassifyCategory(JsonElement tags)
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

    // ── Image URL helpers ────────────────────────────────────────

    /// <summary>
    /// Build a Wikipedia thumbnail URL from an OSM "wikipedia" tag like "it:Duomo di Modena".
    /// </summary>
    public static string? GetWikipediaThumbnailUrl(string? wikiTag)
    {
        if (string.IsNullOrEmpty(wikiTag)) return null;

        var parts = wikiTag.Split(':', 2);
        var lang = parts.Length == 2 ? parts[0] : "en";
        var title = parts.Length == 2 ? parts[1] : parts[0];
        var encoded = Uri.EscapeDataString(title.Replace(' ', '_'));

        return $"https://{lang}.wikipedia.org/wiki/Special:FilePath/{encoded}";
    }

    /// <summary>
    /// Placeholder for Wikidata image lookup. Returns null for now.
    /// </summary>
    public static string? GetWikidataThumbnailUrl(string? wikidataId) => null;

    // ── Tag access helper ────────────────────────────────────────

    public static string? GetTag(JsonElement tags, string key)
    {
        if (tags.ValueKind == JsonValueKind.Undefined) return null;
        return tags.TryGetProperty(key, out var value) ? value.GetString() : null;
    }
}
