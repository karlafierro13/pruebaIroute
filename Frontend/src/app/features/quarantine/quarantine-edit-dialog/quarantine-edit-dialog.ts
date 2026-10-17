import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, input, OnInit, output, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { getErrorMessage } from '../../../core/interceptors/error.interceptor';
import { QuarantineRecord, UpdateQuarantineResult } from '../../../core/models/commerce.models';
import { CommerceService } from '../../../core/services/commerce.service';
import { getCommerceIssues } from '../../../shared/utils/commerce-rules';

@Component({
  selector: 'app-quarantine-edit-dialog',
  imports: [ReactiveFormsModule, DatePipe],
  templateUrl: './quarantine-edit-dialog.html',
  styleUrl: './quarantine-edit-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '(document:keydown.escape)': 'close()' },
})
export class QuarantineEditDialog implements OnInit {
  private readonly commerceService = inject(CommerceService);

  readonly record = input.required<QuarantineRecord>();
  readonly saved = output<UpdateQuarantineResult>();
  readonly closed = output<void>();

  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly form = inject(NonNullableFormBuilder).group({
    pcNomComRed: ['', Validators.maxLength(150)],
    pcTipDoc: ['', Validators.maxLength(10)],
    pcNumDoc: ['', Validators.maxLength(20)],
    pcDireccion: ['', Validators.maxLength(250)],
    pcCiudad: ['', Validators.maxLength(100)],
    pcTelefono: ['', Validators.maxLength(20)],
    pcEmail: ['', Validators.maxLength(150)],
  });

  private readonly formValue = toSignal(this.form.valueChanges, { initialValue: this.form.getRawValue() });

  /** Validación en vivo: anticipa si el registro saldrá de cuarentena al guardar. */
  protected readonly issues = computed(() => getCommerceIssues(this.formValue().pcNomComRed, this.formValue().pcNumDoc));

  ngOnInit(): void {
    const r = this.record();
    this.form.setValue({
      pcNomComRed: r.pcNomComRed ?? '',
      pcTipDoc: r.pcTipDoc ?? '',
      pcNumDoc: r.pcNumDoc ?? '',
      pcDireccion: r.pcDireccion ?? '',
      pcCiudad: r.pcCiudad ?? '',
      pcTelefono: r.pcTelefono ?? '',
      pcEmail: r.pcEmail ?? '',
    });
  }

  protected save(): void {
    if (this.form.invalid || this.saving()) return;

    this.saving.set(true);
    this.error.set(null);

    // Los campos vacíos se envían como null (la BD los guarda como NULL).
    const value = this.form.getRawValue();
    const request = Object.fromEntries(
      Object.entries(value).map(([key, v]) => [key, v.trim() || null]),
    ) as Record<keyof typeof value, string | null>;

    this.commerceService.updateQuarantine(this.record().id, request).subscribe({
      next: (result) => this.saved.emit(result),
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        if (err.status < 500 && err.status !== 0) this.error.set(getErrorMessage(err));
      },
    });
  }

  protected close(): void {
    if (!this.saving()) this.closed.emit();
  }
}
