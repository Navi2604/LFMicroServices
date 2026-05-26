import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { VisitApiService } from '../../core/services/api.service';
import { PatientApiService } from '../../core/services/api.service';
import { SiteProtocolApiService } from '../../core/services/api.service';
import { EnrollmentApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import { SidebarComponent } from '../../shared/sidebar.component';

export interface VisitDto {
  visitID:       number;
  enrollmentID:  number;
  patientName:   string;
  protocolTitle: string;
  visitDate:     string;
  status:        string;
  notes:         string;
}

// ✅ NEW: Visit Detail for bulk management
export interface VisitDetailDto {
  visitDetailID: number;
  visitID:       number;
  enrollmentID:  number;
  patientID:     number;
  patientName:   string;
  visitName:     string;
  status:        'Scheduled' | 'Attended' | 'Missed' | 'Rescheduled';
  attendanceNotes?: string;
  patientEmail?: string;
}

interface ProtocolGroup {
  siteProtocolID:  number;
  protocolTitle:   string;
  siteName:        string;
  status:          string;
  startDate:       string;
  endDate:         string;
  expanded:        boolean;
  patients:        PatientVisitRow[];
  loadingPatients: boolean;
}

interface PatientVisitRow {
  patientID:    number;
  name:         string;
  enrollmentID: number;
  pastVisits:   VisitDto[];
  upcomingVisits: VisitDto[];
  lastVisit:    VisitDto | null;
  nextVisit:    VisitDto | null;
  latestStatus: string;
}

interface VisitManagementView {
  visitID:       number;
  visitDate:     string;
  protocolTitle: string;
  siteName:      string;
  totalPatients: number;
  visitDetails:  VisitDetailDto[];
  stats:         VisitStats;
}

interface VisitStats {
  total:        number;
  scheduled:    number;
  attended:     number;
  missed:       number;
  rescheduled:  number;  // ✅ Changed from 'completed' to 'rescheduled'
}

@Component({
  selector: 'app-visits',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, SidebarComponent],
  templateUrl: './visits.component.html'
})
export class VisitsComponent implements OnInit {
  protocolGroups: ProtocolGroup[] = [];
  allVisits:      VisitDto[]      = [];
  allPatients:    any[]           = [];
  allEnrollments: any[]           = [];
  isLoading       = false;
  successMsg      = '';
  errorMsg        = '';
  unreadCount     = 0;
  canEdit         = false;
  isInvestigator  = false;
  private userId  = 0;

  // Schedule visit modal
  showSchedule       = false;
  scheduleGroup:     ProtocolGroup | null = null;
  scheduleForm       = { visitDate: '', notes: '' };
  scheduleError      = '';
  isSinglePatient    = false;
  singlePatient:     PatientVisitRow | null = null;

  // Edit visit modal
  showEdit       = false;
  editVisit:     VisitDto | null = null;
  editForm       = { status: 'Scheduled', visitDate: '', notes: '' };
  editError      = '';

  // View visit history modal
  showView       = false;
  viewPatient:   PatientVisitRow | null = null;
  viewGroup:     ProtocolGroup | null   = null;

  // ✅ UPDATED: Bulk visit management with search and date logic
  showBulkManagement  = false;
  bulkVisitData:      VisitManagementView | null = null;
  filteredVisitDetails: VisitDetailDto[] = [];
  displayedVisitDetails: VisitDetailDto[] = [];  // ✅ NEW: After search
  currentStatusFilter = 'all';
  bulkDateFilter      = '';
  bulkActionMode      = '';
  bulkActionError     = '';
  bulkConfirmMessage  = '';
  showBulkConfirm     = false;

  // ✅ NEW: Search functionality
  searchTerm: string = '';
  searchPerformed: boolean = false;
  searchResultCount: number = 0;

  // ✅ NEW: Smart date logic
  selectedVisitDateObj: Date | null = null;
  isCurrentOrPastDate: boolean = false;
  isFutureDate: boolean = false;

