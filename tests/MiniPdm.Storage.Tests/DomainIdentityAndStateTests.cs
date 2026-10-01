using MiniPdm.Domain.Objects;
using MiniPdm.Domain.Versions;
using Xunit;

namespace MiniPdm.Storage.Tests;

public sealed class DomainIdentityAndStateTests
{
    [Theory]
    [InlineData("АБВГ.301245.001", true)]
    [InlineData("ABВГ.301245.001", false)]
    [InlineData("АБВГ.301245.001\n", false)]
    [InlineData("АБВГ.30124.001", false)]
    public void Validates_cyrillic_designation(string value, bool expected) => Assert.Equal(expected, ObjectIdentity.IsValidDesignation(value));

    [Fact]
    public void Standard_name_normalization_trims_collapses_and_ignores_case()
    {
        Assert.Equal(ObjectIdentity.NormalizeStandardName("  Болт   М8  "), ObjectIdentity.NormalizeStandardName("болт м8"));
        Assert.NotEqual(ObjectIdentity.NormalizeStandardName("Ø8×20"), ObjectIdentity.NormalizeStandardName("Ø8x20"));
    }

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
