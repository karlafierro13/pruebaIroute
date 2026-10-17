/* =========================================================
   07 - sp_validate_commerce
   Valida los registros de una fecha de proceso con las reglas de
   dbo.fn_commerce_motivo. Los registros inválidos se ELIMINAN de
   commerce y se insertan en commerce_quarantine con su motivo, en
   una única operación atómica.
   Retorna la cantidad de registros enviados a cuarentena.
   ========================================================= */
USE IrouteCommerce;
GO

CREATE OR ALTER PROCEDURE dbo.sp_validate_commerce
    @processdate       DATE,
    @quarantined_count INT = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- DELETE + OUTPUT INTO: elimina de commerce e inserta en cuarentena
        -- en una sola sentencia.
        DELETE c
        OUTPUT deleted.id, deleted.pc_processdate, deleted.pc_codcomred, deleted.pc_nomcomred,
               deleted.pc_tipdoc, deleted.pc_numdoc, deleted.pc_direccion, deleted.pc_ciudad,
               deleted.pc_telefono, deleted.pc_email, m.motivo
        INTO dbo.commerce_quarantine
               (commerce_id, pc_processdate, pc_codcomred, pc_nomcomred,
                pc_tipdoc, pc_numdoc, pc_direccion, pc_ciudad,
                pc_telefono, pc_email, motivo)
        FROM dbo.commerce AS c
        CROSS APPLY dbo.fn_commerce_motivo(c.pc_nomcomred, c.pc_numdoc) AS m
        WHERE c.pc_processdate = @processdate
          AND m.motivo IS NOT NULL;

        SET @quarantined_count = @@ROWCOUNT;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;

    SELECT @quarantined_count AS quarantined_count;
END
GO
