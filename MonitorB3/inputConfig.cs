namespace MonitorB3.App.Models
{
    // estrutura para os 4 parâmetros de linha de comando (Entrada)
    public record InputConfig(string Ativo, decimal PrecoVenda, decimal PrecoCompra, string ToEmail)
    {
        // validação básica para garantir que todos os valores são válidos
        public bool IsValid() =>
            !string.IsNullOrWhiteSpace(Ativo) &&
            PrecoVenda > 0 &&
            PrecoCompra > 0 &&
            !string.IsNullOrWhiteSpace(ToEmail);
    }
}
