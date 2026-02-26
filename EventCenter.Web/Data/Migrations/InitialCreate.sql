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
CREATE TABLE [Events] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(200) NOT NULL,
    [Description] nvarchar(2000) NULL,
    [Location] nvarchar(200) NOT NULL,
    [StartDateUtc] datetime2 NOT NULL,
    [EndDateUtc] datetime2 NOT NULL,
    [RegistrationDeadlineUtc] datetime2 NOT NULL,
    [MaxCapacity] int NOT NULL,
    [MaxCompanions] int NOT NULL,
    [IsPublished] bit NOT NULL,
    CONSTRAINT [PK_Events] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Event_RegistrationDeadlineBeforeStart] CHECK ([RegistrationDeadlineUtc] <= [StartDateUtc])
);

CREATE TABLE [EventAgendaItems] (
    [Id] int NOT NULL IDENTITY,
    [EventId] int NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Description] nvarchar(2000) NULL,
    [StartDateTimeUtc] datetime2 NOT NULL,
    [EndDateTimeUtc] datetime2 NOT NULL,
    [CostForMakler] decimal(18,2) NOT NULL,
    [CostForGuest] decimal(18,2) NOT NULL,
    [IsMandatory] bit NOT NULL,
    [MaxParticipants] int NULL,
    CONSTRAINT [PK_EventAgendaItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EventAgendaItems_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [EventCompanies] (
    [Id] int NOT NULL IDENTITY,
    [EventId] int NOT NULL,
    [CompanyName] nvarchar(200) NOT NULL,
    [ContactEmail] nvarchar(200) NOT NULL,
    [ContactPhone] nvarchar(50) NULL,
    [PricePerPerson] decimal(18,2) NULL,
    [MaxParticipants] int NULL,
    [InvitationCode] nvarchar(100) NULL,
    [InvitationSentUtc] datetime2 NULL,
    CONSTRAINT [PK_EventCompanies] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EventCompanies_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [EventOptions] (
    [Id] int NOT NULL IDENTITY,
    [EventId] int NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Description] nvarchar(1000) NULL,
    [Price] decimal(18,2) NOT NULL,
    [MaxQuantity] int NULL,
    CONSTRAINT [PK_EventOptions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EventOptions_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Registrations] (
    [Id] int NOT NULL IDENTITY,
    [EventId] int NOT NULL,
    [EventCompanyId] int NULL,
    [RegistrationType] nvarchar(50) NOT NULL,
    [FirstName] nvarchar(100) NOT NULL,
    [LastName] nvarchar(100) NOT NULL,
    [Email] nvarchar(200) NOT NULL,
    [Phone] nvarchar(50) NULL,
    [Company] nvarchar(200) NULL,
    [RegistrationDateUtc] datetime2 NOT NULL,
    [IsConfirmed] bit NOT NULL,
    [NumberOfCompanions] int NOT NULL,
    [SpecialRequirements] nvarchar(1000) NULL,
    CONSTRAINT [PK_Registrations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Registrations_EventCompanies_EventCompanyId] FOREIGN KEY ([EventCompanyId]) REFERENCES [EventCompanies] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Registrations_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [RegistrationEventOption] (
    [EventOptionId] int NOT NULL,
    [RegistrationId] int NOT NULL,
    CONSTRAINT [PK_RegistrationEventOption] PRIMARY KEY ([EventOptionId], [RegistrationId]),
    CONSTRAINT [FK_RegistrationEventOption_EventOptions_EventOptionId] FOREIGN KEY ([EventOptionId]) REFERENCES [EventOptions] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RegistrationEventOption_Registrations_RegistrationId] FOREIGN KEY ([RegistrationId]) REFERENCES [Registrations] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_EventAgendaItems_EventId] ON [EventAgendaItems] ([EventId]);

CREATE INDEX [IX_EventCompanies_EventId] ON [EventCompanies] ([EventId]);

CREATE INDEX [IX_EventCompanies_InvitationCode] ON [EventCompanies] ([InvitationCode]);

CREATE INDEX [IX_EventOptions_EventId] ON [EventOptions] ([EventId]);

CREATE INDEX [IX_Events_IsPublished] ON [Events] ([IsPublished]);

CREATE INDEX [IX_RegistrationEventOption_RegistrationId] ON [RegistrationEventOption] ([RegistrationId]);

CREATE INDEX [IX_Registrations_EventCompanyId] ON [Registrations] ([EventCompanyId]);

CREATE INDEX [IX_Registrations_EventId] ON [Registrations] ([EventId]);

CREATE INDEX [IX_Registrations_EventId_Email] ON [Registrations] ([EventId], [Email]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260226125256_InitialCreate', N'9.0.13');

COMMIT;
GO

