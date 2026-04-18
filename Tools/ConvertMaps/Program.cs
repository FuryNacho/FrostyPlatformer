// ConvertMaps — engångsverktyg för att konvertera gamla kartfiler till Tiled JSON-format
//
// Vad det gör:
//   Läser gammal kartformat (TileIndex + AttributeIndex, flat JSON) från sourcePath,
//   konverterar till Tiled JSON (layers-struktur, GID-offset +1 på TileIndex),
//   skriver resultatet till outputPath.
//   Skapar också .tsx tileset-filer som Tiled Map Editor behöver för att visa sprites.
//
// Kör: dotnet run
// Kör med egna sökvägar: dotnet run -- --source <sökväg> --output <sökväg>

#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

// ── Sökvägar ──────────────────────────────────────────────────────────────────

// Hitta projektrooten relativt till det körbara programmet (fungerar även från bin/)
string toolDir    = AppContext.BaseDirectory;
string repoRoot   = FindRepoRoot(toolDir) ?? Path.Combine(toolDir, "..", "..", "..", "..", "..");
string sourcePath = Path.GetFullPath(Path.Combine(repoRoot, "FrostyPlatformer", "Resources", "Assets", "MapData"));
string outputPath = Path.GetFullPath(Path.Combine(sourcePath, "Tiled"));

// Tillåt åsidosättning via argument: --source <sökväg> --output <sökväg>
for (int i = 0; i < args.Length - 1; i++)
{
    if (args[i] == "--source") sourcePath = Path.GetFullPath(args[i + 1]);
    if (args[i] == "--output") outputPath = Path.GetFullPath(args[i + 1]);
}

Console.WriteLine("ConvertMaps — Konverterar till Tiled JSON-format");
Console.WriteLine($"  Källa:  {sourcePath}");
Console.WriteLine($"  Utdata: {outputPath}");
Console.WriteLine();

if (!Directory.Exists(sourcePath))
{
    Console.Error.WriteLine($"FEL: Källmappen hittades inte: {sourcePath}");
    return 1;
}

Directory.CreateDirectory(outputPath);

// ── Tileset-mappning (kart-ID → tilesheet-namn) ────────────────────────────

var tilesetMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    ["worldmap"]  = "tilesheetwm",
    ["mapone"]    = "tilesheetspring",
    ["maptwo"]    = "tilesheetspring",
    ["mapthree"]  = "tilesheetsummer",
    ["mapfour"]   = "tilesheetsummer",
    ["mapfive"]   = "tilesheetfall",
    ["mapsix"]    = "tilesheetfall",
    ["mapseven"]  = "tilesheetwinter",
    ["mapeight"]  = "tilesheetwinter",
    ["mapnine"]   = "tilesheetwinter",
};

// ── Konvertera varje karta ─────────────────────────────────────────────────

int converted = 0;
int failed    = 0;

var jsonReadOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

foreach (var kvp in tilesetMap)
{
    string mapId       = kvp.Key;
    string tilesheet   = kvp.Value;
    string sourceFile  = Path.Combine(sourcePath, mapId + ".json");
    string outputFile  = Path.Combine(outputPath, mapId + ".json");

    if (!File.Exists(sourceFile))
    {
        Console.WriteLine($"  HOPPAR ÖVER  {mapId,-12} — filen saknas: {sourceFile}");
        failed++;
        continue;
    }

    try
    {
        string    oldJson = File.ReadAllText(sourceFile);
        OldMapDto old     = JsonSerializer.Deserialize<OldMapDto>(oldJson, jsonReadOptions)
                            ?? throw new InvalidOperationException("Deserialisering returnerade null.");

        string tiledJson = BuildTiledJson(old, tilesheet);
        File.WriteAllText(outputFile, tiledJson, Encoding.UTF8);

        int tileCount      = old.TileIndex.Length;
        int solidCount     = old.AttributeIndex.Count(v => v == 1);
        Console.WriteLine($"  OK           {mapId,-12} {old.Width}×{old.Height}  tiles={tileCount}  solida={solidCount}  tilesheet={tilesheet}");
        converted++;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  FEL          {mapId,-12} — {ex.Message}");
        failed++;
    }
}

// ── Generera .tsx tileset-filer ────────────────────────────────────────────

Console.WriteLine();
Console.WriteLine("Genererar .tsx tileset-filer...");

var uniqueTilesets = tilesetMap.Values.Distinct(StringComparer.OrdinalIgnoreCase);
foreach (string tilesheet in uniqueTilesets)
{
    string tsxFile = Path.Combine(outputPath, tilesheet + ".tsx");
    // Relativ sökväg från outputPath (MapData/Tiled/) till Sprites/
    string spriteRelPath = $"../../Sprites/{tilesheet}.png";
    string tsx = BuildTsx(tilesheet, spriteRelPath);
    File.WriteAllText(tsxFile, tsx, Encoding.UTF8);
    Console.WriteLine($"  {tilesheet}.tsx → {spriteRelPath}");
}

// ── Sammanfattning ─────────────────────────────────────────────────────────

Console.WriteLine();
Console.WriteLine($"Klart: {converted} konverterade, {failed} misslyckades.");
Console.WriteLine($"Utdatafiler: {outputPath}");

if (failed > 0)
    Console.WriteLine("OBS: Kontrollera att alla kartfiler finns i källmappen.");

