namespace Onboarding.Services.Validators;

public static class CpfValidator
{
    public static bool IsValid(string? cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf)) return false;

        var digits = new string(cpf.Where(char.IsDigit).ToArray());
        if (digits.Length != 11) return false;
        if (digits.Distinct().Count() == 1) return false;

        var numbers = digits.Select(c => c - '0').ToArray();

        var sum = 0;
        for (var i = 0; i < 9; i++) sum += numbers[i] * (10 - i);
        var first = (sum * 10 % 11) % 10;
        if (numbers[9] != first) return false;

        sum = 0;
        for (var i = 0; i < 10; i++) sum += numbers[i] * (11 - i);
        var second = (sum * 10 % 11) % 10;
        return numbers[10] == second;
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
