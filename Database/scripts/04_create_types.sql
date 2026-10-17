/* =========================================================
   04 - Table-Valued Parameter
   Permite enviar todas las filas del CSV en UNA sola llamada al
   SP (evita un round-trip por fila).
   ========================================================= */
USE IrouteCommerce;
GO

IF TYPE_ID(N'dbo.commerce_type') IS NULL
    CREATE TYPE dbo.commerce_type AS TABLE
    (
        pc_processdate  DATE           NOT NULL,
        pc_codcomred    NVARCHAR(20)   NULL,
        pc_nomcomred    NVARCHAR(150)  NULL,
        pc_tipdoc       NVARCHAR(10)   NULL,
        pc_numdoc       NVARCHAR(20)   NULL,
        pc_direccion    NVARCHAR(250)  NULL,
        pc_ciudad       NVARCHAR(100)  NULL,
        pc_telefono     NVARCHAR(20)   NULL,
        pc_email        NVARCHAR(150)  NULL
    );
GO
