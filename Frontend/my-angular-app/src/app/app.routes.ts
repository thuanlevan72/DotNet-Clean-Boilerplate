import { authGuard } from './core/guards/auth.guard';
import { guestGuard } from './core/guards/guest.guard';
import { Todo } from './features/Todo/components/todo/todo';
import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./features/auth/components/login/login').then((m) => m.Login),
  },
  {
    path: 'register',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./features/auth/components/register/register').then((m) => m.Register),
  },
  {
    path: '',
    canActivate: [authGuard], // 🛡️ Bọc Guard ở ngay Layout cha. Chưa đăng nhập là cấm toàn bộ con bên trong!
    loadComponent: () =>
      import('./layout/main-layout/main-layout.component').then((m) => m.MainLayoutComponent),
    children:[
      {
        path: '',
        loadComponent: () =>
          import('./features/Todo/components/todo/todo').then((m) => m.Todo),
      }
    ]
  }
];