  constructor(
    private visitApi:        VisitApiService,
    private patientApi:      PatientApiService,
    private siteProtocolApi: SiteProtocolApiService,
    private enrollmentApi:   EnrollmentApiService,
    private authService:     AuthService,
    private cdr:             ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    const role         = this.authService.getRole();
    this.userId        = this.authService.getUserId();
    this.isInvestigator = role === 'Investigator';
    this.canEdit       = ['Admin', 'Investigator', 'ClinicalTrialManager'].includes(role);
    this.load();
  }

  load(): void {
    this.isLoading = true;
    const spParams: any = {};
    if (this.isInvestigator) spParams['investigatorID'] = this.userId;

    Promise.all([
      this.siteProtocolApi.getAll(spParams).toPromise(),
      this.visitApi.getAll().toPromise(),
      this.patientApi.getAll().toPromise(),
      this.enrollmentApi.getAll({}).toPromise()
    ]).then(([spRes, visitRes, patientRes, enrollRes]: any) => {
      this.isLoading  = false;
      this.allVisits  = visitRes?.success ? visitRes.data : [];
      this.allPatients = patientRes?.success ? patientRes.data : [];
      this.allEnrollments = enrollRes?.success ? enrollRes.data : [];

      const siteProtocols = spRes?.success ? spRes.data : [];
      this.protocolGroups = siteProtocols.map((sp: any) => ({
        siteProtocolID:  sp.siteProtocolID,
        protocolTitle:   sp.protocolTitle,
        siteName:        sp.siteName,
        status:          sp.protocolStatus || sp.status || 'Active',
        startDate:       sp.startDate ?? '',
        endDate:         sp.endDate ?? '',
        expanded:        false,
        patients:        [],
        loadingPatients: false
      }));
      this.cdr.detectChanges();
      this.protocolGroups.forEach(g => this.loadGroupPatients(g));
    }).catch(err => {
      this.isLoading = false;
      console.error('Load error:', err);
      this.cdr.detectChanges();
    });
  }

  toggleGroup(g: ProtocolGroup): void {
    g.expanded = !g.expanded;
    if (g.expanded && g.patients.length === 0) {
      this.loadGroupPatients(g);
    }
  }

  loadGroupPatients(g: ProtocolGroup): void {
    g.loadingPatients = true;
    
    const enrollments = this.allEnrollments.filter((e: any) =>
      e.siteProtocolID === g.siteProtocolID &&
      (e.status === 'Active' || e.status === 'Completed')
    );
    
    console.log(`Loading patients for ${g.protocolTitle}: ${enrollments.length} enrollments`);
    
    g.patients = enrollments.map((e: any) => {
      const patient = this.allPatients.find((p: any) => p.patientID === e.patientID);
      const enrollmentId = e.enrollmentID || e.id;
      const visits = this.allVisits.filter(v => v.enrollmentID === enrollmentId);
      
      const today   = new Date();
      const past    = visits.filter(v => new Date(v.visitDate) <= today)
                           .sort((a, b) => new Date(b.visitDate).getTime() - new Date(a.visitDate).getTime());
      const upcoming = visits.filter(v => new Date(v.visitDate) > today && v.status === 'Scheduled')
                           .sort((a, b) => new Date(a.visitDate).getTime() - new Date(b.visitDate).getTime());
      
      return {
        patientID:      e.patientID,
        name:           patient?.name ?? `PT-${e.patientID}`,
        enrollmentID:   enrollmentId,
        pastVisits:     past,
        upcomingVisits: upcoming,
        lastVisit:      past[0] ?? null,
        nextVisit:      upcoming[0] ?? null,
        latestStatus:   upcoming[0]?.status ?? past[0]?.status ?? '—'
      };
    });
    
    g.loadingPatients = false;
    this.cdr.detectChanges();
  }

  // ── Schedule visit ──────────────────────────────────────────
  openSchedule(g: ProtocolGroup): void {
    this.scheduleGroup = g;
    this.scheduleForm  = { visitDate: '', notes: '' };
    this.scheduleError = '';
    this.showSchedule  = true;
    if (g.patients.length === 0) this.loadGroupPatients(g);
  }

  getScheduleMinDate(): string {
    if (!this.scheduleGroup) return '';
    const startDate = this.scheduleGroup.startDate;
    return startDate ? startDate.split('T')[0] : '';
  }

