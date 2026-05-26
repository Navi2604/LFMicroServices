// protocols.component.ts
import { Component, OnInit, ChangeDetectorRef, ViewEncapsulation } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ProtocolApiService, ProtocolDto, SiteProtocolApiService, EnrollmentApiService, PatientApiService, SiteApiService, UserApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import { SidebarComponent } from '../../shared/sidebar.component';

interface PhaseForm {
  phaseNumber: number;
  startDate: string;
  endDate: string;
}

interface ParsedPhase {
  phaseNumber: number;
  startDate: string;
  endDate: string;
}

@Component({
  selector: 'app-protocols',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, SidebarComponent],
  templateUrl: './protocols.component.html',
  encapsulation: ViewEncapsulation.None
})
export class ProtocolsComponent implements OnInit {
  protocols:  ProtocolDto[] = [];
  filtered:   ProtocolDto[] = [];
  isLoading   = false;
  successMsg  = '';
  errorMsg    = '';
  canEdit     = false;
  unreadCount    = 0;
  isInvestigator = false;
  private userId = 0;

  // ── Tab state ─────────────────────────────────────────────────────────────
  activeTab: 'all' | 'upcoming' | 'ongoing' | 'completed' | 'archived' = 'all';

  // ✅ Pagination ─────────────────────────────────────────────────────────────
  currentPage = 1;
  pageSize = 10;
  totalItems = 0;
  totalPages = 0;
  jumpToPage = 1;
  itemsPerPageOptions = [10, 20, 50];
  paginatedFiltered: ProtocolDto[] = [];

  // ── Modal state ───────────────────────────────────────────────────────────
  showCreate  = false;
  showView    = false;
  showEdit    = false;
  selectedProtocol: ProtocolDto | null = null;
  selectedPhases:   ParsedPhase[] = [];
  viewTab         = 'details';
  protocolPatients: any[] = [];        // flat list (kept for count badge)
  patientsBySite:   {                  // grouped by site — used in Patients tab
    siteProtocolID:    number;
    siteName:          string;
    investigatorName:  string;
    investigatorEmail: string;
    status:            string;
    patients:          any[];
    expanded:          boolean;        // accordion open/closed
  }[] = [];
  patientsLoading  = false;
  protocolSites:   any[] = [];  // For modal Sites tab
  sitesLoading     = false;

  // ✅ NEW: Separate sites into My Sites and Other Sites
  mySites: any[] = [];
  otherSites: any[] = [];
  currentUserID: number = 0;
  otherProtocolsBySite: Map<number, any[]> = new Map();  // ✅ Cache other protocols

  // ✅ NEW: Separate mapping for investigator - keeps site names for list
  investigatorSiteMap: Map<number, { siteName: string; location: string }> = new Map();

  // ✅ NEW: Mapping for Admin - shows all sites per protocol
  adminProtocolSiteMap: Map<number, string> = new Map();  // protocolID -> comma-separated site names

  // ── Create form state ─────────────────────────────────────────────────────
  form = { title: '', startDate: '', endDate: '' };
  phaseCount: number | null = null;
  phases: PhaseForm[] = [];
  phaseCountError = '';
  today: string = new Date().toISOString().split('T')[0];

  // ── Edit form state ───────────────────────────────────────────────────────
  editForm = { title: '', startDate: '', endDate: '' };
  editPhaseCount: number | null = null;
  editPhases: PhaseForm[] = [];
  editPhaseCountError = '';

  // ── Site assignment state ─────────────────────────────────────────────────
  allSites:          any[] = [];
  allInvestigators:  any[] = [];
  // For create form
  siteAssignments:   { siteID: number; investigatorID: number }[] = [];
  newAssignment      = { siteID: 0, investigatorID: 0 };
  // For edit form
  editSiteAssignments:    any[] = [];   // existing site-protocols
  editNewAssignment       = { siteID: 0, investigatorID: 0 };
  siteAssignmentError     = '';

