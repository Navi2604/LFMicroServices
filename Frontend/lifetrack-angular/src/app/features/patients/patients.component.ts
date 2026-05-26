// patients.component.ts
import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  PatientApiService, PatientDto,
  SiteProtocolApiService, EnrollmentApiService,
  ProtocolApiService, VisitApiService
} from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import { SidebarComponent } from '../../shared/sidebar.component';

@Component({
  selector: 'app-patients',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, SidebarComponent],
  templateUrl: './patients.component.html'
})
export class PatientsComponent implements OnInit {
  allPatients:   PatientDto[] = [];
  myPatients:    PatientDto[] = [];
  filtered:      PatientDto[] = [];

  isLoading      = false;
  successMsg     = '';
  errorMsg       = '';
  showEnroll     = false;
  canEnroll      = false;
  canDelete      = false;
  isInvestigator = false;
  unreadCount    = 0;
  searchTerm     = '';
  activeTab      = 'all';    // all | mine
  statusTab      = 'All';    // All | Active | Completed | Withdrawn

  // ✅ Pagination
  currentPage = 1;
  pageSize = 10;
  totalItems = 0;
  totalPages = 0;
  jumpToPage = 1;
  itemsPerPageOptions = [10, 20, 50];
  paginatedFiltered: PatientDto[] = [];

  enrolledPatientIds = new Set<number>();
  patientStatusMap    = new Map<number, string>();
  patientProtocolMap  = new Map<number, string>();

  // View modal
  showView            = false;
  viewTab             = 'overview';
  selectedPatient:    PatientDto | null = null;
  patientEnrollments: any[] = [];
  patientVisits:      any[] = [];
  viewLoading         = false;

  // Edit status modal
  showEditStatus      = false;
  editPatient:        PatientDto | null = null;
  editStatus          = '';
  editEnrollmentId    = 0;

  // Enroll to protocol modal
  showEnrollToProtocol  = false;
  enrollTargetPatient:  PatientDto | null = null;
  availableSiteProtocols: any[] = [];
  enrollProtocolError   = '';
  enrollProtocolForm    = { siteProtocolID: 0, consentDate: '' };

  allProtocols: any[] = [];
  enrollForm = { name: '', email: '', dob: '', contactInfo: '' };
  private userId = 0;

  constructor(
    private patientApi:      PatientApiService,
    private siteProtocolApi: SiteProtocolApiService,
    private enrollmentApi:   EnrollmentApiService,
    private protocolApi:     ProtocolApiService,
    private visitApi:        VisitApiService,
    private authService:     AuthService,
    private cdr:             ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    const role          = this.authService.getRole();
    this.userId         = this.authService.getUserId();
    this.canEnroll      = ['Admin', 'Investigator', 'ClinicalTrialManager'].includes(role);
    this.canDelete      = ['Admin'].includes(role);
    this.isInvestigator = role === 'Investigator';
    this.load();
    this.protocolApi.getAll().subscribe(r => {
      if (r.success) this.allProtocols = r.data;
    });
    this.loadEnrollmentData();
  }

