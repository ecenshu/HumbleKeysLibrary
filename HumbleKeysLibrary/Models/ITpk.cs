namespace HumbleKeys.Models
{
    public interface ITpk
    {
        string machine_name { get; set; }
        string gamekey { get; set; }
        string key_type { get; set; }
        bool visible { get; set; }
        bool sold_out { get; set; }
        string instructions_html { get; set; }
        string key_type_human_name { get; set; }
        string human_name { get; set; }
        string @class { get; set; }
        string library_family_name { get; set; }
        string steam_app_id { get; set; }
        bool is_expired { get; set; }
        Newtonsoft.Json.Linq.JToken redeemed_key_val { get; set; }
        bool is_virtual { get; set; }
    }
}