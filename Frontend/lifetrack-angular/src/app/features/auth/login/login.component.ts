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
  email         = '';
  password      = '';
  isLoading     = false;
  errorMessage  = '';

  constructor(
    private authApi:     AuthApiService,
    private authService: AuthService,
    private router:      Router) {}

  onSubmit(): void {
    if (!this.email || !this.password) {
      this.errorMessage =
        'Please enter email and password.';
      return;
    }
    this.isLoading    = true;
    this.errorMessage = '';

    this.authApi.login(this.email, this.password)
      .subscribe({
        next: (res) => {
          this.isLoading = false;
          if (res.success) {
            this.authService.setUser(res.data);
            this.router.navigate(['/dashboard']);
          } else {
            this.errorMessage = res.message;
          }
        },
        error: (err) => {
          this.isLoading    = false;
          this.errorMessage =
            err.error?.message ?? 'Login failed.';
        }
      });
  }
}