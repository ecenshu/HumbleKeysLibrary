using System;
using HumbleKeys;
using HumbleKeys.Services;
using NSubstitute;
using NUnit.Framework;
using Playnite.SDK;
using Playnite.SDK.Plugins;

namespace HumbleKeysLibraryTest
{
    [TestFixture]
    public class RepositoryTests
    {
        [Test]
        public void InitialiseTest()
        {
            Assert.True(true);
        }

        [Test]
        public void GetUnAuthenticatedTest()
        {
            var api = Substitute.For<IPlayniteAPI>();
            var keysLibrary = new HumbleKeysLibrary(api, new HumbleKeysLibrarySettings(){ ConnectAccount = false }){Properties = { HasSettings = true}};
            var importGames = keysLibrary.ImportGames(new LibraryImportGamesArgs());
            
            Assert.That(importGames, Is.Not.Null);
            Assert.That(importGames, Is.Empty);
        }
    }
}