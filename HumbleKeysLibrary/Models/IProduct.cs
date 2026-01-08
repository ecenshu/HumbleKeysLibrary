namespace HumbleKeys.Models
{
    public interface IProduct
    {
        string category { get; set; }
        string machine_name { get; set; }
        string human_name { get; set; }
        string choice_url { get; set; }
        bool is_subs_v2_product { get; set; }
        bool is_subs_v3_product { get; set; }
    }
}