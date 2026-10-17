/* =========================================================
   09 - sp_list_commerce_dates
   Fechas de proceso cargadas en commerce y su cantidad de registros.
   Alimenta el selector de la pantalla "Procesar".
   ========================================================= */
USE IrouteCommerce;
GO

CREATE OR ALTER PROCEDURE dbo.sp_list_commerce_dates
AS
BEGIN
    SET NOCOUNT ON;

    SELECT pc_processdate, COUNT(*) AS total_records
    FROM dbo.commerce
    GROUP BY pc_processdate
    ORDER BY pc_processdate DESC;
END
GO
