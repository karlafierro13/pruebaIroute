/** Fila del archivo commerce_DDMMYYYY.csv (nombres de columna del CSV). */
export interface CommerceCsvRow {
  pc_processdate: string;
  pc_codcomred: string;
  pc_nomcomred: string;
  pc_tipdoc: string;
  pc_numdoc: string;
  pc_direccion: string;
  pc_ciudad: string;
  pc_telefono: string;
  pc_email: string;
}

export const COMMERCE_COLUMNS: readonly (keyof CommerceCsvRow)[] = [
  'pc_processdate',
  'pc_codcomred',
  'pc_nomcomred',
  'pc_tipdoc',
  'pc_numdoc',
  'pc_direccion',
  'pc_ciudad',
  'pc_telefono',
  'pc_email',
];

export interface UploadResult {
  fileName: string;
  insertedCount: number;
}

export interface ProcessResult {
  processDate: string;
  quarantinedCount: number;
}

export interface ProcessDateSummary {
  pcProcessDate: string;
  totalRecords: number;
}

/** Campos corregibles de un registro en cuarentena. */
export interface UpdateQuarantineRequest {
  pcNomComRed: string | null;
  pcTipDoc: string | null;
  pcNumDoc: string | null;
  pcDireccion: string | null;
  pcCiudad: string | null;
  pcTelefono: string | null;
  pcEmail: string | null;
}

/** released = true: el registro quedó válido y regresó a commerce. */
export interface UpdateQuarantineResult {
  released: boolean;
  motivo: string | null;
}

export interface QuarantineRecord {
  id: number;
  commerceId: number;
  pcProcessDate: string;
  pcCodComRed: string | null;
  pcNomComRed: string | null;
  pcTipDoc: string | null;
  pcNumDoc: string | null;
  pcDireccion: string | null;
  pcCiudad: string | null;
  pcTelefono: string | null;
  pcEmail: string | null;
  motivo: string;
  quarantinedAt: string;
}
