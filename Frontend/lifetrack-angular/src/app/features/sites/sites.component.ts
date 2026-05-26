// sites.component.ts
import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  SiteApiService, SiteDto,
  SiteProtocolApiService, EnrollmentApiService
} from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import { SidebarComponent } from '../../shared/sidebar.component';

@Component({
  selector: 'app-sites',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, SidebarComponent],
  templateUrl: './sites.component.html'
})
export class SitesComponent implements OnInit {
  sites:         SiteDto[] = [];
  filtered:      SiteDto[] = [];
  isLoading      = false;
  successMsg     = '';
  errorMsg       = '';
  canEdit        = false;
  isInvestigator = false;
  unreadCount    = 0;
  activeTab      = 'All';
  private userId = 0;

  // ✅ Pagination
  currentPage = 1;
  pageSize = 10;
  totalItems = 0;
  totalPages = 0;
  jumpToPage = 1;
  itemsPerPageOptions = [10, 20, 50];
  paginatedFiltered: SiteDto[] = [];

  // Create modal
  showCreate = false;
  createForm = { name: '', location: '', email: '', contact: '', status: 'Active' };
  createError = '';

  // Edit modal
  showEdit    = false;
  editSite:   SiteDto | null = null;
  editForm    = { name: '', location: '', email: '', contact: '', status: 'Active' };
  editError   = '';

  // View modal
  showView       = false;
  viewSite:      SiteDto | null = null;
  viewTab        = 'overview';
  viewProtocols: any[] = [];
  viewPatientCount = 0;
  viewLoading    = false;

  readonly statusTabs = ['All', 'Active', 'Inactive'];

  constructor(
    private siteApi:         SiteApiService,
    private siteProtocolApi: SiteProtocolApiService,
    private enrollmentApi:   EnrollmentApiService,
    private authService:     AuthService,
    private cdr:             ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    const role         = this.authService.getRole();
    this.userId        = this.authService.getUserId();
    this.canEdit       = ['Admin', 'ClinicalTrialManager'].includes(role);
    this.isInvestigator = role === 'Investigator';
    this.load();
  }

  load(): void {
    this.isLoading = true;
    if (this.isInvestigator) {
      this.siteProtocolApi.getAll({ investigatorID: this.userId }).subscribe(sp => {
        if (sp.success) {
          const siteIds = new Set(sp.data.map((x: any) => x.siteID));
          this.siteApi.getAll().subscribe(r => {
            this.isLoading = false;
            if (r.success) {
              this.sites = r.data.filter(s => siteIds.has(s.siteID));
              this.applyFilter();
              this.cdr.detectChanges();
            }
          });
        } else { this.isLoading = false; this.cdr.detectChanges(); }
      });
    } else {
      this.siteApi.getAll().subscribe({
        next: r => {
          this.isLoading = false;
          if (r.success) { this.sites = [...r.data]; this.applyFilter(); this.cdr.detectChanges(); }
        },
        error: () => { this.isLoading = false; this.cdr.detectChanges(); }
      });
    }
  }

  setTab(tab: string): void { this.activeTab = tab; this.applyFilter(); }

  applyFilter(): void {
    this.filtered = this.activeTab === 'All'
      ? [...this.sites]
      : this.sites.filter(s => s.status === this.activeTab);
    this.calculateTotalPages(); // ✅ NEW
    this.cdr.detectChanges();
  }

  countFor(tab: string): number {
    return tab === 'All' ? this.sites.length : this.sites.filter(s => s.status === tab).length;
  }

  // ── Create ──────────────────────────────────────────────────
  openCreate(): void { this.createForm = { name: '', location: '', email: '', contact: '', status: 'Active' }; this.createError = ''; this.showCreate = true; }