  constructor(
    private protocolApi:     ProtocolApiService,
    private siteProtocolApi: SiteProtocolApiService,
    private enrollmentApi:   EnrollmentApiService,
    private patientApi:      PatientApiService,
    private siteApi:         SiteApiService,
    private userApi:         UserApiService,
    private authService:     AuthService,
    private cdr:             ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    const role         = this.authService.getRole();
    this.userId        = this.authService.getUserId();
    this.isInvestigator = role === 'Investigator';
    this.canEdit       = ['Admin', 'ClinicalTrialManager'].includes(role);
    this.load();
    // Load allSites for ALL roles (needed for location/email/contact lookup)
    this.siteApi.getAll().subscribe(r => { 
      if (r.success) {
        this.allSites = r.data;
        if (this.canEdit) {
          this.loadAdminSiteNames();
        }
      }
    });
    // Load investigators for ALL roles (Investigator needs it for getInvestigatorEmail on other sites)
    this.userApi.getAll().subscribe(r => {
      if (r.success) this.allInvestigators = r.data.filter((u: any) => u.roleName === 'Investigator');
    });
  }

  // ── Tab switching ─────────────────────────────────────────────────────────

  switchTab(tab: 'all' | 'upcoming' | 'ongoing' | 'completed' | 'archived'): void {
    this.activeTab = tab;
    this.applyFilter();
  }

  applyFilter(): void {
    const sorted = [...this.protocols].sort(
      (a, b) => new Date(b.startDate).getTime() - new Date(a.startDate).getTime()
    );
    if (this.activeTab === 'all') {
      this.filtered = sorted.filter(p => p.status.toLowerCase() !== 'archived');
    } else {
      this.filtered = sorted.filter(p => p.status.toLowerCase() === this.activeTab);
    }
    this.calculateTotalPages(); // ✅ NEW
  }

  // ── Computed status ───────────────────────────────────────────────────────

  get computedStatus(): string {
    return this.calcStatus(this.form.startDate, this.form.endDate);
  }

  calcStatus(start: string, end: string): string {
    if (!start || !end) return 'Draft';
    if (this.today > end) return 'Completed';
    if (this.today >= start && this.today <= end) return 'Ongoing';
    return 'Upcoming';
  }

  get datesReady(): boolean {
    return !!this.form.startDate &&
           !!this.form.endDate &&
           this.form.endDate > this.form.startDate &&
           this.form.startDate >= this.today;
  }

  get editDatesReady(): boolean {
    return !!this.editForm.startDate &&
           !!this.editForm.endDate &&
           this.editForm.endDate > this.editForm.startDate;
  }

  // ── Phase parsing from ProtocolDto ────────────────────────────────────────

  parsePhasesFromDto(p: ProtocolDto): ParsedPhase[] {
    // Try phases array first (direct from backend)
    if (p.phases && p.phases.length > 0) return p.phases;
    // Fall back to phasesJson string
    if (p.phasesJson) {
      try {
        const parsed = JSON.parse(p.phasesJson);
        if (Array.isArray(parsed)) return parsed;
      } catch { }
    }
    return [];
  }

  phaseLabel(p: ProtocolDto): string {
    const phases = this.parsePhasesFromDto(p);
    if (!phases.length) return '';
    return `${phases.length} Phase${phases.length > 1 ? 's' : ''}`;
  }

  phaseStatusFor(start: string, end: string): string {
    return this.calcStatus(start, end);
  }

  // ── View modal ──────────────────────────────────────────────

  openView(p: ProtocolDto): void {
    this.selectedProtocol  = p;
    this.selectedPhases    = this.parsePhasesFromDto(p);
    this.viewTab           = 'details';
    this.protocolPatients  = [];
    this.protocolSites     = [];  // ✅ Reset sites array
    this.mySites           = [];  // ✅ Reset my sites
    this.otherSites        = [];  // ✅ Reset other sites
    this.otherProtocolsBySite.clear();  // ✅ Clear cached other protocols
    this.showView          = true;
    // ✅ Load both sites and patients when opening modal
    this.loadProtocolSites();
    this.loadProtocolPatients();
  }

