import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SidebarComponent } from '../../shared/sidebar.component';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';
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
  canReview     = false;
  canEdit       = false;
  isDM          = false;
  isInvestigator = false;
  myProtocolIds: number[] = [];
  mySPIds: number[] = [];  // investigator's siteProtocol IDs (set at init)
  userRole      = '';

  // DM inline quick-edit
  showQuickEdit    = false;
  quickEditMode    = '';       // 'description' | 'severity'
  quickEditDev: any = null;
  quickEditValue   = '';
  quickEditSaving  = false;
  quickEditError   = '';

  // Read-only detail overlay (non-DM)
  showDetailOverlay  = false;
  detailOverlayTitle = '';
  detailOverlayValue = '';

  // Edit modal state
  showEditModal  = false;
  editForm       = { description: '', severity: 'Minor' };
  editingDev: any = null;
  editSaving     = false;
  editError      = '';

  // DM: click description or severity to edit
  openQuickEdit(dev: any, mode: 'description' | 'severity'): void {
    if (!this.isDM) return;
    if (dev.status !== 'Reported') { this.quickEditError = 'Only Reported deviations can be edited.'; return; }
    this.quickEditDev   = dev;
    this.quickEditMode  = mode;
    this.quickEditValue = mode === 'description' ? dev.description : dev.severity;
    this.quickEditError = '';
    this.showQuickEdit  = true;
  }

  closeQuickEdit(): void { this.showQuickEdit = false; this.quickEditError = ''; }

  submitQuickEdit(): void {
    if (!this.quickEditDev || !this.quickEditValue.trim()) {
      this.quickEditError = 'Value cannot be empty.'; return;
    }
    this.quickEditSaving = true;
    const payload = {
      description: this.quickEditMode === 'description' ? this.quickEditValue.trim() : this.quickEditDev.description,
      severity:    this.quickEditMode === 'severity'    ? this.quickEditValue         : this.quickEditDev.severity
    };
    this.devApi.update(this.quickEditDev.deviationID, payload).subscribe({
      next: (r: any) => {
        this.quickEditSaving = false;
        const ok = r?.success ?? r?.Success ?? true;
        if (ok !== false) {
          const idx = this.items.findIndex((d: any) => d.deviationID === this.quickEditDev.deviationID);
          if (idx !== -1) {
            this.items[idx] = { ...this.items[idx], ...payload, dmEdited: true };
            this.applyFilter();
          }
          this.showQuickEdit = false;
          // 5A: Notify CTM that DM edited this deviation
          this.notifSvc.alert(this.authService.getUserId(),
            `DM updated ${this.quickEditMode} on Deviation #${this.quickEditDev?.deviationID}.`);
          this.cdr.detectChanges();
        } else { this.quickEditError = r?.message || 'Failed to save.'; }
      },
      error: () => { this.quickEditSaving = false; this.quickEditError = 'Failed to save.'; }
    });
  }

  // Non-DM: click to view full text
  openDetailOverlay(title: string, value: string): void {
    if (this.isDM) return;
    this.detailOverlayTitle = title;
    this.detailOverlayValue = value;
    this.showDetailOverlay  = true;
  }
  closeDetailOverlay(): void { this.showDetailOverlay = false; }

  openEdit(d: any): void {
    this.editingDev = d;
    this.editForm   = { description: d.description, severity: d.severity };
    this.editError  = '';
    this.showEditModal = true;
  }

  closeEdit(): void { this.showEditModal = false; }

  saveEdit(): void {
    if (!this.editForm.description.trim()) { this.editError = 'Description is required.'; return; }
    this.editSaving = true;
    this.editError  = '';
    this.devApi.update(this.editingDev.deviationID, this.editForm).subscribe({
      next: (r: any) => {
        this.editSaving = false;
        const ok = r?.success ?? r?.Success ?? true;
        if (ok !== false) {
          const idx = this.items.findIndex((d: any) => d.deviationID === this.editingDev.deviationID);
          if (idx !== -1) {
            this.items[idx] = { ...this.items[idx],
              description: this.editForm.description,
              severity:    this.editForm.severity };
            this.applyFilter();
          }
          this.editError = '';
          setTimeout(() => { this.showEditModal = false; }, 800);
        } else {
          this.editError = r?.message ?? r?.Message ?? 'Failed to update.';
        }
        this.cdr.detectChanges();
      },
      error: (err: any) => {
        this.editSaving = false;
        this.editError  = err?.error?.message ?? err?.error?.Message ?? 'Failed to update.';
        this.cdr.detectChanges();
      }
    });
  }
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
    private notifSvc:    NotificationService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.userRole  = this.authService.getRole();
    this.canReview = ['ClinicalTrialManager', 'RegulatoryOfficer']
                       .includes(this.userRole);
    this.canEdit = this.userRole === 'Investigator';
    this.isDM          = this.userRole === 'DataManager';
    this.isInvestigator = this.userRole === 'Investigator';
    if (this.isInvestigator) {
      const uid = this.authService.getUserId();
      this.spApi.getAll({ investigatorID: uid }).subscribe((r: any) => {
        if (r.success) {
          this.myProtocolIds = [...new Set((r.data ?? []).map((sp: any) => sp.protocolID))] as number[];
          this.mySPIds = (r.data ?? []).map((sp: any) => sp.siteProtocolID) as number[];
        }
        this.loadSiteProtocols();
        this.load();
      });
    } else {
      this.loadSiteProtocols();
      this.load();
    }
  }

  // ── Data loading ──────────────────────────────────────────────────

  load(): void {
    this.isLoading = true;
    this.errorMsg  = '';
    this.devApi.getAll().subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.success) {
          let devs = res.data ?? [];
          // Investigator: filter to their assigned siteProtocols
          // Use mySPIds set at init (avoids async timing issues with spMap)
          if (this.isInvestigator && this.mySPIds.length > 0) {
            const spSet = new Set(this.mySPIds);
            devs = devs.filter((d: any) => spSet.has(d.siteProtocolID));
          }
          this.items = devs;
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
    this.currentPage = 1;
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


  // ── Pagination ────────────────────────────────────────────────────
  currentPage         = 1;
  pageSize            = 10;
  itemsPerPageOptions = [5, 10, 20, 50];

  get paginated(): any[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.filtered.slice(start, start + this.pageSize);
  }
  get totalPages(): number {
    return Math.max(1, Math.ceil(this.filtered.length / this.pageSize));
  }
  getPageNumbers(): number[] {
    const range = 2;
    const pages: number[] = [];
    for (let i = Math.max(1, this.currentPage - range);
             i <= Math.min(this.totalPages, this.currentPage + range); i++) {
      pages.push(i);
    }
    return pages;
  }
  nextPage():                void { if (this.currentPage < this.totalPages) this.currentPage++; }
  prevPage():                void { if (this.currentPage > 1) this.currentPage--; }
  goToPage(p: number):       void { this.currentPage = p; }
  changePageSize(s: number): void { this.pageSize = +s; this.currentPage = 1; }

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
          const devId = this.selected?.deviationID;
          if (this.newStatus === 'Under Review')
            this.notifSvc.alert(this.authService.getUserId(),
              `Deviation #${devId} is now Under Review.`);
          else if (this.newStatus === 'Resolved')
            this.notifSvc.system(this.authService.getUserId(),
              `Deviation #${devId} has been resolved and closed.`);
          else if (this.newStatus === 'Accepted')
            this.notifSvc.system(this.authService.getUserId(),
              `Deviation #${devId} has been accepted by the Regulatory Officer.`);
          else if (this.newStatus === 'Rejected')
            this.notifSvc.alert(this.authService.getUserId(),
              `Deviation #${devId} has been rejected. Please review and resubmit.`);
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