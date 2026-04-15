import { Component, inject } from '@angular/core';
import { AuthApiService } from '../../../features/auth/services/auth-api.service';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { LoadingService } from '../../../shared/components/Loading/loading.service';

@Component({
  selector: 'app-header',
  imports: [],
  templateUrl: './header.html',
  styleUrl: './header.css',
})
export class Header {
  apiAuthService = inject(AuthApiService);
  authService = inject(AuthService);
  loading = inject(LoadingService);
  router = inject(Router);

  async logout(): Promise<void> {
    this.loading.show();
    await this.apiAuthService.logout();
    this.loading.hide();
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/login']);
    }
  }
}
