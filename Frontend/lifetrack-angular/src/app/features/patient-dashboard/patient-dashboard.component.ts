import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import {
  EnrollmentApiService, VisitApiService
} from '../../core/services/api.service';
import { SidebarComponent } from '../../shared/sidebar.component';

@Component({
  selector: 'app-patient-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, SidebarComponent],
  templateUrl: './patient-dashboard.component.html'
})
export class PatientDashboardComponent implements OnInit {
  userName = ''; userId = 0; unreadCount = 0;
  isLoading = false; successMsg = ''; errorMsg = '';

  pendingInvitations: any[] = [];
  pendingWithdrawals: any[] = [];
  activeEnrollments:  any[] = [];
  completedEnrollments: any[] = [];

  upcomingVisits: any[] = [];
  missedVisits:   any[] = [];
  totalVisits     = 0;
  attendedVisits  = 0;
  nextVisitDays: number | null = null;
  nextVisitDate = '';

  constructor(
    private authService:    AuthService,
    private notifSvc:       NotificationService,
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
        this.isLoading = false;
        if (r.success) {
          const all = r.data ?? [];
          this.pendingInvitations   = all.filter((e:any) => e.status === 'Pending');
          this.pendingWithdrawals   = all.filter((e:any) => e.status === 'PendingWithdrawal');
          this.activeEnrollments    = all.filter((e:any) => e.status === 'Active');
          this.completedEnrollments = all.filter((e:any) => e.status === 'Completed');

          // Load visits after we have enrollment IDs
          const enrIds = all.filter((e:any) => e.status === 'Active' || e.status === 'Completed')
                            .map((e:any) => e.enrollmentID);
          this.loadVisits(enrIds);
          this.cdr.detectChanges();
        }
      },
      error: () => { this.isLoading = false; this.cdr.detectChanges(); }
    });

  }

  loadVisits(enrollmentIds: number[]): void {
    if (enrollmentIds.length === 0) return;
    this.visitApi.getAll().subscribe(r => {
      if (!r.success) return;
      const myEnrSet = new Set(enrollmentIds);
      const visits   = (r.data ?? []).filter((v:any) => myEnrSet.has(v.enrollmentID));
      const today    = new Date(); today.setHours(0,0,0,0);

      this.totalVisits    = visits.length;
      this.attendedVisits = visits.filter((v:any) => v.status === 'Attended').length;
      this.missedVisits   = visits.filter((v:any) => v.status === 'Missed');
      this.upcomingVisits = visits
        .filter((v:any) => {
          const d = new Date(v.visitDate); d.setHours(0,0,0,0);
          return (v.status === 'Scheduled' || v.status === 'Rescheduled') && d >= today;
        })
        .sort((a:any,b:any) => new Date(a.visitDate).getTime() - new Date(b.visitDate).getTime())
        .slice(0, 5);

      if (this.upcomingVisits.length > 0) {
        const next = new Date(this.upcomingVisits[0].visitDate); next.setHours(0,0,0,0);
        this.nextVisitDays = Math.ceil((next.getTime() - today.getTime()) / 86400000);
        this.nextVisitDate = next.toLocaleDateString('en-GB', { day:'numeric', month:'short', year:'numeric' });
      }
      this.cdr.detectChanges();
    });
  }

  accept(enrollmentId: number): void {
    this.enrollmentApi.respond(enrollmentId, true).subscribe(r => {
      if (r.success) {
        this.showMsg('success', 'You have accepted the enrollment. Welcome to the trial!');
        // Patient self-notification skipped (PatientID != UserID in staff Users table)
      } else {
        this.showMsg('error', r.message);
      }
    });
  }

  decline(enrollmentId: number): void {
    if (!confirm('Are you sure you want to decline this enrollment?')) return;
    this.enrollmentApi.respond(enrollmentId, false).subscribe(r => {
      if (r.success) { this.showMsg('success', 'Enrollment declined.'); this.load(); }
      else this.showMsg('error', r.message);
    });
  }

  confirmWithdrawal(enrollmentId: number): void {
    if (!confirm('Confirm withdrawal from this trial?')) return;
    this.enrollmentApi.respond(enrollmentId, false).subscribe(r => {
      if (r.success) { this.showMsg('success', 'You have been withdrawn from the trial.'); this.load(); }
      else this.showMsg('error', r.message);
    });
  }

  rejectWithdrawal(enrollmentId: number): void {
    this.enrollmentApi.updateStatus(enrollmentId, { status: 'Active', withdrawalReason: '' }).subscribe(r => {
      if (r.success) { this.showMsg('success', 'Withdrawal request rejected. You remain active in the trial.'); this.load(); }
      else this.showMsg('error', r.message);
    });
  }

  visitStatusClass(status: string): string {
    const m: Record<string,string> = {
      'Scheduled':'pp-badge-blue','Attended':'pp-badge-green',
      'Missed':'pp-badge-red','Rescheduled':'pp-badge-amber'
    };
    return m[status] ?? 'pp-badge-gray';
  }

  private showMsg(type: 'success'|'error', msg: string): void {
    if (type === 'success') { this.successMsg = msg; this.errorMsg = ''; }
    else { this.errorMsg = msg; this.successMsg = ''; }
    setTimeout(() => { this.successMsg = ''; this.errorMsg = ''; this.cdr.detectChanges(); }, 5000);
  }
}