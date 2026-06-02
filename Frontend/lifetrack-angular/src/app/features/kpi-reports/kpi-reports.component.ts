import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SidebarComponent } from '../../shared/sidebar.component';
import { AuthService } from '../../core/services/auth.service';
import {
  KPIReportApiService, KPIReportDto, CreateKPIReportRequest,
  ProtocolApiService, ProtocolDto,
  EnrollmentApiService, AdverseEventApiService,
  SiteProtocolApiService
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
  titleSearch:    string = '';      // search by report title/protocol name
  protocolFilter: number | '' = '';

  // ── Pagination ─────────────────────────────────────────────
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
    const range = 2; const pages: number[] = [];
    for (let i = Math.max(1, this.currentPage - range);
             i <= Math.min(this.totalPages, this.currentPage + range); i++)
      pages.push(i);
    return pages;
  }
  nextPage(): void { if (this.currentPage < this.totalPages) this.currentPage++; }
  prevPage(): void { if (this.currentPage > 1) this.currentPage--; }
  goToPage(p: number): void { this.currentPage = p; }
  changePageSize(s: number): void { this.pageSize = +s; this.currentPage = 1; }
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
  calcLoading = false;
  calcError   = '';

  // View modal
  showViewModal   = false;
  selectedReport: KPIReportDto | null = null;

  constructor(
    private kpiApi:        KPIReportApiService,
    private protocolApi:   ProtocolApiService,
    private enrollmentApi:    EnrollmentApiService,
    private aeApi:            AdverseEventApiService,
    private siteProtocolApi:  SiteProtocolApiService,
    private authService:   AuthService,
    private cdr:           ChangeDetectorRef
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
    if (this.titleSearch.trim()) {
      const q = this.titleSearch.toLowerCase();
      result = result.filter(r => (r.protocolTitle || '').toLowerCase().includes(q));
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
    this.currentPage = 1;
  }

  clearFilters(): void {
    this.titleSearch    = '';
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

  // Called when user picks a protocol — fetches and calculates metrics automatically
  onProtocolChange(): void {
    const pid = +this.genForm.protocolID;
    if (!pid) return;
    this.calcLoading = true;
    this.calcError   = '';
    this.genForm.enrollmentRate = 0;
    this.genForm.dropoutRate    = 0;
    this.genForm.aeCount        = 0;

    // Step 1: Get all site-protocols for this protocol to find their IDs
    // EnrollmentFilterDto only supports siteProtocolID — not protocolID directly
    this.siteProtocolApi.getAll({ protocolID: pid }).toPromise()
      .then((spRes: any) => {
        const spIds: number[] = (spRes?.data ?? []).map((sp: any) => sp.siteProtocolID);

        // Step 2: Fetch enrollments per siteProtocol + AEs in parallel
        const enrollmentCalls = spIds.length > 0
          ? spIds.map((spId: number) =>
              this.enrollmentApi.getAll({ siteProtocolId: spId }).toPromise())
          : [Promise.resolve({ data: [] })];

        return Promise.all([
          Promise.all(enrollmentCalls),
          this.aeApi.getAll({ protocolID: pid }).toPromise()
        ]);
      })
      .then(([enrollResults, aeRes]: any[]) => {
        this.calcLoading = false;

        // Flatten all enrollment arrays from all site-protocols
        const enrollments: any[] = enrollResults
          .flatMap((r: any) => r?.data ?? []);
        const aes: any[] = aeRes?.data ?? [];

        const total     = enrollments.length;
        const active    = enrollments.filter((e: any) => e.status === 'Active').length;
        const completed = enrollments.filter((e: any) => e.status === 'Completed').length;
        const withdrawn = enrollments.filter((e: any) =>
          e.status === 'Withdrawn' || e.status === 'PendingWithdrawal').length;

        // Enrollment Rate = (Active + Completed) / Total × 100
        this.genForm.enrollmentRate = total > 0
          ? Math.round(((active + completed) / total) * 1000) / 10
          : 0;

        // Dropout Rate = Withdrawn / Total × 100
        this.genForm.dropoutRate = total > 0
          ? Math.round((withdrawn / total) * 1000) / 10
          : 0;

        // AE Count = total adverse events for this protocol
        this.genForm.aeCount = aes.length;

        this.cdr.detectChanges();
      })
      .catch(() => {
        this.calcLoading = false;
        this.calcError   = 'Failed to fetch metrics. You can enter values manually.';
        this.cdr.detectChanges();
      });
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