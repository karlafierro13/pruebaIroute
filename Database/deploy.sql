/* =========================================================
   Ejecuta todos los scripts en orden (modo SQLCMD).
   Desde la carpeta Database:
     sqlcmd -S localhost -E -f 65001 -i deploy.sql
   ========================================================= */
:on error exit
:r scripts\01_create_database.sql
:r scripts\02_create_tables.sql
:r scripts\03_alter_commerce_quarantine.sql
:r scripts\04_create_types.sql
:r scripts\05_fn_commerce_motivo.sql
:r scripts\06_sp_create_commerce.sql
:r scripts\07_sp_validate_commerce.sql
:r scripts\08_sp_list_commerce_quarantine.sql
:r scripts\09_sp_list_commerce_dates.sql
:r scripts\10_sp_update_commerce_quarantine.sql
:r scripts\11_sp_delete_commerce_quarantine.sql
PRINT 'Base de datos IrouteCommerce desplegada correctamente.';
