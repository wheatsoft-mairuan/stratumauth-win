// Copyright (C) 2026 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using Stratum.Core.Converter;

namespace Stratum.Win.Services
{
    /// <summary>One importable third-party backup format.</summary>
    public sealed class ImportSource
    {
        public ImportSource(string name, Func<BackupConverter> create)
        {
            Name = name;
            Create = create;
        }

        public string Name { get; }

        public Func<BackupConverter> Create { get; }
    }

    public static class ImportSources
    {
        /// <summary>Order matters for automatic detection: more specific formats first.</summary>
        public static IReadOnlyList<ImportSource> All { get; } = new List<ImportSource>
        {
            new("Aegis", () => new AegisBackupConverter(IconCatalog.Instance, AppServices.CustomIconDecoder)),
            new("Bitwarden", () => new BitwardenBackupConverter(IconCatalog.Instance)),
            new("Ente Auth", () => new EnteAuthBackupConverter(IconCatalog.Instance)),
            new("2FAS", () => new TwoFasBackupConverter(IconCatalog.Instance)),
            new("KeePass", () => new KeePassBackupConverter(IconCatalog.Instance)),
            new("LastPass", () => new LastPassBackupConverter(IconCatalog.Instance)),
            new("Proton Authenticator", () => new ProtonAuthenticatorBackupConverter(IconCatalog.Instance)),
            new("TOTP Authenticator", () => new TotpAuthenticatorBackupConverter(IconCatalog.Instance)),
            new("WinAuth", () => new WinAuthBackupConverter(IconCatalog.Instance)),
            new("FreeOTP+", () => new FreeOtpPlusBackupConverter(IconCatalog.Instance)),
            new("FreeOTP", () => new FreeOtpBackupConverter(IconCatalog.Instance)),
            new("andOTP", () => new AndOtpBackupConverter(IconCatalog.Instance)),
            new("Authenticator Plus", () => new AuthenticatorPlusBackupConverter(IconCatalog.Instance)),
            new("Google Authenticator（迁移码）", () => new GoogleAuthenticatorBackupConverter(IconCatalog.Instance)),
            new("HTML 备份", () => new HtmlBackupConverter(IconCatalog.Instance)),
            new("URI 列表", () => new UriListBackupConverter(IconCatalog.Instance))
        };
    }
}
