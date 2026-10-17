/* =========================================================
   05 - fn_commerce_motivo
   Reglas de validación de un comercio, centralizadas para que
   sp_validate_commerce y sp_update_commerce_quarantine usen
   exactamente los mismos criterios:
     - pc_nomcomred no debe estar vacío.
     - pc_numdoc no debe estar vacío, ni contener letras ni
       caracteres especiales.
   Retorna los motivos separados por "; " o NULL si el registro es válido.
   Es inline (RETURNS TABLE) para que el optimizador la expanda
   dentro de la consulta, sin costo por fila.
   ========================================================= */
USE IrouteCommerce;
GO

CREATE OR ALTER FUNCTION dbo.fn_commerce_motivo
(
    @pc_nomcomred NVARCHAR(150),
    @pc_numdoc    NVARCHAR(20)
)
RETURNS TABLE
AS
RETURN
    -- STUFF quita el primer "; " y devuelve NULL cuando no hay motivos.
    -- Collation binaria: los rangos [0-9] / [A-Za-z] se evalúan por código
    -- de carácter y no según reglas lingüísticas.
    SELECT STUFF(
               CASE WHEN LTRIM(RTRIM(ISNULL(@pc_nomcomred, N''))) = N''
                    THEN N'; El nombre del comercio (nomcomred) se encuentra vacío' ELSE N'' END
             + CASE
                    WHEN LTRIM(RTRIM(ISNULL(@pc_numdoc, N''))) = N''
                        THEN N'; El número de documento (numdoc) se encuentra vacío'
                    ELSE
                        CASE WHEN @pc_numdoc COLLATE Latin1_General_100_BIN2 LIKE N'%[A-Za-zÁÉÍÓÚáéíóúÑñÜü]%'
                             THEN N'; El número de documento (numdoc) contiene letras' ELSE N'' END
                      + CASE WHEN @pc_numdoc COLLATE Latin1_General_100_BIN2 LIKE N'%[^0-9A-Za-zÁÉÍÓÚáéíóúÑñÜü]%'
                             THEN N'; El número de documento (numdoc) contiene caracteres especiales' ELSE N'' END
               END,
               1, 2, N'') AS motivo;
GO
