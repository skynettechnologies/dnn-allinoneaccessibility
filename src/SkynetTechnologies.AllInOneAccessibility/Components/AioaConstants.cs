// Copyright (c) Skynet Technologies USA LLC. Licensed under the MIT License.
// See the LICENSE file in the project root for more information.

namespace SkynetTechnologies.AllInOneAccessibility.Components
{
    using System;
    using System.Collections.Generic;

    /// <summary>Shared constants for the All in One Accessibility extension.</summary>
    public static class AioaConstants
    {
        /// <summary>Persona Bar menu identifier (must match the manifest).</summary>
        public const string MenuIdentifier = "AllInOneAccessibility";

        /// <summary>Prefix for every portal setting written by this extension.</summary>
        public const string SettingPrefix = "AIOA_";

        public const string ColorCodeSettingKey = SettingPrefix + "ColorCode";
        public const string PositionSettingKey = SettingPrefix + "Position";
        public const string TokenSettingKey = SettingPrefix + "Token";

        /// <summary>Platform name reported to the Skynet API.</summary>
        public const string PlatformName = "DNN";

        public const string ApiBaseUrl = "https://ada.skynettechnologies.us/api/";
        public const string AddUserDomainEndpoint = ApiBaseUrl + "add-user-domain";
        public const string WidgetSettingsEndpoint = ApiBaseUrl + "widget-settings";
        public const string WidgetSettingsUpdateEndpoint = ApiBaseUrl + "widget-setting-update-platform";

        /// <summary>The widget script (the "simple script" embed).</summary>
        public const string WidgetScriptUrl = "https://www.skynettechnologies.com/accessibility/js/all-in-one-accessibility-js-widget-minify.js";

        /// <summary>The id attribute the widget script expects on its own script tag.</summary>
        public const string WidgetScriptId = "aioa-adawidget";

        public const string DefaultColorCode = "#420083";
        public const string DefaultPosition = "bottom_right";
        public const string DefaultIconType = "aioa-icon-type-1";
        public const string DefaultIconSize = "aioa-default-icon";

        public const int CustomSizeMin = 20;
        public const int CustomSizeMax = 150;
        public const int CustomPositionMin = 0;
        public const int CustomPositionMax = 250;

        /// <summary>Allowed widget positions (values as used by the Skynet API, including its "middel" spelling).</summary>
        public static readonly ISet<string> Positions = new HashSet<string>(StringComparer.Ordinal)
        {
            "top_left", "top_center", "top_right",
            "middel_left", "middel_right",
            "bottom_left", "bottom_center", "bottom_right",
        };

        /// <summary>Allowed desktop icon sizes.</summary>
        public static readonly ISet<string> IconSizes = new HashSet<string>(StringComparer.Ordinal)
        {
            "aioa-big-icon", "aioa-medium-icon", "aioa-default-icon", "aioa-small-icon", "aioa-extra-small-icon",
        };

        /// <summary>Number of icon types offered (aioa-icon-type-1 … aioa-icon-type-29).</summary>
        public const int IconTypeCount = 29;
    }
}
