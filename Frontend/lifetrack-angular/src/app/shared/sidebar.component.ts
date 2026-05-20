// sidebar.component.ts
import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../core/services/auth.service';

@Component({
  selector: 'lt-sidebar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './sidebar.component.html'
})
export class SidebarComponent implements OnInit {
  @Input() unreadCount = 0;

  userName  = '';
  userRole  = '';
  initials  = '';

  constructor(
    private authService: AuthService,
    private router:      Router
  ) {}

  ngOnInit(): void {
    this.userName = this.authService.getUserName();
    this.userRole = this.authService.getRole();
    this.initials = this.userName
      .split(' ')
      .map(w => w[0] ?? '')
      .join('')
      .substring(0, 2)
      .toUpperCase();
  }

  hasRole(roles: string[]): boolean {
    return roles.includes(this.userRole);
  }

  logout(): void {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    sessionStorage.clear();
    this.router.navigate(['/login']);
  }
}