// Copyright (c) Skynet Technologies USA LLC. Licensed under the MIT License.
// See the LICENSE file in the project root for more information.

namespace SkynetTechnologies.AllInOneAccessibility.Components
{
    using System;

    using Dnn.PersonaBar.Library.AppEvents;
    using Dnn.PersonaBar.Library.AppEvents.Attributes;
    using DotNetNuke.Application;
    using DotNetNuke.Services.Exceptions;
    using DotNetNuke.UI.Skins.EventListeners;

    /// <summary>
    /// Runs once when the DNN application starts (called by the Persona Bar, which is part of every
    /// DNN 9/10 site) and subscribes to the page (skin) pre-render event so the widget script is added
    /// to every page. Nothing is written to web.config, so install and uninstall are clean.
    /// </summary>
    [IgnoreVersionMatchCheck]
    public class WidgetAppEvents : IAppEvents
    {
        private static readonly object Lock = new object();
        private static bool registered;

        public void ApplicationBegin()
        {
            if (registered)
            {
                return;
            }

            lock (Lock)
            {
                if (registered)
                {
                    return;
                }

                DotNetNukeContext.Current.SkinEventListeners.Add(
                    new SkinEventListener(SkinEventType.OnSkinPreRender, OnSkinPreRender));
                registered = true;
            }
        }

        public void ApplicationEnd()
        {
        }

        private static void OnSkinPreRender(object sender, SkinEventArgs e)
        {
            try
            {
                WidgetRenderer.Render(e?.Skin?.Page);
            }
            catch (Exception ex)
            {
                // Never break the website because of the widget.
                Exceptions.LogException(ex);
            }
        }
    }
}
