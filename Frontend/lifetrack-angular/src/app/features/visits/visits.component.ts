// visits.component.ts
import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  VisitApiService, VisitDto,
  PatientApiService, ProtocolApiService
} from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import { SidebarComponent } from '../../shared/sidebar.component';

@Component({
  selector: 'app-visits',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, SidebarComponent],
  templateUrl: './visits.component.html'
})
export class VisitsComponent implements OnInit {
  visits:    VisitDto[] = [];
  filtered:  VisitDto[] = [];
  patients:  any[]      = [];
  protocols: any[]      = [];
  isLoading  = false;
  successMsg = '';
  errorMsg   = '';
  showCreate = false;
  canEdit    = false;
  unreadCount = 0;

  form = {
    patientID: 0, protocolID: 0,
    visitDate: '', status: 'Scheduled', notes: ''
  };

  constructor(
    private visitApi:    VisitApiService,
    private patientApi:  PatientApiService,
    private protocolApi: ProtocolApiService,
    private authService: AuthService,
    private cdr:         ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    const role = this.authService.getRole();
    this.canEdit = ['Admin', 'Investigator', 'ClinicalTrialManager'].includes(role);
    this.load();
    this.patientApi.getAll().subscribe(r => {
      if (r.success) {
        this.patients = [...r.data];
        this.cdr.detectChanges();
      }
    });
    this.protocolApi.getAll().subscribe(r => {
      if (r.success) {
        this.protocols = [...r.data];
        this.cdr.detectChanges();
      }
    });
  }

  load(): void {
    this.isLoading = true;
    this.visitApi.getAll().subscribe({
      next: r => {
        this.isLoading = false;
        if (r.success) {
          this.visits   = [...r.data];
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
    this.visitApi.create(this.form).subscribe({
      next: r => {
        if (r.success) {
          this.successMsg = 'Visit scheduled.';
          this.showCreate = false;
          this.form = {
            patientID: 0, protocolID: 0,
            visitDate: '', status: 'Scheduled', notes: ''
          };
          this.load();
        } else {
          this.errorMsg = r.message;
        }
      }
    });
  }

  delete(id: number): void {
    if (!confirm('Delete this visit?')) return;
    this.visitApi.delete(id).subscribe(r => {
      if (r.success) {
        this.successMsg = 'Deleted.';
        this.load();
      }
    });
  }

  getStatusBadge(status: string): string {
    const map: Record<string, string> = {
      'Scheduled': 'lt-badge lt-badge-blue',
      'Completed': 'lt-badge lt-badge-green',
      'Missed':    'lt-badge lt-badge-red',
      'Cancelled': 'lt-badge lt-badge-gray'
    };
    return map[status] ?? 'lt-badge lt-badge-gray';
  }
}