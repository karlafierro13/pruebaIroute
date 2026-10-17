/* =========================================================
   11 - sp_delete_commerce_quarantine
   Descarta definitivamente un registro en cuarentena.
     50004 - El registro no existe.
   ========================================================= */
USE IrouteCommerce;
GO

CREATE OR ALTER PROCEDURE dbo.sp_delete_commerce_quarantine
    @id BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.commerce_quarantine WHERE id = @id;

    IF @@ROWCOUNT = 0
        THROW 50004, N'El registro en cuarentena no existe.', 1;
END
GO
