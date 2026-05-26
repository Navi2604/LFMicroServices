// audit.component.ts - COMPLETE WITH PAGINATION
import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuditApiService } from '../../core/services/api.service';
import { SidebarComponent } from '../../shared/sidebar.component';

interface AuditLogDto {
  auditID:    number;
  userID:     number;
  action:     string;
  entityType: string;
  entityID:   number;
  details:    string;
  actionTime: string;
}

@Component({
  selector: 'app-audit',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, SidebarComponent],
  templateUrl: './audit.component.html'
})
export class AuditComponent implements OnInit {
  logs:       AuditLogDto[] = [];
  isLoading   = false;
  totalLogs   = 0;
  unreadCount = 0;

  // Filters
  filterAction     = '';
  filterEntityType = '';
  filterFromDate   = '';
  filterToDate     = '';

  // ✅ Pagination
  currentPage = 1;
  pageSize = 50;
  totalPages = 0;
  manualPageInput = '';
  itemsPerPageOptions = [25, 50, 100];

  constructor(
    private auditApi: AuditApiService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    this.isLoading = true;

    const params: any = { page: this.currentPage, pageSize: this.pageSize };
    if (this.filterAction)     params['action']     = this.filterAction;
    if (this.filterEntityType) params['entityType'] = this.filterEntityType;
    if (this.filterFromDate)   params['fromDate']   = this.filterFromDate;
    if (this.filterToDate)     params['toDate']     = this.filterToDate;

    this.auditApi.getLogs(params).subscribe({
      next: r => {
        this.isLoading = false;
        if (r.success) {
          this.logs      = r.data?.logs ?? r.data ?? [];
          this.totalLogs = r.data?.total ?? this.logs.length;
          this.totalPages = Math.ceil(this.totalLogs / this.pageSize); // ✅ NEW
          this.cdr.detectChanges();
        }
      },
      error: () => { this.isLoading = false; this.cdr.detectChanges(); }
    });
  }

  applyFilters(): void {
    this.currentPage = 1;
    this.load();
  }

  clearFilters(): void {
    this.filterAction     = '';
    this.filterEntityType = '';
    this.filterFromDate   = '';
    this.filterToDate     = '';
    this.currentPage = 1;
    this.load();
  }

  // ✅ Pagination methods
  goToManualPage(): void {
    const page = parseInt(this.manualPageInput, 10);
    this.goToPage(page);
    this.manualPageInput = '';
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      this.load();
    }
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
      this.load();
    }
  }

  prevPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.load();
    }
  }

  changePageSize(size: number): void {
    this.pageSize = size;
    this.totalPages = Math.ceil(this.totalLogs / this.pageSize);
    this.currentPage = 1;
    this.load();
  }

  getActionBadge(action: string): string {
    const a = (action || '').toUpperCase();
    if (a === 'CREATE')  return 'lt-badge-green';
    if (a === 'UPDATE')  return 'lt-badge-blue';
    if (a === 'DELETE')  return 'lt-badge-red';
    if (a === 'LOGIN')   return 'lt-badge-amber';
    return 'lt-badge-gray';
  }
}