using System;
using HumbleKeys;
using HumbleKeys.Models;
using NUnit.Framework;

namespace HumbleKeysLibraryTest
{
    [TestFixture]
    public class ExpirableKeyTests
    {
        #region === Base States (Non-Expirable) ===

        [Test]
        public void NoKey_Virtual_NonExpirable_ReturnsUnclaimed()
        {
            var tpk = new Order.TpkdDict.Tpk
            {
                redeemed_key_val = null,
                is_virtual = true,
                num_days_until_expired = 0,
                is_expired = false
            };

            var tags = HumbleKeysLibrary.GetOrderRedemptionTags(tpk);
            Assert.That(tags, Is.EqualTo(new[] { "Key: Unclaimed" }));
            Assert.That(HumbleKeysLibrary.GetOrderRedemptionTagState(tpk), Is.EqualTo("Key: Unclaimed"));
        }

        [Test]
        public void NoKey_NonVirtual_NonExpirable_ReturnsClaimed()
        {
            var tpk = new Order.TpkdDict.Tpk
            {
                redeemed_key_val = null,
                is_virtual = false,
                num_days_until_expired = 0,
                is_expired = false
            };

            var tags = HumbleKeysLibrary.GetOrderRedemptionTags(tpk);
            Assert.That(tags, Is.EqualTo(new[] { "Key: Claimed" }));
            Assert.That(HumbleKeysLibrary.GetOrderRedemptionTagState(tpk), Is.EqualTo("Key: Claimed"));
        }

        [Test]
        public void KeyPresent_NotInSteamLibrary_NonExpirable_ReturnsUnredeemed()
        {
            var tpk = new Order.TpkdDict.Tpk
            {
                key_type = "steam",
                steam_app_id = "12345",
                redeemed_key_val = "ABCD-1234",
                is_expired = false,
                num_days_until_expired = 0
            };

            var tags = HumbleKeysLibrary.GetOrderRedemptionTags(tpk, existsInSteamLibrary: false);
            Assert.That(tags, Is.EqualTo(new[] { "Key: Unredeemed" }));
            Assert.That(HumbleKeysLibrary.GetOrderRedemptionTagState(tpk, existsInSteamLibrary: false), Is.EqualTo("Key: Unredeemed"));
        }

        [Test]
        public void KeyPresent_InSteamLibrary_NonExpirable_ReturnsRedeemed()
        {
            var tpk = new Order.TpkdDict.Tpk
            {
                key_type = "steam",
                steam_app_id = "12345",
                redeemed_key_val = "ABCD-1234",
                is_expired = false,
                num_days_until_expired = 0
            };

            var tags = HumbleKeysLibrary.GetOrderRedemptionTags(tpk, existsInSteamLibrary: true);
            Assert.That(tags, Is.EqualTo(new[] { "Key: Redeemed" }));
            Assert.That(HumbleKeysLibrary.GetOrderRedemptionTagState(tpk, existsInSteamLibrary: true), Is.EqualTo("Key: Redeemed"));
        }

        [Test]
        public void KeyPresent_NonSteam_ReturnsRedeemed()
        {
            var tpk = new Order.TpkdDict.Tpk
            {
                key_type = "gog",
                redeemed_key_val = "GOG-1234",
                is_expired = false,
                num_days_until_expired = 0
            };

            var tags = HumbleKeysLibrary.GetOrderRedemptionTags(tpk, existsInSteamLibrary: false);
            Assert.That(tags, Is.EqualTo(new[] { "Key: Redeemed" }));
            Assert.That(HumbleKeysLibrary.GetOrderRedemptionTagState(tpk, existsInSteamLibrary: false), Is.EqualTo("Key: Redeemed"));
        }

        #endregion

        #region === Expirable Modifier ===

        [Test]
        public void NoKey_Virtual_Expirable_ReturnsUnclaimedAndExpirable()
        {
            var tpk = new Order.TpkdDict.Tpk
            {
                redeemed_key_val = null,
                is_virtual = true,
                num_days_until_expired = 5,
                is_expired = false
            };

            var tags = HumbleKeysLibrary.GetOrderRedemptionTags(tpk);
            Assert.That(tags, Is.EqualTo(new[] { "Key: Unclaimed", "Key: Expirable" }));
            Assert.That(HumbleKeysLibrary.GetOrderRedemptionTagState(tpk), Is.EqualTo("Key: Unclaimed, Key: Expirable"));
        }

        [Test]
        public void NoKey_NonVirtual_Expirable_ReturnsClaimedAndExpirable()
        {
            var tpk = new Order.TpkdDict.Tpk
            {
                redeemed_key_val = null,
                is_virtual = false,
                num_days_until_expired = 5,
                is_expired = false
            };

            var tags = HumbleKeysLibrary.GetOrderRedemptionTags(tpk);
            Assert.That(tags, Is.EqualTo(new[] { "Key: Claimed", "Key: Expirable" }));
            Assert.That(HumbleKeysLibrary.GetOrderRedemptionTagState(tpk), Is.EqualTo("Key: Claimed, Key: Expirable"));
        }

