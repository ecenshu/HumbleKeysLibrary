using HumbleKeys;
using HumbleKeys.Services;
using NSubstitute;
using NUnit.Framework;
using Playnite.SDK;
using Playnite.SDK.Data;
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

        [Test]
        public void TestOrderKeyModel()
        {
            const string jsonString = "[\"69qUx3J0hmJHrsFS\",\"y1VUMmOw4zdyFf6n\"]";
            var settings = new Newtonsoft.Json.JsonSerializerSettings();
            settings.Converters.Add(new OrderKeyConverter());
            var parsedArray = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Collections.Generic.List<OrderKey>>(jsonString, settings);
            Assert.That(parsedArray, Is.Not.Null);
        }
    }
}