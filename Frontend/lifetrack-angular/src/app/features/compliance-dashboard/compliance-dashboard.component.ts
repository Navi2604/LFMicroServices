import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { SidebarComponent } from '../../shared/sidebar.component';
import { AuthService } from '../../core/services/auth.service';
import {
  DocumentApiService,    DocumentDto,
  AdverseEventApiService, AdverseEventDto,
  DeviationApiService,   DeviationDto,
  AuditApiService
} from '../../core/services/api.service';

@Component({
  selector: 'app-compliance-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, SidebarComponent],
  templateUrl: './compliance-dashboard.component.html'
})
export class ComplianceDashboardComponent implements OnInit {

  userName    = '';
  unreadCount = 0;

  // Pending document reviews
  pendingDocs:     DocumentDto[] = [];
  docsLoading      = false;

  // Escalated adverse events
  escalatedAEs:    AdverseEventDto[] = [];
  aesLoading       = false;

  // Open critical deviations
  criticalDevs:    DeviationDto[] = [];
  devsLoading      = false;

  // Recent audit entries
  recentAudit:     any[] = [];
  auditLoading     = false;

  constructor(
    private docApi:  DocumentApiService,
    private aeApi:   AdverseEventApiService,
    private devApi:  DeviationApiService,
    private auditApi: AuditApiService,
    private authService: AuthService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.userName = this.authService.getUserName?.() ?? 'Regulatory Officer';
    this.loadAll();
  }

  loadAll(): void {
    this.loadDocs();
    this.loadAEs();
    this.loadDeviations();
    this.loadAudit();
  }

  // ── Pending document reviews ──────────────────────────────────

  loadDocs(): void {
    this.docsLoading = true;
    this.docApi.getAll({ status: 'Under Review' }).subscribe({
      next: (r) => {
        this.docsLoading = false;
        if (r.success) this.pendingDocs = r.data ?? [];
        this.cdr.detectChanges();
      },
      error: () => { this.docsLoading = false; this.cdr.detectChanges(); }
    });
  }

  // ── Escalated adverse events ──────────────────────────────────

  loadAEs(): void {
    this.aesLoading = true;
    this.aeApi.getAll({ status: 'Escalated' }).subscribe({
      next: (r) => {
        this.aesLoading = false;
        if (r.success) this.escalatedAEs = r.data ?? [];
        this.cdr.detectChanges();
      },
      error: () => { this.aesLoading = false; this.cdr.detectChanges(); }
    });
  }

  // ── Critical open deviations ──────────────────────────────────

  loadDeviations(): void {
    this.devsLoading = true;
    this.devApi.getAll({ severity: 'Critical' }).subscribe({
      next: (r) => {
        this.devsLoading = false;
        if (r.success) {
          // Only show open items — exclude terminal statuses
          const terminal = ['Resolved', 'Accepted', 'Rejected'];
          this.criticalDevs = (r.data ?? []).filter(
            (d: DeviationDto) => !terminal.includes(d.status)
          );
        }
        this.cdr.detectChanges();
      },
      error: () => { this.devsLoading = false; this.cdr.detectChanges(); }
    });
  }

  // ── Recent audit entries ──────────────────────────────────────

  loadAudit(): void {
    this.auditLoading = true;
    this.auditApi.getLogs({ pageSize: 5, page: 1 }).subscribe({
      next: (r) => {
        this.auditLoading = false;
        if (r.success) {
          const data = r.data;
          this.recentAudit = Array.isArray(data)
            ? data.slice(0, 5)
            : (data?.logs ?? []).slice(0, 5);
        }
        this.cdr.detectChanges();
      },
      error: () => { this.auditLoading = false; this.cdr.detectChanges(); }
    });
  }

  // ── Helpers ───────────────────────────────────────────────────

  getAEBadgeStyle(severity: string): string {
    switch (severity) {
      case 'Critical': return 'background:#fce8e6;color:#c0392b;';
      case 'Severe':   return 'background:#fff1e6;color:#c2410c;';
      default:         return 'background:#fef3e2;color:#b45309;';
    }
  }

  getDevBadgeStyle(status: string): string {
    switch (status) {
      case 'Reported':     return 'background:#e8f0fe;color:#1a56db;';
      case 'Under Review': return 'background:#fef3e2;color:#b45309;';
      default:             return 'background:#f3f4f6;color:#6b7280;';
    }
  }

  getDocTypelabel(type: string): string {
    const map: Record<string, string> = {
      'Protocol':             'Protocol',
      'Amendment':            'Amendment',
      'InformedConsentForm':  'ICF',
      'InvestigatorBrochure': 'IB',
      'SafetyReport':         'Safety Rpt',
      'MonitoringReport':     'Monitor Rpt',
      'RegulatorySubmission': 'Reg. Sub.'
    };
    return map[type] ?? type;
  }

  getAuditActionStyle(action: string): string {
    const a = (action || '').toUpperCase();
    if (a === 'CREATE') return 'background:#e6f4ea;color:#1e7e34;';
    if (a === 'UPDATE') return 'background:#e8f0fe;color:#1a56db;';
    if (a === 'DELETE') return 'background:#fce8e6;color:#c0392b;';
    if (a === 'LOGIN')  return 'background:#fef3e2;color:#b45309;';
    return 'background:#f3f4f6;color:#6b7280;';
  }

  get totalAlerts(): number {
    return this.pendingDocs.length + this.escalatedAEs.length + this.criticalDevs.length;
  }
}