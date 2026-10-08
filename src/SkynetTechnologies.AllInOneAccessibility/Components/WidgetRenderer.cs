// Copyright (c) Skynet Technologies USA LLC. Licensed under the MIT License.
// See the LICENSE file in the project root for more information.

namespace SkynetTechnologies.AllInOneAccessibility.Components
{
    using System;
    using System.Reflection;
    using System.Text;
    using System.Web;
    using System.Web.UI;

    using DotNetNuke.Entities.Portals;

    /// <summary>Writes the All in One Accessibility script tag into DNN pages.</summary>
    public static class WidgetRenderer
    {
        private const string ScriptKey = "SkynetTechnologies.AllInOneAccessibility.Widget";

        /// <summary>Adds the widget script to the end of the page body (just before the closing form/body tags).</summary>
        public static void Render(Page page)
        {
            if (page == null || page.ClientScript.IsStartupScriptRegistered(typeof(WidgetRenderer), ScriptKey))
            {
                return;
            }

            var request = page.Request;

            // Skip DNN popup windows (module settings dialogs etc.) - only real pages get the widget.
            if (string.Equals(request.QueryString["popUp"], "true", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var portalSettings = PortalSettings.Current;
            if (portalSettings == null || portalSettings.PortalId < 0)
            {
                return;
            }

            var settings = WidgetSettingsStore.Load(portalSettings.PortalId);
            var tag = BuildScriptTag(settings, GetCspNonce(page));

            page.ClientScript.RegisterStartupScript(typeof(WidgetRenderer), ScriptKey, tag, false);
        }

        /// <summary>
        /// Builds the "simple script" embed:
        /// &lt;script id="aioa-adawidget" src="...widget-minify.js?colorcode=#420083&amp;token=&amp;position=bottom_right" defer&gt;&lt;/script&gt;.
        /// </summary>
        public static string BuildScriptTag(LocalWidgetSettings settings, string nonce)
        {
            var src = new StringBuilder(AioaConstants.WidgetScriptUrl)
                .Append("?colorcode=").Append(settings.ColorCode)
                .Append("&token=").Append(HttpUtility.UrlEncode(settings.Token ?? string.Empty))
                .Append("&position=").Append(settings.Position)
                .ToString();

            var tag = new StringBuilder();
            tag.Append(Environment.NewLine);
            tag.Append("<script id=\"").Append(AioaConstants.WidgetScriptId).Append('"');
            tag.Append(" src=\"").Append(HttpUtility.HtmlAttributeEncode(src)).Append('"');
            if (!string.IsNullOrEmpty(nonce))
            {
                tag.Append(" nonce=\"").Append(HttpUtility.HtmlAttributeEncode(nonce)).Append('"');
            }

            tag.Append(" defer></script>");
            tag.Append(Environment.NewLine);
            return tag.ToString();
        }

        /// <summary>
        /// DNN 10 pages can emit a Content Security Policy. When the page exposes one, add its nonce to
        /// our script tag so nonce-based policies accept it. Done with reflection so the extension
        /// keeps working on DNN versions without CSP support.
        /// </summary>
        private static string GetCspNonce(Page page)
        {
            try
            {
                const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                for (var type = page.GetType(); type != null; type = type.BaseType)
                {
                    var cspProperty = type.GetProperty("ContentSecurityPolicy", Flags | BindingFlags.DeclaredOnly);
                    if (cspProperty == null)
                    {
                        continue;
                    }

                    var csp = cspProperty.GetValue(page, null);
                    var nonceProperty = csp?.GetType().GetProperty("Nonce", BindingFlags.Instance | BindingFlags.Public);
                    return nonceProperty?.GetValue(csp, null) as string;
                }
            }
            catch (Exception)
            {
                // CSP support is optional; never break page rendering because of it.
            }

            return null;
        }
    }
}