  create(): void {
    if (!this.createForm.name.trim() || !this.createForm.location.trim()) {
      this.createError = 'Name and location are required.'; return;
    }
    this.siteApi.create(this.createForm).subscribe({
      next: r => {
        if (r.success) {
          this.showCreate = false;
          this.showMsg('success', `Site "${this.createForm.name}" created.`);
          this.load();
        } else { this.createError = r.message; }
      },
      error: err => { this.createError = err.error?.message ?? 'Failed.'; }
    });
  }

  // ── Edit ────────────────────────────────────────────────────
  openEdit(s: SiteDto): void {
    this.editSite  = s;
    this.editForm  = { name: s.name, location: s.location, email: s.email || '', contact: s.contact || '', status: s.status };
    this.editError = '';
    this.showEdit  = true;
  }

  saveEdit(): void {
    if (!this.editSite) return;
    if (!this.editForm.name.trim() || !this.editForm.location.trim()) {
      this.editError = 'Name and location are required.'; return;
    }
    this.siteApi.update(this.editSite.siteID, this.editForm).subscribe({
      next: r => {
        if (r.success) {
          this.showEdit = false;
          this.showMsg('success', `Site "${this.editForm.name}" updated.`);
          this.load();
        } else { this.editError = r.message; }
      },
      error: err => { this.editError = err.error?.message ?? 'Failed.'; }
    });
  }

  // ── View ────────────────────────────────────────────────────
  openView(s: SiteDto): void {
    this.viewSite         = s;
    this.viewTab          = 'overview';
    this.viewProtocols    = [];
    this.viewPatientCount = 0;
    this.viewLoading      = true;
    this.showView         = true;
    this.loadViewData(s.siteID);
  }

  loadViewData(siteId: number): void {
    this.siteProtocolApi.getAll({ siteID: siteId }).subscribe(r => {
      if (r.success) {
        this.viewProtocols = r.data;
        // Count unique patients across all site-protocols at this site
        const spIds = r.data.map((sp: any) => sp.siteProtocolID);
        if (spIds.length > 0) {
          Promise.all(spIds.map((id: number) =>
            this.enrollmentApi.getAll({ siteProtocolId: id }).toPromise()
          )).then(results => {
            const patientIds = new Set<number>();
            results.forEach((res: any) => {
              if (res?.success) res.data.forEach((e: any) => patientIds.add(e.patientID));
            });
            this.viewPatientCount = patientIds.size;
            this.viewLoading = false;
            this.cdr.detectChanges();
          });
        } else {
          this.viewLoading = false;
          this.cdr.detectChanges();
        }
      }
    });
  }

  closeView(): void { this.showView = false; this.viewSite = null; }

  // ── Helpers ─────────────────────────────────────────────────
  getStatusBadge(status: string): string {
    return status === 'Active' ? 'lt-badge lt-badge-green' : 'lt-badge lt-badge-gray';
  }

  private showMsg(type: 'success' | 'error', msg: string): void {
    if (type === 'success') { this.successMsg = msg; this.errorMsg = ''; }
    else { this.errorMsg = msg; this.successMsg = ''; }
    setTimeout(() => { this.successMsg = ''; this.errorMsg = ''; this.cdr.detectChanges(); }, 3000);
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

  // ✅ Validation: Only allow numbers in Contact field
  onlyNumbers(event: KeyboardEvent): boolean {
    const charCode = event.which ? event.which : event.keyCode;
    if (charCode > 31 && (charCode < 48 || charCode > 57)) {
      event.preventDefault();
      return false;
    }
    return true;
  }

  // ✅ Validation: Strip non-numbers on paste
  onPaste(event: ClipboardEvent): void {
    event.preventDefault();
    const pastedText = event.clipboardData?.getData('text') || '';
    const numbersOnly = pastedText.replace(/[^0-9]/g, '').substring(0, 10);
    
    // Update the appropriate form
    if (this.showCreate) {
      this.createForm.contact = numbersOnly;
    } else if (this.showEdit) {
      this.editForm.contact = numbersOnly;
    }
  }
}