  getScheduleMaxDate(): string {
    if (!this.scheduleGroup) return '';
    const endDate = this.scheduleGroup.endDate;
    return endDate ? endDate.split('T')[0] : '';
  }

  saveSchedule(): void {
    if (!this.scheduleForm.visitDate) {
      this.scheduleError = 'Visit date is required.'; return;
    }
    if (!this.scheduleGroup || this.scheduleGroup.patients.length === 0) {
      this.scheduleError = 'No enrolled patients in this protocol.'; return;
    }

    const visitDate = new Date(this.scheduleForm.visitDate);
    const protoStart = new Date(this.scheduleGroup.startDate);
    const protoEnd = new Date(this.scheduleGroup.endDate);

    if (visitDate < protoStart || visitDate > protoEnd) {
      this.scheduleError = `Visit date must be between ${this.formatDate(this.scheduleGroup.startDate)} and ${this.formatDate(this.scheduleGroup.endDate)}.`;
      return;
    }

    const patientsToSchedule = this.isSinglePatient && this.singlePatient 
      ? [this.singlePatient]
      : this.scheduleGroup.patients;

    const calls = patientsToSchedule.map(p =>
      this.visitApi.create({
        enrollmentID: p.enrollmentID,
        visitDate:    this.scheduleForm.visitDate,
        status:       'Scheduled',
        notes:        this.scheduleForm.notes
      }).toPromise()
    );

    Promise.all(calls).then(() => {
      this.showSchedule = false;
      this.singlePatient = null;
      this.isSinglePatient = false;
      
      const count = patientsToSchedule.length;
      this.showMsg('success', `Visit scheduled for ${count} patient${count !== 1 ? 's' : ''}.`);
      
      setTimeout(() => {
        this.load();
      }, 500);
    }).catch((err) => {
      this.scheduleError = err?.error?.message || 'Some visits failed to schedule.';
    });
  }

  // ── Edit visit ──────────────────────────────────────────────
  openEdit(v: VisitDto): void {
    this.editVisit = v;
    this.editForm  = { status: v.visitDate?.split('T')[0] ?? '', visitDate: v.visitDate?.split('T')[0] ?? '', notes: v.notes ?? '' };
    this.editError = '';
    this.showEdit  = true;
  }

  saveEdit(): void {
    if (!this.editVisit) return;
    this.visitApi.update(this.editVisit.visitID, this.editForm).subscribe({
      next: r => {
        if (r.success) {
          this.showEdit = false;
          this.showMsg('success', 'Visit updated.');
          this.load();
        } else { this.editError = r.message; }
      },
      error: err => { this.editError = err.error?.message ?? 'Failed.'; }
    });
  }

  deleteVisit(v: VisitDto): void {
    if (v.status === 'Completed') {
      this.editError = 'Cannot delete completed visits.';
      return;
    }

    if (!confirm(`Delete visit VIS-${v.visitID}?`)) return;

    this.visitApi.delete(v.visitID).subscribe({
      next: r => {
        if (r?.success) {
          this.editError = '';
          this.showEdit = false;
          this.showMsg('success', 'Visit deleted.');
          setTimeout(() => {
            this.load();
          }, 500);
        } else { 
          this.editError = r?.message || 'Delete failed'; 
        }
      },
      error: err => { 
        this.editError = err?.error?.message || 'Delete failed';
      }
    });
  }

  // ── View history ────────────────────────────────────────────
  openView(p: PatientVisitRow, g: ProtocolGroup): void {
    this.viewPatient = p;
    this.viewGroup   = g;
    this.showView    = true;
  }

  addVisitForPatient(p: PatientVisitRow, g: ProtocolGroup | null): void {
    if (!g) return;
    this.scheduleGroup = g;
    this.scheduleForm  = { visitDate: '', notes: '' };
    this.scheduleError = '';
    this.singlePatient = p;
    this.isSinglePatient = true;
    this.showSchedule  = true;
    this.showView      = false;
  }

