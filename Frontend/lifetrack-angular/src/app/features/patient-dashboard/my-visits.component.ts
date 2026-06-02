import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/services/auth.service';
import { EnrollmentApiService, VisitApiService } from '../../core/services/api.service';
import { SidebarComponent } from '../../shared/sidebar.component';

@Component({
  selector: 'app-my-visits',
  standalone: true,
  imports: [CommonModule, FormsModule, SidebarComponent],
  templateUrl: './my-visits.component.html'
})
export class MyVisitsComponent implements OnInit {
  userName = ''; userId = 0; unreadCount = 0;
  isLoading = false;

  allVisits:      any[] = [];
  filtered:       any[] = [];
  upcomingVisits: any[] = [];
  pastVisits:     any[] = [];

  activeTab: 'upcoming' | 'past' | 'all' = 'upcoming';
  searchQuery = '';

  totalVisits    = 0;
  attendedVisits = 0;
  missedVisits   = 0;
  upcomingCount  = 0;

  constructor(
    private authService:   AuthService,
    private enrollmentApi: EnrollmentApiService,
    private visitApi:      VisitApiService,
    private cdr:           ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.userName = this.authService.getUserName();
    this.userId   = this.authService.getUserId();
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.enrollmentApi.getAll({ patientId: this.userId }).subscribe({
      next: r => {
        if (!r.success) { this.isLoading = false; return; }
        const enrIds = new Set((r.data ?? []).map((e:any) => e.enrollmentID));
        if (enrIds.size === 0) { this.isLoading = false; this.cdr.detectChanges(); return; }

        this.visitApi.getAll().subscribe({
          next: vr => {
            this.isLoading = false;
            if (!vr.success) return;
            const today = new Date(); today.setHours(0,0,0,0);

            this.allVisits = (vr.data ?? [])
              .filter((v:any) => enrIds.has(v.enrollmentID))
              .sort((a:any,b:any) => new Date(a.visitDate).getTime() - new Date(b.visitDate).getTime());

            this.totalVisits    = this.allVisits.length;
            this.attendedVisits = this.allVisits.filter((v:any) => v.status === 'Attended').length;
            this.missedVisits   = this.allVisits.filter((v:any) => v.status === 'Missed').length;

            this.upcomingVisits = this.allVisits.filter((v:any) => {
              const d = new Date(v.visitDate); d.setHours(0,0,0,0);
              return (v.status === 'Scheduled' || v.status === 'Rescheduled') && d >= today;
            });
            this.pastVisits = this.allVisits.filter((v:any) => {
              const d = new Date(v.visitDate); d.setHours(0,0,0,0);
              return d < today || v.status === 'Attended' || v.status === 'Missed';
            });
            this.upcomingCount = this.upcomingVisits.length;
            this.applyFilter();
            this.cdr.detectChanges();
          },
          error: () => { this.isLoading = false; this.cdr.detectChanges(); }
        });
      },
      error: () => { this.isLoading = false; this.cdr.detectChanges(); }
    });
  }

  switchTab(tab: 'upcoming'|'past'|'all'): void {
    this.activeTab = tab; this.applyFilter();
  }

  applyFilter(): void {
    let source = this.activeTab === 'upcoming' ? this.upcomingVisits
               : this.activeTab === 'past'     ? this.pastVisits
               : this.allVisits;
    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase();
      source = source.filter((v:any) =>
        (v.notes || '').toLowerCase().includes(q)
      );
    }
    this.filtered = source;
  }

  visitStatusClass(status: string): string {
    const m: Record<string,string> = {
      'Scheduled':'mv-badge-blue','Attended':'mv-badge-green',
      'Missed':'mv-badge-red','Rescheduled':'mv-badge-amber','Cancelled':'mv-badge-gray'
    };
    return m[status] ?? 'mv-badge-gray';
  }

  isToday(dateStr: string): boolean {
    const d = new Date(dateStr); d.setHours(0,0,0,0);
    const t = new Date(); t.setHours(0,0,0,0);
    return d.getTime() === t.getTime();
  }

  isTomorrow(dateStr: string): boolean {
    const d = new Date(dateStr); d.setHours(0,0,0,0);
    const t = new Date(); t.setHours(0,0,0,0);
    return d.getTime() - t.getTime() === 86400000;
  }

  urgencyClass(v: any): string {
    if (this.isToday(v.visitDate)) return 'mv-urgency-today';
    if (this.isTomorrow(v.visitDate)) return 'mv-urgency-tomorrow';
    return '';
  }
}