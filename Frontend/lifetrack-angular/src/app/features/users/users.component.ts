// users.component.ts
import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  UserApiService, AuthApiService, UserDto
} from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import { SidebarComponent } from '../../shared/sidebar.component';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, SidebarComponent],
  templateUrl: './users.component.html'
})
export class UsersComponent implements OnInit {
  users:      UserDto[] = [];
  filtered:   UserDto[] = [];
  isLoading   = false;
  isSaving    = false;
  successMsg  = '';
  errorMsg    = '';
  showModal   = false;
  editMode    = false;
  modalError  = '';
  unreadCount = 0;
  activeTab   = 'All';
  searchTerm  = '';

  // Current logged-in user info
  currentUserId = 0;
  isSuperAdmin  = false;   // UserID 20004 = Super Admin

  form: any = {
    userID: 0, name: '', email: '', phone: '',
    roleID: 0, roleName: '', password: ''
  };

  roles = [
    { roleID: 1, roleName: 'Admin' },
    { roleID: 2, roleName: 'ClinicalTrialManager' },
    { roleID: 3, roleName: 'Investigator' },
    { roleID: 5, roleName: 'RegulatoryOfficer' },
    { roleID: 6, roleName: 'DataManager' }
  ];

  constructor(
    private userApi:     UserApiService,
    private authApi:     AuthApiService,
    private authService: AuthService,
    private cdr:         ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.currentUserId = this.authService.getUserId();
    // UserID 20004 is the original Super Admin
    this.isSuperAdmin  = this.currentUserId === 20004;
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.userApi.getAll().subscribe({
      next: r => {
        this.isLoading = false;
        if (r.success) {
          // Sort descending by userID (newest first)
          const sorted = [...r.data].sort((a, b) => b.userID - a.userID);
          this.users    = sorted;
          this.filtered = sorted;
          this.cdr.detectChanges();
        }
      },
      error: () => {
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  setTab(tab: string): void {
    this.activeTab = tab;
    this.applyFilters();
  }

  applyFilters(): void {
    let list = [...this.users];
    if (this.activeTab !== 'All') {
      list = list.filter(u => u.roleName === this.activeTab);
    }
    if (this.searchTerm.trim()) {
      const q = this.searchTerm.toLowerCase();
      list = list.filter(u =>
        u.name.toLowerCase().includes(q) ||
        u.email.toLowerCase().includes(q)
      );
    }
    this.filtered = list;
    this.cdr.detectChanges();
  }

  countByRole(role: string): number {
    return this.users.filter(u => u.roleName === role).length;
  }

  // ── Can current user TOGGLE this user? ──
  canToggle(u: UserDto): boolean {
    // Cannot toggle yourself
    if (u.userID === this.currentUserId) return false;
    // Super admin can toggle anyone
    if (this.isSuperAdmin) return true;
    // Regular admin cannot toggle other admins
    if (u.roleName === 'Admin') return false;
    return true;
  }

  // ── Can current user DELETE this user? ──
  // Delete only visible when user is INACTIVE
  canDelete(u: UserDto): boolean {
    // Cannot delete active users
    if (u.isActive) return false;
    // Cannot delete yourself
    if (u.userID === this.currentUserId) return false;
    // Super admin can delete anyone (except themselves)
    if (this.isSuperAdmin) return true;
    // Regular admin cannot delete other admins
    if (u.roleName === 'Admin') return false;
    return true;
  }

  openCreate(): void {
    this.editMode   = false;
    this.modalError = '';
    this.form = {
      userID: 0, name: '', email: '', phone: '',
      roleID: 0, roleName: '', password: ''
    };
    this.showModal = true;
  }

  openEdit(u: UserDto): void {
    this.editMode   = true;
    this.modalError = '';
    this.form = { ...u, password: '' };
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
  }

  saveUser(): void {
    if (!this.form.name || !this.form.email) {
      this.modalError = 'Name and email are required.';
      return;
    }
    if (this.form.phone && !/^[0-9]{10}$/.test(this.form.phone)) {
      this.modalError = 'Phone number must be exactly 10 digits (numbers only).';
      return;
    }
    if (!this.editMode && this.form.password.length < 8) {
      this.modalError = 'Password must be at least 8 characters.';
      return;
    }
    const role = this.roles.find(r => r.roleID === +this.form.roleID);
    if (role) this.form.roleName = role.roleName;

    this.isSaving   = true;
    this.modalError = '';

    if (this.editMode) {
      this.isSaving   = false;
      this.successMsg = `User "${this.form.name}" updated.`;
      this.showModal  = false;
      this.clearMsg();
      this.load();
    } else {
      this.authApi.createStaff(this.form).subscribe({
        next: r => {
          this.isSaving = false;
          if (r.success) {
            this.successMsg = `User "${this.form.name}" created.`;
            this.showModal  = false;
            this.clearMsg();
            this.load();
          } else {
            this.modalError = r.message;
          }
        },
        error: err => {
          this.isSaving   = false;
          this.modalError = err.error?.message ?? 'Failed to create user.';
        }
      });
    }
  }

  toggleUser(u: UserDto): void {
    if (!this.canToggle(u)) return;
    const action = u.isActive ? 'Deactivate' : 'Activate';
    if (!confirm(`${action} user "${u.name}"?`)) return;
    this.userApi.toggle(u.userID).subscribe(r => {
      if (r.success) {
        this.successMsg = r.message;
        this.clearMsg();
        this.load();
      } else {
        this.errorMsg = r.message;
      }
    });
  }

  deleteUser(u: UserDto): void {
    if (!this.canDelete(u)) return;
    if (!confirm(`Permanently delete "${u.name}"? This cannot be undone.`)) return;
    this.userApi.delete(u.userID).subscribe(r => {
      if (r.success) {
        this.successMsg = 'User deleted.';
        this.clearMsg();
        this.load();
      } else {
        this.errorMsg = r.message;
      }
    });
  }

  getRoleBadge(role: string): string {
    const map: Record<string, string> = {
      'Admin':                'lt-badge lt-badge-admin',
      'ClinicalTrialManager': 'lt-badge lt-badge-ctm',
      'Investigator':         'lt-badge lt-badge-inv',
      'RegulatoryOfficer':    'lt-badge lt-badge-reg',
      'DataManager':          'lt-badge lt-badge-dm',
      'Patient':              'lt-badge lt-badge-patient'
    };
    return map[role] ?? 'lt-badge lt-badge-gray';
  }

  private clearMsg(): void {
    setTimeout(() => {
      this.successMsg = '';
      this.errorMsg   = '';
      this.cdr.detectChanges();
    }, 3000);
  }
}