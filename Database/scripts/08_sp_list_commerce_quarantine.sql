/* =========================================================
   08 - sp_list_commerce_quarantine
   Lista los comercios en cuarentena (opcionalmente por fecha).
   ========================================================= */
USE IrouteCommerce;
GO

CREATE OR ALTER PROCEDURE dbo.sp_list_commerce_quarantine
    @processdate DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT id, commerce_id, pc_processdate, pc_codcomred, pc_nomcomred, pc_tipdoc,
           pc_numdoc, pc_direccion, pc_ciudad, pc_telefono, pc_email, motivo, quarantined_at
    FROM dbo.commerce_quarantine
    WHERE @processdate IS NULL OR pc_processdate = @processdate
    ORDER BY quarantined_at DESC, id
    OPTION (RECOMPILE); -- plan adecuado para el filtro opcional
END
GO
