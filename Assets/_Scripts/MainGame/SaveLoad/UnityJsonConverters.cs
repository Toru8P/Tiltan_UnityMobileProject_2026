using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace _Scripts.MainGame.SaveLoad
{
    // Newtonsoft serializes Unity's Vector3/Quaternion badly by default (it walks derived properties
    // like .normalized, producing bloat and reference loops). These converters emit compact {x,y,z[,w]}.
    public class Vector3Converter : JsonConverter<Vector3>
    {
        public override void WriteJson(JsonWriter writer, Vector3 v, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x"); writer.WriteValue(v.x);
            writer.WritePropertyName("y"); writer.WriteValue(v.y);
            writer.WritePropertyName("z"); writer.WriteValue(v.z);
            writer.WriteEndObject();
        }

        public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            JObject o = JObject.Load(reader);
            return new Vector3(
                o["x"]?.Value<float>() ?? 0f,
                o["y"]?.Value<float>() ?? 0f,
                o["z"]?.Value<float>() ?? 0f);
        }
    }

    public class QuaternionConverter : JsonConverter<Quaternion>
    {
        public override void WriteJson(JsonWriter writer, Quaternion q, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x"); writer.WriteValue(q.x);
            writer.WritePropertyName("y"); writer.WriteValue(q.y);
            writer.WritePropertyName("z"); writer.WriteValue(q.z);
            writer.WritePropertyName("w"); writer.WriteValue(q.w);
            writer.WriteEndObject();
        }

        public override Quaternion ReadJson(JsonReader reader, Type objectType, Quaternion existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            JObject o = JObject.Load(reader);
            return new Quaternion(
                o["x"]?.Value<float>() ?? 0f,
                o["y"]?.Value<float>() ?? 0f,
                o["z"]?.Value<float>() ?? 0f,
                o["w"]?.Value<float>() ?? 1f);
        }
    }
}