        [Test]
        public void KeyPresent_NotInSteam_Expirable_ReturnsUnredeemedAndExpirable()
        {
            var tpk = new Order.TpkdDict.Tpk
            {
                key_type = "steam",
                steam_app_id = "12345",
                redeemed_key_val = "ABCD-1234",
                num_days_until_expired = 5,
                is_expired = false
            };

            var tags = HumbleKeysLibrary.GetOrderRedemptionTags(tpk, existsInSteamLibrary: false);
            Assert.That(tags, Is.EqualTo(new[] { "Key: Unredeemed", "Key: Expirable" }));
            Assert.That(HumbleKeysLibrary.GetOrderRedemptionTagState(tpk, existsInSteamLibrary: false), Is.EqualTo("Key: Unredeemed, Key: Expirable"));
        }

        [Test]
        public void KeyPresent_InSteam_Expirable_ReturnsRedeemedAndExpirable()
        {
            var tpk = new Order.TpkdDict.Tpk
            {
                key_type = "steam",
                steam_app_id = "12345",
                redeemed_key_val = "ABCD-1234",
                num_days_until_expired = 5,
                is_expired = false
            };

            var tags = HumbleKeysLibrary.GetOrderRedemptionTags(tpk, existsInSteamLibrary: true);
            Assert.That(tags, Is.EqualTo(new[] { "Key: Redeemed", "Key: Expirable" }));
            Assert.That(HumbleKeysLibrary.GetOrderRedemptionTagState(tpk, existsInSteamLibrary: true), Is.EqualTo("Key: Redeemed, Key: Expirable"));
        }

        #endregion

        #region === Past Expiration / Unredeemable Modifier ===

        [Test]
        public void NoKey_Virtual_Expired_ReturnsUnclaimedAndUnredeemable()
        {
            var tpk = new Order.TpkdDict.Tpk
            {
                redeemed_key_val = null,
                is_virtual = true,
                num_days_until_expired = 0,
                is_expired = true
            };

            var tags = HumbleKeysLibrary.GetOrderRedemptionTags(tpk);
            Assert.That(tags, Is.EqualTo(new[] { "Key: Unclaimed", "Key: Unredeemable" }));
            Assert.That(HumbleKeysLibrary.GetOrderRedemptionTagState(tpk), Is.EqualTo("Key: Unclaimed, Key: Unredeemable"));
        }

        [Test]
        public void NoKey_NonVirtual_Expired_ReturnsClaimedAndUnredeemable()
        {
            var tpk = new Order.TpkdDict.Tpk
            {
                redeemed_key_val = null,
                is_virtual = false,
                num_days_until_expired = 0,
                is_expired = true
            };

            var tags = HumbleKeysLibrary.GetOrderRedemptionTags(tpk);
            Assert.That(tags, Is.EqualTo(new[] { "Key: Claimed", "Key: Unredeemable" }));
            Assert.That(HumbleKeysLibrary.GetOrderRedemptionTagState(tpk), Is.EqualTo("Key: Claimed, Key: Unredeemable"));
        }

        [Test]
        public void KeyPresent_NotInSteam_Expired_ReturnsUnredeemedAndUnredeemable()
        {
            var tpk = new Order.TpkdDict.Tpk
            {
                key_type = "steam",
                steam_app_id = "12345",
                redeemed_key_val = "ABCD-1234",
                num_days_until_expired = 0,
                is_expired = true
            };

            var tags = HumbleKeysLibrary.GetOrderRedemptionTags(tpk, existsInSteamLibrary: false);
            Assert.That(tags, Is.EqualTo(new[] { "Key: Unredeemed", "Key: Unredeemable" }));
            Assert.That(HumbleKeysLibrary.GetOrderRedemptionTagState(tpk, existsInSteamLibrary: false), Is.EqualTo("Key: Unredeemed, Key: Unredeemable"));
        }

        [Test]
        public void KeyPresent_InSteam_Expired_ReturnsRedeemedAndUnredeemable()
        {
            var tpk = new Order.TpkdDict.Tpk
            {
                key_type = "steam",
                steam_app_id = "12345",
                redeemed_key_val = "ABCD-1234",
                num_days_until_expired = 0,
                is_expired = true
            };

            var tags = HumbleKeysLibrary.GetOrderRedemptionTags(tpk, existsInSteamLibrary: true);
            Assert.That(tags, Is.EqualTo(new[] { "Key: Redeemed", "Key: Unredeemable" }));
            Assert.That(HumbleKeysLibrary.GetOrderRedemptionTagState(tpk, existsInSteamLibrary: true), Is.EqualTo("Key: Redeemed, Key: Unredeemable"));
        }

