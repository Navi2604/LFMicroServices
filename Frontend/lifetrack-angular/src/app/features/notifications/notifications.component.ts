// notifications.component.ts
import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { SidebarComponent } from '../../shared/sidebar.component';
import { NotificationApiService } from '../../core/services/api.service';

interface NotificationDto {
  notificationID: number;
  userID:         number;
  message:        string;
  category:       string;
  status:         string;
  createdDate:    string;
}

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [CommonModule, RouterLink, SidebarComponent],
  templateUrl: './notifications.component.html'
})
export class NotificationsComponent implements OnInit {
  notifications: NotificationDto[] = [];
  filtered:      NotificationDto[] = [];
  isLoading    = false;
  unreadCount  = 0;
  activeTab    = 'All';

  constructor(
    private notifApi: NotificationApiService,
    private authService: AuthService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    this.isLoading = true;
    const userId = this.authService.getUserId();

    this.notifApi.getAll({ userId }).subscribe({
      next: r => {
        this.isLoading = false;
        if (r.success) {
          this.notifications = r.data ?? [];
          this.unreadCount   = this.notifications.filter(n => n.status === 'Unread').length;
          this.applyFilter();
          this.cdr.detectChanges();
        }
      },
      error: () => { this.isLoading = false; this.cdr.detectChanges(); }
    });
  }

  setTab(tab: string): void {
    this.activeTab = tab;
    this.applyFilter();
  }

  applyFilter(): void {
    this.filtered = this.activeTab === 'All'
      ? [...this.notifications]
      : this.notifications.filter(n => n.category === this.activeTab);
  }

  markRead(n: NotificationDto): void {
    if (n.status === 'Read') return;
    this.notifApi.markAsRead(n.notificationID).subscribe({
      next: r => {
        if (r.success) {
          n.status = 'Read';
          this.unreadCount = this.notifications.filter(x => x.status === 'Unread').length;
          this.cdr.detectChanges();
        }
      }
    });
  }

  markAllRead(): void {
    const userId = this.authService.getUserId();
    this.notifApi.markAllAsRead(userId).subscribe({
      next: r => {
        if (r.success) {
          this.notifications = this.notifications.map(n => ({ ...n, status: 'Read' }));
          this.unreadCount   = 0;
          this.applyFilter();
          this.cdr.detectChanges();
        }
      }
    });
  }

  getCategoryBadge(cat: string): string {
    const map: Record<string, string> = {
      'Enrollment': 'lt-badge-green',
      'Visit':      'lt-badge-blue',
      'Protocol':   'lt-badge-amber',
      'Alert':      'lt-badge-red',
      'System':     'lt-badge-gray'
    };
    return map[cat] ?? 'lt-badge-gray';
  }
}