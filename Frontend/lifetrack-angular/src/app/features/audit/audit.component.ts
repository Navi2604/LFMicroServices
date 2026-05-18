// audit.component.ts
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

  // ── Filters ────────────────────────────────────────────────
  filterAction     = '';
  filterEntityType = '';
  filterFromDate   = '';
  filterToDate     = '';

  // ── Pagination ──────────────────────────────────────────────
  page     = 1;
  pageSize = 50;

  constructor(
    private auditApi: AuditApiService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    this.isLoading = true;

    const params: any = { page: this.page, pageSize: this.pageSize };
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
          this.cdr.detectChanges();
        }
      },
      error: () => { this.isLoading = false; this.cdr.detectChanges(); }
    });
  }

  applyFilters(): void {
    this.page = 1;
    this.load();
  }

  clearFilters(): void {
    this.filterAction     = '';
    this.filterEntityType = '';
    this.filterFromDate   = '';
    this.filterToDate     = '';
    this.page = 1;
    this.load();
  }

  nextPage(): void { this.page++; this.load(); }
  prevPage(): void { if (this.page > 1) { this.page--; this.load(); } }

  getActionBadge(action: string): string {
    const a = (action || '').toUpperCase();
    if (a === 'CREATE')  return 'lt-badge-green';
    if (a === 'UPDATE')  return 'lt-badge-blue';
    if (a === 'DELETE')  return 'lt-badge-red';
    if (a === 'LOGIN')   return 'lt-badge-amber';
    return 'lt-badge-gray';
  }
}