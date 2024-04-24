using Newtonsoft.Json;
using System.IO;

namespace MilkWang1;

static public class Util
{
    static JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
    {
        PreserveReferencesHandling = PreserveReferencesHandling.All,
        ReferenceLoopHandling = ReferenceLoopHandling.Serialize
    };

    public static T GetData<T>(string path)
    {
        return JsonConvert.DeserializeObject<T>(File.ReadAllText(path), jsonSerializerSettings);
    }

    public static void Save<T>(T obj, string path)
    {
        File.WriteAllText(path, JsonConvert.SerializeObject(obj, jsonSerializerSettings));
    }

    public static void Save2<T>(T obj, string path)
    {
        File.WriteAllText(path, JsonConvert.SerializeObject(obj));
    }
}
