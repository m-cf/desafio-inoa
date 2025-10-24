using System.Text.Json;
using MonitorB3.App.Interfaces;
using MonitorB3.App.Models;

namespace MonitorB3.App.Services
{
    public class CotacaoService : ICotacaoService
    {
        private readonly HttpClient _client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        public async Task<decimal> ObterCotacaoAsync(string ativo, string apiKey)
        {
            string apiUrl = $"https://brapi.dev/api/quote/{ativo}?token={apiKey}";

            try
            {
                HttpResponseMessage response = await _client.GetAsync(apiUrl);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<CotacaoResponse>(responseBody);

                if (data?.results != null && data.results.Length > 0)
                {
                    return data.results[0].regularMarketPrice;
                }

                Console.WriteLine("Aviso: Resposta da API não contém o preço.");
                return -1;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Erro HTTP (API): Verifique se a chave está correta. Detalhes: {ex.Message}");
                return -1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro Inesperado na Cotação: {ex.Message}");
                return -1;
            }
        }
    }
}
