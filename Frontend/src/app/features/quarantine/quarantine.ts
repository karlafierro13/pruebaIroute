import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, linkedSignal, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { catchError, finalize, of, switchMap, tap } from 'rxjs';

import { QuarantineRecord, UpdateQuarantineResult } from '../../core/models/commerce.models';
import { CommerceService } from '../../core/services/commerce.service';
import { NotificationService } from '../../core/services/notification.service';
import { QuarantineEditDialog } from './quarantine-edit-dialog/quarantine-edit-dialog';

interface QuarantineView extends QuarantineRecord {
  motivos: string[];
}

@Component({
  selector: 'app-quarantine',
  imports: [FormsModule, DatePipe, QuarantineEditDialog],
  templateUrl: './quarantine.html',
  styleUrl: './quarantine.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Quarantine {
  private readonly commerceService = inject(CommerceService);
  private readonly notifications = inject(NotificationService);

  /** Fecha recibida por query param (?date=yyyy-MM-dd). */
  readonly date = input<string>();

  protected readonly dateFilter = linkedSignal(() => this.date() ?? '');
  protected readonly search = signal('');
  protected readonly loading = signal(false);
  protected readonly editing = signal<QuarantineRecord | null>(null);
  protected readonly confirmingDeleteId = signal<number | null>(null);
  private readonly reloadTrigger = signal(0);

  // Cada cambio de fecha (o recarga) consulta al API; switchMap cancela la petición anterior.
  private readonly query = computed(() => ({ date: this.dateFilter(), reload: this.reloadTrigger() }));
  private readonly records = toSignal(
    toObservable(this.query).pipe(
      tap(() => this.loading.set(true)),
      switchMap(({ date }) =>
        this.commerceService.getQuarantine(date || null).pipe(
          catchError(() => of<QuarantineRecord[]>([])),
          finalize(() => this.loading.set(false)),
        ),
      ),
    ),
    { initialValue: [] },
  );

  protected readonly rows = computed<QuarantineView[]>(() => {
    const term = this.search().trim().toLowerCase();
    return this.records()
      .filter(
        (r) =>
          !term ||
          [r.pcCodComRed, r.pcNomComRed, r.pcNumDoc, r.motivo].some((v) => v?.toLowerCase().includes(term)),
      )
      .map((r) => ({ ...r, motivos: r.motivo.split('; ') }));
  });

  protected readonly summary = computed(() => {
    const records = this.records();
    const count = (text: string) => records.filter((r) => r.motivo.includes(text)).length;
    return {
      total: records.length,
      emptyName: count('(nomcomred) se encuentra vacío'),
      emptyDoc: count('(numdoc) se encuentra vacío'),
      invalidDoc: records.filter((r) => /contiene (letras|caracteres)/.test(r.motivo)).length,
    };
  });

  protected clearFilters(): void {
    this.dateFilter.set('');
    this.search.set('');
  }

  protected onSaved(result: UpdateQuarantineResult): void {
    const code = this.editing()?.pcCodComRed;
    this.editing.set(null);

    if (result.released) {
      this.notifications.success(`El comercio ${code} fue corregido y regresó a commerce.`);
    } else {
      this.notifications.info(`El comercio ${code} se actualizó, pero sigue en cuarentena: ${result.motivo}`);
    }
    this.reload();
  }

  protected discard(record: QuarantineRecord): void {
    this.confirmingDeleteId.set(null);
    this.commerceService.deleteQuarantine(record.id).subscribe({
      next: () => {
        this.notifications.success(`El comercio ${record.pcCodComRed} fue descartado.`);
        this.reload();
      },
      error: () => this.reload(), // p. ej. 404 si otro usuario ya lo eliminó
    });
  }

  private reload(): void {
    this.reloadTrigger.update((n) => n + 1);
  }
}