  // ✅ UPDATED: Bulk visit management with search and date logic
  openBulkManagement(g: ProtocolGroup): void {
    this.bulkVisitData = null;
    // Auto-set today's date
    const today = new Date();
    const yyyy = today.getFullYear();
    const mm = String(today.getMonth() + 1).padStart(2, '0');
    const dd = String(today.getDate()).padStart(2, '0');
    this.bulkDateFilter = `${yyyy}-${mm}-${dd}`;
    this.checkDateType(this.bulkDateFilter);
    this.currentStatusFilter = 'all';
    this.selectedVisitIds.clear();
    this.bulkActionError = '';
    this.searchTerm = '';
    this.searchPerformed = false;
    this.viewGroup = g;
    this.showBulkManagement = true;
    this.loadBulkVisitData(g);
  }

  loadBulkVisitData(g: ProtocolGroup): void {
    if (g.patients.length === 0) {
      this.loadGroupPatients(g);
    }

    // ✅ NEW: Check date type
    if (this.bulkDateFilter) {
      this.checkDateType(this.bulkDateFilter);
    }

    const filterDate = this.bulkDateFilter ? new Date(this.bulkDateFilter) : null;

    const visitDetails: VisitDetailDto[] = [];
    let detailId = 1;

    g.patients.forEach(p => {
      const allPatientVisits = [...p.pastVisits, ...p.upcomingVisits];
      
      allPatientVisits.forEach(v => {
        if (!filterDate || new Date(v.visitDate).toDateString() === filterDate.toDateString()) {
          visitDetails.push({
            visitDetailID: detailId++,
            visitID: v.visitID,
            enrollmentID: p.enrollmentID,
            patientID: p.patientID,
            patientName: p.name,
            visitName: `Visit on ${this.formatDate(v.visitDate)}`,
            status: v.status as any,
            attendanceNotes: v.notes,
            patientEmail: this.allPatients.find((pt: any) => pt.patientID === p.patientID)?.email || ''
          });
        }
      });
    });

    const stats: VisitStats = {
      total: visitDetails.length,
      scheduled: visitDetails.filter(v => v.status === 'Scheduled').length,
      attended: visitDetails.filter(v => v.status === 'Attended').length,
      missed: visitDetails.filter(v => v.status === 'Missed').length,
      rescheduled: visitDetails.filter(v => v.status === 'Rescheduled').length
    };

    this.bulkVisitData = {
      visitID: 0,
      visitDate: this.bulkDateFilter,
      protocolTitle: g.protocolTitle,
      siteName: g.siteName,
      totalPatients: visitDetails.length,
      visitDetails,
      stats
    };

    this.applyBulkStatusFilter('all');
    this.cdr.detectChanges();
  }

  applyBulkStatusFilter(status: string): void {
    this.currentStatusFilter = status;
    this.selectedVisitIds.clear();
    this.searchTerm = '';
    this.searchPerformed = false;

    if (!this.bulkVisitData) return;

    if (status === 'all') {
      this.filteredVisitDetails = [...this.bulkVisitData.visitDetails];
    } else {
      this.filteredVisitDetails = this.bulkVisitData.visitDetails.filter(
        v => v.status.toLowerCase() === status.toLowerCase()
      );
    }

    this.displayedVisitDetails = [...this.filteredVisitDetails];
  }

  // ✅ NEW: Check if date is past/current/future
  checkDateType(dateStr: string): void {
    this.selectedVisitDateObj = new Date(dateStr);
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    
    const selectedDate = new Date(dateStr);
    selectedDate.setHours(0, 0, 0, 0);
    
    this.isCurrentOrPastDate = selectedDate <= today;
    this.isFutureDate = selectedDate > today;
  }

  // ✅ NEW: Get available actions based on date
  getAvailableActions(): string[] {
    if (this.isCurrentOrPastDate) {
      return ['mark-attended', 'mark-missed'];
    } else if (this.isFutureDate) {
      return ['reschedule'];
    }
    return [];
  }

  // ── Smart selection: persists across search ──────────────────
  // Use real visitID (not a generated detailId) as the key
  selectedVisitIds = new Set<number>();

  isVisitSelected(v: VisitDetailDto): boolean {
    return this.selectedVisitIds.has(v.visitID);
  }

  toggleBulkSelect(v: VisitDetailDto): void {
    if (this.selectedVisitIds.has(v.visitID)) this.selectedVisitIds.delete(v.visitID);
    else this.selectedVisitIds.add(v.visitID);
    this.cdr.detectChanges();
  }

