using System.Text.Json;
using MiniPdm.Domain.Objects;
using MiniPdm.Modules.Import.Abstractions.Cad;
using MiniPdm.Modules.Import.DtoModels.Cad;

namespace MiniPdm.Modules.Import.Infrastructure.Cad;

public sealed class FileJsonCadDocumentReader(string directory) : ICadDocumentReader
{
    private static readonly JsonDocumentOptions JsonOptions = new() { CommentHandling = JsonCommentHandling.Disallow };

    public async Task<CadReadResult> ReadAsync(CadDocumentRef document, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsFileName(document.FileName))
            return Failure("Document reference must contain a file name only.");

        var path = Path.Combine(directory, document.FileName);
        byte[] bytes;
        try
        {
            bytes = await File.ReadAllBytesAsync(path, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (FileNotFoundException)
        {
            return Failure($"CAD file '{document.FileName}' was not found.");
        }
        catch (DirectoryNotFoundException)
        {
            return Failure($"CAD file '{document.FileName}' was not found.");
        }
        catch (IOException ex)
        {
            return Failure($"Could not read CAD file '{document.FileName}': {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Failure($"Could not read CAD file '{document.FileName}': {ex.Message}");
        }

        try
        {
            var jsonBytes = bytes.AsMemory(bytes.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }) ? 3 : 0);
            using var json = JsonDocument.Parse(jsonBytes, JsonOptions);
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return Failure("CAD document must be a JSON object.");

            if (!root.TryGetProperty("formatVersion", out var formatVersion) ||
                formatVersion.ValueKind != JsonValueKind.Number || !formatVersion.TryGetInt32(out var version) || version != 1)
                return Failure("Unsupported or missing formatVersion; expected 1.");

            if (!root.TryGetProperty("type", out var typeElement) || typeElement.ValueKind != JsonValueKind.String)
                return Failure("Missing or invalid type; expected Assembly, Part, or StandardPart.");
            var typeText = typeElement.GetString();
            var type = typeText switch
            {
                "Assembly" => PdmObjectType.Assembly,
                "Part" => PdmObjectType.Part,
                "StandardPart" => PdmObjectType.StandardPart,
                _ => (PdmObjectType?)null
            };
            if (type is null)
                return Failure("Invalid type; expected Assembly, Part, or StandardPart.");

            if (!root.TryGetProperty("name", out var nameElement) || nameElement.ValueKind != JsonValueKind.String)
                return Failure("Missing or invalid name.");

            string? designation = ReadOptionalString(root, "designation", out var designationError);
            if (designationError is not null)
                return Failure(designationError);

            var name = nameElement.GetString()!;
            CadDocument Partial(string? partialMaterial = null, decimal? partialMass = null,
                IReadOnlyList<CadComponent>? partialComponents = null) =>
                new(document.FileName, type.Value, designation, name, partialMaterial, partialMass,
                    partialComponents ?? Array.Empty<CadComponent>());

            var extension = Path.GetExtension(document.FileName);
            if ((type == PdmObjectType.Assembly && !extension.Equals(".a3d", StringComparison.OrdinalIgnoreCase)) ||
                (type != PdmObjectType.Assembly && !extension.Equals(".m3d", StringComparison.OrdinalIgnoreCase)))
                return Failure($"CAD file extension '{extension}' does not match type '{typeText}'.", Partial());

            string? material = null;
            decimal? mass = null;
            if (root.TryGetProperty("properties", out var properties) && properties.ValueKind != JsonValueKind.Null)
            {
                if (properties.ValueKind != JsonValueKind.Object)
                    return Failure("properties must be an object or null.", Partial());
                material = ReadOptionalString(properties, "material", out var materialError);
                if (materialError is not null)
                    return Failure(materialError, Partial());
                if (properties.TryGetProperty("mass", out var massElement) && massElement.ValueKind != JsonValueKind.Null)
                {
                    if (massElement.ValueKind != JsonValueKind.Number || !massElement.TryGetDecimal(out var parsedMass))
                        return Failure("properties.mass must be a decimal number or null.", Partial(material));
                    mass = parsedMass;
                }
            }

            if (!root.TryGetProperty("components", out var componentsElement) || componentsElement.ValueKind != JsonValueKind.Array)
                return Failure("components must be an array.", Partial(material, mass));

            var components = new List<CadComponent>();
            foreach (var component in componentsElement.EnumerateArray())
            {
                if (component.ValueKind != JsonValueKind.Object ||
                    !component.TryGetProperty("file", out var fileElement) || fileElement.ValueKind != JsonValueKind.String ||
                    !IsFileName(fileElement.GetString()) ||
                    !component.TryGetProperty("count", out var countElement) || countElement.ValueKind != JsonValueKind.Number ||
                    !countElement.TryGetInt32(out var count))
                    return Failure("Each component must have a file name and an integer count.", Partial(material, mass, components));
                components.Add(new CadComponent(fileElement.GetString()!, count));
            }

            return new CadReadResult(Partial(material, mass, components), null);
        }
        catch (JsonException ex)
        {
            return Failure($"Invalid JSON in CAD file '{document.FileName}': {ex.Message}");
        }
    }

    private static string? ReadOptionalString(JsonElement parent, string property, out string? error)
    {
        error = null;
        if (!parent.TryGetProperty(property, out var element) || element.ValueKind == JsonValueKind.Null)
            return null;
        if (element.ValueKind != JsonValueKind.String)
        {
            error = $"{property} must be a string or null.";
            return null;
        }
        return element.GetString();
    }

    private static bool IsFileName(string? value) => !string.IsNullOrWhiteSpace(value) &&
        string.Equals(value, Path.GetFileName(value), StringComparison.Ordinal) &&
        !value.Contains('/') && !value.Contains('\\') && value is not "." and not "..";

    private static CadReadResult Failure(string error, CadDocument? partialDocument = null) => new(partialDocument, error);
}
