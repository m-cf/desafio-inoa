namespace MonitorB3.App.Models
{
    // Estrutura para desserializar a resposta da API (brapi)
    public record CotacaoResult(decimal regularMarketPrice, string symbol);
    public record CotacaoResponse(CotacaoResult[] results);
}
