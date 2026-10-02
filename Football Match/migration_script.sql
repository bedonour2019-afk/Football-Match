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
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916144100_InitialClean'
)
BEGIN
    CREATE TABLE [Attendances] (
        [Id] int NOT NULL IDENTITY,
        [PhoneNumber] nvarchar(max) NOT NULL,
        [FriendName] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [Note] nvarchar(max) NULL,
        [ProfilePicturePath] nvarchar(max) NULL,
        [RespondedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Attendances] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916144100_InitialClean'
)
BEGIN
    CREATE TABLE [ChatMessages] (
        [Id] int NOT NULL IDENTITY,
        [AttendanceId] int NOT NULL,
        [Content] nvarchar(max) NULL,
        [MediaPath] nvarchar(max) NULL,
        [MediaType] nvarchar(max) NULL,
        [SentAt] datetime2 NOT NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_ChatMessages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ChatMessages_Attendances_AttendanceId] FOREIGN KEY ([AttendanceId]) REFERENCES [Attendances] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916144100_InitialClean'
)
BEGIN
    CREATE TABLE [MessageReactions] (
        [Id] int NOT NULL IDENTITY,
        [ChatMessageId] int NOT NULL,
        [AttendanceId] int NOT NULL,
        [ReactionType] nvarchar(max) NOT NULL,
        [ChatMessageId1] int NULL,
        CONSTRAINT [PK_MessageReactions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MessageReactions_Attendances_AttendanceId] FOREIGN KEY ([AttendanceId]) REFERENCES [Attendances] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_MessageReactions_ChatMessages_ChatMessageId] FOREIGN KEY ([ChatMessageId]) REFERENCES [ChatMessages] ([Id]),
        CONSTRAINT [FK_MessageReactions_ChatMessages_ChatMessageId1] FOREIGN KEY ([ChatMessageId1]) REFERENCES [ChatMessages] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916144100_InitialClean'
)
BEGIN
    CREATE INDEX [IX_ChatMessages_AttendanceId] ON [ChatMessages] ([AttendanceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916144100_InitialClean'
)
BEGIN
    CREATE INDEX [IX_MessageReactions_AttendanceId] ON [MessageReactions] ([AttendanceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916144100_InitialClean'
)
BEGIN
    CREATE INDEX [IX_MessageReactions_ChatMessageId] ON [MessageReactions] ([ChatMessageId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916144100_InitialClean'
)
BEGIN
    CREATE INDEX [IX_MessageReactions_ChatMessageId1] ON [MessageReactions] ([ChatMessageId1]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916144100_InitialClean'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260916144100_InitialClean', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929205053_BigFeatureUpdate'
)
BEGIN
    ALTER TABLE [MessageReactions] DROP CONSTRAINT [FK_MessageReactions_Attendances_AttendanceId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929205053_BigFeatureUpdate'
)
BEGIN
    ALTER TABLE [MessageReactions] DROP CONSTRAINT [FK_MessageReactions_ChatMessages_ChatMessageId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929205053_BigFeatureUpdate'
)
BEGIN
    ALTER TABLE [MessageReactions] DROP CONSTRAINT [FK_MessageReactions_ChatMessages_ChatMessageId1];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929205053_BigFeatureUpdate'
)
BEGIN
    DROP INDEX [IX_MessageReactions_ChatMessageId1] ON [MessageReactions];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929205053_BigFeatureUpdate'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MessageReactions]') AND [c].[name] = N'ChatMessageId1');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [MessageReactions] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [MessageReactions] DROP COLUMN [ChatMessageId1];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929205053_BigFeatureUpdate'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MessageReactions]') AND [c].[name] = N'ReactionType');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [MessageReactions] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [MessageReactions] ALTER COLUMN [ReactionType] nvarchar(20) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929205053_BigFeatureUpdate'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ChatMessages]') AND [c].[name] = N'MediaType');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [ChatMessages] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [ChatMessages] ALTER COLUMN [MediaType] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929205053_BigFeatureUpdate'
)
BEGIN
    DECLARE @var3 nvarchar(max);
    SELECT @var3 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ChatMessages]') AND [c].[name] = N'MediaPath');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [ChatMessages] DROP CONSTRAINT ' + @var3 + ';');
    ALTER TABLE [ChatMessages] ALTER COLUMN [MediaPath] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929205053_BigFeatureUpdate'
)
BEGIN
    DECLARE @var4 nvarchar(max);
    SELECT @var4 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ChatMessages]') AND [c].[name] = N'Content');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [ChatMessages] DROP CONSTRAINT ' + @var4 + ';');
    ALTER TABLE [ChatMessages] ALTER COLUMN [Content] nvarchar(2000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929205053_BigFeatureUpdate'
)
BEGIN
    DECLARE @var5 nvarchar(max);
    SELECT @var5 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Attendances]') AND [c].[name] = N'PhoneNumber');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Attendances] DROP CONSTRAINT ' + @var5 + ';');
    ALTER TABLE [Attendances] ALTER COLUMN [PhoneNumber] nvarchar(20) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929205053_BigFeatureUpdate'
)
BEGIN
    DECLARE @var6 nvarchar(max);
    SELECT @var6 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Attendances]') AND [c].[name] = N'FriendName');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Attendances] DROP CONSTRAINT ' + @var6 + ';');
    ALTER TABLE [Attendances] ALTER COLUMN [FriendName] nvarchar(100) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929205053_BigFeatureUpdate'
)
BEGIN
    ALTER TABLE [Attendances] ADD [Bio] nvarchar(300) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929205053_BigFeatureUpdate'
)
BEGIN
    ALTER TABLE [Attendances] ADD [CoverPhotoPath] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929205053_BigFeatureUpdate'
)
BEGIN
    ALTER TABLE [Attendances] ADD [IsMVP] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929205053_BigFeatureUpdate'
)
BEGIN
    ALTER TABLE [Attendances] ADD [Nickname] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929205053_BigFeatureUpdate'
)
BEGIN
    ALTER TABLE [Attendances] ADD [PlayerPosition] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929205053_BigFeatureUpdate'
)
BEGIN
    ALTER TABLE [Attendances] ADD [PlayerRating] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929205053_BigFeatureUpdate'
)
BEGIN
    ALTER TABLE [Attendances] ADD [PlayerTag] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929205053_BigFeatureUpdate'
)
BEGIN
    ALTER TABLE [Attendances] ADD [TeamNumber] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929205053_BigFeatureUpdate'
)
BEGIN
    CREATE TABLE [Posts] (
        [Id] int NOT NULL IDENTITY,
        [AttendanceId] int NOT NULL,
        [Content] nvarchar(2000) NULL,
        [MediaPath] nvarchar(500) NULL,
        [MediaType] nvarchar(50) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [IsDeleted] bit NOT NULL,
        [IsAdminPost] bit NOT NULL,
        CONSTRAINT [PK_Posts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Posts_Attendances_AttendanceId] FOREIGN KEY ([AttendanceId]) REFERENCES [Attendances] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929205053_BigFeatureUpdate'
)
BEGIN
    CREATE TABLE [PushSubscribers] (
        [Id] int NOT NULL IDENTITY,
        [AttendanceId] int NULL,
        [Endpoint] nvarchar(1000) NOT NULL,
        [P256dh] nvarchar(255) NOT NULL,
        [Auth] nvarchar(255) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_PushSubscribers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PushSubscribers_Attendances_AttendanceId] FOREIGN KEY ([AttendanceId]) REFERENCES [Attendances] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929205053_BigFeatureUpdate'
)
BEGIN
    CREATE TABLE [Stories] (
        [Id] int NOT NULL IDENTITY,
        [AttendanceId] int NOT NULL,
        [Content] nvarchar(500) NULL,
        [MediaPath] nvarchar(500) NULL,
        [MediaType] nvarchar(50) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Stories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Stories_Attendances_AttendanceId] FOREIGN KEY ([AttendanceId]) REFERENCES [Attendances] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929205053_BigFeatureUpdate'
)
BEGIN
    CREATE TABLE [PostComments] (
        [Id] int NOT NULL IDENTITY,
        [PostId] int NOT NULL,
        [AttendanceId] int NOT NULL,
        [Content] nvarchar(1000) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_PostComments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PostComments_Attendances_AttendanceId] FOREIGN KEY ([AttendanceId]) REFERENCES [Attendances] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PostComments_Posts_PostId] FOREIGN KEY ([PostId]) REFERENCES [Posts] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929205053_BigFeatureUpdate'
)
BEGIN
    CREATE TABLE [PostReactions] (
        [Id] int NOT NULL IDENTITY,
        [PostId] int NOT NULL,
        [AttendanceId] int NOT NULL,
        [ReactionType] nvarchar(20) NOT NULL,
        CONSTRAINT [PK_PostReactions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PostReactions_Attendances_AttendanceId] FOREIGN KEY ([AttendanceId]) REFERENCES [Attendances] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PostReactions_Posts_PostId] FOREIGN KEY ([PostId]) REFERENCES [Posts] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929205053_BigFeatureUpdate'
)
BEGIN
    CREATE INDEX [IX_PostComments_AttendanceId] ON [PostComments] ([AttendanceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929205053_BigFeatureUpdate'
)
BEGIN
    CREATE INDEX [IX_PostComments_PostId] ON [PostComments] ([PostId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929205053_BigFeatureUpdate'
)
BEGIN
    CREATE INDEX [IX_PostReactions_AttendanceId] ON [PostReactions] ([AttendanceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929205053_BigFeatureUpdate'
)
BEGIN
    CREATE INDEX [IX_PostReactions_PostId] ON [PostReactions] ([PostId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929205053_BigFeatureUpdate'
)
BEGIN
    CREATE INDEX [IX_Posts_AttendanceId] ON [Posts] ([AttendanceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929205053_BigFeatureUpdate'
)
BEGIN
    CREATE INDEX [IX_PushSubscribers_AttendanceId] ON [PushSubscribers] ([AttendanceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929205053_BigFeatureUpdate'
)
BEGIN
    CREATE INDEX [IX_Stories_AttendanceId] ON [Stories] ([AttendanceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929205053_BigFeatureUpdate'
)
BEGIN
    ALTER TABLE [MessageReactions] ADD CONSTRAINT [FK_MessageReactions_Attendances_AttendanceId] FOREIGN KEY ([AttendanceId]) REFERENCES [Attendances] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929205053_BigFeatureUpdate'
)
BEGIN
    ALTER TABLE [MessageReactions] ADD CONSTRAINT [FK_MessageReactions_ChatMessages_ChatMessageId] FOREIGN KEY ([ChatMessageId]) REFERENCES [ChatMessages] ([Id]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929205053_BigFeatureUpdate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260929205053_BigFeatureUpdate', N'10.0.11');
END;

COMMIT;
GO

