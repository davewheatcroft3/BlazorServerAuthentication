using BlazorServerAuthentication;

namespace BlazerServerAuthentication.Sample.Web.Data
{
    [BlazorAuthenticatedApiClient]
    public partial class WeatherForecastApiClient
    {
        private async Task<WeatherForecast[]> _GetForecastAsync()
        {
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