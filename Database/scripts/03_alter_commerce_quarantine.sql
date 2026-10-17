/* =========================================================
   03 - Requerimiento: agregar la columna "motivo" a commerce_quarantine
   ========================================================= */
USE IrouteCommerce;
GO

IF COL_LENGTH(N'dbo.commerce_quarantine', N'motivo') IS NULL
    ALTER TABLE dbo.commerce_quarantine ADD motivo NVARCHAR(500) NOT NULL
        CONSTRAINT DF_commerce_quarantine_motivo DEFAULT N'';
GO
