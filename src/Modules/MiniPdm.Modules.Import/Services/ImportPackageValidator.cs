using MiniPdm.Contracts.Modules.Import.DtoModels;
using MiniPdm.Domain.Objects;
using MiniPdm.Domain.Composition;
using MiniPdm.Domain.Versions;
using MiniPdm.Modules.Import.Abstractions.Cad;
using MiniPdm.Modules.Import.DtoModels.Cad;
using MiniPdm.Storage.Abstractions.Import;

namespace MiniPdm.Modules.Import.Services;

internal sealed class ImportPackageValidator(IReadOnlyList<ImportPackageValidator.FileEntry> input)
{
    internal sealed class FileEntry(string fileName, CadDocument? document, string? readError)
    {
        public string FileName { get; } = fileName;
        public CadDocument? Document { get; } = document;
        public string? Reason { get; set; } = readError;
        public List<string> Warnings { get; } = [];
        public Dictionary<string, int> ComponentCounts { get; } = new(StringComparer.Ordinal);
        public ImportFileAction? Action { get; set; }
        public bool Accepted => Reason is null;
        public string IdentityKey => Document is null ? $"file:{FileName}" : GetIdentityKey(Document);
        public ImportFileResultDto ToDto() => new(FileName,
            Accepted ? ImportFileStatus.Accepted : ImportFileStatus.Rejected,
            Reason, Action, Warnings.ToArray());
    }

    private readonly List<FileEntry> _files = input.ToList();
    private readonly Dictionary<string, FileEntry> _byFile = new(StringComparer.Ordinal);

    public IReadOnlyList<FileEntry> Files => _files;
    public IReadOnlyDictionary<string, FileEntry> ByFile => _byFile;

    public void Validate()
    {
        foreach (var grouping in _files.GroupBy(x => x.FileName, StringComparer.Ordinal).Where(x => x.Count() > 1))
            foreach (var file in grouping) Reject(file, "The package contains duplicate file names.");
        foreach (var file in _files)
        {
            if (file.Document is null)
            {
                if (file.Reason is null) Reject(file, "The CAD reader returned no document.");
                continue;
            }
            ValidateAttributes(file);
            ValidateComponents(file);
        }

        foreach (var group in _files.Where(x => x.Document is not null && HasIdentity(x.Document))
                     .GroupBy(x => x.IdentityKey, StringComparer.Ordinal).Where(x => x.Count() > 1))
            foreach (var file in group) Reject(file, "More than one package document has this PDM identity.");

        _byFile.Clear();
        foreach (var file in _files)
            if (!_byFile.ContainsKey(file.FileName)) _byFile.Add(file.FileName, file);

        RejectBadReferences();
        RejectPackageCycles();
    }

    public ImportLookup CreateLookup()
    {
        var docs = _files.Where(x => x.Document is not null).Select(x => x.Document!).ToArray();
        return new ImportLookup(
            docs.Where(x => x.Designation is not null).Select(x => x.Designation!).Distinct(StringComparer.Ordinal).ToArray(),
            docs.Where(x => x.Type == PdmObjectType.StandardPart && !string.IsNullOrWhiteSpace(x.Name))
                .Select(x => ObjectIdentity.NormalizeStandardName(x.Name)).Distinct(StringComparer.Ordinal).ToArray());
    }

    public ImportReportDto ToReport(Guid importId) => new(importId, _files.Select(x => x.ToDto()).ToArray());

