// patients.component.ts
import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { PatientApiService, PatientDto } from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import { SidebarComponent } from '../../shared/sidebar.component';

@Component({
  selector: 'app-patients',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, SidebarComponent],
  templateUrl: './patients.component.html'
})
export class PatientsComponent implements OnInit {
  patients:   PatientDto[] = [];
  filtered:   PatientDto[] = [];
  isLoading   = false;
  successMsg  = '';
  errorMsg    = '';
  showEnroll  = false;
  canEnroll   = false;
  unreadCount = 0;
  searchTerm  = '';

  enrollForm = {
    name: '', email: '', dob: '', contactInfo: ''
  };

  constructor(
    private patientApi:  PatientApiService,
    private authService: AuthService,
    private cdr:         ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    const role = this.authService.getRole();
    this.canEnroll = ['Admin', 'Investigator', 'ClinicalTrialManager'].includes(role);
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.patientApi.getAll().subscribe({
      next: r => {
        this.isLoading = false;
        if (r.success) {
          this.patients = [...r.data];
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

  applyFilter(): void {
    const q = this.searchTerm.toLowerCase();
    this.filtered = q
      ? this.patients.filter(p =>
          p.name.toLowerCase().includes(q) ||
          p.email.toLowerCase().includes(q)
        )
      : [...this.patients];
    this.cdr.detectChanges();
  }

  enroll(): void {
    this.patientApi.enroll(this.enrollForm).subscribe({
      next: r => {
        if (r.success) {
          this.successMsg = 'Patient enrolled successfully.';
          this.showEnroll = false;
          this.enrollForm = { name: '', email: '', dob: '', contactInfo: '' };
          this.load();
          setTimeout(() => this.successMsg = '', 3000);
        } else {
          this.errorMsg = r.message;
        }
      },
      error: err => {
        this.errorMsg = err.error?.message ?? 'Failed.';
      }
    });
  }

  updateStatus(id: number, status: string): void {
    if (!status) return;
    this.patientApi.updateStatus(id, status).subscribe(r => {
      if (r.success) {
        this.successMsg = 'Status updated.';
        this.load();
      }
    });
  }

  delete(id: number): void {
    if (!confirm('Delete this patient?')) return;
    this.patientApi.delete(id).subscribe(r => {
      if (r.success) {
        this.successMsg = 'Patient deleted.';
        this.load();
      } else {
        this.errorMsg = r.message;
      }
    });
  }

  getStatusBadge(status: string): string {
    const map: Record<string, string> = {
      'Active':    'lt-badge lt-badge-green',
      'Screening': 'lt-badge lt-badge-blue',
      'Completed': 'lt-badge lt-badge-gray',
      'Withdrawn': 'lt-badge lt-badge-red'
    };
    return map[status] ?? 'lt-badge lt-badge-gray';
  }
}