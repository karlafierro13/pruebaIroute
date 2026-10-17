/* =========================================================
   02 - Tablas
   commerce            : misma estructura del CSV (+ id y auditoría).
   commerce_quarantine : registros rechazados por la validación.
   Las columnas de datos son NVARCHAR porque se reciben "en crudo"
   y la validación ocurre después, en sp_validate_commerce.
   ========================================================= */
USE IrouteCommerce;
GO

IF OBJECT_ID(N'dbo.commerce', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.commerce
    (
        id              BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_commerce PRIMARY KEY,
        pc_processdate  DATE           NOT NULL,
        pc_codcomred    NVARCHAR(20)   NULL,
        pc_nomcomred    NVARCHAR(150)  NULL,
        pc_tipdoc       NVARCHAR(10)   NULL,
        pc_numdoc       NVARCHAR(20)   NULL,
        pc_direccion    NVARCHAR(250)  NULL,
        pc_ciudad       NVARCHAR(100)  NULL,
        pc_telefono     NVARCHAR(20)   NULL,
        pc_email        NVARCHAR(150)  NULL,
        created_at      DATETIME2(0)   NOT NULL CONSTRAINT DF_commerce_created_at DEFAULT SYSDATETIME()
    );

    -- La validación siempre filtra por fecha de proceso.
    CREATE INDEX IX_commerce_processdate ON dbo.commerce (pc_processdate);
END
GO

IF OBJECT_ID(N'dbo.commerce_quarantine', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.commerce_quarantine
    (
        id              BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_commerce_quarantine PRIMARY KEY,
        commerce_id     BIGINT         NOT NULL,
        pc_processdate  DATE           NOT NULL,
        pc_codcomred    NVARCHAR(20)   NULL,
        pc_nomcomred    NVARCHAR(150)  NULL,
        pc_tipdoc       NVARCHAR(10)   NULL,
        pc_numdoc       NVARCHAR(20)   NULL,
        pc_direccion    NVARCHAR(250)  NULL,
        pc_ciudad       NVARCHAR(100)  NULL,
        pc_telefono     NVARCHAR(20)   NULL,
        pc_email        NVARCHAR(150)  NULL,
        quarantined_at  DATETIME2(0)   NOT NULL CONSTRAINT DF_commerce_quarantine_quarantined_at DEFAULT SYSDATETIME()
    );

    CREATE INDEX IX_commerce_quarantine_processdate ON dbo.commerce_quarantine (pc_processdate);
END
GO

-- pc_codcomred identifica al comercio: soporta la validación de duplicados al cargar.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_commerce_codcomred')
    CREATE INDEX IX_commerce_codcomred ON dbo.commerce (pc_codcomred);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_commerce_quarantine_codcomred')
    CREATE INDEX IX_commerce_quarantine_codcomred ON dbo.commerce_quarantine (pc_codcomred);
GO