  loadEnrollmentData(): void {
    this.enrollmentApi.getAll().subscribe(r => {
      if (r.success) {
        // Only count non-declined, non-pending as enrolled
        this.enrolledPatientIds = new Set(
          r.data.filter((e: any) => e.status !== 'Declined').map((e: any) => e.patientID)
        );
        const statusMap = new Map<number, string>();
        r.data.forEach((e: any) => {
          if (e.status === 'Declined') return; // skip declined
          const existing = statusMap.get(e.patientID);
          // Priority: Active > Pending > Completed > Withdrawn
          if (!existing || e.status === 'Active') statusMap.set(e.patientID, e.status);
          else if (e.status === 'Pending' && existing !== 'Active') statusMap.set(e.patientID, e.status);
          else if (e.status === 'PendingWithdrawal' && existing === 'Active') statusMap.set(e.patientID, e.status);
        });
        this.patientStatusMap = statusMap;

        // Build protocol map using siteProtocols to get titles
        const enrollments = r.data;
        this.siteProtocolApi.getAll().subscribe(spRes => {
          if (spRes.success) {
            const spMap = new Map<number, string>();
            spRes.data.forEach((sp: any) => spMap.set(sp.siteProtocolID, sp.protocolTitle));
            const protocolMap = new Map<number, string>();
            enrollments.forEach((e: any) => {
              if (e.status === 'Active') {
                const sp = spRes.data.find((s: any) => s.siteProtocolID === e.siteProtocolID);
                if (sp) protocolMap.set(e.patientID, `${sp.protocolTitle} — ${sp.siteName}`);
              }
            });
            this.patientProtocolMap = protocolMap;
            // Also store for completed/withdrawn — use latest enrollment
            enrollments.forEach((e: any) => {
              if (!protocolMap.has(e.patientID)) {
                const sp = spRes.data.find((s: any) => s.siteProtocolID === e.siteProtocolID);
                if (sp) protocolMap.set(e.patientID, `${sp.protocolTitle} — ${sp.siteName}`);
              }
            });
            this.patientProtocolMap = protocolMap;
            this.cdr.detectChanges();
          }
        });
      }
    });
  }

  load(): void {
    this.isLoading = true;
    this.patientApi.getAll().subscribe({
      next: r => {
        if (r.success) {
          this.allPatients = r.data ?? [];
          if (this.isInvestigator) {
            this.siteProtocolApi.getAll({ investigatorID: this.userId }).subscribe(sp => {
              if (sp.success && sp.data.length > 0) {
                const spIds = sp.data.map((x: any) => x.siteProtocolID);
                Promise.all(spIds.map((id: number) =>
                  this.enrollmentApi.getAll({ siteProtocolId: id }).toPromise()
                )).then(results => {
                  const myPatientIds = new Set<number>();
                  results.forEach((res: any) => {
                    if (res?.success) res.data.forEach((e: any) => myPatientIds.add(e.patientID));
                  });
                  this.myPatients = this.allPatients.filter(p => myPatientIds.has(p.patientID));
                  this.isLoading  = false;
                  this.buildSiteProtocolGroups();
                  this.applyAllFilters();
                  this.cdr.detectChanges();
                });
              } else {
                this.myPatients = [];
                this.isLoading  = false;
                this.applyAllFilters();
                this.cdr.detectChanges();
              }
            });
          } else {
            this.isLoading = false;
            this.applyAllFilters();
            this.cdr.detectChanges();
          }
        }
      },
      error: () => { this.isLoading = false; this.cdr.detectChanges(); }
    });
  }

  switchTab(tab: string): void {
    this.activeTab  = tab;
    // Reset to All, but if on mine tab and statusTab is '', reset to All
    if (tab === 'mine' && this.statusTab === '') this.statusTab = 'All';
    else this.statusTab = 'All';
    this.searchTerm = '';
    this.applyAllFilters();
  }

  setStatusTab(st: string): void {
    this.statusTab = st;
    this.applyAllFilters();
  }

  applyAllFilters(): void {
    let source = this.activeTab === 'mine' ? this.myPatients : this.allPatients;
    
    // ✅ NEW: For "all" tab - exclude Active patients from the entire list
    if (this.activeTab === 'all') {
      source = source.filter(p => {
        const status = this.getPatientStatus(p);
        // Hide Active patients, show everything else: Not Enrolled, Pending, Completed, Withdrawn
        return status !== 'Active';
      });
    }
    
    // Status tab filter
    if (this.statusTab === '') {
      source = source.filter(p => !this.getPatientStatus(p));
    } else if (this.statusTab !== 'All') {
      source = source.filter(p => this.getPatientStatus(p) === this.statusTab);
    }
    // Search filter
    const q = this.searchTerm.toLowerCase();
    this.filtered = q
      ? source.filter(p =>
          p.name.toLowerCase().includes(q) ||
          p.email.toLowerCase().includes(q) ||
          (this.getPatientProtocol(p) || '').toLowerCase().includes(q)
        )
      : [...source];
    this.calculateTotalPages(); // ✅ NEW
    this.cdr.detectChanges();
  }

