// app.routes.ts
import { Routes } from '@angular/router';
import { AuthGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  // Public
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login/login.component')
        .then(m => m.LoginComponent)
  },
  {
    path: 'register',
    loadComponent: () =>
      import('./features/auth/register/register.component')
        .then(m => m.RegisterComponent)
  },

  // Protected — all roles
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./features/dashboard/dashboard.component')
        .then(m => m.DashboardComponent),
    canActivate: [AuthGuard]
  },
  {
    path: 'my-dashboard',
    loadComponent: () =>
      import('./features/patient-dashboard/patient-dashboard.component')
        .then(m => m.PatientDashboardComponent),
    canActivate: [AuthGuard],
    data: { roles: ['Patient'] }
  },
  {
    path: 'notifications',
    loadComponent: () =>
      import('./features/notifications/notifications.component')
        .then(m => m.NotificationsComponent),
    canActivate: [AuthGuard]
  },

  // Admin only
  {
    path: 'users',
    loadComponent: () =>
      import('./features/users/users.component')
        .then(m => m.UsersComponent),
    canActivate: [AuthGuard],
    data: { roles: ['Admin'] }
  },
  {
    path: 'roles',
    loadComponent: () =>
      import('./features/roles/roles.component')
        .then(m => m.RolesComponent),
    canActivate: [AuthGuard],
    data: { roles: ['Admin'] }
  },
  {
    path: 'audit',
    loadComponent: () =>
      import('./features/audit/audit.component')
        .then(m => m.AuditComponent),
    canActivate: [AuthGuard],
    data: { roles: ['Admin'] }
  },

  // Patients
  {
    path: 'patients',
    loadComponent: () =>
      import('./features/patients/patients.component')
        .then(m => m.PatientsComponent),
    canActivate: [AuthGuard],
    data: { roles: ['Admin', 'Investigator', 'ClinicalTrialManager', 'DataManager', 'RegulatoryOfficer'] }
  },

  // Protocols
  {
    path: 'protocols',
    loadComponent: () =>
      import('./features/protocols/protocols.component')
        .then(m => m.ProtocolsComponent),
    canActivate: [AuthGuard],
    data: { roles: ['Admin', 'ClinicalTrialManager', 'Investigator', 'RegulatoryOfficer', 'DataManager'] }
  },

  // Sites
  {
    path: 'sites',
    loadComponent: () =>
      import('./features/sites/sites.component')
        .then(m => m.SitesComponent),
    canActivate: [AuthGuard],
    data: { roles: ['Admin', 'ClinicalTrialManager', 'Investigator'] }
  },

  // Visits
  {
    path: 'visits',
    loadComponent: () =>
      import('./features/visits/visits.component')
        .then(m => m.VisitsComponent),
    canActivate: [AuthGuard],
    data: { roles: ['Admin', 'Investigator', 'ClinicalTrialManager'] }
  },

  // Defaults
  { path: '',   redirectTo: 'login', pathMatch: 'full' },
  { path: '**', redirectTo: 'login' }
];