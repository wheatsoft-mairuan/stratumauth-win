# Stratum for Windows

*Note: This warehouse is not yet fully completed.*
A Windows desktop port of [Stratum](https://github.com/stratumauth/app), the open source
two-factor authentication app for Android.

The port keeps the original engine: authenticators, generators, categories, icon handling
and backup converters come straight from `Stratum.Core` and the portable part of the Android
data layer. Only the UI shell is new (WPF).

## Features

- TOTP, HOTP, Steam, Yandex and mOTP codes, SHA-1 / SHA-256 / SHA-512, 6-8 digits
- Dark, keyboard friendly list with live countdown rings and click-to-copy
- Search by issuer or username, categories in a sidebar, custom ordering
- ~1,060 bundled service icons, automatic icon detection by issuer name, custom icons from
  any image file
- Import from Stratum backup (`.stratum`), Aegis, Bitwarden, Ente Auth, 2FAS, KeePass,
  LastPass, Proton, TOTP Authenticator, WinAuth, FreeOTP(+), andOTP, Authenticator Plus,
  Google Authenticator export QR, HTML backups and URI lists
- Export to an encrypted `.stratum` backup (Argon2id + AES-256-GCM), an HTML page or a URI list

## Backups are compatible with Android

The `.stratum` format is the one Stratum for Android reads and writes, so a backup made on
Windows can be restored on Android and vice versa. Encrypted backups ask for the password
when importing.

## Where the data lives

```
%APPDATA%\Stratum\
    authenticator.db3     SQLCipher database (same schema as Android)
    database.key          database passphrase, protected with Windows DPAPI (current user)
    settings.json         preferences
    Logs\stratum-*.log
```

Set the `STRATUM_DATA_DIR` environment variable to use a different directory (handy for
testing against throw-away data).

Because the database passphrase is sealed with DPAPI, the app unlocks itself for the Windows
user who created it. Other accounts on the same machine cannot decrypt it.

## Building

Requires the .NET 10 SDK (Windows) and network access for the NuGet packages.

```
dotnet build Stratum.Desktop.sln
dotnet run --project Stratum.Win\Stratum.Win.csproj
```

`Stratum.Desktop.sln` contains only the desktop projects (`Stratum.Core`, `Stratum.Win`,
`Stratum.Test`). The original `Stratum.sln` still holds the Android projects and needs the
Android workload.

Publish a self-contained-ish single folder build:

```
dotnet publish Stratum.Win\Stratum.Win.csproj -c Release -r win-x64 --self-contained false -o publish
```

## Tests

`Stratum.Test` is the original Core test suite, retargeted to `net10.0`. It passes in full
against the ported engine (backup converters, generators, URI parsing, encryption).

```
dotnet test Stratum.Desktop.sln
```

The Core library is verified on every change; the UI is checked with rendered screenshots
(`Stratum.exe --smoke <png>`, `--smoke-dialog <editor|uri|import|backup|icons|edit|settings|assign> <png>`).

## Security notes

- Codes are computed locally; nothing leaves the machine except the optional HTML/URI exports
  you create yourself.
- The clipboard is used when you copy a code; Windows clipboard history may keep a copy.
- Screenshots of the app are not blocked (unlike Android, where the port keeps the
  "no screenshots" behaviour of the flags you set).

## Licence

GPL-3.0, like the upstream project. See `LICENSE`.