  countByStatus(status: string): number {
    let source = this.activeTab === 'mine' ? this.myPatients : this.allPatients;
    
    // ✅ NEW: For "all" tab - exclude Active patients
    if (this.activeTab === 'all') {
      source = source.filter(p => this.getPatientStatus(p) !== 'Active');
    }
    
    if (status === 'All') return source.length;
    if (status === '') return source.filter(p => !this.getPatientStatus(p)).length;
    if (status === 'Pending') return source.filter(p => this.getPatientStatus(p) === 'Pending').length;
    if (status === 'Re-enroll') {
      return source.filter(p => {
        const s = this.getPatientStatus(p);
        return s === 'Completed' || s === 'Withdrawn';
      }).length;
    }
    return source.filter(p => this.getPatientStatus(p) === status).length;
  }

  getPatientStatus(p: PatientDto): string {
    return this.patientStatusMap.get(p.patientID) || p.enrollmentStatus || '';
  }

  getPatientProtocol(p: PatientDto): string {
    return this.patientProtocolMap.get(p.patientID) || '';
  }

  isEnrolled(p: PatientDto): boolean {
    return this.enrolledPatientIds.has(p.patientID) || !!p.enrollmentStatus;
  }

  // Can enroll: not enrolled at all, OR status is Withdrawn (can join new protocol)
  canEnrollPatient(p: PatientDto): boolean {
    const status = this.getPatientStatus(p);
    return !status || status === 'Withdrawn';
  }

  // ── Site-protocol groups for Investigator My Patients ───────
  siteProtocolGroups: any[] = [];
  expandedGroupIds = new Set<number>();
  groupSearchTerm: Map<number, string> = new Map();
  groupStatusTab:  Map<number, string> = new Map();

  buildSiteProtocolGroups(): void {
    if (!this.isInvestigator) return;
    this.siteProtocolApi.getAll({ investigatorID: this.userId }).subscribe(sp => {
      if (!sp.success) return;
      this.siteProtocolGroups = sp.data.map((s: any) => ({
        siteProtocolID: s.siteProtocolID,
        protocolTitle:  s.protocolTitle,
        siteName:       s.siteName,
        status:         s.protocolStatus || s.status || '',
        patients:       [] as PatientDto[]
      }));
      this.siteProtocolGroups.forEach(g => {
        this.groupSearchTerm.set(g.siteProtocolID, '');
        this.groupStatusTab.set(g.siteProtocolID, 'All');
      });
      this.enrollmentApi.getAll().subscribe(er => {
        if (!er.success) return;
        this.siteProtocolGroups.forEach(g => {
          const enrolledIds = er.data
            .filter((e: any) => e.siteProtocolID === g.siteProtocolID)
            .map((e: any) => e.patientID);
          g.patients = this.myPatients.filter((p: PatientDto) => enrolledIds.includes(p.patientID));
        });
        this.cdr.detectChanges();
      });
    });
  }

  toggleProtocolGroup(id: number): void {
    if (this.expandedGroupIds.has(id)) this.expandedGroupIds.delete(id);
    else this.expandedGroupIds.add(id);
    this.cdr.detectChanges();
  }

  isGroupExpanded(id: number): boolean {
    return this.expandedGroupIds.has(id);
  }

  getGroupSearch(id: number): string {
    return this.groupSearchTerm.get(id) ?? '';
  }

  setGroupSearch(id: number, val: string): void {
    this.groupSearchTerm.set(id, val);
    this.cdr.detectChanges();
  }

  getGroupStatus(id: number): string {
    return this.groupStatusTab.get(id) ?? 'All';
  }

  setGroupStatus(id: number, status: string): void {
    this.groupStatusTab.set(id, status);
    this.cdr.detectChanges();
  }

