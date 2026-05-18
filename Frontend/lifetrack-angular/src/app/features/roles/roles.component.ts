import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  RoleApiService, UserApiService,
  RoleDto, UserDto
} from '../../core/services/api.service';

@Component({
  selector: 'app-roles',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './roles.component.html'
})
export class RolesComponent implements OnInit {
  roles:      RoleDto[] = [];
  users:      UserDto[] = [];
  successMsg  = '';
  errorMsg    = '';
  selectedUserId  = 0;
  selectedRoleID  = 0;

  constructor(
    private roleApi: RoleApiService,
    private userApi: UserApiService) {}

  ngOnInit(): void {
    this.userApi.getAll().subscribe(r => {
      if (r.success) this.users = r.data;
    });
    this.roles = [
      { roleID: 1, roleName: 'Admin' },
      { roleID: 2, roleName: 'ClinicalTrialManager' },
      { roleID: 3, roleName: 'Investigator' },
      { roleID: 5, roleName: 'RegulatoryOfficer' },
      { roleID: 6, roleName: 'DataManager' }
    ];
  }

  assignRole(): void {
    const role = this.roles.find(
      r => r.roleID === +this.selectedRoleID);
    if (!role) return;
    this.roleApi.assign(
      this.selectedUserId,
      role.roleID,
      role.roleName
    ).subscribe({
      next: r => {
        if (r.success) {
          this.successMsg =
            `Role ${role.roleName} assigned.`;
          this.userApi.getAll().subscribe(u => {
            if (u.success) this.users = u.data;
          });
        } else { this.errorMsg = r.message; }
      }
    });
  }
}