return failed == 0 ? 0 : 1;

// ── Hjälpmetoder ───────────────────────────────────────────────────────────

static string? FindRepoRoot(string startDir)
{
    // Gå uppåt i mappträdet tills vi hittar en .sln-fil (indikerar repo-roten)
    var dir = new DirectoryInfo(startDir);
    while (dir != null)
    {
        if (dir.GetFiles("*.sln").Length > 0)
            return dir.FullName;
        dir = dir.Parent;
    }
    return null;
}

static string BuildTiledJson(OldMapDto old, string tilesheet)
{
    // TileIndex: lägg till GID-offset +1 på varje värde.
    // I Tiled betyder GID 0 "ingen tile" — vi använder GID 1 för sprite-index 0 (lufttile).
    int[] tilesData     = old.TileIndex.Select(idx => idx + 1).ToArray();

    // AttributeIndex: inga GID:n, 0 och 1 är boolean-flaggor.
    // Behålls oförändrade. Spelet läser dessa direkt utan offset.
    int[] collisionData = old.AttributeIndex;

    // Bygg JSON manuellt för exakt kontroll över formatering och fältordning.
    // System.Text.Json.JsonSerializer skulle skriva allt på en rad med standardinställningar —
    // det gör de konverterade filerna omöjliga att läsa manuellt.
    var sb = new StringBuilder();
    sb.AppendLine("{");
    sb.AppendLine($"  \"width\": {old.Width},");
    sb.AppendLine($"  \"height\": {old.Height},");
    sb.AppendLine("  \"tilewidth\": 16,");
    sb.AppendLine("  \"tileheight\": 16,");
    sb.AppendLine("  \"infinite\": false,");
    sb.AppendLine("  \"tilesets\": [");
    sb.AppendLine("    {");
    sb.AppendLine("      \"firstgid\": 1,");
    sb.AppendLine($"      \"source\": \"{tilesheet}.tsx\"");
    sb.AppendLine("    }");
    sb.AppendLine("  ],");
    sb.AppendLine("  \"layers\": [");
    sb.AppendLine("    {");
    sb.AppendLine("      \"name\": \"Tiles\",");
    sb.AppendLine("      \"type\": \"tilelayer\",");
    sb.AppendLine($"      \"width\": {old.Width},");
    sb.AppendLine($"      \"height\": {old.Height},");
    sb.AppendLine("      \"x\": 0,");
    sb.AppendLine("      \"y\": 0,");
    sb.AppendLine("      \"opacity\": 1,");
    sb.AppendLine("      \"visible\": true,");
    sb.Append("      \"data\": [");
    AppendIntArray(sb, tilesData, old.Width);
    sb.AppendLine("      ]");
    sb.AppendLine("    },");
    sb.AppendLine("    {");
    sb.AppendLine("      \"name\": \"Collision\",");
    sb.AppendLine("      \"type\": \"tilelayer\",");
    sb.AppendLine($"      \"width\": {old.Width},");
    sb.AppendLine($"      \"height\": {old.Height},");
    sb.AppendLine("      \"x\": 0,");
    sb.AppendLine("      \"y\": 0,");
    sb.AppendLine("      \"opacity\": 1,");
    sb.AppendLine("      \"visible\": false,");
    sb.Append("      \"data\": [");
    AppendIntArray(sb, collisionData, old.Width);
    sb.AppendLine("      ]");
    sb.AppendLine("    }");
    sb.AppendLine("  ]");
    sb.Append("}");
    return sb.ToString();
}

static void AppendIntArray(StringBuilder sb, int[] data, int rowWidth)
{
    // Skriv arrayen med en rad per tile-rad för läsbarhet
    sb.AppendLine();
    for (int i = 0; i < data.Length; i++)
    {
        bool firstInRow = (i % rowWidth) == 0;
        bool lastInRow  = (i % rowWidth) == rowWidth - 1;
        bool last       = i == data.Length - 1;

        if (firstInRow) sb.Append("        ");
        sb.Append(data[i]);
        if (!last)      sb.Append(",");
        if (lastInRow)  sb.AppendLine();
    }
}

static string BuildTsx(string tilesheet, string spriteRelPath)
{
    // TSX-format: XML som Tiled Map Editor läser för att visa rätt sprites i palette-panelen.
    // tilecount=25 (5×5) är minimiantal; spelet bryr sig inte om TSX-filen.
    // columns=5 matchar GameConstants.TileSheetColumns.
    return $"""
<?xml version="1.0" encoding="UTF-8"?>
<tileset version="1.10" name="{tilesheet}" tilewidth="16" tileheight="16" tilecount="25" columns="5">
  <image source="{spriteRelPath}" width="80" height="80"/>
</tileset>
""";
}

// ── DTOs ──────────────────────────────────────────────────────────────────────

/// <summary>Speglar det gamla kartformatet för deserialisering.</summary>
class OldMapDto
{
    [JsonPropertyName("TileIndex")]
    public int[] TileIndex { get; set; } = Array.Empty<int>();

    [JsonPropertyName("AttributeIndex")]
    public int[] AttributeIndex { get; set; } = Array.Empty<int>();

    [JsonPropertyName("Width")]
    public int Width { get; set; }

    [JsonPropertyName("Height")]
    public int Height { get; set; }
}
