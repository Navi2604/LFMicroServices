import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SidebarComponent } from '../../shared/sidebar.component';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import {
  AdverseEventApiService, AdverseEventDto,
  ProtocolApiService, SiteProtocolApiService
} from '../../core/services/api.service';

type StatusTab = 'all' | 'Reported' | 'Under Review' | 'Resolved' | 'Escalated';

@Component({
  selector: 'app-adverse-events',
  standalone: true,
  imports: [CommonModule, FormsModule, SidebarComponent],
  templateUrl: './adverse-events.component.html'
})
export class AdverseEventsComponent implements OnInit {
  events:      AdverseEventDto[] = [];
  filtered:    AdverseEventDto[] = [];
  protocolMap: { [id: number]: string } = {};

  isLoading   = false;
  canReview     = false;
  canEdit       = false;   // Investigator: edit description+severity on Reported
  isDM          = false;   // DataManager: inline edit description+severity
  isInvestigator = false;
  myProtocolIds: number[] = [];  // Investigator: only show their protocols

  // Inline quick-edit modal (DM)
  showQuickEdit    = false;
  quickEditMode    = '';       // 'description' | 'severity'
  quickEditAe: any = null;
  quickEditValue   = '';
  quickEditSaving  = false;
  quickEditError   = '';

  // Read-only detail overlay (non-DM clicking description/severity)
  showDetailOverlay   = false;
  detailOverlayTitle  = '';
  detailOverlayValue  = '';

  // Edit modal state
  showEditModal  = false;
  editForm       = { description: '', severity: 'Mild' };
  editingEvent: any = null;
  editSaving     = false;
  editError      = '';

  // DM: click description or severity cell to edit
  openQuickEdit(ae: any, mode: 'description' | 'severity'): void {
    if (!this.isDM) return;
    if (ae.status !== 'Reported') { this.quickEditError = 'Only Reported AEs can be edited.'; return; }
    this.quickEditAe    = ae;
    this.quickEditMode  = mode;
    this.quickEditValue = mode === 'description' ? ae.description : ae.severity;
    this.quickEditError = '';
    this.showQuickEdit  = true;
  }

  closeQuickEdit(): void { this.showQuickEdit = false; this.quickEditError = ''; }

  submitQuickEdit(): void {
    if (!this.quickEditAe || !this.quickEditValue.trim()) {
      this.quickEditError = 'Value cannot be empty.'; return;
    }
    this.quickEditSaving = true;
    const payload = {
      description: this.quickEditMode === 'description' ? this.quickEditValue.trim() : this.quickEditAe.description,
      severity:    this.quickEditMode === 'severity'    ? this.quickEditValue         : this.quickEditAe.severity
    };
    this.aeApi.update(this.quickEditAe.eventID, payload).subscribe({
      next: (r: any) => {
        this.quickEditSaving = false;
        const ok = r?.success ?? r?.Success ?? true;
        if (ok !== false) {
          const idx = this.events.findIndex((e: any) => e.eventID === this.quickEditAe.eventID);
          if (idx !== -1) {
            this.events[idx] = { ...this.events[idx], ...payload, dmEdited: true };
            this.applyFilter();
          }
          this.showQuickEdit = false;
          // 5A: Notify CTM that DM edited this AE
          this.notifSvc.alert(this.authService.getUserId(),
            `DM updated ${this.quickEditMode} on AE #${this.quickEditAe?.eventID}.`);
          this.cdr.detectChanges();
        } else { this.quickEditError = r?.message || 'Failed to save.'; }
      },
      error: () => { this.quickEditSaving = false; this.quickEditError = 'Failed to save.'; }
    });
  }

  // Non-DM: click description or severity to view full text
  openDetailOverlay(title: string, value: string): void {
    if (this.isDM) return;
    this.detailOverlayTitle = title;
    this.detailOverlayValue = value;
    this.showDetailOverlay  = true;
  }
  closeDetailOverlay(): void { this.showDetailOverlay = false; }

  openEdit(ae: any): void {
    this.editingEvent = ae;
    this.editForm     = { description: ae.description, severity: ae.severity };
    this.editError    = '';
    this.showEditModal = true;
  }

