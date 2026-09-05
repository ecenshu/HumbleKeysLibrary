using System;
using System.Collections.Generic;
using HumbleKeys;
using HumbleKeys.Services;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using NUnit.Framework;
using Playnite.SDK;
using Playnite.SDK.Plugins;

namespace HumbleKeysLibraryTest
{
    [TestFixture]
    public class RepositoryTests
    {
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
        public void CachedGetLibraryKeysResponseTest()
        {
            var sourceRepository = Substitute.For<IHumbleOrderRepository>();
            sourceRepository.GetLibraryKeys().ReturnsNull();
            var nextSource = Substitute.For<IHumbleOrderRepository>();
            nextSource.GetLibraryKeys().Returns(new List<string>());
            var repository = new HumbleOrderCachedRepository(sourceRepository, nextSource);
            var libraryKeys = repository.GetLibraryKeys();
            Assert.That(libraryKeys, Is.Not.Null);
            Assert.That(nextSource.Received().GetLibraryKeys(), Is.Empty);
        }
    }
}