  loadProtocolSites(): void {
    if (!this.selectedProtocol) return;
    this.sitesLoading = true;
    this.siteProtocolApi.getAll({ protocolID: this.selectedProtocol.protocolID }).subscribe(r => {
      this.sitesLoading = false;
      if (r.success) {
        this.protocolSites = r.data;
        this.separateSites();  // ✅ NEW: Separate into My Sites and Other Sites
        this.cdr.detectChanges();
      }
    });
  }

  loadProtocolPatients(): void {
    if (!this.selectedProtocol) return;
    this.patientsLoading  = true;
    this.patientsBySite   = [];
    this.protocolPatients = [];

    const params: any = { protocolID: this.selectedProtocol.protocolID };
    if (this.isInvestigator) params['investigatorID'] = this.userId;

    this.siteProtocolApi.getAll(params).subscribe(sp => {
      if (!sp.success || sp.data.length === 0) {
        this.patientsLoading = false;
        this.cdr.detectChanges();
        return;
      }

      const siteProtocols = sp.data; // [{siteProtocolID, siteName, investigatorID, status, ...}]

      // Load all patients once, then for each site-protocol load its enrollments
      this.patientApi.getAll().subscribe(allPatientsRes => {
        if (!allPatientsRes.success) { this.patientsLoading = false; return; }
        const allPatients = allPatientsRes.data;

        Promise.all(
          siteProtocols.map((site: any) =>
            this.enrollmentApi.getAll({ siteProtocolId: site.siteProtocolID }).toPromise()
              .then((res: any) => ({
                siteProtocolID:    site.siteProtocolID,
                siteName:          site.siteName,
                investigatorName:  site.investigatorName  || this.getInvestigatorName(site.investigatorID),
                investigatorEmail: site.investigatorEmail || this.getInvestigatorEmail(site.investigatorID),
                status:            site.status,
                enrollments:       res?.success ? res.data : []
              }))
          )
        ).then(siteResults => {
          const allPatientIds = new Set<number>();

          this.patientsBySite = siteResults.map((s: any, idx: number) => {
            const patientIds = s.enrollments.map((e: any) => e.patientID);
            const patients   = allPatients
              .filter((p: any) => patientIds.includes(p.patientID))
              .map((p: any) => {
                const enr = s.enrollments.find((e: any) => e.patientID === p.patientID);
                return { ...p, enrollmentStatus: enr?.status || 'Enrolled' };
              });
            patientIds.forEach((id: number) => allPatientIds.add(id));
            return {
              siteProtocolID:    s.siteProtocolID,
              siteName:          s.siteName,
              investigatorName:  s.investigatorName,
              investigatorEmail: s.investigatorEmail,
              status:            s.status,
              patients,
              expanded:          idx === 0   // first site open by default
            };
          });

          this.protocolPatients = allPatients.filter(
            (p: any) => allPatientIds.has(p.patientID));
          this.patientsLoading  = false;
          this.cdr.detectChanges();
        });
      });
    });
  }

  closeView(): void {
    this.showView = false;
    this.selectedProtocol = null;
    this.selectedPhases = [];
  }

  // ── Edit modal ────────────────────────────────────────────────────────────

  openEdit(p: ProtocolDto): void {
    this.selectedProtocol = p;
    this.editForm = {
      title:     p.title,
      startDate: p.startDate?.split('T')[0] ?? '',
      endDate:   p.endDate?.split('T')[0] ?? ''
    };
    const existing = this.parsePhasesFromDto(p);
    this.editPhases = existing.map(ph => ({
      phaseNumber: ph.phaseNumber,
      startDate:   ph.startDate?.split('T')[0] ?? '',
      endDate:     ph.endDate?.split('T')[0] ?? ''
    }));
    this.editPhaseCount = this.editPhases.length || null;
    this.editPhaseCountError = '';
    this.editSiteAssignments = [];
    this.editNewAssignment = { siteID: 0, investigatorID: 0 };
    this.siteAssignmentError = '';
    this.showEdit = true;
    this.loadEditSiteAssignments();
  }

