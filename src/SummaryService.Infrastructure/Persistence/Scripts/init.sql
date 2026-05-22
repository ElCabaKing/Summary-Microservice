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
