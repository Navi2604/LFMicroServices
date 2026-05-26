import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SidebarComponent } from '../../shared/sidebar.component';
import { AuthService } from '../../core/services/auth.service';
import {
  DeviationApiService, DeviationDto,
  SiteProtocolApiService, SiteProtocolDto
} from '../../core/services/api.service';

type StatusTab = 'all' | 'Reported' | 'Under Review' | 'Resolved' | 'Accepted' | 'Rejected';

@Component({
  selector: 'app-deviations',
  standalone: true,
  imports: [CommonModule, FormsModule, SidebarComponent],
  templateUrl: './deviations.component.html'
})
export class DeviationsComponent implements OnInit {
  items:    DeviationDto[] = [];
  filtered: DeviationDto[] = [];
  spMap:    { [id: number]: SiteProtocolDto } = {};

  isLoading   = false;
  canReview   = false;
  userRole    = '';
  unreadCount = 0;
  errorMsg    = '';

  // Filter state
  activeTab:      StatusTab = 'all';
  severityFilter: string    = 'all';
  searchQuery:    string    = '';

  // Modal state
  showModal    = false;
  selected:    DeviationDto | null = null;
  newStatus    = '';
  isUpdating   = false;
  updateError  = '';
  updateSuccess = '';

  constructor(
    private devApi: DeviationApiService,
    private spApi:  SiteProtocolApiService,
    private authService: AuthService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.userRole  = this.authService.getRole();
    this.canReview = [ 'ClinicalTrialManager', 'RegulatoryOfficer']
                       .includes(this.userRole);
    this.loadSiteProtocols();
    this.load();
  }

  // ── Data loading ──────────────────────────────────────────────────

  load(): void {
    this.isLoading = true;
    this.errorMsg  = '';
    this.devApi.getAll().subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.success) {
          this.items = res.data ?? [];
          this.applyFilter();
        } else {
          this.errorMsg = res.message;
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMsg  = err.error?.message ?? 'Failed to load deviations.';
        this.cdr.detectChanges();
      }
    });
  }

  loadSiteProtocols(): void {
    this.spApi.getAll().subscribe({
      next: (res) => {
        if (res.success) {
          this.spMap = {};
          for (const sp of res.data) {
            this.spMap[sp.siteProtocolID] = sp;
          }
          this.cdr.detectChanges();
        }
      }
    });
  }

  // ── Filtering ─────────────────────────────────────────────────────

  switchTab(tab: StatusTab): void { this.activeTab = tab; this.applyFilter(); }
  onSeverityChange(): void { this.applyFilter(); }
  onSearchChange():   void { this.applyFilter(); }

  applyFilter(): void {
    let result = [...this.items];
    if (this.activeTab !== 'all') {
      result = result.filter(d => d.status === this.activeTab);
    }
    if (this.severityFilter !== 'all') {
      result = result.filter(d => d.severity === this.severityFilter);
    }
    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase();
      result = result.filter(d => {
        const sp = this.spMap[d.siteProtocolID];
        return d.description?.toLowerCase().includes(q) ||
               sp?.siteName?.toLowerCase().includes(q) ||
               sp?.protocolTitle?.toLowerCase().includes(q);
      });
    }
    this.filtered = result;
  }

  // ── Helpers ───────────────────────────────────────────────────────

  countByStatus(status: string): number {
    return this.items.filter(d => d.status === status).length;
  }

  getSiteName(spID: number): string {
    return this.spMap[spID]?.siteName || `Site #${spID}`;
  }

  getProtocolTitle(spID: number): string {
    return this.spMap[spID]?.protocolTitle || `Protocol #${spID}`;
  }

  getInvestigatorName(spID: number): string {
    return this.spMap[spID]?.investigatorName || '—';
  }

  getSeverityClass(severity: string): string {
    switch (severity) {
      case 'Minor':    return 'dv-pill dv-pill-green';
      case 'Major':    return 'dv-pill dv-pill-orange';
      case 'Critical': return 'dv-pill dv-pill-red';
      default:         return 'dv-pill dv-pill-gray';
    }
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'Reported':     return 'dv-pill dv-pill-blue';
      case 'Under Review': return 'dv-pill dv-pill-amber';
      case 'Resolved':     return 'dv-pill dv-pill-green';
      case 'Accepted':     return 'dv-pill dv-pill-teal';
      case 'Rejected':     return 'dv-pill dv-pill-gray';
      default:             return 'dv-pill dv-pill-gray';
    }
  }

  truncate(text: string, n = 60): string {
    if (!text) return '';
    return text.length > n ? text.substring(0, n) + '…' : text;
  }

  exportToCsv(): void {
  const headers = [
    'Deviation ID', 'Site', 'Protocol',
    'Investigator', 'Description', 'Severity', 'Status'
  ];
  const rows = this.filtered.map(d => [
    `DEV-${d.deviationID}`,
    this.getSiteName(d.siteProtocolID),
    this.getProtocolTitle(d.siteProtocolID),
    this.getInvestigatorName(d.siteProtocolID),
    d.description,
    d.severity,
    d.status
  ]);
  const date = new Date().toISOString().slice(0, 10);
  this.downloadCsv(`deviations-${date}.csv`, headers, rows);
}

private downloadCsv(filename: string, headers: string[], rows: (string | number)[][]): void {
  const escape = (val: string | number): string => {
    const s = String(val ?? '');
    return s.includes(',') || s.includes('"') || s.includes('\n')
      ? `"${s.replace(/"/g, '""')}"` : s;
  };
  const csv = [headers, ...rows].map(row => row.map(escape).join(',')).join('\n');
  const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
  const url  = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url; link.download = filename;
  document.body.appendChild(link); link.click();
  document.body.removeChild(link); URL.revokeObjectURL(url);
}

  // ── Modal ─────────────────────────────────────────────────────────

  openReview(item: DeviationDto): void {
    this.selected      = item;
    this.newStatus     = item.status;
    this.updateError   = '';
    this.updateSuccess = '';
    this.showModal     = true;
  }

  closeModal(): void {
    this.showModal     = false;
    this.selected      = null;
    this.newStatus     = '';
    this.updateError   = '';
    this.updateSuccess = '';
  }

  updateStatus(): void {
    if (!this.selected || this.newStatus === this.selected.status) return;

    this.isUpdating  = true;
    this.updateError = '';

    this.devApi.updateStatus(this.selected.deviationID, this.newStatus).subscribe({
      next: (res) => {
        this.isUpdating = false;
        if (res.success) {
          const idx = this.items.findIndex(d => d.deviationID === this.selected!.deviationID);
          if (idx > -1) {
            this.items[idx] = { ...this.items[idx], status: this.newStatus };
            this.selected   = this.items[idx];
          }
          this.applyFilter();
          this.updateSuccess = 'Status updated successfully.';
          setTimeout(() => this.closeModal(), 1300);
        } else {
          this.updateError = res.message || 'Failed to update status.';
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isUpdating = false;
        if (err.status === 403) {
          this.updateError = 'Permission denied. Ensure the backend C2.1 fix was applied and SiteService was restarted.';
        } else {
          this.updateError = err.error?.message ?? 'Failed to update status.';
        }
        this.cdr.detectChanges();
      }
    });
  }
}