  closeEdit(): void {
    this.showEdit = false;
    this.selectedProtocol = null;
    this.editPhases = [];
    this.editPhaseCount = null;
  }

  // ── Edit phase helpers ────────────────────────────────────────────────────

  onEditProtoDatesChange(): void {
    this.editPhases = [];
    this.editPhaseCount = null;
    this.editPhaseCountError = '';
  }

  onEditPhaseCountInput(): void {
    this.editPhaseCountError = '';
    this.editPhases = [];
    const count = this.editPhaseCount;
    if (!count || count < 1) { this.editPhaseCountError = 'Enter a number greater than 0'; return; }
    for (let i = 0; i < count; i++) {
      this.editPhases.push({
        phaseNumber: i + 1,
        startDate: i === 0 ? this.editForm.startDate : '',
        endDate:   i === count - 1 ? this.editForm.endDate : ''
      });
    }
  }

  editPhaseStartMin(i: number): string {
    if (i === 0) return this.editForm.startDate;
    return this.editPhases[i - 1]?.endDate || this.editForm.startDate;
  }

  editPhaseEndMin(i: number): string {
    return this.editPhases[i]?.startDate || this.editForm.startDate;
  }

  onEditPhaseStartChange(i: number): void {
    const s = this.editPhases[i].startDate;
    if (this.editPhases[i].endDate && this.editPhases[i].endDate < s)
      this.editPhases[i].endDate = '';
  }

  onEditPhaseEndChange(i: number): void {
    const e = this.editPhases[i].endDate;
    if (i < this.editPhases.length - 1) {
      const next = this.editPhases[i + 1];
      if (next.startDate && next.startDate < e) { next.startDate = ''; next.endDate = ''; }
    }
  }

  // ── Save edit ─────────────────────────────────────────────────────────────

  saveEdit(): void {
    if (!this.selectedProtocol) return;
    if (!this.editForm.title.trim()) { this.showMsg('error', 'Title is required.'); return; }
    if (!this.editDatesReady) { this.showMsg('error', 'Set valid start and end dates.'); return; }
    if (this.editPhases.length === 0) { this.showMsg('error', 'Add at least one phase.'); return; }
    for (const p of this.editPhases) {
      if (!p.startDate || !p.endDate) { this.showMsg('error', `Fill in all dates for Phase ${p.phaseNumber}.`); return; }
    }

    const payload = {
      title:     this.editForm.title.trim(),
      startDate: this.editForm.startDate,
      endDate:   this.editForm.endDate,
      phases:    this.editPhases.map(p => ({
        phaseNumber: p.phaseNumber,
        startDate:   p.startDate,
        endDate:     p.endDate
      }))
    };

    this.protocolApi.update(this.selectedProtocol.protocolID, payload).subscribe({
      next: r => {
        if (r.success) {
          this.closeEdit();
          this.load();
          this.showMsg('success', `Protocol "${payload.title}" updated.`);
        } else {
          this.showMsg('error', r.message);
        }
      },
      error: err => this.showMsg('error', err?.error?.message || 'Update failed.')
    });
  }

  // ── Delete ────────────────────────────────────────────────────────────────

  delete(p: ProtocolDto): void {
    if (p.status === 'Upcoming') {
      if (!confirm(`Archive protocol "${p.title}"? It will move to the Archived tab where it can be permanently deleted.`)) return;
      this.protocolApi.archive(p.protocolID).subscribe({
        next: r => {
          if (r.success) {
            this.closeEdit();
            this.showMsg('success', `Protocol "${p.title}" archived. Go to the Archived tab to permanently delete it.`);
            this.load();
          } else { this.showMsg('error', r.message); }
        },
        error: err => this.showMsg('error', err?.error?.message || 'Archive failed.')
      });
    } else if (p.status === 'Archived') {
      if (!confirm(`Permanently delete "${p.title}"? This cannot be undone.`)) return;
      this.protocolApi.delete(p.protocolID).subscribe({
        next: r => {
          if (r.success) {
            this.closeEdit();
            this.showMsg('success', `Protocol "${p.title}" permanently deleted.`);
            this.load();
          } else { this.showMsg('error', r.message); }
        },
        error: err => this.showMsg('error', err?.error?.message || 'Delete failed.')
      });
    }
  }

