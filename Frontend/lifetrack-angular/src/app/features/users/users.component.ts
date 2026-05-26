// users.component.ts
import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { UserApiService, AuthApiService, UserDto } from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import { SidebarComponent } from '../../shared/sidebar.component';

// The first admin created (seeded) — only this user can edit other admins.
// Adjust this ID to match your actual super-admin UserID in the database.
const SUPER_ADMIN_ID = 20004;

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, SidebarComponent],
  templateUrl: './users.component.html'
})
export class UsersComponent implements OnInit {
  users:     UserDto[] = [];
  filtered:  UserDto[] = [];
  paginated: UserDto[] = [];

  isLoading  = false;
  isSaving   = false;
  successMsg = '';
  errorMsg   = '';
  showModal  = false;
  editMode   = false;
  modalError = '';
  unreadCount = 0;
  activeTab   = 'All';
  searchTerm  = '';

  currentPage = 1;
  pageSize    = 10;
  totalPages  = 0;
  itemsPerPageOptions = [10, 20, 50];

  currentUserId   = 0;
  currentUserRole = '';
  isSuperAdmin    = false;

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
    this.currentUserId   = this.authService.getUserId();
    this.currentUserRole = this.authService.getRole();
    this.isSuperAdmin    = this.currentUserId === SUPER_ADMIN_ID;
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.userApi.getAll().subscribe({
      next: r => {
        this.isLoading = false;
        if (r.success) {
          this.users = [...r.data].sort((a, b) => b.userID - a.userID);
          this.applyFilters();
        }
        this.cdr.detectChanges();
      },
      error: () => { this.isLoading = false; this.cdr.detectChanges(); }
    });
  }

  setTab(tab: string): void {
    this.activeTab   = tab;
    this.currentPage = 1;
    this.applyFilters();
  }

  applyFilters(): void {
    let list = [...this.users];
    if (this.activeTab !== 'All')
      list = list.filter(u => u.roleName === this.activeTab);
    if (this.searchTerm.trim()) {
      const q = this.searchTerm.toLowerCase();
      list = list.filter(u =>
        u.name.toLowerCase().includes(q) ||
        u.email.toLowerCase().includes(q));
    }
    this.filtered    = list;
    this.totalPages  = Math.ceil(this.filtered.length / this.pageSize) || 1;
    this.currentPage = Math.min(this.currentPage, this.totalPages);
    this.applyPagination();
  }

  applyPagination(): void {
    const start = (this.currentPage - 1) * this.pageSize;
    this.paginated = this.filtered.slice(start, start + this.pageSize);
    this.cdr.detectChanges();
  }

  goToPage(p: number): void {
    if (p >= 1 && p <= this.totalPages) {
      this.currentPage = p;
      this.applyPagination();
    }
  }
  prevPage(): void { this.goToPage(this.currentPage - 1); }
  nextPage(): void { this.goToPage(this.currentPage + 1); }
  changePageSize(size: number): void {
    this.pageSize   = +size;
    this.currentPage = 1;
    this.totalPages  = Math.ceil(this.filtered.length / this.pageSize) || 1;
    this.applyPagination();
  }
  getPageNumbers(): number[] {
    const out: number[] = [];
    const s = Math.max(1, this.currentPage - 4);
    const e = Math.min(this.totalPages, s + 9);
    for (let i = s; i <= e; i++) out.push(i);
    return out;
  }

  countByRole(role: string): number {
    return this.users.filter(u => u.roleName === role).length;
  }

  // ── EDIT permission ───────────────────────────────────────
  // Rules:
  // • Cannot edit yourself
  // • Cannot edit another Admin UNLESS you are the super admin
  // • Investigators can be edited (name/phone/email) but role-change
  //   forces Inactive on backend automatically
  canEdit(u: UserDto): boolean {
    if (u.userID === this.currentUserId) return false;
    if (u.roleName === 'Admin' && !this.isSuperAdmin) return false;
    return true;
  }

  editTooltip(u: UserDto): string {
    if (u.userID === this.currentUserId)
      return 'You cannot edit your own account here';
    if (u.roleName === 'Admin' && !this.isSuperAdmin)
      return 'Only the super admin can edit other admin accounts';
    return 'Edit user details';
  }

  // ── TOGGLE permission ─────────────────────────────────────
  // Rules:
  // • Investigators → NEVER (status is automatic, show lock icon)
  // • Yourself → never
  // • Admin → only super admin can toggle another admin
  // • Everyone else → free toggle
  canToggle(u: UserDto): boolean {
    if (u.roleName === 'Investigator') return false;
    if (u.userID === this.currentUserId) return false;
    if (u.roleName === 'Admin' && !this.isSuperAdmin) return false;
    return true;
  }

  toggleTooltip(u: UserDto): string {
    if (u.roleName === 'Investigator')
      return 'Status is automatic — Active when assigned to a protocol site, Inactive otherwise';
    if (u.userID === this.currentUserId)
      return 'You cannot change your own status';
    if (u.roleName === 'Admin' && !this.isSuperAdmin)
      return 'Only the super admin can activate/deactivate admin accounts';
    return u.isActive ? 'Click to deactivate' : 'Click to activate';
  }

  // ── Open / close modal ────────────────────────────────────
  openCreate(): void {
    this.editMode   = false;
    this.modalError = '';
    this.form = { userID: 0, name: '', email: '', phone: '', roleID: 0, roleName: '', password: '' };
    this.showModal = true;
  }

  openEdit(u: UserDto): void {
    if (!this.canEdit(u)) {
      this.errorMsg = this.editTooltip(u);
      this.clearMsg();
      return;
    }
    this.editMode   = true;
    this.modalError = '';
    this.form       = { ...u, password: '' };
    this.showModal  = true;
  }

  closeModal(): void { this.showModal = false; }

  // ── Save ──────────────────────────────────────────────────
  saveUser(): void {
    this.modalError = '';

    if (!this.form.name?.trim())  { this.modalError = 'Full name is required.'; return; }
    if (!this.form.email?.trim()) { this.modalError = 'Email is required.'; return; }
    if (this.form.phone && !/^[0-9]{10}$/.test(this.form.phone)) {
      this.modalError = 'Phone must be exactly 10 digits.'; return;
    }
    if (!this.form.roleID || +this.form.roleID === 0) {
      this.modalError = 'Please select a role.'; return;
    }
    if (!this.editMode && (!this.form.password || this.form.password.length < 8)) {
      this.modalError = 'Password must be at least 8 characters.'; return;
    }

    const role = this.roles.find(r => r.roleID === +this.form.roleID);
    if (role) this.form.roleName = role.roleName;

    this.isSaving = true;

    if (this.editMode) {
      const payload = {
        name:   this.form.name.trim(),
        email:  this.form.email.trim(),
        phone:  this.form.phone?.trim() ?? '',
        roleID: +this.form.roleID
      };
      this.userApi.update(this.form.userID, payload).subscribe({
        next: r => {
          this.isSaving = false;
          if (r.success) {
            this.successMsg = r.message || `"${this.form.name}" updated.`;
            this.showModal  = false;
            this.clearMsg();
            this.load();
          } else {
            this.modalError = r.message || 'Update failed.';
          }
        },
        error: err => {
          this.isSaving   = false;
          this.modalError = err.error?.message || 'Failed to update user.';
        }
      });

    } else {
      this.authApi.createStaff(this.form).subscribe({
        next: r => {
          this.isSaving = false;
          if (r.success) {
            this.successMsg = `"${this.form.name}" created successfully.`;
            this.showModal  = false;
            this.clearMsg();
            this.load();
          } else {
            this.modalError = r.message || 'Failed to create user.';
          }
        },
        error: err => {
          this.isSaving   = false;
          this.modalError = err.error?.message || 'Failed to create user.';
        }
      });
    }
  }

  // ── Toggle ────────────────────────────────────────────────
  toggleUser(u: UserDto): void {
    if (!this.canToggle(u)) return;
    const action = u.isActive ? 'deactivate' : 'activate';
    if (!confirm(`Are you sure you want to ${action} "${u.name}"?`)) return;

    this.userApi.toggle(u.userID).subscribe({
      next: r => {
        if (r.success) { this.successMsg = r.message; }
        else            { this.errorMsg   = r.message; }
        this.clearMsg();
        this.load();
      },
      error: err => {
        this.errorMsg = err.error?.message || 'Failed to update status.';
        this.clearMsg();
      }
    });
  }

  getRoleBadge(role: string): string {
    const m: Record<string, string> = {
      'Admin':                'lt-badge lt-badge-admin',
      'ClinicalTrialManager': 'lt-badge lt-badge-ctm',
      'Investigator':         'lt-badge lt-badge-inv',
      'RegulatoryOfficer':    'lt-badge lt-badge-reg',
      'DataManager':          'lt-badge lt-badge-dm',
    };
    return m[role] ?? 'lt-badge lt-badge-gray';
  }

  private clearMsg(): void {
    setTimeout(() => {
      this.successMsg = '';
      this.errorMsg   = '';
      this.cdr.detectChanges();
    }, 4000);
  }
}