using MiniPdm.Domain.Objects;
using MiniPdm.Domain.Versions;
using Xunit;

namespace MiniPdm.Storage.Tests;

/// <summary>
/// Проверяет правила идентичности объекта и допустимые переходы состояния версии.
/// </summary>
public sealed class DomainIdentityAndStateTests
{
    /// <summary>
    /// Проверяет ожидаемое поведение сценария «Validates_cyrillic_designation».
    /// </summary>
    /// <param name="value">Обозначение, для которого проверяется допустимость кириллических символов.</param>
    /// <param name="expected">Ожидаемый результат проверки обозначения.</param>
    [Theory]
    [InlineData("АБВГ.301245.001", true)]
    [InlineData("ABВГ.301245.001", false)]
    [InlineData("АБВГ.301245.001\n", false)]
    [InlineData("АБВГ.30124.001", false)]
    public void Validates_cyrillic_designation(string value, bool expected) => Assert.Equal(expected, ObjectIdentity.IsValidDesignation(value));

    /// <summary>
    /// Проверяет ожидаемое поведение сценария «Standard_name_normalization_trims_collapses_and_ignores_case».
    /// </summary>
    [Fact]
    public void Standard_name_normalization_trims_collapses_and_ignores_case()
    {
        Assert.Equal(ObjectIdentity.NormalizeStandardName("  Болт   М8  "), ObjectIdentity.NormalizeStandardName("болт м8"));
        Assert.NotEqual(ObjectIdentity.NormalizeStandardName("Ø8×20"), ObjectIdentity.NormalizeStandardName("Ø8x20"));
    }

    /// <summary>
    /// Проверяет ожидаемое поведение сценария «Enforces_the_allowed_version_state_transitions».
    /// </summary>
    /// <param name="from">Значение, используемое в проверяемом сценарии.</param>
    /// <param name="to">Значение, используемое в проверяемом сценарии.</param>
    /// <param name="expected">Значение, используемое в проверяемом сценарии.</param>
    [Theory]
    [InlineData(VersionState.InWork, VersionState.Approved, true)]
    [InlineData(VersionState.InWork, VersionState.Cancelled, true)]
    [InlineData(VersionState.Approved, VersionState.Cancelled, true)]
    [InlineData(VersionState.Approved, VersionState.InWork, false)]
    [InlineData(VersionState.Cancelled, VersionState.InWork, false)]
    [InlineData(VersionState.Cancelled, VersionState.Approved, false)]
    public void Enforces_the_allowed_version_state_transitions(VersionState from, VersionState to, bool expected) =>
        Assert.Equal(expected, ObjectVersion.CanTransition(from, to));
}
