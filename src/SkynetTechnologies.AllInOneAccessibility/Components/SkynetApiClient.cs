// Copyright (c) Skynet Technologies USA LLC. Licensed under the MIT License.
// See the LICENSE file in the project root for more information.

namespace SkynetTechnologies.AllInOneAccessibility.Components
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>Result of a call to the Skynet API.</summary>
    public sealed class ApiResult
    {
        public bool Success { get; set; }

        public int StatusCode { get; set; }

        public string Body { get; set; }

        public string Error { get; set; }
    }

    /// <summary>
    /// Server-side calls to the All in One Accessibility API.
    /// This is the .NET port of <c>functions.php</c> (SkynetWidget::getWidgetInfo / fetchWidgetSettings)
    /// plus the settings update call that the PHP page made from the browser.
    /// </summary>
    public static class SkynetApiClient
    {
        private static readonly HttpClient Client = CreateClient();

        /// <summary>Registers the domain for the free widget (port of SkynetWidget::getWidgetInfo).</summary>
        public static Task<ApiResult> RegisterDomainAsync(string domain)
        {
            var payload = new Dictionary<string, string>
            {
                { "name", domain },
                { "email", "no-reply@" + domain },
                { "company_name", string.Empty },
                { "website", Convert.ToBase64String(Encoding.UTF8.GetBytes(domain)) },
                { "package_type", "free-widget" },
                { "start_date", DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'+0000'", CultureInfo.InvariantCulture) },
                { "end_date", string.Empty },
                { "price", string.Empty },
                { "discount_price", "0" },
                { "platform", AioaConstants.PlatformName },
                { "api_key", string.Empty },
                { "is_trial_period", string.Empty },
                { "is_free_widget", "1" },
                { "bill_address", string.Empty },
                { "country", string.Empty },
                { "state", string.Empty },
                { "city", string.Empty },
                { "post_code", string.Empty },
                { "transaction_id", string.Empty },
                { "subscr_id", string.Empty },
                { "payment_source", string.Empty },
            };

            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            return PostAsync(AioaConstants.AddUserDomainEndpoint, content);
        }

        /// <summary>
        /// Gets the widget settings for a domain (the "Data" object), or null when the domain is unknown.
        /// </summary>
        public static async Task<JObject> GetWidgetSettingsAsync(string domain)
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string> { { "website_url", domain } });
            var result = await PostAsync(AioaConstants.WidgetSettingsEndpoint, content).ConfigureAwait(false);
            if (!result.Success || string.IsNullOrWhiteSpace(result.Body))
            {
                return null;
            }

            try
            {
                var json = JToken.Parse(result.Body) as JObject;
                var data = json?["Data"] as JObject;
                return data != null && data.HasValues ? data : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the settings, registering the domain first when it is not known yet
        /// (port of SkynetWidget::fetchWidgetSettings, with a single retry instead of unbounded recursion).
        /// </summary>
        public static async Task<JObject> GetOrRegisterWidgetSettingsAsync(string domain)
        {
            var data = await GetWidgetSettingsAsync(domain).ConfigureAwait(false);
            if (data != null)
            {
                return data;
            }

            await RegisterDomainAsync(domain).ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            return await GetWidgetSettingsAsync(domain).ConfigureAwait(false);
        }

        /// <summary>Saves the widget settings (the call the PHP page made to widget-setting-update-platform).</summary>
        public static Task<ApiResult> UpdateWidgetSettingsAsync(IDictionary<string, string> fields)
        {
            return PostAsync(AioaConstants.WidgetSettingsUpdateEndpoint, new FormUrlEncodedContent(fields));
        }

        private static async Task<ApiResult> PostAsync(string url, HttpContent content)
        {
            try
            {
                using (content)
                using (var response = await Client.PostAsync(url, content).ConfigureAwait(false))
                {
                    var body = response.Content == null
                        ? string.Empty
                        : await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    return new ApiResult
                    {
                        Success = response.IsSuccessStatusCode,
                        StatusCode = (int)response.StatusCode,
                        Body = body,
                        Error = response.IsSuccessStatusCode ? null : response.ReasonPhrase,
                    };
                }
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException || ex is WebException)
            {
                return new ApiResult { Success = false, StatusCode = 0, Error = ex.Message };
            }
        }

        private static HttpClient CreateClient()
        {
            // Make sure TLS 1.2 is available on older Windows/.NET Framework configurations.
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("AllInOneAccessibility-DNN/1.0");
            return client;
        }
    }
}
