import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { getErrorDetails, getErrorMessage } from '../../core/interceptors/error.interceptor';
import { COMMERCE_COLUMNS, CommerceCsvRow, UploadResult } from '../../core/models/commerce.models';
import { CommerceService } from '../../core/services/commerce.service';
import { NotificationService } from '../../core/services/notification.service';
import { getCommerceFileDate, getCommerceIssues, normalizeProcessDate } from '../../shared/utils/commerce-rules';
import { parseCsv } from '../../shared/utils/csv-parser';

interface PreviewRow {
  line: number;
  data: CommerceCsvRow;
  issues: string[];
}

const PAGE_SIZE = 15;
const MAX_FILE_SIZE = 10 * 1024 * 1024;
const MAX_LISTED_ERRORS = 10;

@Component({
  selector: 'app-upload',
  imports: [RouterLink],
  templateUrl: './upload.html',
  styleUrl: './upload.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Upload {
  private readonly commerceService = inject(CommerceService);
  private readonly notifications = inject(NotificationService);

  protected readonly columns = COMMERCE_COLUMNS;

  protected readonly file = signal<File | null>(null);
  /** Fecha del nombre del archivo (yyyy-MM-dd). */
  protected readonly fileDate = signal<string | null>(null);
  protected readonly rows = signal<PreviewRow[]>([]);
  protected readonly errors = signal<string[]>([]);
  protected readonly uploading = signal(false);
  protected readonly result = signal<UploadResult | null>(null);
  protected readonly dragging = signal(false);
  protected readonly onlyIssues = signal(false);
  protected readonly page = signal(1);

  protected readonly issuesCount = computed(() => this.rows().filter((r) => r.issues.length > 0).length);
  protected readonly processDates = computed(() => [...new Set(this.rows().map((r) => r.data.pc_processdate))]);

  protected readonly filteredRows = computed(() =>
    this.onlyIssues() ? this.rows().filter((r) => r.issues.length > 0) : this.rows(),
  );
  protected readonly totalPages = computed(() => Math.max(1, Math.ceil(this.filteredRows().length / PAGE_SIZE)));
  protected readonly pagedRows = computed(() => {
    const start = (this.page() - 1) * PAGE_SIZE;
    return this.filteredRows().slice(start, start + PAGE_SIZE);
  });

  protected readonly canUpload = computed(
    () => !!this.file() && this.rows().length > 0 && this.errors().length === 0 && !this.uploading(),
  );

  protected onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const selected = input.files?.[0];
    if (selected) void this.loadFile(selected);
    input.value = ''; // permite volver a seleccionar el mismo archivo
  }

  protected onDrop(event: DragEvent): void {
    event.preventDefault();
    this.dragging.set(false);
    const dropped = event.dataTransfer?.files?.[0];
    if (dropped) void this.loadFile(dropped);
  }

  protected onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.dragging.set(true);
  }

  protected toggleOnlyIssues(): void {
    this.onlyIssues.update((v) => !v);
    this.page.set(1);
  }

  protected changePage(delta: number): void {
    this.page.update((p) => Math.min(this.totalPages(), Math.max(1, p + delta)));
  }

  protected reset(): void {
    this.file.set(null);
    this.fileDate.set(null);
    this.rows.set([]);
    this.errors.set([]);
    this.result.set(null);
    this.onlyIssues.set(false);
    this.page.set(1);
  }

  protected upload(): void {
    const file = this.file();
    if (!file || !this.canUpload()) return;

    this.uploading.set(true);
    this.commerceService.upload(file).subscribe({
      next: (result) => {
        this.result.set(result);
        this.uploading.set(false);
        this.notifications.success(`Se registraron ${result.insertedCount} comercios.`);
      },
      error: (err: HttpErrorResponse) => {
        this.uploading.set(false);
        // 400: formato inválido · 409: fecha ya cargada o comercios ya registrados
        if (err.status === 400 || err.status === 409) {
          this.errors.set([getErrorMessage(err), ...getErrorDetails(err)]);
        }
      },
    });
  }

  /** Valida y lee el archivo en el navegador para la previsualización. */
  private async loadFile(file: File): Promise<void> {
    this.reset();
    this.file.set(file);

    const fileDate = getCommerceFileDate(file.name);
    this.fileDate.set(fileDate);

    const errors: string[] = [];
    if (!fileDate) errors.push('El nombre debe tener el formato commerce_DDMMYYYY.csv.');
    if (file.size === 0) errors.push('El archivo se encuentra vacío.');
    if (file.size > MAX_FILE_SIZE) errors.push('El archivo supera el tamaño máximo de 10 MB.');

    if (errors.length === 0 && fileDate) {
      const parsed = parseCsv<CommerceCsvRow>(await file.text(), COMMERCE_COLUMNS);

      if (parsed.missingColumns.length > 0) {
        errors.push(`Columnas faltantes en la cabecera: ${parsed.missingColumns.join(', ')}.`);
      } else if (parsed.rows.length === 0) {
        errors.push('El archivo no contiene registros.');
      } else {
        const rows = parsed.rows.map((data, i) => ({
          line: i + 2,
          data,
          issues: getCommerceIssues(data.pc_nomcomred, data.pc_numdoc),
        }));
        errors.push(...this.getBlockingErrors(rows, fileDate));
        this.rows.set(rows);
      }
    }

    this.errors.set(errors);
  }

  /**
   * Reglas que impiden cargar duplicados (las mismas que aplica el API):
   * fecha igual a la del nombre, código de comercio obligatorio y único.
   */
  private getBlockingErrors(rows: PreviewRow[], fileDate: string): string[] {
    const errors: string[] = [];
    const firstLineByCode = new Map<string, number>();

    for (const { line, data } of rows) {
      const code = data.pc_codcomred.trim().toUpperCase();

      if (normalizeProcessDate(data.pc_processdate) !== fileDate) {
        errors.push(`Línea ${line}: pc_processdate (${data.pc_processdate || 'vacía'}) no coincide con la fecha del archivo.`);
      }

      const firstLine = firstLineByCode.get(code);
      if (!code) {
        errors.push(`Línea ${line}: pc_codcomred es obligatorio.`);
      } else if (firstLine) {
        errors.push(`Línea ${line}: el comercio ${data.pc_codcomred} está repetido (línea ${firstLine}).`);
      } else {
        firstLineByCode.set(code, line);
      }
    }

    return errors.length > MAX_LISTED_ERRORS
      ? [...errors.slice(0, MAX_LISTED_ERRORS), `… y ${errors.length - MAX_LISTED_ERRORS} errores más.`]
      : errors;
  }
}
