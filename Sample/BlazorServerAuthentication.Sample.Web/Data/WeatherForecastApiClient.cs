using BlazorServerAuthentication;

namespace BlazerServerAuthentication.Sample.Web.Data
{
    public class WeatherForecastApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpClientAuthenticator _httpClientAuthenticator;

        public WeatherForecastApiClient(HttpClient httpClient, IHttpClientAuthenticator httpClientAuthenticator)
        {
            _httpClient = httpClient;
            _httpClientAuthenticator = httpClientAuthenticator;
        }

        public async Task<WeatherForecast[]> GetForecastAsync()
        {
            await _httpClientAuthenticator.PrepareHttpClientAsync(_httpClient);

            var response = await _httpClient.GetAsync("/weatherforecast");

            if (response.IsSuccessStatusCode)
            {
                var parsedContent = await response.Content.ReadFromJsonAsync<WeatherForecast[]>();

                return parsedContent ?? new WeatherForecast[0];
            }
            else
            {
                return new WeatherForecast[0];
            }
        }
    }
}