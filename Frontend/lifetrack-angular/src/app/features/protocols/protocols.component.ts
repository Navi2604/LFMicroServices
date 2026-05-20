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

  // ── Modal state ───────────────────────────────────────────────────────────
  showCreate  = false;
  showView    = false;
  showEdit    = false;
  selectedProtocol: ProtocolDto | null = null;
  selectedPhases:   ParsedPhase[] = [];
  viewTab         = 'details';
  protocolPatients: any[] = [];
  patientsLoading  = false;
  protocolSites:   any[] = [];
  sitesLoading     = false;

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
    if (this.canEdit) {
      this.siteApi.getAll().subscribe(r => { if (r.success) this.allSites = r.data.filter((s: any) => s.status === 'Active'); });
      this.userApi.getAll().subscribe(r => {
        if (r.success) this.allInvestigators = r.data.filter((u: any) => u.roleName === 'Investigator');
      });
    }
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

  // ── View modal ────────────────────────────────────────────────────────────

  openView(p: ProtocolDto): void {
    this.selectedProtocol  = p;
    this.selectedPhases    = this.parsePhasesFromDto(p);
    this.viewTab           = 'details';
    this.protocolPatients  = [];
    this.protocolSites     = [];
    this.showView          = true;
    this.loadProtocolSites();
  }

  loadProtocolSites(): void {
    if (!this.selectedProtocol) return;
    this.sitesLoading = true;
    this.siteProtocolApi.getAll({ protocolID: this.selectedProtocol.protocolID }).subscribe(r => {
      this.sitesLoading = false;
      if (r.success) {
        this.protocolSites = r.data;
        this.cdr.detectChanges();
      }
    });
  }

  loadProtocolPatients(): void {
    if (!this.selectedProtocol) return;
    this.patientsLoading = true;
    const protocolId = this.selectedProtocol.protocolID;

    // Get site-protocols for this protocol (filtered by investigator if needed)
    const params: any = { protocolID: protocolId };
    if (this.isInvestigator) params['investigatorID'] = this.userId;

    this.siteProtocolApi.getAll(params).subscribe(sp => {
      if (sp.success && sp.data.length > 0) {
        const spIds = sp.data.map((x: any) => x.siteProtocolID);

        Promise.all(spIds.map((id: number) =>
          this.enrollmentApi.getAll({ siteProtocolId: id }).toPromise()
        )).then(results => {
          const patientIds = new Set<number>();
          results.forEach((res: any) => {
            if (res?.success) res.data.forEach((e: any) => patientIds.add(e.patientID));
          });

          this.patientApi.getAll().subscribe(r => {
            this.patientsLoading = false;
            if (r.success) {
              this.protocolPatients = r.data.filter(p => patientIds.has(p.patientID));
              this.cdr.detectChanges();
            }
          });
        });
      } else {
        this.patientsLoading  = false;
        this.protocolPatients = [];
        this.cdr.detectChanges();
      }
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
    if (!confirm(`Archive protocol "${p.title}"? It will move to the Archived tab.`)) return;
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

  // ── Site assignment helpers ───────────────────────────────────────────────

  getSiteName(id: number): string {
    return this.allSites.find(s => s.siteID === +id)?.name ?? '';
  }

  getInvestigatorName(id: number): string {
    return this.allInvestigators.find(u => u.userID === +id)?.name ?? '';
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
  canArchive(p: ProtocolDto): boolean { return p.status === 'Ongoing' || p.status === 'Completed'; }
}