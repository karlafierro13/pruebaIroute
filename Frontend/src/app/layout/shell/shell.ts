import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { AuthService } from '../../core/services/auth.service';

interface NavItem {
  path: string;
  label: string;
  step: number;
}

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Shell {
  protected readonly auth = inject(AuthService);

  protected readonly navItems: NavItem[] = [
    { path: '/upload', label: 'Cargar archivo', step: 1 },
    { path: '/process', label: 'Procesar', step: 2 },
    { path: '/quarantine', label: 'Cuarentena', step: 3 },
  ];
}
