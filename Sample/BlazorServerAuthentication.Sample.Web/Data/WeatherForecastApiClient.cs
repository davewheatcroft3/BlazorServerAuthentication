namespace BlazerServerAuthentication.Sample.Web.Data
{
    public class WeatherForecastApiClient
    {
        private readonly HttpClient _httpClient;

        public WeatherForecastApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<WeatherForecast[]> GetForecastAsync()
        {
            var response = await _httpClient.GetAsync("/weatherforecast");

            response.EnsureSuccessStatusCode();

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