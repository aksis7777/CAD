using MiniPdm.Domain.Objects;

namespace MiniPdm.Domain.Versions;

/// <summary>
///     Результаты проверки атрибутов предлагаемой версии объекта.
/// </summary>
public sealed record VersionAttributeValidation(IReadOnlyList<string> Errors, IReadOnlyList<string> Warnings)
{
    /// <summary>
    ///     Значения, нарушающие ограничения типа объекта или хранилища.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = Errors;

    /// <summary>
    ///     Допустимые значения, на которые следует обратить внимание оператора.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = Warnings;

    /// <summary>
    ///     Показывает, завершилась ли проверка без ошибок.
    /// </summary>
    public bool IsValid => Errors.Count == 0;
}

/// <summary>
///     Проверяет атрибуты версии с учётом типа объекта PDM.
/// </summary>
public static class VersionAttributeRules
{
    /// <summary>
    ///     Проверяет наименование, материал и массу по правилам типа объекта и ограничениям базы данных.
    /// </summary>
    /// <param name="type">
    ///     Тип проверяемого объекта.
    /// </param>
    /// <param name="name">
    ///     Наименование версии или стандартного изделия.
    /// </param>
    /// <param name="material">
    ///     Материал детали, если применимо.
    /// </param>
    /// <param name="mass">
    ///     Масса одного изделия в килограммах, если применимо.
    /// </param>
    /// <returns>
    ///     Ошибки проверки и предупреждения, не препятствующие сохранению.
    /// </returns>
    public static VersionAttributeValidation Validate(PdmObjectType type, string? name, string? material, decimal? mass)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        if (string.IsNullOrWhiteSpace(name))
            errors.Add("Name is required.");
        if (name?.Length > 512)
            errors.Add("Name exceeds 512 characters.");
        if (material?.Length > 256)
            errors.Add("Material exceeds 256 characters.");
        if (type == PdmObjectType.StandardPart && name is not null && ObjectIdentity.NormalizeStandardName(name).Length > 512)
            errors.Add("Normalized standard part name exceeds 512 characters.");
        if (mass is < 0)
            errors.Add("Mass cannot be negative.");
        if (mass is { } value && (value > 999999999999.999999m || decimal.Round(value, 6) != value))
            errors.Add("Mass must fit decimal(18,6).");

        switch (type)
        {
            case PdmObjectType.Assembly:
                if (material is not null || mass is not null)
                    errors.Add("Assembly material and mass must be absent.");
                break;
            case PdmObjectType.Part:
                if (string.IsNullOrWhiteSpace(material))
                    errors.Add("Part material is required.");
                if (mass is null)
                    warnings.Add("Part mass is missing.");
                else if (mass == 0)
                    warnings.Add("Part mass is zero.");
                break;
            case PdmObjectType.StandardPart:
                if (material is not null)
                    errors.Add("Standard parts must not have material.");
                if (mass is null)
                    errors.Add("Standard part mass is required.");
                else if (mass == 0)
                    warnings.Add("Standard part mass is zero.");
                break;
            default:
                errors.Add("Unknown PDM object type.");
                break;
        }
        return new VersionAttributeValidation(errors, warnings);
    }
}
