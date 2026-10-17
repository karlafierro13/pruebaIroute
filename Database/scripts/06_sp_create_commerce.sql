/* =========================================================
   06 - sp_create_commerce
   Registra las filas del archivo CSV en la tabla commerce.
   Antes de insertar valida (todo o nada):
     50001 - La fecha de proceso ya fue cargada.
     50002 - El archivo repite comercios (pc_codcomred).
     50003 - Algún comercio ya está registrado (commerce o cuarentena).
     50005 - No se obtuvo el bloqueo de carga.
     50006 - Filas sin código de comercio.
   Retorna la cantidad de registros insertados.
   ========================================================= */
USE IrouteCommerce;
GO

CREATE OR ALTER PROCEDURE dbo.sp_create_commerce
    @commerce dbo.commerce_type READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @message NVARCHAR(2048), @list NVARCHAR(1000), @lock INT, @inserted INT;

    IF EXISTS (SELECT 1 FROM @commerce WHERE NULLIF(LTRIM(RTRIM(pc_codcomred)), N'') IS NULL)
        THROW 50006, N'Todos los registros deben tener código de comercio (pc_codcomred).', 1;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Serializa las cargas: dos cargas simultáneas no pueden pasar las
        -- validaciones de duplicados al mismo tiempo.
        EXEC @lock = sp_getapplock @Resource = N'commerce_load', @LockMode = N'Exclusive',
                                   @LockOwner = N'Transaction', @LockTimeout = 30000;
        IF @lock < 0
            THROW 50005, N'Hay otra carga en curso. Intente nuevamente en unos segundos.', 1;

        -- 50001: fecha ya cargada (en commerce o en cuarentena)
        SELECT @list = STRING_AGG(CONVERT(NVARCHAR(10), d.pc_processdate, 103), N', ')
        FROM (SELECT DISTINCT pc_processdate FROM @commerce) AS d
        WHERE EXISTS (SELECT 1 FROM dbo.commerce AS c WHERE c.pc_processdate = d.pc_processdate)
           OR EXISTS (SELECT 1 FROM dbo.commerce_quarantine AS q WHERE q.pc_processdate = d.pc_processdate);

        IF @list IS NOT NULL
        BEGIN
            SET @message = CONCAT(N'La fecha de proceso ', @list, N' ya fue cargada.');
            THROW 50001, @message, 1;
        END;

        -- 50002: comercios repetidos dentro del archivo
        SELECT @list = STRING_AGG(d.code, N', ')
        FROM (SELECT TOP (20) LTRIM(RTRIM(pc_codcomred)) AS code
              FROM @commerce
              GROUP BY LTRIM(RTRIM(pc_codcomred))
              HAVING COUNT(*) > 1) AS d;

        IF @list IS NOT NULL
        BEGIN
            SET @message = CONCAT(N'El archivo contiene comercios repetidos: ', @list, N'.');
            THROW 50002, @message, 1;
        END;

        -- 50003: comercios que ya existen, aunque provengan de otro archivo o fecha
        SELECT @list = STRING_AGG(d.code, N', ')
        FROM (SELECT TOP (20) LTRIM(RTRIM(t.pc_codcomred)) AS code
              FROM @commerce AS t
              WHERE EXISTS (SELECT 1 FROM dbo.commerce AS c WHERE c.pc_codcomred = LTRIM(RTRIM(t.pc_codcomred)))
                 OR EXISTS (SELECT 1 FROM dbo.commerce_quarantine AS q WHERE q.pc_codcomred = LTRIM(RTRIM(t.pc_codcomred)))) AS d;

        IF @list IS NOT NULL
        BEGIN
            SET @message = CONCAT(N'Los siguientes comercios ya están registrados: ', @list, N'.');
            THROW 50003, @message, 1;
        END;

        INSERT INTO dbo.commerce
            (pc_processdate, pc_codcomred, pc_nomcomred, pc_tipdoc, pc_numdoc,
             pc_direccion, pc_ciudad, pc_telefono, pc_email)
        SELECT pc_processdate,
               LTRIM(RTRIM(pc_codcomred)),
               NULLIF(LTRIM(RTRIM(pc_nomcomred)), N''),
               NULLIF(LTRIM(RTRIM(pc_tipdoc)), N''),
               NULLIF(LTRIM(RTRIM(pc_numdoc)), N''),
               NULLIF(LTRIM(RTRIM(pc_direccion)), N''),
               NULLIF(LTRIM(RTRIM(pc_ciudad)), N''),
               NULLIF(LTRIM(RTRIM(pc_telefono)), N''),
               NULLIF(LTRIM(RTRIM(pc_email)), N'')
        FROM @commerce;

        SET @inserted = @@ROWCOUNT;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;

    SELECT @inserted AS inserted_count;
END
GO
