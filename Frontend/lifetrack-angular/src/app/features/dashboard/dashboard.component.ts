// dashboard.component.ts
import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../core/services/auth.service';
import {
  PatientApiService, ProtocolApiService, SiteApiService,
  UserApiService, SiteProtocolApiService,
  EnrollmentApiService, AuditApiService
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
  userId          = 0;
  unreadCount     = 0;

  totalUsers      = 0;
  totalPatients   = 0;
  myPatientCount  = 0;
  activeProtocols = 0;
  totalSites      = 0;
  upcomingVisits  = 0;
  completedVisits = 0;

  recentProtocols:  any[] = [];
  recentSites:      any[] = [];
  recentAuditLogs:  any[] = [];

  constructor(
    private authService:     AuthService,
    private patientApi:      PatientApiService,
    private protocolApi:     ProtocolApiService,
    private siteApi:         SiteApiService,
    private userApi:         UserApiService,
    private siteProtocolApi: SiteProtocolApiService,
    private enrollmentApi:   EnrollmentApiService,
    private auditApi:        AuditApiService,
    private cdr:             ChangeDetectorRef,
    private router:          Router
  ) {}

  ngOnInit(): void {
    this.userName = this.authService.getUserName();
    this.userRole = this.authService.getRole();
    this.userId   = this.authService.getUserId();
    // Redirect Patient role to their own dashboard
    if (this.userRole === 'Patient') {
      this.router.navigate(['/my-dashboard']);
      return;
    }
    this.loadStats();
  }

  hasRole(roles: string[]): boolean { return roles.includes(this.userRole); }
  isInvestigator(): boolean { return this.userRole === 'Investigator'; }

  hasAssignedProtocols = true; // loaded from siteProtocols

  loadStats(): void {
    if (this.isInvestigator()) {
      // ── Investigator: only his assigned data ──────────────────
      this.siteProtocolApi.getAll({ investigatorID: this.userId }).subscribe(r => {
        if (r.success) {
          const sps = r.data ?? [];
          const protocolIds = [...new Set(sps.map((sp: any) => sp.protocolID))] as number[];
          const siteIds     = [...new Set(sps.map((sp: any) => sp.siteID))] as number[];
          const spIds       = sps.map((sp: any) => sp.siteProtocolID) as number[];

          this.totalSites = siteIds.length;

          // Filter protocols
          this.protocolApi.getAll().subscribe(pr => {
            if (pr.success) {
              const now = new Date();
              const mine = pr.data.filter(p => protocolIds.includes(p.protocolID));
              this.activeProtocols = mine.filter(p =>
                new Date(p.startDate) <= now && new Date(p.endDate) >= now).length;
              this.recentProtocols = mine.slice(0, 4);
              this.cdr.detectChanges();
            }
          });

          // Filter sites
          this.siteApi.getAll().subscribe(sr => {
            if (sr.success) {
              this.recentSites = sr.data.filter(s => siteIds.includes(s.siteID)).slice(0, 4);
              this.cdr.detectChanges();
            }
          });

          // Count my patients via enrollments
          if (spIds.length > 0) {
            const calls = spIds.map((id: number) =>
              this.enrollmentApi.getAll({ siteProtocolId: id }).toPromise()
            );
            Promise.all(calls).then(results => {
              const patientIds = new Set<number>();
              results.forEach((res: any) => {
                if (res?.success) res.data.forEach((e: any) => patientIds.add(e.patientID));
              });
              this.myPatientCount = patientIds.size;
              this.cdr.detectChanges();
            });
          }

          this.cdr.detectChanges();
        }
      });

    } else {
      // ── All other roles: see everything ───────────────────────
      if (this.hasRole(['Admin','ClinicalTrialManager','DataManager','RegulatoryOfficer'])) {
        this.patientApi.getAll().subscribe(r => {
          if (r.success) { this.totalPatients = r.data.length; this.cdr.detectChanges(); }
        });
      }

      if (this.hasRole(['Admin','ClinicalTrialManager','DataManager','RegulatoryOfficer'])) {
        this.protocolApi.getAll().subscribe(r => {
          if (r.success) {
            const now = new Date();
            this.activeProtocols = r.data.filter(p =>
              new Date(p.startDate) <= now && new Date(p.endDate) >= now).length;
            this.recentProtocols = r.data.slice(0, 4);
            this.cdr.detectChanges();
          }
        });
      }

      if (this.hasRole(['Admin','ClinicalTrialManager'])) {
        this.siteApi.getAll().subscribe(r => {
          if (r.success) {
            this.totalSites  = r.data.length;
            this.recentSites = r.data.slice(0, 4);
            this.cdr.detectChanges();
          }
        });

        this.userApi.getAll().subscribe(r => {
          if (r.success) { this.totalUsers = r.data.length; this.cdr.detectChanges(); }
        });
      }

      // Recent audit logs — Admin only
      if (this.hasRole(['Admin'])) {
        this.auditApi.getLogs({ page: 1, pageSize: 10 }).subscribe(r => {
          if (r.success) {
            this.recentAuditLogs = r.data?.logs ?? r.data ?? [];
            this.cdr.detectChanges();
          }
        });
      }
    }
  }

  getProtocolBadge(status: string): string {
    const m: Record<string,string> = {
      'Ongoing':'db-badge-green','Active':'db-badge-green',
      'Upcoming':'db-badge-amber','Completed':'db-badge-blue',
      'Archived':'db-badge-gray','Cancelled':'db-badge-red'
    };
    return m[status] ?? 'db-badge-gray';
  }

  getSiteBadge(status: string): string {
    const m: Record<string,string> = {
      'Active':'db-badge-green','Ongoing':'db-badge-green',
      'Upcoming':'db-badge-amber','Inactive':'db-badge-gray','Closed':'db-badge-red'
    };
    return m[status] ?? 'db-badge-gray';
  }
}