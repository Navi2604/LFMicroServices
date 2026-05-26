import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SidebarComponent } from '../../shared/sidebar.component';
import { AuthService } from '../../core/services/auth.service';
import {
  KPIReportApiService, KPIReportDto, CreateKPIReportRequest,
  ProtocolApiService, ProtocolDto
} from '../../core/services/api.service';

@Component({
  selector: 'app-kpi-reports',
  standalone: true,
  imports: [CommonModule, FormsModule, SidebarComponent],
  templateUrl: './kpi-reports.component.html'
})
export class KpiReportsComponent implements OnInit {
  reports:   KPIReportDto[] = [];
  filtered:  KPIReportDto[] = [];
  protocols: ProtocolDto[]  = [];

  isLoading   = false;
  canGenerate = false;
  userRole    = '';
  unreadCount = 0;
  errorMsg    = '';

  // Filter state
  protocolFilter: number | '' = '';
  fromDateFilter: string = '';
  toDateFilter:   string = '';

  // Generate modal
  showGenerateModal = false;
  isGenerating      = false;
  genError          = '';
  genForm: CreateKPIReportRequest = {
    protocolID: 0, scope: '',
    enrollmentRate: 0, dropoutRate: 0, aeCount: 0
  };

  // View modal
  showViewModal   = false;
  selectedReport: KPIReportDto | null = null;

  constructor(
    private kpiApi:      KPIReportApiService,
    private protocolApi: ProtocolApiService,
    private authService: AuthService,
    private cdr:         ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.userRole    = this.authService.getRole();
    this.canGenerate = ['Admin', 'ClinicalTrialManager', 'DataManager']
                         .includes(this.userRole);
    this.loadProtocols();
    this.load();
  }

  // ── Data ──────────────────────────────────────────────────────

  load(): void {
    this.isLoading = true;
    this.errorMsg  = '';
    this.kpiApi.getAll().subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.success) {
          this.reports = (res.data ?? []).sort((a, b) =>
            new Date(b.generatedDate).getTime() - new Date(a.generatedDate).getTime()
          );
          this.applyFilter();
        } else {
          this.errorMsg = res.message;
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMsg  = err.error?.message ?? 'Failed to load KPI reports.';
        this.cdr.detectChanges();
      }
    });
  }

  loadProtocols(): void {
    this.protocolApi.getAll().subscribe({
      next: (res) => {
        if (res.success) {
          this.protocols = res.data ?? [];
          this.cdr.detectChanges();
        }
      }
    });
  }

  // ── Filtering ─────────────────────────────────────────────────

  onFilterChange(): void { this.applyFilter(); }

  applyFilter(): void {
    let result = [...this.reports];
    if (this.protocolFilter) {
      result = result.filter(r => r.protocolID === Number(this.protocolFilter));
    }
    if (this.fromDateFilter) {
      const from = new Date(this.fromDateFilter).getTime();
      result = result.filter(r => new Date(r.generatedDate).getTime() >= from);
    }
    if (this.toDateFilter) {
      const to = new Date(this.toDateFilter).getTime() + 86400000; // include the end date
      result = result.filter(r => new Date(r.generatedDate).getTime() <= to);
    }
    this.filtered = result;
  }

  clearFilters(): void {
    this.protocolFilter = '';
    this.fromDateFilter = '';
    this.toDateFilter   = '';
    this.applyFilter();
  }

  get hasActiveFilter(): boolean {
    return !!(this.protocolFilter || this.fromDateFilter || this.toDateFilter);
  }

  // ── Generate modal ────────────────────────────────────────────

  openGenerateModal(): void {
    this.genForm      = { protocolID: 0, scope: '', enrollmentRate: 0, dropoutRate: 0, aeCount: 0 };
    this.genError     = '';
    this.isGenerating = false;
    this.showGenerateModal = true;
  }

  closeGenerateModal(): void {
    this.showGenerateModal = false;
    this.genError = '';
  }

  isGenFormValid(): boolean {
    return this.genForm.protocolID > 0 &&
           this.genForm.enrollmentRate >= 0 && this.genForm.enrollmentRate <= 100 &&
           this.genForm.dropoutRate    >= 0 && this.genForm.dropoutRate    <= 100 &&
           this.genForm.aeCount        >= 0;
  }

  submitGenerate(): void {
    if (!this.isGenFormValid() || this.isGenerating) return;
    this.isGenerating = true;
    this.genError     = '';

    this.kpiApi.create(this.genForm).subscribe({
      next: (res) => {
        this.isGenerating = false;
        if (res.success) {
          this.reports.unshift(res.data);
          this.applyFilter();
          this.closeGenerateModal();
        } else {
          this.genError = res.message || 'Failed to generate report.';
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isGenerating = false;
        this.genError     = err.error?.message ?? 'Failed to generate report.';
        this.cdr.detectChanges();
      }
    });
  }

  // ── View modal ────────────────────────────────────────────────

  openView(report: KPIReportDto): void {
    this.selectedReport = report;
    this.showViewModal  = true;
  }

  closeViewModal(): void {
    this.showViewModal  = false;
    this.selectedReport = null;
  }

  // ── Helpers ───────────────────────────────────────────────────

  getEnrollmentClass(rate: number): string {
    if (rate >= 70) return 'kpi-pill kpi-pill-green';
    if (rate >= 40) return 'kpi-pill kpi-pill-amber';
    return 'kpi-pill kpi-pill-red';
  }

  getDropoutClass(rate: number): string {
    if (rate <= 10) return 'kpi-pill kpi-pill-green';
    if (rate <= 25) return 'kpi-pill kpi-pill-amber';
    return 'kpi-pill kpi-pill-red';
  }

  getAEClass(count: number): string {
    if (count === 0) return 'kpi-pill kpi-pill-green';
    if (count <= 5)  return 'kpi-pill kpi-pill-amber';
    return 'kpi-pill kpi-pill-red';
  }

  getProtocolName(id: number): string {
    return this.protocols.find(p => p.protocolID === id)?.title || `Protocol #${id}`;
  }

  formatRate(rate: number): string {
    return (rate ?? 0).toFixed(1) + '%';
  }

  exportToCsv(): void {
  const headers = [
    'Report ID', 'Protocol', 'Scope',
    'Enrollment Rate (%)', 'Dropout Rate (%)', 'AE Count', 'Generated Date'
  ];
  const rows = this.filtered.map(r => [
    `KPI-${r.reportID}`,
    r.protocolTitle || this.getProtocolName(r.protocolID),
    r.scope || '',
    r.enrollmentRate.toFixed(1),
    r.dropoutRate.toFixed(1),
    r.aeCount,
    new Date(r.generatedDate).toLocaleDateString('en-GB')
  ]);
  const date = new Date().toISOString().slice(0, 10);
  this.downloadCsv(`kpi-reports-${date}.csv`, headers, rows);
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

  getEnrollmentColor(rate: number): string {
    if (rate >= 70) return 'green';
    if (rate >= 40) return 'amber';
    return 'red';
  }

  getDropoutColor(rate: number): string {
    if (rate <= 10) return 'green';
    if (rate <= 25) return 'amber';
    return 'red';
  }

  getAEColor(count: number): string {
    if (count === 0) return 'green';
    if (count <= 5)  return 'amber';
    return 'red';
  }
}