  closeEdit(): void { this.showEditModal = false; }

  saveEdit(): void {
    if (!this.editForm.description.trim()) { this.editError = 'Description is required.'; return; }
    this.editSaving = true;
    this.editError  = '';
    this.aeApi.update(this.editingEvent.eventID, this.editForm).subscribe({
      next: (r: any) => {
        this.editSaving = false;
        // Handle both camelCase and PascalCase response
        const ok = r?.success ?? r?.Success ?? true; // default true since data saved
        if (ok !== false) {
          const idx = this.events.findIndex((e: any) => e.eventID === this.editingEvent.eventID);
          if (idx !== -1) {
            this.events[idx] = { ...this.events[idx],
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
  userRole    = '';
  unreadCount = 0;
  errorMsg    = '';

  // Filter state
  activeTab:      StatusTab = 'all';
  severityFilter: string    = 'all';
  searchQuery:    string    = '';

  // Modal state
  showModal     = false;
  selectedEvent: AdverseEventDto | null = null;
  newStatus     = '';
  isUpdating    = false;
  updateError   = '';
  updateSuccess = '';

  constructor(
    private aeApi:       AdverseEventApiService,
    private protocolApi: ProtocolApiService,
    private authService:    AuthService,
    private notifSvc:      NotificationService,
    private siteProtocolApi: SiteProtocolApiService,
    private cdr:         ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.userRole  = this.authService.getRole();
    this.canReview = ['ClinicalTrialManager', 'RegulatoryOfficer']
                       .includes(this.userRole);
    this.canEdit = this.userRole === 'Investigator';
    this.isDM          = this.userRole === 'DataManager';
    this.isInvestigator = this.userRole === 'Investigator';
    // Investigator: get their siteProtocol assignments first, then load AEs
    if (this.isInvestigator) {
      const uid = this.authService.getUserId();
      this.siteProtocolApi.getAll({ investigatorID: uid }).subscribe(r => {
        if (r.success) {
          this.myProtocolIds = [...new Set((r.data ?? []).map((sp: any) => sp.protocolID))];
        }
        this.loadProtocols();
        this.load();
      });
    } else {
      this.loadProtocols();
      this.load();
    }
  }

  // ── Data loading ──────────────────────────────────────────────────

  load(): void {
    this.isLoading = true;
    this.errorMsg  = '';
    this.aeApi.getAll().subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.success) {
          let evts = res.data ?? [];
          // Investigator: only show AEs from their assigned protocols
          if (this.isInvestigator && this.myProtocolIds.length > 0) {
            evts = evts.filter((e: any) => this.myProtocolIds.includes(e.protocolID));
          }
          this.events = evts;
          this.applyFilter();
        } else {
          this.errorMsg = res.message;
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMsg  = err.error?.message ?? 'Failed to load adverse events.';
        this.cdr.detectChanges();
      }
    });
  }

  loadProtocols(): void {
    this.protocolApi.getAll().subscribe({
      next: (res) => {
        if (res.success) {
          this.protocolMap = {};
          for (const p of res.data) {
            this.protocolMap[p.protocolID] = p.title;
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
    let result = [...this.events];
    if (this.activeTab !== 'all') {
      result = result.filter(e => e.status === this.activeTab);
    }
    if (this.severityFilter !== 'all') {
      result = result.filter(e => e.severity === this.severityFilter);
    }
    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase();
      result = result.filter(e =>
        (e.patientName?.toLowerCase().includes(q)) ||
        (e.description?.toLowerCase().includes(q))
      );
    }
    result.sort((a, b) =>
      new Date(b.reportedDate).getTime() - new Date(a.reportedDate).getTime()
    );
    this.filtered = result;
    this.currentPage = 1;
  }

  // ── Helpers ───────────────────────────────────────────────────────

  countByStatus(status: string): number {
    return this.events.filter(e => e.status === status).length;
  }

  getProtocolName(protocolID: number): string {
    return this.protocolMap[protocolID] || `Protocol #${protocolID}`;
  }

  getSeverityClass(severity: string): string {
    switch (severity) {
      case 'Mild':     return 'ae-pill ae-pill-green';
      case 'Moderate': return 'ae-pill ae-pill-amber';
      case 'Severe':   return 'ae-pill ae-pill-orange';
      case 'Critical': return 'ae-pill ae-pill-red';
      default:         return 'ae-pill ae-pill-gray';
    }
  }

  get availableStatuses(): string[] {
  if (this.userRole === 'RegulatoryOfficer') {
    // RO only resolves escalated items or sends back for more CTM review
    return ['Under Review', 'Resolved'];
  }
  // CTM and Admin see all options
  return ['Reported', 'Under Review', 'Resolved', 'Escalated'];
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
      case 'Reported':     return 'ae-pill ae-pill-blue';
      case 'Under Review': return 'ae-pill ae-pill-amber';
      case 'Resolved':     return 'ae-pill ae-pill-green';
      case 'Escalated':    return 'ae-pill ae-pill-red';
      default:             return 'ae-pill ae-pill-gray';
    }
  }

  truncate(text: string, n = 60): string {
    if (!text) return '';
    return text.length > n ? text.substring(0, n) + '…' : text;
  }

  exportToCsv(): void {
  const headers = [
    'Event ID', 'Patient Name', 'Patient ID',
    'Protocol', 'Description', 'Severity', 'Status', 'Reported Date'
  ];
  const rows = this.filtered.map(ae => [
    `AE-${ae.eventID}`,
    ae.patientName,
    `PT-${ae.patientID}`,
    this.getProtocolName(ae.protocolID),
    ae.description,
    ae.severity,
    ae.status,
    new Date(ae.reportedDate).toLocaleDateString('en-GB')
  ]);
  const date = new Date().toISOString().slice(0, 10);
  this.downloadCsv(`adverse-events-${date}.csv`, headers, rows);
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

  openReview(event: AdverseEventDto): void {
    this.selectedEvent = event;
    this.newStatus     = event.status;
    this.updateError   = '';
    this.updateSuccess = '';
    this.showModal     = true;
  }

  closeModal(): void {
    this.showModal     = false;
    this.selectedEvent = null;
    this.newStatus     = '';
    this.updateError   = '';
    this.updateSuccess = '';
  }

  updateStatus(): void {
    if (!this.selectedEvent || this.newStatus === this.selectedEvent.status) return;

    this.isUpdating  = true;
    this.updateError = '';

    this.aeApi.updateStatus(this.selectedEvent.eventID, this.newStatus).subscribe({
      next: (res) => {
        this.isUpdating = false;
        if (res.success) {
          // Update the in-memory list so the table reflects the change immediately
          const idx = this.events.findIndex(e => e.eventID === this.selectedEvent!.eventID);
          if (idx > -1) {
            this.events[idx] = { ...this.events[idx], status: this.newStatus };
            // Also update the cached selectedEvent so the modal shows new status
            this.selectedEvent = this.events[idx];
          }
          this.applyFilter();
          this.updateSuccess = 'Status updated successfully.';

          // Notify relevant users based on new status
          const aeId = this.selectedEvent?.eventID;
          const patId = this.selectedEvent?.patientID;
          if (this.newStatus === 'Under Review')
            this.notifSvc.alert(this.authService.getUserId(),
              `AE #${aeId} is now Under Review.`);
          else if (this.newStatus === 'Escalated') {
            // Notify current reviewer (PatientID != UserID so we only notify staff)
            this.notifSvc.alert(this.authService.getUserId(),
              `AE #${aeId} has been escalated. Regulatory Officer review required.`);
          } else if (this.newStatus === 'Resolved') {
            this.notifSvc.system(this.authService.getUserId(),
              `AE #${aeId} has been resolved and closed.`);
          }

          setTimeout(() => this.closeModal(), 1300);
        } else {
          this.updateError = res.message || 'Failed to update status.';
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isUpdating  = false;
        if (err.status === 403) {
          this.updateError = 'You do not have permission to update this status. (Did the PatientService restart after the C1.1 backend fix?)';
        } else {
          this.updateError = err.error?.message ?? 'Failed to update status.';
        }
        this.cdr.detectChanges();
      }
    });
  }
}