    private void ValidateAttributes(FileEntry file)
    {
        var d = file.Document!;
        var validation = VersionAttributeRules.Validate(d.Type, d.Name, d.Material, d.Mass);
        var typeErrors = validation.Errors.Where(x => x is not "Name is required." and not "Name exceeds 512 characters."
            and not "Material exceeds 256 characters." and not "Normalized standard part name exceeds 512 characters."
            and not "Mass cannot be negative." and not "Mass must fit decimal(18,6).").ToArray();
        foreach (var error in validation.Errors.Except(typeErrors)) Reject(file, error);
        if (d.Type is PdmObjectType.Assembly or PdmObjectType.Part)
        {
            if (d.Designation is null || !ObjectIdentity.IsValidDesignation(d.Designation))
                Reject(file, "A valid Cyrillic designation is required.");
        }
        else if (d.Type == PdmObjectType.StandardPart && d.Designation is not null)
            Reject(file, "Standard parts must not have a designation.");
        foreach (var error in typeErrors) Reject(file, error);
        file.Warnings.AddRange(validation.Warnings);
    }

    private static void ValidateComponents(FileEntry file)
    {
        var d = file.Document!;
        if (d.Type != PdmObjectType.Assembly && d.Components.Count != 0)
            Reject(file, "Only assemblies may contain components.");
        var validRows = new List<(string Child, int Quantity)>();
        foreach (var component in d.Components)
        {
            if (string.IsNullOrWhiteSpace(component.File))
            {
                Reject(file, "A component file name is empty.");
                continue;
            }
            if (component.Count <= 0)
            {
                Reject(file, $"Component '{component.File}' must have a positive count.");
                continue;
            }
            validRows.Add((component.File, component.Count));
        }
        var normalized = CompositionRules.Normalize(validRows);
        foreach (var error in normalized.Errors) Reject(file, error);
        foreach (var pair in normalized.Items) file.ComponentCounts[pair.Key] = pair.Value;
        if (normalized.Warnings.Count != 0 && file.Reason is null)
            file.Warnings.Add("Repeated component rows were combined by file name.");
    }

    private void RejectBadReferences()
    {
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var file in _files.Where(x => x.Accepted && x.Document?.Type == PdmObjectType.Assembly))
            {
                foreach (var componentName in file.ComponentCounts.Keys)
                {
                    if (!_byFile.TryGetValue(componentName, out var child))
                    {
                        Reject(file, $"Component file '{componentName}' is missing from this package.");
                        changed = true;
                        break;
                    }
                    if (!child.Accepted)
                    {
                        Reject(file, $"Component file '{componentName}' was rejected.");
                        changed = true;
                        break;
                    }
                }
            }
        }
    }

    private void RejectPackageCycles()
    {
        var cyclic = new HashSet<FileEntry>();
        var assemblies = _files.Where(x => x.Accepted && x.Document?.Type == PdmObjectType.Assembly).ToArray();
        foreach (var start in assemblies)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var pending = new Stack<FileEntry>();
            foreach (var childName in start.ComponentCounts.Keys)
                if (_byFile.TryGetValue(childName, out var child) && child.Accepted && child.Document?.Type == PdmObjectType.Assembly)
                    pending.Push(child);
            while (pending.TryPop(out var current))
            {
                if (ReferenceEquals(current, start)) { cyclic.Add(start); break; }
                if (!seen.Add(current.FileName)) continue;
                foreach (var childName in current.ComponentCounts.Keys)
                    if (_byFile.TryGetValue(childName, out var child) && child.Accepted && child.Document?.Type == PdmObjectType.Assembly)
                        pending.Push(child);
            }
        }
        foreach (var file in cyclic) Reject(file, "The package component graph contains a cycle.");
        RejectBadReferences();
    }

    internal static string GetIdentityKey(CadDocument d) => d.Type == PdmObjectType.StandardPart
        ? "N:" + ObjectIdentity.NormalizeStandardName(d.Name)
        : "D:" + (d.Designation ?? "");
    private static bool HasIdentity(CadDocument d) => d.Type == PdmObjectType.StandardPart
        ? !string.IsNullOrWhiteSpace(d.Name)
        : !string.IsNullOrWhiteSpace(d.Designation);
    private static void Reject(FileEntry file, string reason)
    {
        file.Reason ??= reason;
        file.Action = null;
    }
}
