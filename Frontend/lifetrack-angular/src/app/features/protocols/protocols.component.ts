// protocols.component.ts
import { Component, OnInit, ChangeDetectorRef, ViewEncapsulation } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ProtocolApiService, ProtocolDto } from '../../core/services/api.service';
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
  unreadCount = 0;

  // ── Tab state ─────────────────────────────────────────────────────────────
  activeTab: 'all' | 'upcoming' | 'ongoing' | 'completed' | 'archived' = 'all';

  // ── Modal state ───────────────────────────────────────────────────────────
  showCreate  = false;
  showView    = false;
  showEdit    = false;
  selectedProtocol: ProtocolDto | null = null;
  selectedPhases: ParsedPhase[] = [];

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

  constructor(
    private protocolApi: ProtocolApiService,
    private authService: AuthService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.canEdit = ['Admin', 'ClinicalTrialManager'].includes(this.authService.getRole());
    this.load();
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
    this.selectedProtocol = p;
    this.selectedPhases = this.parsePhasesFromDto(p);
    this.showView = true;
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
    this.showEdit = true;
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
    if (!confirm(`Delete protocol "${p.title}"? This cannot be undone.`)) return;
    this.protocolApi.delete(p.protocolID).subscribe({
      next: r => {
        if (r.success) {
          this.closeEdit();
          this.load();
          this.showMsg('success', `Protocol "${p.title}" deleted.`);
        } else {
          this.showMsg('error', r.message);
        }
      },
      error: err => this.showMsg('error', err?.error?.message || 'Delete failed.')
    });
  }

  // ── Archive ───────────────────────────────────────────────────────────────

  archive(p: ProtocolDto): void {
    if (!confirm(`Archive protocol "${p.title}"? It will no longer appear in active lists.`)) return;
    this.protocolApi.update(p.protocolID, {
      title:     p.title,
      startDate: p.startDate,
      endDate:   p.endDate,
      status:    'Archived',
      phases:    this.parsePhasesFromDto(p)
    }).subscribe({
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
          this.showCreate = false;
          this.resetForm();
          this.load();
          this.showMsg('success', `Protocol "${payload.title}" created.`);
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

  canDelete(p: ProtocolDto): boolean { return p.status === 'Upcoming'; }
  canArchive(p: ProtocolDto): boolean { return p.status === 'Ongoing' || p.status === 'Completed'; }
} 