IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'SummaryService')
BEGIN
    CREATE DATABASE SummaryService;
END
GO

USE SummaryService;
GO

/* =========================================================
   CLIENTS
========================================================= */
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Clients')
BEGIN
    CREATE TABLE Clients (
        Id              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
        CompanyName     NVARCHAR(200)       NOT NULL,
        Email           NVARCHAR(200)       NULL,
        ContactName     NVARCHAR(200)       NULL,

        TenantId        NVARCHAR(100)       NOT NULL,
        Domain          NVARCHAR(255)       NOT NULL,

        IsActive        BIT                 NOT NULL DEFAULT 1,

        CreatedAt       DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt       DATETIME2           NULL,

        CONSTRAINT PK_Clients
            PRIMARY KEY NONCLUSTERED (Id),

        CONSTRAINT UQ_Clients_TenantId
            UNIQUE (TenantId),

        CONSTRAINT UQ_Clients_Domain
            UNIQUE (Domain)
    );

    CREATE CLUSTERED INDEX IX_Clients_TenantId
        ON Clients (TenantId);

    CREATE INDEX IX_Clients_Domain
        ON Clients (Domain);
END
GO

/* =========================================================
   API KEYS
========================================================= */
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ApiKeys')
BEGIN
    CREATE TABLE ApiKeys (
        Id              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),

        KeyHash         NVARCHAR(64)        NOT NULL,

        TenantId        NVARCHAR(100)       NOT NULL,

        Role            NVARCHAR(20)        NOT NULL DEFAULT 'user',

        IsActive        BIT                 NOT NULL DEFAULT 1,

        CreatedAt       DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt       DATETIME2           NULL,

        CONSTRAINT PK_ApiKeys
            PRIMARY KEY NONCLUSTERED (Id),

        CONSTRAINT UQ_ApiKeys_KeyHash
            UNIQUE (KeyHash)
    );

    CREATE CLUSTERED INDEX IX_ApiKeys_TenantId
        ON ApiKeys (TenantId);
END
GO

/* =========================================================
   EXAMPLE TENANT TABLE
========================================================= */
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Documents')
BEGIN
    CREATE TABLE Documents (
        Id              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),

        TenantId        NVARCHAR(100)       NOT NULL,

        FileName        NVARCHAR(255)       NOT NULL,
        Summary         NVARCHAR(MAX)       NULL,

        CreatedAt       DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_Documents
            PRIMARY KEY CLUSTERED (Id)
    );

    CREATE INDEX IX_Documents_TenantId
        ON Documents (TenantId);
END
GO

/* =========================================================
   RLS SCHEMA
========================================================= */
IF NOT EXISTS (
    SELECT *
    FROM sys.schemas
    WHERE name = 'Security'
)
BEGIN
    EXEC('CREATE SCHEMA Security');
END
GO

/* =========================================================
   RLS PREDICATE FUNCTION
========================================================= */
CREATE OR ALTER FUNCTION Security.fnTenantAccessPredicate
(
    @TenantId NVARCHAR(100)
)
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN
(
    SELECT 1 AS fn_securitypredicate_result
    WHERE
        CAST(SESSION_CONTEXT(N'TenantId') AS NVARCHAR(100)) = @TenantId
        OR CAST(SESSION_CONTEXT(N'Role') AS NVARCHAR(20)) = N'admin'
);
GO

/* =========================================================
   SECURITY POLICY
========================================================= */
IF NOT EXISTS (
    SELECT *
    FROM sys.security_policies
    WHERE name = 'TenantSecurityPolicy'
)
BEGIN
    CREATE SECURITY POLICY Security.TenantSecurityPolicy
    ADD FILTER PREDICATE
        Security.fnTenantAccessPredicate(TenantId)
        ON dbo.Documents,

    ADD BLOCK PREDICATE
        Security.fnTenantAccessPredicate(TenantId)
        ON dbo.Documents AFTER INSERT,

    ADD BLOCK PREDICATE
        Security.fnTenantAccessPredicate(TenantId)
        ON dbo.Documents AFTER UPDATE
    WITH (STATE = ON);
END
GO

/* =========================================================
   ADMIN CLIENT
========================================================= */
IF NOT EXISTS (
    SELECT *
    FROM Clients
    WHERE TenantId = 'admin'
)
BEGIN
    INSERT INTO Clients (
        Id,
        CompanyName,
        TenantId,
        Domain,
        IsActive,
        CreatedAt
    )
    VALUES (
        NEWID(),
        'System Admin',
        'admin',
        'http://127.0.0.1:5500',
        1,
        SYSUTCDATETIME()
    );
END
GO

/* =========================================================
   ADMIN API KEY
========================================================= */
IF NOT EXISTS (
    SELECT *
    FROM ApiKeys
    WHERE TenantId = 'admin'
)
BEGIN
    INSERT INTO ApiKeys (
        Id,
        KeyHash,
        TenantId,
        Role,
        IsActive,
        CreatedAt
    )
    VALUES (
        NEWID(),
        'dcca10361a8dc11e81d111f5fb3cfca3cbe862dcb7ef3eb97d73c826e58d9233',
        'admin',
        'admin',
        1,
        SYSUTCDATETIME()
    );
END
GO