  // ── Archive ───────────────────────────────────────────────────────────────

  archive(p: ProtocolDto): void {
    if (p.status === 'Archived') {
      this.showMsg('error', 'This protocol is already archived.');
      return;
    }
    if (!confirm(`Archive protocol "${p.title}"?\n\nIt will move to the Archived tab. You can restore it later.`)) return;
    this.protocolApi.archive(p.protocolID).subscribe({
      next: r => {
        if (r.success) {
          this.closeEdit();
          this.load();
          this.showMsg('success', `Protocol "${p.title}" archived.`);
        } else {
          this.showMsg('error', r.message);
        }
      },
      error: err => this.showMsg('error', err?.error?.message || 'Archive failed.')
    });
  }

  // Restore an archived protocol back to its computed status (Upcoming/Ongoing/Completed)
  moveBack(p: ProtocolDto): void {
    const today = new Date();
    const start = new Date(p.startDate);
    const end   = new Date(p.endDate);

    let restoredStatus = 'Upcoming';
    if (today > end)         restoredStatus = 'Completed';
    else if (today >= start) restoredStatus = 'Ongoing';

    if (!confirm(
      `Restore "${p.title}" from archive?\n\nIt will be moved back to "${restoredStatus}" based on its dates.`
    )) return;

    this.protocolApi.unarchive(p.protocolID).subscribe({
      next: r => {
        if (r.success) {
          this.closeEdit();
          this.load();
          this.showMsg('success', `Protocol "${p.title}" restored to ${restoredStatus}.`);
        } else {
          this.showMsg('error', r.message || 'Failed to restore protocol.');
        }
      },
      error: (err: any) =>
        this.showMsg('error', err?.error?.message || 'Failed to restore protocol.')
    });
  }

  // ── Create protocol ───────────────────────────────────────────────────────

  onProtoDatesChange(): void {
    this.phases = [];
    this.phaseCount = null;
    this.phaseCountError = '';
  }

  onPhaseCountInput(): void {
    this.phaseCountError = '';
    this.phases = [];
    const count = this.phaseCount;
    if (!count || count < 1) { this.phaseCountError = 'Enter a number greater than 0'; return; }
    for (let i = 0; i < count; i++) {
      this.phases.push({
        phaseNumber: i + 1,
        startDate: i === 0 ? this.form.startDate : '',
        endDate:   i === count - 1 ? this.form.endDate : ''
      });
    }
  }

  phaseStartMin(i: number): string {
    if (i === 0) return this.form.startDate;
    return this.phases[i - 1]?.endDate || this.form.startDate;
  }

  phaseEndMin(i: number): string {
    return this.phases[i]?.startDate || this.form.startDate;
  }

  onPhaseStartChange(i: number): void {
    const s = this.phases[i].startDate;
    if (this.phases[i].endDate && this.phases[i].endDate < s) this.phases[i].endDate = '';
  }

  onPhaseEndChange(i: number): void {
    const e = this.phases[i].endDate;
    if (i < this.phases.length - 1) {
      const next = this.phases[i + 1];
      if (next.startDate && next.startDate < e) { next.startDate = ''; next.endDate = ''; }
    }
  }

  // ✅ NEW: Load site names for Admin
  loadAdminSiteNames(): void {
    this.adminProtocolSiteMap.clear();
    
    // Load ALL site-protocols at once
    this.siteProtocolApi.getAll({}).subscribe({
      next: (response: any) => {
        if (response.success && response.data) {
          // Group by protocolID
          const groupedByProtocol = new Map<number, string[]>();
          
          response.data.forEach((sp: any) => {
            if (!groupedByProtocol.has(sp.protocolID)) {
              groupedByProtocol.set(sp.protocolID, []);
            }
            groupedByProtocol.get(sp.protocolID)!.push(sp.siteName);
          });
          
          // Store in adminProtocolSiteMap
          groupedByProtocol.forEach((siteNames, protocolID) => {
            this.adminProtocolSiteMap.set(protocolID, siteNames.join(', '));
          });
          
          this.cdr.detectChanges();
        }
      }
    });
  }

