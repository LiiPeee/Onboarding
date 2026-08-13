using FluentAssertions;
using Onboarding.Services.Validators;
using Xunit;

namespace Onboarding.Tests.Application;

public class CpfValidatorTests
{
    [Theory]
    [InlineData("52998224725")]      // válido sem máscara
    [InlineData("529.982.247-25")]   // válido com máscara
    [InlineData("198.166.187-14")]   // 11 dígitos, aceito mesmo sem dígitos verificadores
    [InlineData("18866677733")]      // 11 dígitos, aceito mesmo sem dígitos verificadores
    public void IsValid_ValidCpf_ReturnsTrue(string cpf)
        => CpfValidator.IsValid(cpf).Should().BeTrue();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("111.111.111-11")]   // todos iguais
    [InlineData("5299822472")]       // curto demais
    public void IsValid_InvalidCpf_ReturnsFalse(string? cpf)
        => CpfValidator.IsValid(cpf).Should().BeFalse();
}