  getFilteredGroupPatients(g: any): PatientDto[] {
    const search = (this.groupSearchTerm.get(g.siteProtocolID) ?? '').toLowerCase();
    const status = this.groupStatusTab.get(g.siteProtocolID) ?? 'All';
    return g.patients.filter((p: PatientDto) => {
      const s = this.getPatientStatus(p);
      const statusMatch = status === 'All' || s === status;
      const searchMatch = !search ||
        p.name.toLowerCase().includes(search) ||
        p.email.toLowerCase().includes(search);
      return statusMatch && searchMatch;
    });
  }

  countGroupByStatus(g: any, status: string): number {
    if (status === 'All') return g.patients.length;
    return g.patients.filter((p: PatientDto) => this.getPatientStatus(p) === status).length;
  }

  // ── View modal ──────────────────────────────────────────────
  openView(p: PatientDto): void {
    this.selectedPatient    = p;
    this.viewTab            = 'overview';
    this.patientEnrollments = [];
    this.patientVisits      = [];
    this.showView           = true;
    this.viewLoading        = true;
    this.enrollmentApi.getAll({ patientId: p.patientID }).subscribe(r => {
      if (r.success) { this.patientEnrollments = r.data ?? []; this.viewLoading = false; this.cdr.detectChanges(); }
    });
    this.visitApi.getAll().subscribe(r => {
      if (r.success) { this.patientVisits = r.data.filter((v: any) => v.patientID === p.patientID); this.cdr.detectChanges(); }
    });
  }

  closeView(): void { this.showView = false; this.selectedPatient = null; }

  getInitials(name: string): string {
    return name.split(' ').map(w => w[0]).join('').substring(0, 2).toUpperCase();
  }

  getEnrollmentStatusColor(status: string): string {
    const m: Record<string,string> = { 'Active':'#e6f4ea','Completed':'#e8f0fe','Withdrawn':'#fce8e6','Screening':'#fef3e2' };
    return m[status] ?? '#f3f4f6';
  }

  getEnrollmentStatusText(status: string): string {
    const m: Record<string,string> = { 'Active':'#27500a','Completed':'#0c447c','Withdrawn':'#7f1d1d','Screening':'#78350f' };
    return m[status] ?? '#374151';
  }

  // ── Edit status modal ────────────────────────────────────────
  openEditStatus(p: PatientDto): void {
    this.editPatient     = p;
    this.editStatus      = this.getPatientStatus(p);
    this.editEnrollmentId = 0;
    // Find the active enrollment ID for this patient
    this.enrollmentApi.getAll({ patientId: p.patientID }).subscribe(r => {
      if (r.success) {
        const active = r.data.find((e: any) => e.status === 'Active');
        if (active) this.editEnrollmentId = active.enrollmentID;
        this.cdr.detectChanges();
      }
    });
    this.showEditStatus = true;
  }

  saveEditStatus(): void {
    if (!this.editPatient || !this.editStatus) return;

    // If withdrawing — update enrollment to PendingWithdrawal and send to patient for consent
    if (this.editStatus === 'Withdrawn' && this.editEnrollmentId > 0) {
      this.enrollmentApi.updateStatus(this.editEnrollmentId, { status: 'PendingWithdrawal', withdrawalReason: '' }).subscribe(r => {
        if (r.success) {
          this.showEditStatus = false;
          this.showMsg('success', 'Withdrawal request sent to patient for consent.');
          this.loadEnrollmentData();
          this.load();
        } else { this.showMsg('error', r.message); }
      });
    } else {
      // For other status changes — direct update
      this.patientApi.updateStatus(this.editPatient!.patientID, this.editStatus).subscribe(r => {
        if (r.success) {
          this.showEditStatus = false;
          this.showMsg('success', 'Status updated.');
          this.loadEnrollmentData();
          this.load();
        } else { this.showMsg('error', r.message); }
      });
    }
  }

