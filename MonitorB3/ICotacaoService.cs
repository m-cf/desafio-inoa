namespace MonitorB3.App.Interfaces
{
    // define a responsabilidade de obter a cotação (pode ser mockada em testes)
    public interface ICotacaoService
    {
        Task<decimal> ObterCotacaoAsync(string ativo, string apiKey);
    }
}
