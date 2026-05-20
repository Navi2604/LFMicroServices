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

// ✅ CORRECTED: Backend returns enrollmentID + patientName, not patientID
export interface VisitDto {
  visitID:       number;
  enrollmentID:  number;
  patientName:   string;
  protocolTitle: string;
  visitDate:     string;
  status:        string;
  notes:         string;
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
  isSinglePatient    = false;  // ✅ NEW: Track if scheduling for single patient
  singlePatient:     PatientVisitRow | null = null;  // ✅ NEW: Store single patient

  // Edit visit modal
  showEdit       = false;
  editVisit:     VisitDto | null = null;
  editForm       = { status: 'Scheduled', visitDate: '', notes: '' };
  editError      = '';

  // View visit history modal
  showView       = false;
  viewPatient:   PatientVisitRow | null = null;
  viewGroup:     ProtocolGroup | null   = null;

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
      this.enrollmentApi.getAll({}).toPromise()  // ✅ Load ALL enrollments once
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
      // Pre-load patient counts for all groups
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
    
    // ✅ FIXED: Filter from already-loaded allEnrollments instead of making new API call
    const enrollments = this.allEnrollments.filter((e: any) =>
      e.siteProtocolID === g.siteProtocolID &&
      (e.status === 'Active' || e.status === 'Completed')
    );
    
    console.log(`Loading patients for ${g.protocolTitle} (SP: ${g.siteProtocolID}): ${enrollments.length} enrollments`);
    
    g.patients = enrollments.map((e: any) => {
      const patient = this.allPatients.find((p: any) => p.patientID === e.patientID);
      
      // ✅ Match visits by enrollmentID
      const enrollmentId = e.enrollmentID || e.id;
      const visits = this.allVisits.filter(v => v.enrollmentID === enrollmentId);
      
      console.log(`  - ${patient?.name} (enrollmentID: ${enrollmentId}): ${visits.length} visits`);
      
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
    // Ensure patients are loaded
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

    // Validate date is within protocol range
    const visitDate = new Date(this.scheduleForm.visitDate);
    const protoStart = new Date(this.scheduleGroup.startDate);
    const protoEnd = new Date(this.scheduleGroup.endDate);

    if (visitDate < protoStart || visitDate > protoEnd) {
      this.scheduleError = `Visit date must be between ${this.formatDate(this.scheduleGroup.startDate)} and ${this.formatDate(this.scheduleGroup.endDate)}.`;
      return;
    }

    // ✅ Check if this is for a single patient or all patients
    const patientsToSchedule = this.isSinglePatient && this.singlePatient 
      ? [this.singlePatient]  // Only this one patient
      : this.scheduleGroup.patients;  // All patients in protocol

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
      // Clear individual patient flags
      this.singlePatient = null;
      this.isSinglePatient = false;
      
      const count = patientsToSchedule.length;
      this.showMsg('success', `Visit scheduled for ${count} patient${count !== 1 ? 's' : ''}.`);
      
      // Close modal and reload all data
      setTimeout(() => {
        this.load();
      }, 500);
    }).catch((err) => {
      this.scheduleError = err?.error?.message || 'Some visits failed to schedule. Please try again.';
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

    console.log('========== DELETE START ==========');
    console.log('Visit ID:', v.visitID);
    console.log('Visit Status:', v.status);
    
    const token = localStorage.getItem('authToken');
    console.log('Token exists:', !!token);
    console.log('Token length:', token?.length);
    console.log('API URL:', this.visitApi['url'] || 'unknown');
    
    this.visitApi.delete(v.visitID).subscribe({
      next: r => {
        console.log('✅ DELETE SUCCESS');
        console.log('Response:', r);
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
        console.error('❌ DELETE ERROR');
        console.error('Status:', err?.status);
        console.error('StatusText:', err?.statusText);
        console.error('URL:', err?.url);
        console.error('Message:', err?.error?.message);
        console.error('Full error:', err);
        console.log('========== DELETE END ==========');
        
        if (err?.status === 401) {
          this.editError = '401 Unauthorized - Check token or role';
        } else if (err?.status === 403) {
          this.editError = '403 Forbidden - You do not have permission';
        } else if (err?.status === 404) {
          this.editError = '404 Not Found - Endpoint does not exist';
        } else if (err?.status === 0) {
          this.editError = 'Network error - Backend not responding';
        } else {
          this.editError = `Error ${err?.status}: ${err?.error?.message || 'Unknown'}`;
        }
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
    // ✅ Store individual patient and open single-patient schedule modal
    this.scheduleGroup = g;
    this.scheduleForm  = { visitDate: '', notes: '' };
    this.scheduleError = '';
    this.singlePatient = p;  // ✅ Store the individual patient
    this.isSinglePatient = true;  // ✅ Mark as single patient mode
    this.showSchedule  = true;
    this.showView      = false;
    
    console.log(`Adding visit for patient ${p.name} (enrollmentID: ${p.enrollmentID})`);
  }

  // ── Helpers ─────────────────────────────────────────────────
  refreshVisits(): void {
    // Full reload to ensure all data is fresh
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
      'Missed': 'vb-miss', 'Cancelled': 'vb-canc'
    };
    return map[status] ?? 'vb-canc';
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