        [Test]
        public void ExpirationDateInPast_TriggersUnredeemableModifier()
        {
            var tpk = new Order.TpkdDict.Tpk
            {
                key_type = "steam",
                steam_app_id = "12345",
                redeemed_key_val = "ABCD-1234",
                expiration_date = DateTime.Now.AddDays(-1),
                is_expired = false
            };

            var tags = HumbleKeysLibrary.GetOrderRedemptionTags(tpk, existsInSteamLibrary: true);
            Assert.That(tags, Is.EqualTo(new[] { "Key: Redeemed", "Key: Unredeemable" }));
        }

        #endregion

        #region === Field Mapping & Tag Constants ===

        [Test]
        public void MapToPreservesExpirationFields()
        {
            var expDate = new DateTime(2026, 12, 31, 23, 59, 59);
            ITpk tpk = new Order.TpkdDict.Tpk
            {
                gamekey = "test_gamekey",
                machine_name = "test_machine",
                expiration_date = expDate,
                num_days_until_expired = 14
            };

            var mappedTpk = tpk.MapTo<HumbleKeys.Services.GameKey.Models.Order.TpkdDict.Tpk>();

            Assert.That(mappedTpk, Is.Not.Null);
            Assert.That(mappedTpk.expiration_date, Is.EqualTo(expDate));
            Assert.That(mappedTpk.num_days_until_expired, Is.EqualTo(14));
        }

        [Test]
        public void ServicesTpkConstructorAndUpdatesPreserveExpirationFields()
        {
            var expDate = new DateTime(2026, 12, 31, 23, 59, 59);
            ITpk originalTpk = new Order.TpkdDict.Tpk
            {
                gamekey = "test_gamekey",
                machine_name = "test_machine",
                expiration_date = expDate,
                num_days_until_expired = 14
            };

            var servicesTpk = new HumbleKeys.Services.GameKey.Models.Order.TpkdDict.Tpk(originalTpk);
            Assert.That(servicesTpk.expiration_date, Is.EqualTo(expDate));
            Assert.That(servicesTpk.num_days_until_expired, Is.EqualTo(14));

            var newExpDate = new DateTime(2027, 1, 15);
            ITpk updatedTpk = new Order.TpkdDict.Tpk
            {
                gamekey = "test_gamekey",
                machine_name = "test_machine",
                expiration_date = newExpDate,
                num_days_until_expired = 30
            };

            servicesTpk.UpdateValues(updatedTpk);
            Assert.That(servicesTpk.expiration_date, Is.EqualTo(newExpDate));
            Assert.That(servicesTpk.num_days_until_expired, Is.EqualTo(30));
        }

        [Test]
        public void TagConstantsAndPastTags_ContainAllTags()
        {
            Assert.That(HumbleKeysLibrary.REDEEMED_STR, Is.EqualTo("Key: Redeemed"));
            Assert.That(HumbleKeysLibrary.UNREDEEMED_STR, Is.EqualTo("Key: Unredeemed"));
            Assert.That(HumbleKeysLibrary.UNREDEEMABLE_STR, Is.EqualTo("Key: Unredeemable"));
            Assert.That(HumbleKeysLibrary.EXPIRABLE_STR, Is.EqualTo("Key: Expirable"));
            Assert.That(HumbleKeysLibrary.CLAIMED_STR, Is.EqualTo("Key: Claimed"));
            Assert.That(HumbleKeysLibrary.UNCLAIMED_STR, Is.EqualTo("Key: Unclaimed"));

            Assert.That(HumbleKeysLibrary.PAST_TAGS, Does.Contain(HumbleKeysLibrary.REDEEMED_STR));
            Assert.That(HumbleKeysLibrary.PAST_TAGS, Does.Contain(HumbleKeysLibrary.UNREDEEMED_STR));
            Assert.That(HumbleKeysLibrary.PAST_TAGS, Does.Contain(HumbleKeysLibrary.UNREDEEMABLE_STR));
            Assert.That(HumbleKeysLibrary.PAST_TAGS, Does.Contain(HumbleKeysLibrary.EXPIRABLE_STR));
            Assert.That(HumbleKeysLibrary.PAST_TAGS, Does.Contain(HumbleKeysLibrary.CLAIMED_STR));
            Assert.That(HumbleKeysLibrary.PAST_TAGS, Does.Contain(HumbleKeysLibrary.UNCLAIMED_STR));
            Assert.That(HumbleKeysLibrary.PAST_TAGS, Does.Contain("Key: Expired"));
            Assert.That(HumbleKeysLibrary.PAST_TAGS, Does.Contain("Expired"));
        }

        #endregion
    }
}
