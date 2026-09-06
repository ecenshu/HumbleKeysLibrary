## What's Changed
# 0.4.1
* Display a progress bar in the sidebar to convey the status of scraping orders from Humble API
* Cancelling during scraping is now possible
* [OPTIONAL] Keys that are expirable will be tagged with 'Key: Expirable' and when detected during a scan will add a notification of how long until the key expires
* [OPTIONAL] Keys that have been redeemed can be checked against the Steam Library plugin to see if the redeemed key has actually been redeemed, if it hasn't a notification will display during a library scan
* Any game keys that have not been redeemed and sold out will now display a notification during a library scan
* Game Added date set to Humble purchase/order date to maintain sort order
* [Bugfix] Game entries with empty notes were not getting the expiry note added
* Refined key status taxonomy into two orthogonal dimensions:
  * Base States: 'Key: Redeemed' (key revealed & in Steam library), 'Key: Unredeemed' (key revealed & not in Steam), 'Key: Claimed' (purchased/claimed into Humble account, key not yet revealed), and 'Key: Unclaimed' (virtual Choice bundle option, no key revealed).
  * Lifecycle Modifiers: 'Key: Expirable' (for active expirable keys) and 'Key: Unredeemable' (for expired keys or exhausted choices) applied in conjunction with the base state.
* Unredeemable games that already exist in the Steam library receive both 'Key: Redeemed' and 'Key: Unredeemable', preventing them from being deleted or flagged as no longer redeemable.

# 0.4.0
* Fixed issue where games in the Exclusion List were being re-imported

# 0.3.10
* Local database stores order data, if an order has been completely redeemed, do not contact humble for updates

# 0.3.9
* IMPORTANT - Platform field is no longer used by default for the Redemption Store (i.e. Steam, GOG, etc.), but now Source is (helps some metadata plugins properly match games)
* Added dropdown setting to add Redemption Store (e.g. Steam) to either Source (now default), Tag, Category, or Platform (no longer default) field, or None (disabled)
* Added checkbox setting to add Key Redemption status Tag (default enabled)
* Added checkbox setting to "Add Humble & Steam links" (default enabled)
* Added checkbox setting to add "Nintendo Switch" to Platform for all Nintendo keys
* Added checkbox setting to add default of "PC (Windows)" to Platform for all other keys
* Updated settings UI to be more compact and add the above features
* Fixed a couple misc. bugs related to key redemption tags and tag methodology
* Added more logging to help debug excessive library update time reported by some users

# 0.3.8
* Restored plugin name
* Restored plugin GUID to fix broken auto-update process from old versions, prevent duplicate old & new plugins installed at the same time, and ensure "Already installed" button works properly in Add-on Browser

# 0.3.6
* Added support for multiple languages (currently only English is implemented, but other languages can now be added)
* Language is determined by Windows culture (may add a setting for it later)
* Fixed missing "Connect account" description next to checkbox
* Fixed missing "Authenticate" button text
* Now shows authentication status next to the button like other library add-ons

# 0.3.4
* Altered how tags are handled to deal with scenario where tags get removed manually via Manage Library function of Playnite
* Corrected tooltips for Unredeemable key handling
* Remove prerequisite "Import Choice Game" from "Unredeemable key handling" options
* Correct github action to build against correct tag version
* Update ChoiceMonth model to include ChoicesRemaining and ChoicesMade
* Update Order model to determine virtual orders (items added from Bundle instead of from persisted record on server)
* Alter HumbleKeysAccountClient to add virtual orders that have not yet been added to the Order
* Add additional logic to HumbleKeysLibrary to handle unredeemable virtual orders (either expired and cannot be redeemed or part of a Bundle where all choices have been made)
* Add new option to allow for either tagging a Game as Key "Unredeemable" or not add to the library
* Correct version number to match release version
* Added Optional feature to import games in Humble Choice Monthly bundles.
* Added Optional feature to create tags based on Bundle Names (Either all Bundles or Monthly only)
* Added Optional feature to cache API Objects as JSON files in the ExtensionsData directory

# 0.2.0
Updated for new SDK. Also fixes Newtonsoft.Json exceptions thrown when Humble API returns **redeemed_key_val**
as a JObject instead of JString.

Tested against:
Playnite 9.18
SDK 6.2.2
Desktop 2.1.0

# 0.1.4
Adds **Ignore Redeemed Keys** setting. When checked, the library does not import any keys which have
already been redeemed.

# 0.1.3
Compiled for Playnite 8.0

Removes references to Playnite and Playnite.Common assemblies to comply with SDK changes in Playnite 8.

# 0.1.2
Compiled for Playnite 7.9

Improves key type filtering and adds settings view localization. Also changes "Platform" for keys to reflect the
TPKD machine name, as the human names were too inconsistent and creating many single-game platforms.

# 0.1.1
Adds **nintendo_direct** tag to Humble key_type whitelist and removes extraneous **Humble Key: {key_type}** tagging,
since that information is already in "Platform".


# 0.1.0 Pre-release
Release v0.1.0
Installation
Drag and drop the .pext file onto your Playnite window.

Release Details
First release implements:

* Querying the Humble API for "TPKD" objects that represent game keys.
* Checking against a key type whitelist (currently only permits steam keys)
* Reporting the Redeemed / Unredeemed status as tags
* Updates Redeemed / Unredeemed on previously imported games when loading the Humble Keys Library
* Creates a link to your Humble "downloads" page

Upcoming features:

* Other key types in the white list
* Integrating localization