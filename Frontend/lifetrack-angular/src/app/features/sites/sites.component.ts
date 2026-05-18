// sites.component.ts
import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { SiteApiService, SiteDto } from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import { SidebarComponent } from '../../shared/sidebar.component';

@Component({
  selector: 'app-sites',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, SidebarComponent],
  templateUrl: './sites.component.html'
})
export class SitesComponent implements OnInit {
  sites:      SiteDto[] = [];
  filtered:   SiteDto[] = [];
  isLoading   = false;
  successMsg  = '';
  errorMsg    = '';
  showCreate  = false;
  canEdit     = false;
  unreadCount = 0;

  form = {
    name: '', location: '',
    investigatorID: 0, status: 'Active'
  };

  constructor(
    private siteApi:     SiteApiService,
    private authService: AuthService,
    private cdr:         ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.canEdit = ['Admin', 'ClinicalTrialManager'].includes(this.authService.getRole());
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.siteApi.getAll().subscribe({
      next: r => {
        this.isLoading = false;
        if (r.success) {
          this.sites    = [...r.data];
          this.filtered = [...r.data];
          this.cdr.detectChanges();
        }
      },
      error: () => {
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  create(): void {
    this.siteApi.create(this.form).subscribe({
      next: r => {
        if (r.success) {
          this.successMsg = `Site "${this.form.name}" created.`;
          this.showCreate = false;
          this.form = {
            name: '', location: '',
            investigatorID: 0, status: 'Active'
          };
          this.load();
        } else {
          this.errorMsg = r.message;
        }
      }
    });
  }

  delete(id: number): void {
    if (!confirm('Delete this site?')) return;
    this.siteApi.delete(id).subscribe(r => {
      if (r.success) {
        this.successMsg = 'Site deleted.';
        this.load();
      }
    });
  }

  getStatusBadge(status: string): string {
    const map: Record<string, string> = {
      'Active':   'lt-badge lt-badge-green',
      'Ongoing':  'lt-badge lt-badge-green',
      'Upcoming': 'lt-badge lt-badge-amber',
      'Inactive': 'lt-badge lt-badge-gray',
      'Closed':   'lt-badge lt-badge-red'
    };
    return map[status] ?? 'lt-badge lt-badge-gray';
  }
}