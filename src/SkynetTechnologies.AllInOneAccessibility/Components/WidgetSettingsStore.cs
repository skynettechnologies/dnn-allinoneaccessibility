// Copyright (c) Skynet Technologies USA LLC. Licensed under the MIT License.
// See the LICENSE file in the project root for more information.

namespace SkynetTechnologies.AllInOneAccessibility.Components
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text.RegularExpressions;

    using DotNetNuke.Common.Utilities;
    using DotNetNuke.Entities.Portals;

    /// <summary>The values the front-end script tag needs, kept per portal.</summary>
    public sealed class LocalWidgetSettings
    {
        public string ColorCode { get; set; }

        public string Position { get; set; }

        public string Token { get; set; }
    }

    /// <summary>
    /// Reads and writes the small local copy of the widget settings (stored as DNN portal settings).
    /// The full settings live on the Skynet side, keyed by the website domain; this copy only feeds
    /// the colorcode/position/token parameters of the front-end script tag.
    /// </summary>
    public static class WidgetSettingsStore
    {
        private static readonly Regex HexColor = new Regex("^#?([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$", RegexOptions.Compiled);
        private static readonly Regex IconType = new Regex("^aioa-icon-type-([0-9]{1,2})$", RegexOptions.Compiled);
        private static readonly Regex TokenPattern = new Regex("^[A-Za-z0-9_-]*$", RegexOptions.Compiled);

        public static LocalWidgetSettings Load(int portalId)
        {
            // GetPortalSettings is cached by DNN, so this is cheap on every page request.
            var all = PortalController.Instance.GetPortalSettings(portalId);
            var color = Get(all, AioaConstants.ColorCodeSettingKey, AioaConstants.DefaultColorCode);
            var position = Get(all, AioaConstants.PositionSettingKey, AioaConstants.DefaultPosition);
            var token = Get(all, AioaConstants.TokenSettingKey, string.Empty);

            return new LocalWidgetSettings
            {
                ColorCode = NormalizeColor(color) ?? AioaConstants.DefaultColorCode,
                Position = IsValidPosition(position) ? position : AioaConstants.DefaultPosition,
                Token = TokenPattern.IsMatch(token ?? string.Empty) ? (token ?? string.Empty) : string.Empty,
            };
        }

        public static void Save(int portalId, string colorCode, string position)
        {
            var color = NormalizeColor(colorCode);
            if (color != null)
            {
                PortalController.Instance.UpdatePortalSetting(portalId, AioaConstants.ColorCodeSettingKey, color, true, Null.NullString, false);
            }

            if (IsValidPosition(position))
            {
                PortalController.Instance.UpdatePortalSetting(portalId, AioaConstants.PositionSettingKey, position, true, Null.NullString, false);
            }
        }

        private static string Get(IDictionary<string, string> settings, string key, string defaultValue)
        {
            string value;
            return settings != null && settings.TryGetValue(key, out value) && !string.IsNullOrEmpty(value) ? value : defaultValue;
        }

        /// <summary>Returns the colour as "#RRGGBB" (or "#RGB"), or null when it is not a valid hex colour.</summary>
        public static string NormalizeColor(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            value = value.Trim();
            if (!HexColor.IsMatch(value))
            {
                return null;
            }

            return "#" + value.TrimStart('#').ToUpperInvariant();
        }

        public static bool IsValidPosition(string value)
        {
            return !string.IsNullOrEmpty(value) && AioaConstants.Positions.Contains(value);
        }

        public static bool IsValidIconSize(string value)
        {
            return !string.IsNullOrEmpty(value) && AioaConstants.IconSizes.Contains(value);
        }

        public static bool IsValidIconType(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            var match = IconType.Match(value);
            if (!match.Success)
            {
                return false;
            }

            var number = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            return number >= 1 && number <= AioaConstants.IconTypeCount;
        }
    }
}
