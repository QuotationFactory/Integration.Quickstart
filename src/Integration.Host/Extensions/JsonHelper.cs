using System;
using System.IO;
using Integration.Common.Serialization;
using Newtonsoft.Json;
using Versioned.ExternalDataContracts;

namespace Integration.Host.Extensions;

public static class JsonHelper
{
    public static JsonSerializerSettings JsonSerializerSettings { get; } = GetJsonSerializerSettings();

    public static T Deserialize<T>(string jsonFilePath)
    {
        if (string.IsNullOrWhiteSpace(jsonFilePath))
        {
            throw new ArgumentNullException(nameof(jsonFilePath));
        }

        // read all text from file that is created
        var json = File.ReadAllText(jsonFilePath);

        // convert json to project object
        return JsonConvert.DeserializeObject<T>(json, JsonSerializerSettings);
    }

    private static JsonSerializerSettings GetJsonSerializerSettings()
    {
        // define json serializer settings
        var settings = new JsonSerializerSettings();
        settings.SetJsonSettings();
        settings.AddJsonConverters();
        settings.SerializationBinder = new CrossPlatformTypeBinder();

        return settings;
    }
}
