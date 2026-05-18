// dashboard.component.ts
import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import {
  PatientApiService, ProtocolApiService,
  SiteApiService, UserApiService
} from '../../core/services/api.service';
import { SidebarComponent } from '../../shared/sidebar.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, SidebarComponent],
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent implements OnInit {
  userName        = '';
  userRole        = '';
  unreadCount     = 0;

  totalUsers      = 0;
  totalPatients   = 0;
  activeProtocols = 0;
  totalSites      = 0;
  upcomingVisits  = 0;
  completedVisits = 0;

  recentProtocols: any[] = [];
  recentSites:     any[] = [];

  constructor(
    private authService: AuthService,
    private patientApi:  PatientApiService,
    private protocolApi: ProtocolApiService,
    private siteApi:     SiteApiService,
    private userApi:     UserApiService,
    private cdr:         ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.userName = this.authService.getUserName();
    this.userRole = this.authService.getRole();
    this.loadStats();
  }

  hasRole(roles: string[]): boolean {
    return roles.includes(this.userRole);
  }

  loadStats(): void {
    if (this.hasRole(['Admin', 'ClinicalTrialManager',
                      'Investigator', 'DataManager', 'RegulatoryOfficer'])) {

      this.patientApi.getAll().subscribe(r => {
        if (r.success) {
          this.totalPatients = r.data.length;
          this.cdr.detectChanges();
        }
      });

      this.protocolApi.getAll().subscribe(r => {
        if (r.success) {
          const now = new Date();
          this.activeProtocols = r.data.filter(p =>
            new Date(p.startDate) <= now && new Date(p.endDate) >= now
          ).length;
          this.recentProtocols = [...r.data.slice(0, 4)];
          this.cdr.detectChanges();
        }
      });
    }

    if (this.hasRole(['Admin', 'ClinicalTrialManager', 'Investigator'])) {
      this.siteApi.getAll().subscribe(r => {
        if (r.success) {
          this.totalSites  = r.data.length;
          this.recentSites = [...r.data.slice(0, 4)];
          this.cdr.detectChanges();
        }
      });
    }

    if (this.hasRole(['Admin', 'ClinicalTrialManager'])) {
      this.userApi.getAll().subscribe(r => {
        if (r.success) {
          this.totalUsers = r.data.length;
          this.cdr.detectChanges();
        }
      });
    }
  }

  getProtocolBadge(status: string): string {
    const map: Record<string, string> = {
      'Active':    'lt-badge lt-badge-green',
      'Ongoing':   'lt-badge lt-badge-green',
      'Upcoming':  'lt-badge lt-badge-amber',
      'Completed': 'lt-badge lt-badge-blue',
      'On Hold':   'lt-badge lt-badge-amber',
      'Cancelled': 'lt-badge lt-badge-red',
      'Draft':     'lt-badge lt-badge-gray'
    };
    return map[status] ?? 'lt-badge lt-badge-gray';
  }

  getSiteBadge(status: string): string {
    const map: Record<string, string> = {
      'Active':   'lt-badge lt-badge-green',
      'Ongoing':  'lt-badge lt-badge-green',
      'Upcoming': 'lt-badge lt-badge-amber',
      'Inactive': 'lt-badge lt-badge-gray',
      'Closed':   'lt-badge lt-badge-red'
    };
    return map[status] ?? 'lt-badge lt-badge-gray';
  }
}