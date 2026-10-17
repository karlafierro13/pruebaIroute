import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-toast-container',
  template: `
    <div class="toasts" aria-live="polite">
      @for (toast of notifications.toasts(); track toast.id) {
        <div [class]="'toast toast--' + toast.type" role="status">
          <span>{{ toast.message }}</span>
          <button type="button" aria-label="Cerrar" (click)="notifications.dismiss(toast.id)">×</button>
        </div>
      }
    </div>
  `,
  styles: `
    .toasts {
      position: fixed;
      right: 1rem;
      bottom: 1rem;
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      z-index: 1000;
      max-width: min(420px, calc(100vw - 2rem));
    }
    .toast {
      display: flex;
      align-items: flex-start;
      gap: 0.75rem;
      padding: 0.75rem 1rem;
      border-radius: var(--radius);
      color: #fff;
      box-shadow: var(--shadow-lg);
      animation: slide-in 0.2s ease-out;
    }
    .toast span { flex: 1; }
    .toast button { background: none; border: 0; color: inherit; font-size: 1.25rem; line-height: 1; cursor: pointer; }
    .toast--success { background: var(--success); }
    .toast--error { background: var(--danger); }
    .toast--info { background: var(--primary); }
    @keyframes slide-in { from { transform: translateY(8px); opacity: 0; } }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ToastContainer {
  protected readonly notifications = inject(NotificationService);
}
