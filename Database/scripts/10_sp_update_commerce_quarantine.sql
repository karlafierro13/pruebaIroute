/* =========================================================
   10 - sp_update_commerce_quarantine
   Corrige un registro en cuarentena y lo vuelve a validar:
     - Si ahora es válido, regresa a commerce y sale de la cuarentena.
     - Si sigue siendo inválido, se actualizan sus datos y su motivo.
   pc_processdate y pc_codcomred no se editan (identifican al comercio).
     50004 - El registro no existe.
   Retorna released (1 = devuelto a commerce) y el motivo vigente.
   ========================================================= */
USE IrouteCommerce;
GO

CREATE OR ALTER PROCEDURE dbo.sp_update_commerce_quarantine
    @id           BIGINT,
    @pc_nomcomred NVARCHAR(150),
    @pc_tipdoc    NVARCHAR(10),
    @pc_numdoc    NVARCHAR(20),
    @pc_direccion NVARCHAR(250),
    @pc_ciudad    NVARCHAR(100),
    @pc_telefono  NVARCHAR(20),
    @pc_email     NVARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SELECT @pc_nomcomred = NULLIF(LTRIM(RTRIM(@pc_nomcomred)), N''),
           @pc_tipdoc    = NULLIF(LTRIM(RTRIM(@pc_tipdoc)), N''),
           @pc_numdoc    = NULLIF(LTRIM(RTRIM(@pc_numdoc)), N''),
           @pc_direccion = NULLIF(LTRIM(RTRIM(@pc_direccion)), N''),
           @pc_ciudad    = NULLIF(LTRIM(RTRIM(@pc_ciudad)), N''),
           @pc_telefono  = NULLIF(LTRIM(RTRIM(@pc_telefono)), N''),
           @pc_email     = NULLIF(LTRIM(RTRIM(@pc_email)), N'');

    DECLARE @motivo NVARCHAR(500) =
        (SELECT motivo FROM dbo.fn_commerce_motivo(@pc_nomcomred, @pc_numdoc));

    BEGIN TRY
        BEGIN TRANSACTION;

        -- UPDLOCK/HOLDLOCK: nadie más modifica el registro mientras se procesa.
        IF NOT EXISTS (SELECT 1 FROM dbo.commerce_quarantine WITH (UPDLOCK, HOLDLOCK) WHERE id = @id)
            THROW 50004, N'El registro en cuarentena no existe.', 1;

        IF @motivo IS NULL
        BEGIN
            INSERT INTO dbo.commerce
                (pc_processdate, pc_codcomred, pc_nomcomred, pc_tipdoc, pc_numdoc,
                 pc_direccion, pc_ciudad, pc_telefono, pc_email)
            SELECT pc_processdate, pc_codcomred, @pc_nomcomred, @pc_tipdoc, @pc_numdoc,
                   @pc_direccion, @pc_ciudad, @pc_telefono, @pc_email
            FROM dbo.commerce_quarantine
            WHERE id = @id;

            DELETE FROM dbo.commerce_quarantine WHERE id = @id;
        END
        ELSE
        BEGIN
            UPDATE dbo.commerce_quarantine
            SET pc_nomcomred = @pc_nomcomred,
                pc_tipdoc    = @pc_tipdoc,
                pc_numdoc    = @pc_numdoc,
                pc_direccion = @pc_direccion,
                pc_ciudad    = @pc_ciudad,
                pc_telefono  = @pc_telefono,
                pc_email     = @pc_email,
                motivo       = @motivo
            WHERE id = @id;
        END;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;

    SELECT CAST(IIF(@motivo IS NULL, 1, 0) AS BIT) AS released, @motivo AS motivo;
END
GO
