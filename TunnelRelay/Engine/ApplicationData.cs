﻿namespace TunnelRelay.Engine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using TunnelRelay.Diagnostics;

    /// <summary>
    /// Application data.
    /// </summary>
    internal class ApplicationData
    {
        /// <summary>
        /// Gets or sets the service bus shared key encrypted.
        /// </summary>
        [JsonProperty(PropertyName = "serviceBusSharedKey")]
        private byte[] serviceBusSharedKeyBytes;

        /// <summary>
        /// Initializes static members of the <see cref="ApplicationData"/> class.
        /// </summary>
        static ApplicationData()
        {
            if (File.Exists("appSettings.json"))
            {
                Logger.LogInfo(CallInfo.Site(), "Loading existing settings");
                Instance = JsonConvert.DeserializeObject<ApplicationData>(File.ReadAllText("appSettings.json"));
            }
            else
            {
                Logger.LogInfo(CallInfo.Site(), "Appsettings don't exist. Creating new one.");
                Instance = new ApplicationData
                {
                    RedirectionUrl = "http://localhost:3979/",
                    Version = 2,
                };
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationData"/> class.
        /// </summary>
        public ApplicationData()
        {
            this.EnabledPlugins = new HashSet<string>();
            this.PluginSettingsMap = new Dictionary<string, Dictionary<string, string>>();
        }

        /// <summary>
        /// Gets or sets the application data instance.
        /// </summary>
        [JsonIgnore]
        public static ApplicationData Instance { get; set; }

        /// <summary>
        /// Gets or sets the redirection URL.
        /// </summary>
        [JsonProperty(PropertyName = "redirectionUrl")]
        public string RedirectionUrl { get; set; }

        /// <summary>
        /// Gets or sets the hybrid connection URL.
        /// </summary>
        [JsonProperty(PropertyName = "hybridConnectionUrl")]
        public string HybridConnectionUrl { get; set; }

        /// <summary>
        /// Gets or sets the hybrid connection name.
        /// </summary>
        [JsonProperty(PropertyName = "hybridConnectionName")]
        public string HybridConnectionName { get; set; }

        /// <summary>
        /// Gets or sets the name of the hybrid connection key.
        /// </summary>
        [JsonProperty(PropertyName = "hybridConnectionKeyName")]
        public string HybridConnectionKeyName { get; set; }

        /// <summary>
        /// Gets or sets the hybrid connection shared key.
        /// </summary>
        [JsonIgnore]
        public string HybridConnectionSharedKey
        {
            get
            {
                return Convert.ToBase64String(DataProtection.Unprotect(this.serviceBusSharedKeyBytes));
            }

            set
            {
                this.serviceBusSharedKeyBytes = DataProtection.Protect(Convert.FromBase64String(value));
            }
        }

        /// <summary>
        /// Gets or sets the list of enabled plugins.
        /// </summary>
        [JsonProperty(PropertyName = "enabledPlugins")]
        public HashSet<string> EnabledPlugins { get; set; }

        /// <summary>
        /// Gets or sets the plugin settings map.
        /// </summary>
        [JsonProperty(PropertyName = "pluginSettingsMap")]
        public Dictionary<string, Dictionary<string, string>> PluginSettingsMap { get; set; }

        /// <summary>
        /// Gets or sets the version of the config.
        /// </summary>
        [JsonProperty(PropertyName = "version")]
        public int Version { get; set; }

        /// <summary>
        /// Called after deserialization is done.
        /// </summary>
        /// <param name="context">Deserialization context.</param>
        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            if (this.Version < 2)
            {
                this.HybridConnectionKeyName = null;
                this.serviceBusSharedKeyBytes = null;
                this.HybridConnectionUrl = null;
            }
        }

        /// <summary>
        /// Saves the settings to file.
        /// </summary>
        public void SaveSettings()
        {
            File.WriteAllText("appSettings.json", JsonConvert.SerializeObject(this));
        }

        /// <summary>
        /// Logouts this instance.
        /// </summary>
        public static void Logout()
        {
            Logger.LogInfo(CallInfo.Site(), "Logging out");
            Instance = new ApplicationData
            {
                RedirectionUrl = "http://localhost:3979/",
            };
        }

        /// <summary>
        /// Gets the exported settings.
        /// </summary>
        /// <returns>Settings which can be moved in between machines.</returns>
        public static string GetExportedSettings()
        {
            // Clone the current object into a new one.
            ApplicationData applicationData = JObject.FromObject(ApplicationData.Instance).ToObject<ApplicationData>();

            // Unprotect the data before exporting.
            applicationData.serviceBusSharedKeyBytes = DataProtection.Unprotect(applicationData.serviceBusSharedKeyBytes);

            return JsonConvert.SerializeObject(applicationData);
        }

        /// <summary>
        /// Imports the settings.
        /// </summary>
        /// <param name="serializedSettings">The serialized settings.</param>
        public static void ImportSettings(string serializedSettings)
        {
            ApplicationData applicationData = JsonConvert.DeserializeObject<ApplicationData>(serializedSettings);

            // Encrypt the data with DPAPI.
            applicationData.serviceBusSharedKeyBytes = DataProtection.Protect(applicationData.serviceBusSharedKeyBytes);

            ApplicationData.Instance = applicationData;
        }
    }
}