  // ── Enroll to protocol ───────────────────────────────────────
  openEnrollToProtocol(p: PatientDto): void {
    this.enrollTargetPatient  = p;
    this.enrollProtocolError  = '';
    this.enrollProtocolForm   = { siteProtocolID: 0, consentDate: '' };
    this.showEnrollToProtocol = true;
    const params: any = {};
    if (this.isInvestigator) params['investigatorID'] = this.userId;
    this.siteProtocolApi.getAll(params).subscribe(r => {
      if (r.success) { this.availableSiteProtocols = r.data.filter((sp: any) => sp.status === 'Active'); this.cdr.detectChanges(); }
    });
  }

  enrollToProtocol(): void {
    if (!this.enrollTargetPatient || !this.enrollProtocolForm.siteProtocolID) {
      this.enrollProtocolError = 'Please select a site-protocol.'; return;
    }
    const payload = {
      patientID:      this.enrollTargetPatient.patientID,
      siteProtocolID: +this.enrollProtocolForm.siteProtocolID,
      consentDate:    this.enrollProtocolForm.consentDate || null
    };
    this.enrollmentApi.create(payload).subscribe({
      next: r => {
        if (r.success) {
          this.showEnrollToProtocol = false;
          this.showMsg('success', `Invitation sent to ${this.enrollTargetPatient?.name}. Awaiting patient consent.`);
          this.loadEnrollmentData();
          this.load();
        } else { this.enrollProtocolError = r.message; }
      },
      error: err => { this.enrollProtocolError = err.error?.message ?? 'Enrollment failed.'; }
    });
  }

  // ── Enroll new patient ───────────────────────────────────────
  enroll(): void {
    if (!this.enrollForm.name || !this.enrollForm.email) { this.errorMsg = 'Name and email are required.'; return; }
    this.patientApi.enroll(this.enrollForm).subscribe({
      next: r => {
        if (r.success) {
          this.showMsg('success', 'Patient enrolled successfully.');
          this.showEnroll = false;
          this.enrollForm = { name: '', email: '', dob: '', contactInfo: '' };
          this.load();
        } else { this.showMsg('error', r.message); }
      },
      error: err => this.showMsg('error', err.error?.message ?? 'Failed.')
    });
  }

  delete(id: number): void {
    if (!confirm('Delete this patient?')) return;
    this.patientApi.delete(id).subscribe(r => {
      if (r.success) { this.showMsg('success', 'Patient deleted.'); this.load(); }
      else { this.showMsg('error', r.message); }
    });
  }

  private showMsg(type: 'success' | 'error', msg: string): void {
    if (type === 'success') { this.successMsg = msg; this.errorMsg = ''; }
    else { this.errorMsg = msg; this.successMsg = ''; }
    setTimeout(() => { this.successMsg = ''; this.errorMsg = ''; this.cdr.detectChanges(); }, 4000);
  }

  getStatusBadge(status: string): string {
    const map: Record<string, string> = {
      'Active':'lt-badge-green','Completed':'lt-badge-gray',
      'Withdrawn':'lt-badge-red','Ongoing':'lt-badge-green',
      'Pending':'lt-badge-amber',
      'PendingWithdrawal':'lt-badge-red'
    };
    return map[status] ?? 'lt-badge-gray';
  }

  // ✅ Pagination methods
  calculateTotalPages(): void {
    this.totalItems = this.filtered.length;
    this.totalPages = Math.ceil(this.totalItems / this.pageSize);
    this.currentPage = 1;
    this.applyPagination();
  }

  applyPagination(): void {
    const start = (this.currentPage - 1) * this.pageSize;
    const end = start + this.pageSize;
    this.paginatedFiltered = this.filtered.slice(start, end);
    this.cdr.detectChanges();
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      this.applyPagination();
    }
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
      this.applyPagination();
    }
  }

  prevPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.applyPagination();
    }
  }

  changePageSize(size: number): void {
    this.pageSize = size;
    this.calculateTotalPages();
  }

  getPaginationButtons(): number[] {
    const buttons: number[] = [];
    const maxButtons = 10;
    const startPage = Math.max(1, this.currentPage - 4);
    const endPage = Math.min(this.totalPages, startPage + maxButtons - 1);
    
    for (let i = startPage; i <= endPage; i++) {
      buttons.push(i);
    }
    
    return buttons;
  }
}