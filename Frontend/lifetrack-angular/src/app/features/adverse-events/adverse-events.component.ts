import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SidebarComponent } from '../../shared/sidebar.component';
import { AuthService } from '../../core/services/auth.service';
import {
  AdverseEventApiService, AdverseEventDto,
  ProtocolApiService
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
  canReview   = false;
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
    private authService: AuthService,
    private cdr:         ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.userRole  = this.authService.getRole();
    this.canReview = ['ClinicalTrialManager', 'RegulatoryOfficer']
                       .includes(this.userRole);
    this.loadProtocols();
    this.load();
  }

  // ── Data loading ──────────────────────────────────────────────────

  load(): void {
    this.isLoading = true;
    this.errorMsg  = '';
    this.aeApi.getAll().subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.success) {
          this.events = res.data ?? [];
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
          // Auto-close after a short delay
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