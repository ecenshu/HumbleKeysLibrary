using HumbleKeys;
using NSubstitute;
using NUnit.Framework;
using Playnite.SDK;
using Playnite.SDK.Plugins;

namespace HumbleKeysLibraryTest
{
    [TestFixture]
    public class LibraryTests
    {
        [Test]
        public void NullSteamLinkContinuesImport()
        {
            var api = Substitute.For<IPlayniteAPI>();
            var settings = new HumbleKeysLibrarySettings
            {
                ConnectAccount = true
            };
            var library = new HumbleKeysLibrary(api, settings);
            var games = library.ImportGames(new LibraryImportGamesArgs());
        }
    }
}