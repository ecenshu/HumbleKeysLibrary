using System.Collections.Generic;

namespace HumbleKeys.Services
{
    public class HumbleKeysAccountClientSettings : IHumbleKeysAccountClientSettings
    {
        public bool CacheEnabled { get; set; } = false;
        public string CachePath { get; set; }
        public void BeginEdit()
        {
        }

        public void EndEdit()
        {
        }

        public void CancelEdit()
        {
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            if (string.IsNullOrEmpty(CachePath)) errors.Add("Cache path is required.");
            return errors.Count == 0;
        }
    }
}