// login.component.ts
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthApiService } from '../../../core/services/api.service';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './login.component.html'
})
export class LoginComponent {
  email     = '';
  password  = '';
  errorMsg  = '';
  isLoading = false;
  loginType = 'staff'; // 'staff' | 'patient'

  constructor(
    private authApi:     AuthApiService,
    private authService: AuthService,
    private router:      Router
  ) {}

  login(): void {
    if (!this.email || !this.password) {
      this.errorMsg = 'Email and password are required.'; return;
    }
    this.isLoading = true;
    this.errorMsg  = '';

    if (this.loginType === 'patient') {
      // Patient login — searches Patients table
      this.authApi.loginPatient(this.email, this.password).subscribe({
        next: r => {
          this.isLoading = false;
          if (r.success) {
            this.authService.setUser(r.data);
            this.router.navigate(['/my-dashboard']);
          } else {
            this.errorMsg = r.message || 'Invalid email or password.';
          }
        },
        error: err => {
          this.isLoading = false;
          this.errorMsg  = err.error?.message ?? 'Login failed. Please try again.';
        }
      });
    } else {
      // Staff login — searches Users table
      this.authApi.login(this.email, this.password).subscribe({
        next: r => {
          this.isLoading = false;
          if (r.success) {
            this.authService.setUser(r.data);
            this.router.navigate(['/dashboard']);
          } else {
            this.errorMsg = r.message || 'Invalid email or password.';
          }
        },
        error: err => {
          this.isLoading = false;
          this.errorMsg  = err.error?.message ?? 'Login failed. Please try again.';
        }
      });
    }
  }
}