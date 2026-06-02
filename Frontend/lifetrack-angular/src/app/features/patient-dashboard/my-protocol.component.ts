import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../core/services/auth.service';
import {
  EnrollmentApiService, VisitApiService
} from '../../core/services/api.service';
import { SidebarComponent } from '../../shared/sidebar.component';

@Component({
  selector: 'app-my-protocol',
  standalone: true,
  imports: [CommonModule, SidebarComponent],
  templateUrl: './my-protocol.component.html'
})
export class MyProtocolComponent implements OnInit {
  userName = ''; userId = 0; unreadCount = 0;
  isLoading = false;
  protocols: any[] = [];   // enriched enrollment + protocol + visit stats

  constructor(
    private authService:    AuthService,
    private enrollmentApi:  EnrollmentApiService,
    private visitApi:       VisitApiService,
    private cdr:            ChangeDetectorRef
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
        const enrollments = r.data ?? [];
        if (enrollments.length === 0) { this.isLoading = false; this.cdr.detectChanges(); return; }

        // Load visits - use enrollment data for site/protocol info (Patient role can't access siteProtocol)
        this.visitApi.getAll().subscribe(vr => {
          this.isLoading = false;
          const allVisits = vr.success ? (vr.data ?? []) : [];
          const today = new Date(); today.setHours(0,0,0,0);

          this.protocols = enrollments.map((e: any) => {
            // Backend now joins Investigator + Site into EnrollmentDto
            const investigatorName    = e.investigatorName    || '—';
            const investigatorEmail   = e.investigatorEmail   || '—';
            const investigatorContact = e.investigatorContact || '—';
            const siteName            = e.siteName            || '—';
            const siteEmail           = e.siteEmail           || '—';
            const siteContact         = e.siteContact         || '—';
            const myVisits = allVisits.filter((v:any) => v.enrollmentID === e.enrollmentID);
            const attended  = myVisits.filter((v:any) => v.status === 'Attended').length;
            const missed    = myVisits.filter((v:any) => v.status === 'Missed').length;
            const upcoming  = myVisits.filter((v:any) => {
              const d = new Date(v.visitDate); d.setHours(0,0,0,0);
              return (v.status === 'Scheduled' || v.status === 'Rescheduled') && d >= today;
            });
            const nextVisit = upcoming.sort((a:any,b:any) => new Date(a.visitDate).getTime() - new Date(b.visitDate).getTime())[0];
            return {
              ...e,
              investigatorName,
              investigatorEmail,
              investigatorContact,
              siteName,
              siteEmail,
              siteContact,
              visitTotal:    myVisits.length,
              visitAttended: attended,
              visitMissed:   missed,
              visitUpcoming: upcoming.length,
              nextVisitDate: nextVisit ? nextVisit.visitDate : null,
              nextVisitName: nextVisit ? nextVisit.notes : null,
              attendancePct: myVisits.length > 0 ? Math.round((attended / myVisits.length) * 100) : 0
            };
          });
          this.cdr.detectChanges();
        });
      },
      error: () => { this.isLoading = false; this.cdr.detectChanges(); }
    });
  }

  enrollStatusClass(status: string): string {
    const m: Record<string,string> = {
      'Active':'mp-status-active','Completed':'mp-status-completed',
      'Withdrawn':'mp-status-withdrawn','Pending':'mp-status-pending',
      'PendingWithdrawal':'mp-status-amber'
    };
    return m[status] ?? 'mp-status-gray';
  }

  barColor(pct: number): string {
    return pct >= 80 ? '#16a34a' : pct >= 50 ? '#d97706' : '#dc2626';
  }

  daysUntil(dateStr: string): string {
    if (!dateStr) return '';
    const d = new Date(dateStr); d.setHours(0,0,0,0);
    const t = new Date(); t.setHours(0,0,0,0);
    const diff = Math.ceil((d.getTime() - t.getTime()) / 86400000);
    if (diff === 0) return 'Today';
    if (diff === 1) return 'Tomorrow';
    if (diff < 0)  return `${Math.abs(diff)}d ago`;
    return `In ${diff} days`;
  }
}