  getSiteName(protocolID: number): string {
    // ✅ For Investigator: lookup from investigatorSiteMap (persistent across modal open/close)
    if (this.isInvestigator) {
      const site = this.investigatorSiteMap.get(protocolID);
      return site?.siteName ?? '';
    }
    // ✅ For Admin: lookup from adminProtocolSiteMap
    return this.adminProtocolSiteMap.get(protocolID) || '—';
  }

  get activeSites(): any[] {
    return this.allSites.filter(s => s.status === 'Active');
  }

  getSiteLocation(id: number): string {
    // ✅ For Investigator: get location from investigatorSiteMap (id is protocolID)
    if (this.isInvestigator && this.investigatorSiteMap.has(id)) {
      const site = this.investigatorSiteMap.get(id);
      return site?.location ?? '';
    }
    // ✅ Otherwise look up by siteID from allSites
    const site = this.allSites.find(s => s.siteID === id);
    return site?.location || '—';
  }

  // ✅ NEW: Get site email from allSites array
  getSiteEmail(siteID: number): string {
    const site = this.allSites.find(s => s.siteID === siteID);
    return site?.email || '—';
  }

  // ✅ NEW: Get site contact from allSites array
  getSiteContact(siteID: number): string {
    const site = this.allSites.find(s => s.siteID === siteID);
    return site?.contact || site?.phone || '—';
  }

  getInvestigatorName(id: number): string {
    return this.allInvestigators.find(u => +u.userID === +id)?.name ?? '';
  }

  addSiteAssignment(): void {
    if (!this.newAssignment.siteID || !this.newAssignment.investigatorID) {
      this.siteAssignmentError = 'Select both a site and an investigator.'; return;
    }
    const dup = this.siteAssignments.find(a => a.siteID === +this.newAssignment.siteID);
    if (dup) { this.siteAssignmentError = 'This site is already assigned.'; return; }
    this.siteAssignments.push({ siteID: +this.newAssignment.siteID, investigatorID: +this.newAssignment.investigatorID });
    this.newAssignment = { siteID: 0, investigatorID: 0 };
    this.siteAssignmentError = '';
  }

  removeSiteAssignment(i: number): void { this.siteAssignments.splice(i, 1); }

  addEditSiteAssignment(): void {
    if (!this.editNewAssignment.siteID || !this.editNewAssignment.investigatorID) {
      this.siteAssignmentError = 'Select both a site and an investigator.'; return;
    }
    const dup = this.editSiteAssignments.find((a: any) => a.siteID === +this.editNewAssignment.siteID);
    if (dup) { this.siteAssignmentError = 'This site is already assigned.'; return; }
    // Save to backend immediately
    if (!this.selectedProtocol) return;
    this.siteProtocolApi.create({
      protocolID:     this.selectedProtocol.protocolID,
      siteID:         +this.editNewAssignment.siteID,
      investigatorID: +this.editNewAssignment.investigatorID,
      status:         'Active'
    }).subscribe(r => {
      if (r.success) {
        this.loadEditSiteAssignments();
        this.editNewAssignment = { siteID: 0, investigatorID: 0 };
        this.siteAssignmentError = '';
      } else { this.siteAssignmentError = r.message; }
    });
  }

  loadEditSiteAssignments(): void {
    if (!this.selectedProtocol) return;
    this.siteProtocolApi.getAll({ protocolID: this.selectedProtocol.protocolID }).subscribe(r => {
      if (r.success) { this.editSiteAssignments = r.data; this.cdr.detectChanges(); }
    });
  }

