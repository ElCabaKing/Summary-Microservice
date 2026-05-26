IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'SummaryService')
BEGIN
    CREATE DATABASE SummaryService;
END
GO

USE SummaryService;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Clients')
BEGIN
    CREATE TABLE Clients (
        Id              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
        CompanyName     NVARCHAR(200)       NOT NULL,
        Email           NVARCHAR(200)       NULL,
        ContactName     NVARCHAR(200)       NULL,
        TenantId        NVARCHAR(100)       NOT NULL,
        IsActive        BIT                 NOT NULL DEFAULT 1,
        CreatedAt       DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt       DATETIME2           NULL,

        CONSTRAINT PK_Clients PRIMARY KEY NONCLUSTERED (Id),
        CONSTRAINT UQ_Clients_TenantId UNIQUE (TenantId)
    );

    CREATE CLUSTERED INDEX IX_Clients_TenantId
        ON Clients (TenantId);
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ApiKeys')
BEGIN
    CREATE TABLE ApiKeys (
        Id              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
        KeyHash         NVARCHAR(64)        NOT NULL,
        KeyPrefix       NVARCHAR(14)        NOT NULL,
        TenantId        NVARCHAR(100)       NOT NULL,
        Role            NVARCHAR(20)        NOT NULL DEFAULT 'user',
        IsActive        BIT                 NOT NULL DEFAULT 1,
        CreatedAt       DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt       DATETIME2           NULL,

        CONSTRAINT PK_ApiKeys PRIMARY KEY NONCLUSTERED (Id),
        CONSTRAINT UQ_ApiKeys_KeyHash UNIQUE (KeyHash)
    );

    CREATE CLUSTERED INDEX IX_ApiKeys_KeyPrefix
        ON ApiKeys (KeyPrefix);
END
GO

-- Seed del admin key (smm_adminadmin + 64 hex chars = 256 bits)
-- Key: smm_adminadmin8c0013772288e07a80a60b5775e2304840376e93038336325feb5c79af0e67c7
IF NOT EXISTS (SELECT * FROM ApiKeys WHERE KeyPrefix = 'smm_adminadmin')
BEGIN
    INSERT INTO ApiKeys (Id, KeyHash, KeyPrefix, TenantId, Role, IsActive, CreatedAt)
    VALUES (
        NEWID(),
        '019aaeb814ef9c76956ad56a7455b4b0d25f53885341a261a01453c36dbbdb6c',
        'smm_adminadmin',
        'admin',
        'admin',
        1,
        SYSUTCDATETIME()
    );
END
GO
