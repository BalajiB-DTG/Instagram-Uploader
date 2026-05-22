using InstagramUploaderApi.Models;
using Newtonsoft.Json;
using System.Text;

namespace InstagramUploaderApi.Services
{
    public class InstagramService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public InstagramService(
            IConfiguration configuration,
            HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public async Task<string> PublishImageAsync(
            InstagramPublishRequest request)
        {
            var accessToken =
                _configuration["InstagramSettings:AccessToken"];

            var instagramBusinessId =
                _configuration["InstagramSettings:InstagramBusinessId"];

            // STEP 1 - CREATE MEDIA CONTAINER

            string createMediaUrl =
                $"https://graph.instagram.com/v22.0/{instagramBusinessId}/media?access_token={accessToken}";

            var mediaPayload = new
            {
                image_url = request.ImageUrl,
                caption = request.Caption
            };

            var mediaJson =
                JsonConvert.SerializeObject(mediaPayload);

            var mediaContent =
                new StringContent(
                    mediaJson,
                    Encoding.UTF8,
                    "application/json");

            var mediaResponse =
                await _httpClient.PostAsync(
                    createMediaUrl,
                    mediaContent);

            var mediaResult =
                await mediaResponse.Content.ReadAsStringAsync();

            if (!mediaResponse.IsSuccessStatusCode)
            {
                return mediaResult;
            }

            var mediaObject =
                JsonConvert.DeserializeObject<MediaContainerResponse>(
                    mediaResult);

            string creationId = mediaObject.id;

            // STEP 1.5 - WAIT FOR MEDIA CONTAINER TO BE READY
            
            string statusUrl = $"https://graph.instagram.com/v22.0/{creationId}?fields=status_code&access_token={accessToken}";
            bool isReady = false;
            
            for (int i = 0; i < 10; i++)
            {
                var statusResponse = await _httpClient.GetAsync(statusUrl);
                if (statusResponse.IsSuccessStatusCode)
                {
                    var statusResult = await statusResponse.Content.ReadAsStringAsync();
                    dynamic statusObj = JsonConvert.DeserializeObject(statusResult);
                    string statusCode = statusObj.status_code;

                    if (statusCode == "FINISHED")
                    {
                        isReady = true;
                        break;
                    }
                    if (statusCode == "ERROR")
                    {
                        return $"Error processing media container: {statusResult}";
                    }
                }

                // Wait 3 seconds before checking again
                await Task.Delay(3000);
            }

            if (!isReady)
            {
                return "{\"error\": {\"message\": \"Media container processing timed out. Please try again.\"}}";
            }

            // STEP 2 - PUBLISH MEDIA

            string publishUrl =
                $"https://graph.instagram.com/v22.0/{instagramBusinessId}/media_publish?access_token={accessToken}";

            var publishPayload = new
            {
                creation_id = creationId
            };

            var publishJson =
                JsonConvert.SerializeObject(publishPayload);

            var publishContent =
                new StringContent(
                    publishJson,
                    Encoding.UTF8,
                    "application/json");

            var publishResponse =
                await _httpClient.PostAsync(
                    publishUrl,
                    publishContent);

            var publishResult =
                await publishResponse.Content.ReadAsStringAsync();

            return publishResult;
        }
    }
}