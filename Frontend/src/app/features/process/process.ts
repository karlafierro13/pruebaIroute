import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, input, linkedSignal, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { getErrorDetails, getErrorMessage } from '../../core/interceptors/error.interceptor';
import { ProcessDateSummary, ProcessResult } from '../../core/models/commerce.models';
import { CommerceService } from '../../core/services/commerce.service';
import { NotificationService } from '../../core/services/notification.service';

@Component({
  selector: 'app-process',
  imports: [FormsModule, RouterLink, DatePipe],
  templateUrl: './process.html',
  styleUrl: './process.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Process implements OnInit {
  private readonly commerceService = inject(CommerceService);
  private readonly notifications = inject(NotificationService);

  /** Fecha recibida por query param (?date=yyyy-MM-dd) desde la pantalla de carga. */
  readonly date = input<string>();

  protected readonly selectedDate = linkedSignal(() => this.date() ?? '');
  protected readonly dates = signal<ProcessDateSummary[]>([]);
  protected readonly loadingDates = signal(false);
  protected readonly processing = signal(false);
  protected readonly result = signal<ProcessResult | null>(null);
  protected readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.loadDates();
  }

  protected selectDate(date: string): void {
    this.selectedDate.set(date);
    this.result.set(null);
    this.error.set(null);
  }

  protected process(): void {
    const date = this.selectedDate();
    if (!date || this.processing()) return;

    this.processing.set(true);
    this.error.set(null);
    this.result.set(null);

    this.commerceService.process(date).subscribe({
      next: (result) => {
        this.result.set(result);
        this.processing.set(false);
        this.notifications.success(`Proceso finalizado: ${result.quarantinedCount} registros en cuarentena.`);
        this.loadDates();
      },
      error: (err: HttpErrorResponse) => {
        this.processing.set(false);
        if (err.status === 400) this.error.set([getErrorMessage(err), ...getErrorDetails(err)].join(' '));
      },
    });
  }

  protected loadDates(): void {
    this.loadingDates.set(true);
    this.commerceService.getProcessDates().subscribe({
      next: (dates) => {
        this.dates.set(dates);
        this.loadingDates.set(false);
      },
      error: () => this.loadingDates.set(false),
    });
  }
}
