using Playnite.SDK;
using System;

namespace HumbleKeys
{
    public class HumbleKeysLibraryClient : LibraryClient
    {
        public override bool IsInstalled => true;

        public override void Open()
        {
            throw new NotImplementedException();
        }
    }
}