  create(): void {
    if (!this.form.title.trim()) { this.errorMsg = 'Title is required.'; return; }
    if (!this.datesReady) { this.errorMsg = 'Set valid protocol start and end dates first.'; return; }
    if (this.phases.length === 0) { this.errorMsg = 'Add at least one phase.'; return; }
    for (const p of this.phases) {
      if (!p.startDate || !p.endDate) { this.errorMsg = `Fill in all dates for Phase ${p.phaseNumber}.`; return; }
    }
    // ✅ VALIDATE: At least one site must be assigned
    if (this.siteAssignments.length === 0) {
      this.errorMsg = 'You must assign at least one site to create a protocol.';
      this.siteAssignmentError = 'At least one site assignment is required.';
      return;
    }

    const payload = {
      title:     this.form.title.trim(),
      startDate: this.form.startDate,
      endDate:   this.form.endDate,
      phases:    this.phases.map(p => ({ phaseNumber: p.phaseNumber, startDate: p.startDate, endDate: p.endDate }))
    };

    this.protocolApi.create(payload).subscribe({
      next: r => {
        if (r.success) {
          const protocolId = r.data?.protocolID;
          // Create site-protocol records
          if (protocolId && this.siteAssignments.length > 0) {
            Promise.all(this.siteAssignments.map(a =>
              this.siteProtocolApi.create({
                protocolID: protocolId, siteID: a.siteID,
                investigatorID: a.investigatorID, status: 'Active'
              }).toPromise()
            )).then(() => {
              this.showCreate = false;
              this.resetForm();
              this.load();
              this.showMsg('success', `Protocol "${payload.title}" created with ${this.siteAssignments.length} site(s).`);
            });
          } else {
            this.showCreate = false;
            this.resetForm();
            this.load();
            this.showMsg('success', `Protocol "${payload.title}" created.`);
          }
        } else {
          this.showMsg('error', r.message);
        }
      },
      error: err => this.showMsg('error', err?.error?.message || 'Failed to create protocol.')
    });
  }

  // ── Load ──────────────────────────────────────────────────────────────────

  load(): void {
    this.isLoading = true;

    if (this.isInvestigator) {
      this.siteProtocolApi.getAll({ investigatorID: this.userId }).subscribe(sp => {
        if (sp.success) {
          // ✅ Store in investigatorSiteMap (separate from protocolSites for modal)
          this.investigatorSiteMap.clear();
          sp.data.forEach((x: any) => {
            this.investigatorSiteMap.set(x.protocolID, {
              siteName: x.siteName,
              location: x.location || x.siteLocation || ''
            });
          });
          
          const protocolIds = new Set(sp.data.map((x: any) => x.protocolID));
          this.protocolApi.getAll().subscribe(r => {
            this.isLoading = false;
            if (r.success) {
              const mine = r.data.filter(p => protocolIds.has(p.protocolID));
              this.protocols = mine;
              this.applyFilter();
              this.cdr.detectChanges();
            }
          });
        } else {
          this.isLoading = false;
          this.cdr.detectChanges();
        }
      });
      return;
    }

    this.protocolApi.getAll().subscribe({
      next: r => {
        this.isLoading = false;
        if (r.success) {
          this.protocols = r.data ?? [];
          this.applyFilter();
          
          // ✅ For Admin: Load site names after protocols are loaded
          if (!this.isInvestigator && this.allSites.length > 0) {
            this.loadAdminSiteNames();
          }
          
          this.cdr.detectChanges();
        }
      },
      error: () => { this.isLoading = false; this.cdr.detectChanges(); }
    });
  }

