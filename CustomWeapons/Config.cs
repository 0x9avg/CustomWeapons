using CounterStrikeSharp.API.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.Json.Serialization;

namespace CustomWeapons
{
    public class WeaponSkin
    {
        public string? name { get; set; }
        public string? viewmodel { get; set; }
        public string? worldmodel { get; set; }
        public string? @private { get; set; }
        public string? private_prefix { get; set; }
        public string? hide { get; set; }
    }

    [Newtonsoft.Json.JsonConverter(typeof(WeaponCategoryConverter))]
    public class WeaponCategory
    {
        // Название категории, например, "Нож"
        public string? name { get; set; }

        // Скины, где ключ – идентификатор скина (например, "v_octane"), а значение – объект WeaponSkin
        public Dictionary<string, WeaponSkin> Skins { get; set; } = new Dictionary<string, WeaponSkin>();
    }

    public class CustomWeaponsRoot
    {
        // Словарь категорий, где ключ – идентификатор категории (например, "weapon_knife")
        public Dictionary<string, WeaponCategory>? CustomWeapons { get; set; }
    }

    public class WeaponCategoryConverter : Newtonsoft.Json.JsonConverter<WeaponCategory>
    {
        public override WeaponCategory? ReadJson(JsonReader reader, Type objectType, WeaponCategory? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject obj = JObject.Load(reader);
            var category = new WeaponCategory();

            // Извлекаем название категории (если присутствует)
            if (obj.TryGetValue("name", out JToken? nameToken))
            {
                category.name = nameToken.ToString();
                obj.Remove("name"); // удаляем, чтобы оставшиеся свойства воспринимать как скины
            }

            // Перебираем оставшиеся свойства и десериализуем каждое как WeaponSkin
            foreach (var prop in obj.Properties())
            {
                WeaponSkin? skin = prop.Value.ToObject<WeaponSkin>(serializer);
                if (skin != null)
                {
                    category.Skins[prop.Name] = skin;
                }
            }
            return category;
        }

        public override void WriteJson(JsonWriter writer, WeaponCategory? value, JsonSerializer serializer)
        {
            throw new NotImplementedException("Serialization не реализована");
        }
    }

    public class CustomWeaponsConfig : BasePluginConfig
    {
        [JsonPropertyName("DatabaseHost")]
        public string DatabaseHost { get; set; } = "";

        [JsonPropertyName("DatabasePort")]
        public int DatabasePort { get; set; } = 3306;

        [JsonPropertyName("DatabaseUser")]
        public string DatabaseUser { get; set; } = "";

        [JsonPropertyName("DatabasePassword")]
        public string DatabasePassword { get; set; } = "";

        [JsonPropertyName("DatabaseName")]
        public string DatabaseName { get; set; } = "";

        [JsonPropertyName("MenuType")]
        public string MenuType { get; set; } = "selectable";
    }
}