namespace Onboarding.Services.Validators;

public static class CpfValidator
{
    public static bool IsValid(string? cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf)) return false;

        var digits = NormalizeCpf(cpf);
        if (digits.Length != 11) return false;
        if (digits.Distinct().Count() == 1) return false;

        return true;
    }
    public static string NormalizeCpf(string cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
            return string.Empty;

        return new string(cpf.Where(char.IsDigit).ToArray());
    }
    public static string Mask(string cpf)
    {
        var normalizedCpf = NormalizeCpf(cpf);

        if (normalizedCpf.Length != 11)
            return cpf;

        return $"***.***.{normalizedCpf.Substring(6, 3)}-{normalizedCpf.Substring(9, 2)}";
    }

}