  // Select-all selects all filteredVisitDetails (not just displayed)
  // Deselect-all only deselects currently displayed
  toggleBulkSelectAll(): void {
    const allDisplayedSelected = this.displayedVisitDetails.every(v => this.selectedVisitIds.has(v.visitID));
    if (allDisplayedSelected && this.displayedVisitDetails.length > 0) {
      // Deselect only what's displayed
      this.displayedVisitDetails.forEach(v => this.selectedVisitIds.delete(v.visitID));
    } else {
      // Select all in filteredVisitDetails (full list, not just search results)
      this.filteredVisitDetails.forEach(v => this.selectedVisitIds.add(v.visitID));
    }
    this.cdr.detectChanges();
  }

  isAllDisplayedSelected(): boolean {
    return this.displayedVisitDetails.length > 0 &&
      this.displayedVisitDetails.every(v => this.selectedVisitIds.has(v.visitID));
  }

  // ── Search: selected items float to top when search cleared ──
  performSearch(term: string): void {
    this.searchTerm = term.toLowerCase();
    if (!this.searchTerm) {
      // Float selected items to top
      const selected = this.filteredVisitDetails.filter(v => this.selectedVisitIds.has(v.visitID));
      const unselected = this.filteredVisitDetails.filter(v => !this.selectedVisitIds.has(v.visitID));
      this.displayedVisitDetails = [...selected, ...unselected];
      this.searchPerformed = false;
    } else {
      this.displayedVisitDetails = this.filteredVisitDetails.filter(v =>
        v.patientName.toLowerCase().includes(this.searchTerm) ||
        v.patientEmail?.toLowerCase().includes(this.searchTerm)
      );
      this.searchPerformed = true;
    }
    this.searchResultCount = this.displayedVisitDetails.length;
    this.cdr.detectChanges();
  }

  clearSearch(): void {
    this.performSearch('');
    this.cdr.detectChanges();
  }

  // ── Bulk action: actually call API ───────────────────────────
  applyBulkAction(): void {
    if (this.selectedVisitIds.size === 0) {
      this.bulkActionError = 'Please select at least one patient.'; return;
    }
    if (!this.bulkActionMode) {
      this.bulkActionError = 'Please select an action.'; return;
    }
    let actionText = '';
    if (this.bulkActionMode === 'mark-attended') actionText = 'Mark as Attended';
    else if (this.bulkActionMode === 'mark-missed') actionText = 'Mark as Missed';
    else if (this.bulkActionMode === 'reschedule') actionText = 'Reschedule';
    this.bulkConfirmMessage = `${actionText} for ${this.selectedVisitIds.size} patient(s)?`;
    this.showBulkConfirm = true;
  }

  confirmBulkAction(): void {
    const ids = Array.from(this.selectedVisitIds);
    const selectedDetails = this.filteredVisitDetails.filter(v => ids.includes(v.visitID));

    if (this.bulkActionMode === 'mark-attended') {
      this.executeBulkUpdate(selectedDetails, 'Attended');
    } else if (this.bulkActionMode === 'mark-missed') {
      this.executeBulkUpdate(selectedDetails, 'Missed');
    } else if (this.bulkActionMode === 'reschedule') {
      this.executeBulkUpdate(selectedDetails, 'Rescheduled');
    }
    this.showBulkConfirm = false;
    this.bulkActionMode = '';
  }

  executeBulkUpdate(details: VisitDetailDto[], newStatus: string): void {
    if (details.length === 0) return;
    const calls = details.map(d =>
      this.visitApi.updateStatus(d.visitID, newStatus).toPromise()
    );
    Promise.all(calls).then(() => {
      this.showMsg('success', `✓ Updated ${details.length} patient(s) as ${newStatus}`);
      this.selectedVisitIds.clear();
      this.showBulkManagement = false;
      this.load();
    }).catch(() => {
      this.bulkActionError = 'Some updates failed. Please try again.';
      this.cdr.detectChanges();
    });
  }

