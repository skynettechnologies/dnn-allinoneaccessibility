// Copyright (c) Skynet Technologies USA LLC. Licensed under the MIT License.
// See the LICENSE file in the project root for more information.

namespace SkynetTechnologies.AllInOneAccessibility.Services
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web;
    using System.Web.Http;

    using Dnn.PersonaBar.Library;
    using Dnn.PersonaBar.Library.Attributes;
    using DotNetNuke.Services.Exceptions;
    using DotNetNuke.Web.Api;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using SkynetTechnologies.AllInOneAccessibility.Components;

    /// <summary>
    /// Admin API behind the Persona Bar settings page (the .NET version of route.php).
    /// URL: /API/PersonaBar/AllInOneAccessibility/{action}. Only administrators / hosts can call it.
    /// </summary>
    [MenuPermission(MenuName = AioaConstants.MenuIdentifier)]
    public class AllInOneAccessibilityController : PersonaBarApiController
    {
        /// <summary>GET: loads the widget settings for this website (registers the domain first if needed).</summary>
        [HttpGet]
        public async Task<HttpResponseMessage> GetSettings()
        {
            try
            {
                // Read request-bound values before awaiting.
                var domain = GetDomain();
                var portalId = this.PortalId;
                var data = await SkynetApiClient.GetOrRegisterWidgetSettingsAsync(domain);

                if (data == null)
                {
                    return this.Request.CreateResponse(HttpStatusCode.OK, new
                    {
                        success = false,
                        domain,
                        message = "Could not load the widget settings from All in One Accessibility. Default values are shown; saving will create the settings.",
                        data = (object)null,
                    });
                }

                // Keep the local copy used by the front-end script tag in sync.
                WidgetSettingsStore.Save(portalId, data.Value<string>("widget_color_code"), data.Value<string>("widget_position"));

                return this.Request.CreateResponse(HttpStatusCode.OK, new { success = true, domain, data });
            }
            catch (Exception ex)
            {
                Exceptions.LogException(ex);
                return this.Request.CreateResponse(HttpStatusCode.InternalServerError, new { success = false, message = "An unexpected error occurred while loading the settings." });
            }
        }

        /// <summary>POST: validates and saves the widget settings.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<HttpResponseMessage> SaveSettings(SaveSettingsRequest model)
        {
            try
            {
                if (model == null)
                {
                    return this.Invalid("No settings were received.");
                }

                var color = WidgetSettingsStore.NormalizeColor(model.WidgetColorCode);
                if (color == null)
                {
                    return this.Invalid("Please enter a valid hex color code, for example FF5733.");
                }

                if (!WidgetSettingsStore.IsValidPosition(model.WidgetPosition))
                {
                    return this.Invalid("Please select where to place the accessibility icon.");
                }

                if (!WidgetSettingsStore.IsValidIconType(model.WidgetIconType))
                {
                    return this.Invalid("Please select an icon type.");
                }

                if (!WidgetSettingsStore.IsValidIconSize(model.WidgetIconSize))
                {
                    return this.Invalid("Please select an icon size.");
                }

                var isCustomSize = model.IsWidgetCustomSize == 1 ? 1 : 0;
                var isCustomPosition = model.IsWidgetCustomPosition == 1 ? 1 : 0;
                var widgetSize = model.WidgetSize == 1 ? 1 : 0;

                var customSize = model.WidgetIconSizeCustom;
                if (isCustomSize == 1)
                {
                    if (!customSize.HasValue || customSize.Value < AioaConstants.CustomSizeMin)
                    {
                        return this.Invalid("Please enter an exact icon size of at least 20.");
                    }

                    if (customSize.Value > AioaConstants.CustomSizeMax)
                    {
                        return this.Invalid("Please enter an exact icon size of at most 150.");
                    }
                }

                var domain = GetDomain();
                var portalId = this.PortalId;
                var fields = new Dictionary<string, string>
                {
                    { "u", domain },
                    { "widget_position", model.WidgetPosition },
                    { "is_widget_custom_position", ToString(isCustomPosition) },
                    { "is_widget_custom_size", ToString(isCustomSize) },
                    { "widget_color_code", color },
                    { "widget_icon_type", model.WidgetIconType },
                    { "widget_icon_size", model.WidgetIconSize },
                    { "widget_size", ToString(widgetSize) },
                    { "widget_icon_size_custom", customSize.HasValue ? ToString(customSize.Value) : string.Empty },
                    { "widget_position_right", ClampPosition(model.WidgetPositionRight) },
                    { "widget_position_left", ClampPosition(model.WidgetPositionLeft) },
                    { "widget_position_top", ClampPosition(model.WidgetPositionTop) },
                    { "widget_position_bottom", ClampPosition(model.WidgetPositionBottom) },
                };

                var result = await SkynetApiClient.UpdateWidgetSettingsAsync(fields);
                if (!result.Success)
                {
                    return this.Request.CreateResponse(HttpStatusCode.BadGateway, new
                    {
                        success = false,
                        message = "All in One Accessibility could not save the settings. Please try again in a moment.",
                    });
                }

                WidgetSettingsStore.Save(portalId, color, model.WidgetPosition);

                return this.Request.CreateResponse(HttpStatusCode.OK, new { success = true, message = "Settings saved successfully!" });
            }
            catch (Exception ex)
            {
                Exceptions.LogException(ex);
                return this.Request.CreateResponse(HttpStatusCode.InternalServerError, new { success = false, message = "An unexpected error occurred while saving the settings." });
            }
        }

        private static string GetDomain()
        {
            // The website's host name as the visitor sees it (e.g. www.example.com); the widget is keyed on it.
            return HttpContext.Current.Request.Url.Host.ToLowerInvariant();
        }

        private static string ClampPosition(int? value)
        {
            if (!value.HasValue)
            {
                return string.Empty;
            }

            var clamped = Math.Min(Math.Max(value.Value, AioaConstants.CustomPositionMin), AioaConstants.CustomPositionMax);
            return ToString(clamped);
        }

        private static string ToString(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private HttpResponseMessage Invalid(string message)
        {
            return this.Request.CreateResponse(HttpStatusCode.BadRequest, new { success = false, message });
        }
    }

    /// <summary>Settings posted by the Persona Bar page (same field names as the PHP version).</summary>
    public class SaveSettingsRequest
    {
        [JsonProperty("widget_position")]
        public string WidgetPosition { get; set; }

        [JsonProperty("is_widget_custom_position")]
        public int IsWidgetCustomPosition { get; set; }

        [JsonProperty("is_widget_custom_size")]
        public int IsWidgetCustomSize { get; set; }

        [JsonProperty("widget_color_code")]
        public string WidgetColorCode { get; set; }

        [JsonProperty("widget_icon_type")]
        public string WidgetIconType { get; set; }

        [JsonProperty("widget_icon_size")]
        public string WidgetIconSize { get; set; }

        [JsonProperty("widget_size")]
        public int WidgetSize { get; set; }

        [JsonProperty("widget_icon_size_custom")]
        public int? WidgetIconSizeCustom { get; set; }

        [JsonProperty("widget_position_right")]
        public int? WidgetPositionRight { get; set; }

        [JsonProperty("widget_position_left")]
        public int? WidgetPositionLeft { get; set; }

        [JsonProperty("widget_position_top")]
        public int? WidgetPositionTop { get; set; }

        [JsonProperty("widget_position_bottom")]
        public int? WidgetPositionBottom { get; set; }
    }
}
