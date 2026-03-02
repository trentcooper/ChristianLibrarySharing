IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [Roles] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Users] (
    [Id] nvarchar(450) NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [LastLoginAt] datetime2 NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [RoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_RoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RoleClaims_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Books] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(500) NOT NULL,
    [Author] nvarchar(300) NOT NULL,
    [ISBN] nvarchar(20) NULL,
    [Publisher] nvarchar(200) NULL,
    [PublicationYear] int NULL,
    [Genre] nvarchar(100) NULL,
    [Condition] int NOT NULL,
    [Description] nvarchar(2000) NULL,
    [CoverImageUrl] nvarchar(500) NULL,
    [PageCount] int NULL,
    [Language] nvarchar(50) NULL DEFAULT N'English',
    [IsAvailable] bit NOT NULL DEFAULT CAST(1 AS bit),
    [IsVisible] bit NOT NULL DEFAULT CAST(1 AS bit),
    [OwnerId] nvarchar(450) NOT NULL,
    [OwnerNotes] nvarchar(1000) NULL,
    [BorrowCount] int NOT NULL DEFAULT 0,
    [AverageRating] decimal(3,2) NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [UpdatedAt] datetime2 NULL,
    [CreatedBy] nvarchar(max) NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DeletedAt] datetime2 NULL,
    CONSTRAINT [PK_Books] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Books_Users_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Messages] (
    [Id] int NOT NULL IDENTITY,
    [SenderId] nvarchar(450) NOT NULL,
    [RecipientId] nvarchar(450) NOT NULL,
    [Subject] nvarchar(200) NULL,
    [Content] nvarchar(max) NOT NULL,
    [IsRead] bit NOT NULL DEFAULT CAST(0 AS bit),
    [ReadAt] datetime2 NULL,
    [BookId] int NULL,
    [BorrowRequestId] int NULL,
    [LoanId] int NULL,
    [SenderDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [RecipientDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [UpdatedAt] datetime2 NULL,
    [CreatedBy] nvarchar(max) NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DeletedAt] datetime2 NULL,
    CONSTRAINT [PK_Messages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Messages_Users_RecipientId] FOREIGN KEY ([RecipientId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Messages_Users_SenderId] FOREIGN KEY ([SenderId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Notifications] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [Type] int NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Message] nvarchar(1000) NOT NULL,
    [IsRead] bit NOT NULL DEFAULT CAST(0 AS bit),
    [ReadAt] datetime2 NULL,
    [ActionUrl] nvarchar(500) NULL,
    [BookId] int NULL,
    [BorrowRequestId] int NULL,
    [LoanId] int NULL,
    [MessageId] int NULL,
    [Priority] int NOT NULL DEFAULT 2,
    [EmailSent] bit NOT NULL DEFAULT CAST(0 AS bit),
    [SmsSent] bit NOT NULL DEFAULT CAST(0 AS bit),
    [PushSent] bit NOT NULL DEFAULT CAST(0 AS bit),
    [ExpiresAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [UpdatedAt] datetime2 NULL,
    [CreatedBy] nvarchar(max) NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DeletedAt] datetime2 NULL,
    CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Notifications_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [UserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_UserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserClaims_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [UserLogins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_UserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_UserLogins_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [UserProfiles] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [FirstName] nvarchar(100) NOT NULL,
    [LastName] nvarchar(100) NOT NULL,
    [Bio] nvarchar(1000) NULL,
    [PhoneNumber] nvarchar(20) NULL,
    [DateOfBirth] datetime2 NULL,
    [ProfilePictureUrl] nvarchar(500) NULL,
    [Street] nvarchar(200) NULL,
    [City] nvarchar(100) NULL,
    [State] nvarchar(100) NULL,
    [ZipCode] nvarchar(20) NULL,
    [Country] nvarchar(100) NULL,
    [Latitude] decimal(10,7) NULL,
    [Longitude] decimal(10,7) NULL,
    [ChurchName] nvarchar(200) NULL,
    [ChurchLocation] nvarchar(200) NULL,
    [Visibility] int NOT NULL,
    [ShowFullName] bit NOT NULL DEFAULT CAST(1 AS bit),
    [ShowEmail] bit NOT NULL DEFAULT CAST(0 AS bit),
    [ShowPhone] bit NOT NULL DEFAULT CAST(0 AS bit),
    [ShowExactAddress] bit NOT NULL DEFAULT CAST(0 AS bit),
    [ShowCityState] bit NOT NULL DEFAULT CAST(1 AS bit),
    [ShowDateOfBirth] bit NOT NULL,
    [EmailNotifications] bit NOT NULL DEFAULT CAST(1 AS bit),
    [SmsNotifications] bit NOT NULL DEFAULT CAST(0 AS bit),
    [PushNotifications] bit NOT NULL DEFAULT CAST(1 AS bit),
    [NotificationFrequency] int NOT NULL DEFAULT 1,
    [NotifyOnBorrowRequest] bit NOT NULL,
    [NotifyOnRequestApproval] bit NOT NULL,
    [NotifyOnRequestDenial] bit NOT NULL,
    [NotifyOnDueDate] bit NOT NULL,
    [NotifyOnReturn] bit NOT NULL,
    [NotifyOnNewMessage] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [UpdatedAt] datetime2 NULL,
    [CreatedBy] nvarchar(max) NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DeletedAt] datetime2 NULL,
    CONSTRAINT [PK_UserProfiles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserProfiles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [UserRoles] (
    [UserId] nvarchar(450) NOT NULL,
    [RoleId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_UserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [UserTokens] (
    [UserId] nvarchar(450) NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_UserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_UserTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [BorrowRequests] (
    [Id] int NOT NULL IDENTITY,
    [BookId] int NOT NULL,
    [BorrowerId] nvarchar(450) NOT NULL,
    [LenderId] nvarchar(450) NOT NULL,
    [Status] int NOT NULL,
    [RequestedStartDate] datetime2 NOT NULL,
    [RequestedEndDate] datetime2 NOT NULL,
    [Message] nvarchar(1000) NULL,
    [ResponseMessage] nvarchar(1000) NULL,
    [RespondedAt] datetime2 NULL,
    [ExpiresAt] datetime2 NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [UpdatedAt] datetime2 NULL,
    [CreatedBy] nvarchar(max) NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DeletedAt] datetime2 NULL,
    CONSTRAINT [PK_BorrowRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BorrowRequests_Books_BookId] FOREIGN KEY ([BookId]) REFERENCES [Books] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_BorrowRequests_Users_BorrowerId] FOREIGN KEY ([BorrowerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_BorrowRequests_Users_LenderId] FOREIGN KEY ([LenderId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Loans] (
    [Id] int NOT NULL IDENTITY,
    [BookId] int NOT NULL,
    [BorrowerId] nvarchar(450) NOT NULL,
    [LenderId] nvarchar(450) NOT NULL,
    [BorrowRequestId] int NULL,
    [Status] int NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [DueDate] datetime2 NOT NULL,
    [ReturnedDate] datetime2 NULL,
    [ExtensionDays] int NOT NULL DEFAULT 0,
    [ExtensionRequested] bit NOT NULL DEFAULT CAST(0 AS bit),
    [RemindersSent] int NOT NULL DEFAULT 0,
    [LenderNotes] nvarchar(1000) NULL,
    [BorrowerNotes] nvarchar(1000) NULL,
    [ReturnCondition] int NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [UpdatedAt] datetime2 NULL,
    [CreatedBy] nvarchar(max) NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DeletedAt] datetime2 NULL,
    CONSTRAINT [PK_Loans] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Loans_Books_BookId] FOREIGN KEY ([BookId]) REFERENCES [Books] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Loans_BorrowRequests_BorrowRequestId] FOREIGN KEY ([BorrowRequestId]) REFERENCES [BorrowRequests] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Loans_Users_BorrowerId] FOREIGN KEY ([BorrowerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Loans_Users_LenderId] FOREIGN KEY ([LenderId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO

CREATE INDEX [IX_Books_Author] ON [Books] ([Author]);
GO

CREATE INDEX [IX_Books_Genre] ON [Books] ([Genre]);
GO

CREATE INDEX [IX_Books_IsAvailable] ON [Books] ([IsAvailable]);
GO

CREATE INDEX [IX_Books_ISBN] ON [Books] ([ISBN]);
GO

CREATE INDEX [IX_Books_IsDeleted] ON [Books] ([IsDeleted]);
GO

CREATE INDEX [IX_Books_IsVisible] ON [Books] ([IsVisible]);
GO

CREATE INDEX [IX_Books_OwnerId] ON [Books] ([OwnerId]);
GO

CREATE INDEX [IX_Books_Title] ON [Books] ([Title]);
GO

CREATE INDEX [IX_Books_Title_Author] ON [Books] ([Title], [Author]);
GO

CREATE INDEX [IX_BorrowRequests_BookId] ON [BorrowRequests] ([BookId]);
GO

CREATE INDEX [IX_BorrowRequests_BorrowerId] ON [BorrowRequests] ([BorrowerId]);
GO

CREATE INDEX [IX_BorrowRequests_ExpiresAt] ON [BorrowRequests] ([ExpiresAt]);
GO

CREATE INDEX [IX_BorrowRequests_IsDeleted] ON [BorrowRequests] ([IsDeleted]);
GO

CREATE INDEX [IX_BorrowRequests_LenderId] ON [BorrowRequests] ([LenderId]);
GO

CREATE INDEX [IX_BorrowRequests_RequestedStartDate] ON [BorrowRequests] ([RequestedStartDate]);
GO

CREATE INDEX [IX_BorrowRequests_Status] ON [BorrowRequests] ([Status]);
GO

CREATE INDEX [IX_Loans_BookId] ON [Loans] ([BookId]);
GO

CREATE INDEX [IX_Loans_BorrowerId] ON [Loans] ([BorrowerId]);
GO

CREATE UNIQUE INDEX [IX_Loans_BorrowRequestId] ON [Loans] ([BorrowRequestId]) WHERE [BorrowRequestId] IS NOT NULL;
GO

CREATE INDEX [IX_Loans_DueDate] ON [Loans] ([DueDate]);
GO

CREATE INDEX [IX_Loans_IsDeleted] ON [Loans] ([IsDeleted]);
GO

CREATE INDEX [IX_Loans_LenderId] ON [Loans] ([LenderId]);
GO

CREATE INDEX [IX_Loans_ReturnedDate] ON [Loans] ([ReturnedDate]);
GO

CREATE INDEX [IX_Loans_StartDate] ON [Loans] ([StartDate]);
GO

CREATE INDEX [IX_Loans_Status] ON [Loans] ([Status]);
GO

CREATE INDEX [IX_Messages_BookId] ON [Messages] ([BookId]);
GO

CREATE INDEX [IX_Messages_BorrowRequestId] ON [Messages] ([BorrowRequestId]);
GO

CREATE INDEX [IX_Messages_IsDeleted] ON [Messages] ([IsDeleted]);
GO

CREATE INDEX [IX_Messages_IsRead] ON [Messages] ([IsRead]);
GO

CREATE INDEX [IX_Messages_LoanId] ON [Messages] ([LoanId]);
GO

CREATE INDEX [IX_Messages_RecipientId] ON [Messages] ([RecipientId]);
GO

CREATE INDEX [IX_Messages_SenderId] ON [Messages] ([SenderId]);
GO

CREATE INDEX [IX_Messages_SenderId_RecipientId_CreatedAt] ON [Messages] ([SenderId], [RecipientId], [CreatedAt]);
GO

CREATE INDEX [IX_Notifications_BookId] ON [Notifications] ([BookId]);
GO

CREATE INDEX [IX_Notifications_BorrowRequestId] ON [Notifications] ([BorrowRequestId]);
GO

CREATE INDEX [IX_Notifications_ExpiresAt] ON [Notifications] ([ExpiresAt]);
GO

CREATE INDEX [IX_Notifications_IsDeleted] ON [Notifications] ([IsDeleted]);
GO

CREATE INDEX [IX_Notifications_IsRead] ON [Notifications] ([IsRead]);
GO

CREATE INDEX [IX_Notifications_LoanId] ON [Notifications] ([LoanId]);
GO

CREATE INDEX [IX_Notifications_MessageId] ON [Notifications] ([MessageId]);
GO

CREATE INDEX [IX_Notifications_Priority] ON [Notifications] ([Priority]);
GO

CREATE INDEX [IX_Notifications_Type] ON [Notifications] ([Type]);
GO

CREATE INDEX [IX_Notifications_UserId] ON [Notifications] ([UserId]);
GO

CREATE INDEX [IX_Notifications_UserId_IsRead_CreatedAt] ON [Notifications] ([UserId], [IsRead], [CreatedAt]);
GO

CREATE INDEX [IX_RoleClaims_RoleId] ON [RoleClaims] ([RoleId]);
GO

CREATE UNIQUE INDEX [RoleNameIndex] ON [Roles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;
GO

CREATE INDEX [IX_UserClaims_UserId] ON [UserClaims] ([UserId]);
GO

CREATE INDEX [IX_UserLogins_UserId] ON [UserLogins] ([UserId]);
GO

CREATE INDEX [IX_UserProfiles_ChurchName] ON [UserProfiles] ([ChurchName]);
GO

CREATE INDEX [IX_UserProfiles_City] ON [UserProfiles] ([City]);
GO

CREATE INDEX [IX_UserProfiles_Latitude_Longitude] ON [UserProfiles] ([Latitude], [Longitude]);
GO

CREATE INDEX [IX_UserProfiles_State] ON [UserProfiles] ([State]);
GO

CREATE UNIQUE INDEX [IX_UserProfiles_UserId] ON [UserProfiles] ([UserId]);
GO

CREATE INDEX [IX_UserRoles_RoleId] ON [UserRoles] ([RoleId]);
GO

CREATE INDEX [EmailIndex] ON [Users] ([NormalizedEmail]);
GO

CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]) WHERE [Email] IS NOT NULL;
GO

CREATE INDEX [IX_Users_IsActive] ON [Users] ([IsActive]);
GO

CREATE INDEX [IX_Users_IsDeleted] ON [Users] ([IsDeleted]);
GO

CREATE UNIQUE INDEX [IX_Users_UserName] ON [Users] ([UserName]) WHERE [UserName] IS NOT NULL;
GO

CREATE UNIQUE INDEX [UserNameIndex] ON [Users] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260115204757_InitialCreate', N'8.0.22');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [UserProfiles] ADD [ProfilePictureThumbnailUrl] nvarchar(500) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260116021353_AddProfilePictureThumbnailUrl', N'8.0.22');
GO

COMMIT;
GO