  resetForm(): void {
    this.form = { title: '', startDate: '', endDate: '' };
    this.phases = [];
    this.phaseCount = null;
    this.phaseCountError = '';
    this.siteAssignments = [];
    this.newAssignment = { siteID: 0, investigatorID: 0 };
    this.siteAssignmentError = '';
    this.errorMsg = '';
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  private showMsg(type: 'success' | 'error', msg: string): void {
    if (type === 'success') { this.successMsg = msg; this.errorMsg = ''; }
    else { this.errorMsg = msg; this.successMsg = ''; }
    setTimeout(() => { this.successMsg = ''; this.errorMsg = ''; this.cdr.detectChanges(); }, 4000);
  }

  countFor(tab: string): number {
    if (tab === 'all') return this.protocols.filter(p => p.status.toLowerCase() !== 'archived').length;
    return this.protocols.filter(p => p.status.toLowerCase() === tab).length;
  }

  getStatusBadge(status: string): string {
    const map: Record<string, string> = {
      'Ongoing':   'lt-badge-green',
      'Active':    'lt-badge-green',
      'Upcoming':  'lt-badge-amber',
      'Completed': 'lt-badge-blue',
      'Archived':  'lt-badge-archived',
      'On Hold':   'lt-badge-amber',
      'Cancelled': 'lt-badge-red',
      'Draft':     'lt-badge-gray'
    };
    return map[status] ?? 'lt-badge-gray';
  }

  canDelete(p: ProtocolDto): boolean { return p.status === 'Upcoming' || p.status === 'Archived'; }
  canArchive(p: ProtocolDto): boolean { return p.status !== 'Archived'; }

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

  // ✅ NEW: Separate sites into My Sites and Other Sites
  separateSites(): void {
    this.currentUserID = this.authService.getUserId();
    
    this.mySites = this.protocolSites.filter(
      site => site.investigatorID === this.currentUserID
    ).map(site => ({ ...site, showDetails: false }));  // ✅ Reset showDetails flag
    
    this.otherSites = this.protocolSites.filter(
      site => site.investigatorID !== this.currentUserID
    ).map(site => ({ ...site, showDetails: false }));  // ✅ Reset showDetails flag
  }

  // ✅ NEW: Navigate to site details
  viewSiteDetails(siteID: number): void {
    console.log('Navigate to site:', siteID);
    // TODO: Add router navigation if needed
    // this.router.navigate(['/sites', siteID]);
  }

  // ✅ NEW: Navigate to site protocol details
  viewMoreDetails(siteProtocolID: number): void {
    console.log('Navigate to site protocol:', siteProtocolID);
    // TODO: Add router navigation if needed
    // this.router.navigate(['/site-protocols', siteProtocolID]);
  }

  // ✅ NEW: Toggle site details visibility
  toggleSiteDetails(site: any): void {
    site.showDetails = !site.showDetails;
    // Load other protocols at this site when expanding
    if (site.showDetails && !this.otherProtocolsBySite.has(site.siteID)) {
      this.loadOtherProtocolsAtSite(site.siteID);
    }
  }

  // ✅ NEW: Load other protocols running at this site
  loadOtherProtocolsAtSite(siteID: number): void {
    this.siteProtocolApi.getAll({ siteID: siteID }).subscribe({
      next: (response: any) => {
        if (response.success) {
          const protocols = response.data
            .filter((sp: any) => sp.protocolID !== this.selectedProtocol?.protocolID)
            .map((sp: any) => ({
              title: sp.protocolTitle,
              protocolID: sp.protocolID,
              investigatorName: sp.investigatorName,
              investigatorEmail: this.getInvestigatorEmail(sp.investigatorID),
              investigatorContact: this.getInvestigatorContact(sp.investigatorID)
            }));
          this.otherProtocolsBySite.set(siteID, protocols);
          this.cdr.detectChanges();
        }
      }
    });
  }

  // ✅ NEW: Get other protocols running at this site (from cache)
  getOtherProtocolsAtSite(siteID: number): any[] {
    return this.otherProtocolsBySite.get(siteID) || [];
  }

  // ✅ NEW: Get investigator email from allInvestigators array
  getInvestigatorEmail(investigatorID: number): string {
    const investigator = this.allInvestigators.find(inv => +inv.userID === +investigatorID);
    return investigator?.email || '—';
  }

  // ✅ NEW: Get investigator contact from allInvestigators array
  getInvestigatorContact(investigatorID: number): string {
    const investigator = this.allInvestigators.find(inv => inv.userID === investigatorID);
    return investigator?.contact || investigator?.phone || '—';
  }
}