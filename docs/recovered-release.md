# Recovered V3 release

This release restores the locally preserved V3 application and the pricing fixes recorded in the August 13–14, 2026 VPS deployment conversations.

## Included behavior

- Product prices accept dot or comma decimal input with up to three decimal places.
- Editing sale TTC preserves purchase prices and updates sale HT and margin.
- SQL Server stores purchase HT and sale TTC as `decimal(18,3)`.
- The recovered frontend includes theme initialization, print refinements, and case-correct static asset paths for Linux.
- Invoice scanning uses the recovered Gemini integration. Configure its key privately through `Gemini__ApiKey`; no working provider key is included.
- Portable Docker configuration and installation documentation are included with placeholder settings.

## Compatibility with existing GitHub installations

The repository keeps its existing `20260808130705_RemoveUnusedProduitsBrouillon` migration, whose table removal is already conditional. The older V3 migration with a different name is intentionally not copied: an installation that already removed the table can safely apply the conditional repository migration without dropping a missing table. Existing GitHub request-validator files are retained.

The three-decimal migration keeps its original deployed ID, `20260813154723_AllowThreeDecimalProductPrices`, so a database that already applied it does not repeat that change. Back up the target database before an upgrade. Do not downgrade the price columns to two decimals without reviewing the resulting precision loss.

## Verification and limits

On October 28, 2026, the recovered release was built, tested, published, and run against a separate local SQL Server preview database. Verification covered login, product pricing, real decimal keystrokes, saving and reopening three-decimal values, frontend asset paths, and published-file packaging.

The VPS SSH endpoint and production domain were unreachable during recovery. This is a recovery from the local V3 source and recorded deployment history, not a verified export of the server's current filesystem. AI invoice extraction requires a separately configured provider key and was not called during local validation. Existing NuGet audit warnings for the transitive `System.Security.Cryptography.Pkcs` dependency remain; dependency upgrades are outside this release-recovery change.

Production credentials, `.env`, development configuration, database contents, backups, build output, and server-specific configuration are excluded from Git.