  // ✅ NEW: Delete individual visit
  deleteVisitDetail(detail: VisitDetailDto): void {
    if (!confirm(`Delete visit for ${detail.patientName}?`)) return;

    console.log('Deleting visit detail:', detail.visitDetailID);
    
    // Call backend to delete
    this.visitApi.delete(detail.visitID).subscribe({
      next: (r: any) => {
        if (r?.success) {
          this.showMsg('success', `Visit deleted for ${detail.patientName}`);
          // Remove from displayed list
          this.displayedVisitDetails = this.displayedVisitDetails.filter(
            v => v.visitDetailID !== detail.visitDetailID
          );
          this.filteredVisitDetails = this.filteredVisitDetails.filter(
            v => v.visitDetailID !== detail.visitDetailID
          );
          // Update bulk data
          if (this.bulkVisitData) {
            this.bulkVisitData.visitDetails = this.bulkVisitData.visitDetails.filter(
              v => v.visitDetailID !== detail.visitDetailID
            );
          }
          // Reset selection
          this.selectedVisitIds.delete(detail.visitID);
          this.cdr.detectChanges();
        }
      },
      error: (err: any) => {
        this.showMsg('error', 'Failed to delete visit');
      }
    });
  }

  // ── Helpers ──────────────────────────────────────────────────

  closeBulkManagement(): void {
    this.showBulkManagement = false;
    this.selectedVisitIds.clear();
    this.bulkActionMode = '';
    this.bulkActionError = '';
    this.searchTerm = '';
    this.searchPerformed = false;
  }

  // ── Helpers ─────────────────────────────────────────────────
  refreshVisits(): void {
    this.load();
  }

  totalVisits(g: ProtocolGroup): number {
    return g.patients.reduce((sum, p) => sum + p.pastVisits.length + p.upcomingVisits.length, 0);
  }

  scheduledCount(g: ProtocolGroup): number {
    return g.patients.reduce((sum, p) => sum + p.upcomingVisits.length, 0);
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '—';
    const d = new Date(dateStr);
    return d.toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
  }

  getStatusBadge(status: string): string {
    const map: Record<string, string> = {
      'Scheduled': 'vb-sched', 'Completed': 'vb-comp',
      'Missed': 'vb-miss', 'Cancelled': 'vb-canc',
      'Attended': 'vb-attended', 'Rescheduled': 'vb-resched'
    };
    return map[status] ?? 'vb-canc';
  }

  getStatusBulkBadge(status: string): string {
    const map: Record<string, string> = {
      'Scheduled': 'badge-scheduled',
      'Attended': 'badge-attended',
      'Missed': 'badge-missed',
      'Rescheduled': 'badge-rescheduled'
    };
    return map[status] ?? 'badge-default';
  }

  getProtoBadge(status: string): string {
    const map: Record<string,string> = {
      'Ongoing':   'lt-badge lt-badge-green',
      'Active':    'lt-badge lt-badge-green',
      'Upcoming':  'lt-badge lt-badge-amber',
      'Completed': 'lt-badge lt-badge-gray',
      'Archived':  'lt-badge lt-badge-gray'
    };
    return map[status] ?? 'lt-badge lt-badge-gray';
  }

  canScheduleVisit(g: ProtocolGroup): boolean {
    return g.status === 'Ongoing' || g.status === 'Active';
  }

  isUpcoming(v: VisitDto): boolean {
    return new Date(v.visitDate) > new Date() && v.status === 'Scheduled';
  }

  viewTotalVisits(): number {
    if (!this.viewPatient) return 0;
    return this.viewPatient.pastVisits.length + this.viewPatient.upcomingVisits.length;
  }

  viewCompletedCount(): number {
    return this.viewPatient?.pastVisits.filter(v => v.status === 'Completed').length ?? 0;
  }

  viewMissedCount(): number {
    return this.viewPatient?.pastVisits.filter(v => v.status === 'Missed').length ?? 0;
  }

  private showMsg(type: 'success'|'error', msg: string): void {
    if (type === 'success') { this.successMsg = msg; this.errorMsg = ''; }
    else { this.errorMsg = msg; this.successMsg = ''; }
    setTimeout(() => { this.successMsg = ''; this.errorMsg = ''; this.cdr.detectChanges(); }, 3000);
  }
}