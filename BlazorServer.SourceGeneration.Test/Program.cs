using BlazorServerAuthentication;

namespace BlazorAuthenticate.SourceGeneration.Test
{
    [BlazorAuthenticatedApiClient]
    public partial class ApiClient
    {
        [BlazorAuthenticate]
        private async Task _GetAsync()
        {
            await Task.CompletedTask;
            Console.WriteLine($"Test Get");
        }

        [BlazorAuthenticate]
        private async Task<int> _PostAsync()
        {
            Console.WriteLine($"Test Post");
            return await Task.FromResult(1);
        }
    }

    public class MockHttpClientAuthenticator : IHttpClientAuthenticator
    {
        public Task PrepareHttpClientAsync(HttpClient httpClient)
        {
            Console.WriteLine("Prepare Http Client");
            return Task.CompletedTask;
        }
    }

    public static class Program
    {
        public static void Main()
        {
            var apiClient = new ApiClient(new HttpClient(), new MockHttpClientAuthenticator());

            _ = apiClient.GetAsync();

            _ = apiClient.PostAsync();
        }
    }
}
