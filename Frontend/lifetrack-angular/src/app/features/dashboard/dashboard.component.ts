// dashboard.component.ts
import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../core/services/auth.service';
import {
  PatientApiService, ProtocolApiService, SiteApiService,
  UserApiService, SiteProtocolApiService, EnrollmentApiService
} from '../../core/services/api.service';
import { SidebarComponent } from '../../shared/sidebar.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, SidebarComponent],
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent implements OnInit {
  userName = '';
  userRole = '';
  userId   = 0;
  unreadCount = 0;

  // ── KPI numbers ──────────────────────────────────────────────
  totalUsers       = 0;
  totalPatients    = 0;
  myPatientCount   = 0;
  activeProtocols  = 0;
  completedProtocols = 0;
  upcomingProtocols  = 0;
  totalSites       = 0;
  activeSites      = 0;
  totalEnrollments = 0;
  activeEnrollments = 0;
  upcomingVisits   = 0;
  completedVisits  = 0;
  activeInvestigators = 0;

  // ── Loading flags ─────────────────────────────────────────────
  loadingUsers      = false;
  loadingPatients   = false;
  loadingProtocols  = false;
  loadingSites      = false;
  loadingEnrollments = false;

  hasAssignedProtocols = true;

  constructor(
    private authService:     AuthService,
    private patientApi:      PatientApiService,
    private protocolApi:     ProtocolApiService,
    private siteApi:         SiteApiService,
    private userApi:         UserApiService,
    private siteProtocolApi: SiteProtocolApiService,
    private enrollmentApi:   EnrollmentApiService,
    private cdr:             ChangeDetectorRef,
    private router:          Router
  ) {}

  ngOnInit(): void {
    this.userName = this.authService.getUserName();
    this.userRole = this.authService.getRole();
    this.userId   = this.authService.getUserId();
    if (this.userRole === 'Patient') {
      this.router.navigate(['/my-dashboard']);
      return;
    }
    this.loadStats();
  }

  hasRole(roles: string[]): boolean { return roles.includes(this.userRole); }
  isInvestigator(): boolean         { return this.userRole === 'Investigator'; }

  get currentHour(): number { return new Date().getHours(); }
  get greeting(): string {
    const h = this.currentHour;
    if (h < 12) return 'Good morning';
    if (h < 17) return 'Good afternoon';
    return 'Good evening';
  }

  loadStats(): void {
    const now = new Date();

    if (this.isInvestigator()) {
      // Investigator: load only assigned data
      this.siteProtocolApi.getAll({ investigatorID: this.userId }).subscribe(r => {
        if (!r.success) return;
        const sps       = r.data ?? [];
        const protocolIds = [...new Set(sps.map((sp: any) => sp.protocolID))] as number[];
        const siteIds     = [...new Set(sps.map((sp: any) => sp.siteID))]     as number[];
        const spIds       = sps.map((sp: any) => sp.siteProtocolID)            as number[];

        this.hasAssignedProtocols = sps.length > 0;
        this.totalSites = siteIds.length;

        this.protocolApi.getAll().subscribe(pr => {
          if (!pr.success) return;
          const mine = pr.data.filter((p: any) => protocolIds.includes(p.protocolID));
          this.activeProtocols    = mine.filter((p: any) => new Date(p.startDate) <= now && new Date(p.endDate) >= now).length;
          this.completedProtocols = mine.filter((p: any) => p.status === 'Completed').length;
          this.upcomingProtocols  = mine.filter((p: any) => new Date(p.startDate) > now).length;
          this.cdr.detectChanges();
        });

        this.siteApi.getAll().subscribe(sr => {
          if (!sr.success) return;
          const mySites   = sr.data.filter((s: any) => siteIds.includes(s.siteID));
          this.activeSites = mySites.filter((s: any) => s.status === 'Active').length;
          this.cdr.detectChanges();
        });

        if (spIds.length > 0) {
          Promise.all(spIds.map((id: number) =>
            this.enrollmentApi.getAll({ siteProtocolId: id }).toPromise()
          )).then(results => {
            const patientIds = new Set<number>();
            let activeEnr = 0;
            results.forEach((res: any) => {
              if (res?.success) res.data.forEach((e: any) => {
                patientIds.add(e.patientID);
                if (e.status === 'Active' || e.status === 'Enrolled') activeEnr++;
              });
            });
            this.myPatientCount   = patientIds.size;
            this.activeEnrollments = activeEnr;
            this.totalEnrollments  = patientIds.size;
            this.cdr.detectChanges();
          });
        }

        this.cdr.detectChanges();
      });

    } else {
      // All other staff roles
      if (this.hasRole(['Admin', 'ClinicalTrialManager'])) {
        this.loadingUsers = true;
        this.userApi.getAll().subscribe(r => {
          this.loadingUsers = false;
          if (!r.success) return;
          this.totalUsers = r.data.length;
          this.activeInvestigators = r.data.filter((u: any) => u.roleName === 'Investigator' && u.isActive).length;
          this.cdr.detectChanges();
        });
      }

      if (this.hasRole(['Admin', 'ClinicalTrialManager', 'DataManager', 'RegulatoryOfficer'])) {
        this.loadingPatients = true;
        this.patientApi.getAll().subscribe(r => {
          this.loadingPatients = false;
          if (!r.success) return;
          this.totalPatients = r.data.length;
          this.cdr.detectChanges();
        });

        this.loadingProtocols = true;
        this.protocolApi.getAll().subscribe(r => {
          this.loadingProtocols = false;
          if (!r.success) return;
          this.activeProtocols    = r.data.filter((p: any) => new Date(p.startDate) <= now && new Date(p.endDate) >= now).length;
          this.completedProtocols = r.data.filter((p: any) => p.status === 'Completed').length;
          this.upcomingProtocols  = r.data.filter((p: any) => new Date(p.startDate) > now).length;
          this.cdr.detectChanges();
        });
      }

      if (this.hasRole(['Admin', 'ClinicalTrialManager'])) {
        this.loadingSites = true;
        this.siteApi.getAll().subscribe(r => {
          this.loadingSites = false;
          if (!r.success) return;
          this.totalSites  = r.data.length;
          this.activeSites = r.data.filter((s: any) => s.status === 'Active').length;
          this.cdr.detectChanges();
        });

        this.loadingEnrollments = true;
        this.enrollmentApi.getAll().subscribe(r => {
          this.loadingEnrollments = false;
          if (!r.success) return;
          this.totalEnrollments  = r.data.length;
          this.activeEnrollments = r.data.filter((e: any) => e.status === 'Active' || e.status === 'Enrolled').length;
          this.cdr.detectChanges();
        });
      }
    }
  }
}