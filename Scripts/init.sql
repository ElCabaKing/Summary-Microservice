IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'SummaryService')
BEGIN
    CREATE DATABASE SummaryService;
END
GO

USE SummaryService;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TenantProviders')
BEGIN
    CREATE TABLE TenantProviders (
        Id              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
        TenantId        NVARCHAR(100)       NOT NULL,
        Provider        NVARCHAR(50)        NOT NULL,
        EncryptedApiKey NVARCHAR(500)       NOT NULL,
        IsActive        BIT                 NOT NULL DEFAULT 1,
        CreatedAt       DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt       DATETIME2           NULL,

        CONSTRAINT PK_TenantProviders PRIMARY KEY NONCLUSTERED (Id),
        CONSTRAINT UQ_TenantProviders_Provider
            UNIQUE (TenantId, Provider)
    );

    CREATE CLUSTERED INDEX IX_TenantProviders_TenantId
        ON TenantProviders (TenantId);
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ApiKeys')
BEGIN
    CREATE TABLE ApiKeys (
        Id              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
        KeyHash         NVARCHAR(64)        NOT NULL,
        KeyPrefix       NVARCHAR(8)         NOT NULL,
        TenantId        NVARCHAR(100)       NOT NULL,
        Role            NVARCHAR(20)        NOT NULL DEFAULT 'user',
        IsActive        BIT                 NOT NULL DEFAULT 1,
        CreatedAt       DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt       DATETIME2           NULL,

        CONSTRAINT PK_ApiKeys PRIMARY KEY NONCLUSTERED (Id),
        CONSTRAINT UQ_ApiKeys_KeyHash UNIQUE (KeyHash)
    );

    CREATE CLUSTERED INDEX IX_ApiKeys_KeyHash
        ON ApiKeys (KeyHash);
END
GO
