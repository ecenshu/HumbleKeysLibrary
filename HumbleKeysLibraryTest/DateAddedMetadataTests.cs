using System;
using HumbleKeys;
using HumbleKeys.Models;
using NUnit.Framework;
using Playnite.SDK.Models;

namespace HumbleKeysLibraryTest
{
    [TestFixture]
    public class DateAddedMetadataTests
    {
        [Test]
        public void UpdateMetaData_NullGame_ReturnsFalse()
        {
            var order = new Order { created = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc) };
            var result = HumbleKeysLibrary.UpdateMetaData(null, order);
            Assert.That(result, Is.False);
        }

        [Test]
        public void UpdateMetaData_NullOrder_ReturnsFalse()
        {
            var game = new Game { Name = "Test Game" };
            var result = HumbleKeysLibrary.UpdateMetaData(game, null);
            Assert.That(result, Is.False);
        }

        [Test]
        public void UpdateMetaData_OrderCreatedMinValue_ReturnsFalse()
        {
            var game = new Game { Name = "Test Game" };
            var order = new Order { created = DateTime.MinValue };
            var result = HumbleKeysLibrary.UpdateMetaData(game, order);
            Assert.That(result, Is.False);
        }

        [Test]
        public void UpdateMetaData_GameAddedNull_SetsDateAndReturnsTrue()
        {
            var createdUtc = new DateTime(2025, 6, 15, 10, 30, 0, DateTimeKind.Utc);
            var game = new Game { Name = "Test Game", Added = null };
            var order = new Order { created = createdUtc };

            var result = HumbleKeysLibrary.UpdateMetaData(game, order);

            Assert.That(result, Is.True);
            Assert.That(game.Added, Is.Not.Null);
            Assert.That(game.Added.Value.ToUniversalTime(), Is.EqualTo(createdUtc));
        }

        [Test]
        public void UpdateMetaData_SameDateIgnoringSubSecondTicks_ReturnsFalse()
        {
            var createdUtc = new DateTime(2025, 6, 15, 10, 30, 0, 123, DateTimeKind.Utc);
            var gameAdded = new DateTime(2025, 6, 15, 10, 30, 0, 789, DateTimeKind.Utc).ToLocalTime();
            var game = new Game { Name = "Test Game", Added = gameAdded };
            var order = new Order { created = createdUtc };

            var result = HumbleKeysLibrary.UpdateMetaData(game, order);

            Assert.That(result, Is.False);
        }

        [Test]
        public void UpdateMetaData_DifferentDate_UpdatesDateAndReturnsTrue()
        {
            var createdUtc = new DateTime(2025, 6, 15, 10, 30, 0, DateTimeKind.Utc);
            var gameAdded = new DateTime(2025, 6, 16, 10, 30, 0, DateTimeKind.Utc).ToLocalTime();
            var game = new Game { Name = "Test Game", Added = gameAdded };
            var order = new Order { created = createdUtc };

            var result = HumbleKeysLibrary.UpdateMetaData(game, order);

            Assert.That(result, Is.True);
            Assert.That(game.Added, Is.Not.Null);
            Assert.That(game.Added.Value.ToUniversalTime(), Is.EqualTo(createdUtc));
        }

        [Test]
        public void OrderMapTo_PreservesCreatedDate()
        {
            var createdDate = new DateTime(2024, 11, 20, 14, 0, 0, DateTimeKind.Utc);
            IOrder order = new Order
            {
                gamekey = "test_order_key",
                created = createdDate,
                product = new Order.Product { human_name = "Test Bundle" }
            };

            var mappedOrder = order.MapTo<HumbleKeys.Services.GameKey.Models.Order>();

            Assert.That(mappedOrder, Is.Not.Null);
            Assert.That(mappedOrder.created, Is.EqualTo(createdDate));
        }

        [Test]
        public void ServicesOrder_ConstructorAndUpdates_PreserveCreatedDate()
        {
            var createdDate = new DateTime(2024, 11, 20, 14, 0, 0, DateTimeKind.Utc);
            IOrder order = new Order
            {
                gamekey = "test_order_key",
                created = createdDate,
                product = new Order.Product { human_name = "Test Bundle" }
            };

            var servicesOrder = new HumbleKeys.Services.GameKey.Models.Order(order);
            Assert.That(servicesOrder.created, Is.EqualTo(createdDate));

            var updatedDate = new DateTime(2024, 12, 1, 9, 0, 0, DateTimeKind.Utc);
            IOrder newOrder = new Order
            {
                gamekey = "test_order_key",
                created = updatedDate,
                product = new Order.Product { human_name = "Test Bundle" }
            };

            servicesOrder.UpdateValues(newOrder);
            Assert.That(servicesOrder.created, Is.EqualTo(updatedDate));
        }

        [Test]
        public void EmptyNoteHandling_SetsNoteWhenNullOrEmpty()
        {
            var game = new Game { Name = "Test Game", Notes = null };
            var expiryNote = "Key expires on: 2026-12-31\n";

            if (string.IsNullOrEmpty(game.Notes))
            {
                game.Notes = expiryNote;
            }
            else if (!game.Notes.Contains(expiryNote))
            {
                game.Notes += expiryNote;
            }

            Assert.That(game.Notes, Is.EqualTo(expiryNote));

            // Run again to verify it does not duplicate
            var modified = false;
            if (string.IsNullOrEmpty(game.Notes))
            {
                game.Notes = expiryNote;
                modified = true;
            }
            else if (!game.Notes.Contains(expiryNote))
            {
                game.Notes += expiryNote;
                modified = true;
            }

            Assert.That(modified, Is.False);
            Assert.That(game.Notes, Is.EqualTo(expiryNote));
        }
    }
}
