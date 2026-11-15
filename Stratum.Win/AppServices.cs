// Copyright (C) 2026 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Threading.Tasks;
using Stratum.Core;
using Stratum.Core.Comparer;
using Stratum.Core.Entity;
using Stratum.Core.Persistence;
using Stratum.Core.Service;
using Stratum.Core.Service.Impl;
using Stratum.Win.Data;
using Stratum.Win.Data.Persistence;
using Stratum.Win.Data.Persistence.View;
using Stratum.Win.Data.Persistence.View.Impl;
using Stratum.Win.Services;

namespace Stratum.Win
{
    /// <summary>
    /// Tiny composition root: wires the ported data layer to the Core services.
    /// </summary>
    public static class AppServices
    {
        private static DpapiSecretStore _secretStore;

        public static Database Database { get; private set; }

        public static DatabaseSession DatabaseSession { get; private set; }

        public static IAuthenticatorRepository AuthenticatorRepository { get; private set; }

        public static ICategoryRepository CategoryRepository { get; private set; }

        public static IAuthenticatorCategoryRepository AuthenticatorCategoryRepository { get; private set; }

        public static ICustomIconRepository CustomIconRepository { get; private set; }

        public static IAuthenticatorService AuthenticatorService { get; private set; }

        public static ICategoryService CategoryService { get; private set; }

        public static ICustomIconService CustomIconService { get; private set; }

        public static IBackupService BackupService { get; private set; }

        public static IRestoreService RestoreService { get; private set; }

        public static IImportService ImportService { get; private set; }

        public static IAuthenticatorView AuthenticatorView { get; private set; }

        public static IDefaultIconView DefaultIconView { get; private set; }

        public static ICustomIconDecoder CustomIconDecoder { get; private set; }

        public static bool IsInitialized { get; private set; }

        private static Task _initTask;

        /// <summary>Idempotent initialization shared by the UI.</summary>
        public static Task Initializer => _initTask ??= InitializeAsync();

        public static async Task InitializeAsync()
        {
            if (IsInitialized)
            {
                return;
            }

            SQLitePCL.Batteries_V2.Init();

            Database = new Database();
            DatabaseSession = new DatabaseSession(Database);

            _secretStore = new DpapiSecretStore(AppPaths.DatabaseSecretPath);
            var password = _secretStore.LoadOrCreate();

            await DatabaseSession.UnlockAsync(password, Database.Origin.Application);

            AuthenticatorRepository = new AuthenticatorRepository(Database);
            CategoryRepository = new CategoryRepository(Database);
            AuthenticatorCategoryRepository = new AuthenticatorCategoryRepository(Database);
            CustomIconRepository = new CustomIconRepository(Database);

            var assetProvider = new FileAssetProvider();

            CustomIconDecoder = new CustomIconDecoder();

            CustomIconService = new CustomIconService(CustomIconRepository, AuthenticatorRepository);
            AuthenticatorService = new AuthenticatorService(AuthenticatorRepository,
                AuthenticatorCategoryRepository, CustomIconService, new AuthenticatorComparer());
            CategoryService = new CategoryService(CategoryRepository, AuthenticatorCategoryRepository,
                new CategoryComparer(), new AuthenticatorCategoryComparer());
            BackupService = new BackupService(AuthenticatorRepository, CategoryRepository,
                AuthenticatorCategoryRepository, CustomIconRepository, assetProvider);
            RestoreService = new RestoreService(AuthenticatorService, CategoryService, CustomIconService);
            ImportService = new ImportService(RestoreService);

            AuthenticatorView = new AuthenticatorView(AuthenticatorRepository, AuthenticatorCategoryRepository);
            DefaultIconView = new DefaultIconView();

            await AuthenticatorView.LoadFromPersistenceAsync();

            IsInitialized = true;
        }
    }
}
