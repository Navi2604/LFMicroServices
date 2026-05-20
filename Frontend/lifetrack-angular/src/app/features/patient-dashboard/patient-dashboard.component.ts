// patient-dashboard.component.ts
import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { EnrollmentApiService } from '../../core/services/api.service';
import { SidebarComponent } from '../../shared/sidebar.component';

@Component({
  selector: 'app-patient-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, SidebarComponent],
  templateUrl: './patient-dashboard.component.html'
})
export class PatientDashboardComponent implements OnInit {
  userName         = '';
  userId           = 0;
  unreadCount      = 0;
  isLoading        = false;
  successMsg       = '';
  errorMsg         = '';

  pendingInvitations:  any[] = [];
  pendingWithdrawals:  any[] = [];
  activeEnrollments:   any[] = [];

  constructor(
    private authService:   AuthService,
    private enrollmentApi: EnrollmentApiService,
    private cdr:           ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.userName = this.authService.getUserName();
    this.userId   = this.authService.getUserId();
    this.load();
  }

  load(): void {
    this.isLoading = true;
    // Get all enrollments for this patient
    this.enrollmentApi.getAll({ patientId: this.userId }).subscribe({
      next: r => {
        this.isLoading = false;
        if (r.success) {
          this.pendingInvitations  = r.data.filter((e: any) => e.status === 'Pending');
          this.pendingWithdrawals   = r.data.filter((e: any) => e.status === 'PendingWithdrawal');
          this.activeEnrollments    = r.data.filter((e: any) => e.status === 'Active');
          this.cdr.detectChanges();
        }
      },
      error: () => { this.isLoading = false; this.cdr.detectChanges(); }
    });
  }

  accept(enrollmentId: number): void {
    this.enrollmentApi.respond(enrollmentId, true).subscribe(r => {
      if (r.success) {
        this.showMsg('success', 'You have accepted the enrollment. Welcome to the trial!');
        this.load();
      } else { this.showMsg('error', r.message); }
    });
  }

  decline(enrollmentId: number): void {
    if (!confirm('Are you sure you want to decline this enrollment?')) return;
    this.enrollmentApi.respond(enrollmentId, false).subscribe(r => {
      if (r.success) {
        this.showMsg('success', 'Enrollment declined.');
        this.load();
      } else { this.showMsg('error', r.message); }
    });
  }

  confirmWithdrawal(enrollmentId: number): void {
    if (!confirm('Confirm withdrawal from this trial?')) return;
    this.enrollmentApi.respond(enrollmentId, false).subscribe(r => {
      if (r.success) {
        this.showMsg('success', 'You have been withdrawn from the trial.');
        this.load();
      } else { this.showMsg('error', r.message); }
    });
  }

  rejectWithdrawal(enrollmentId: number): void {
    this.enrollmentApi.updateStatus(enrollmentId, { status: 'Active', withdrawalReason: '' }).subscribe(r => {
      if (r.success) {
        this.showMsg('success', 'Withdrawal request rejected. You remain active in the trial.');
        this.load();
      } else { this.showMsg('error', r.message); }
    });
  }

  private showMsg(type: 'success' | 'error', msg: string): void {
    if (type === 'success') { this.successMsg = msg; this.errorMsg = ''; }
    else { this.errorMsg = msg; this.successMsg = ''; }
    setTimeout(() => { this.successMsg = ''; this.errorMsg = ''; this.cdr.detectChanges(); }, 5000);
  }
}