import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthApiService } from '../../../core/services/api.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './register.component.html'
})
export class RegisterComponent {
  form = {
    name: '', email: '', dob: '',
    contactInfo: '', password: '',
    confirmPassword: ''
  };
  isLoading    = false;
  errorMessage = '';
  successMsg   = '';

  constructor(
    private authApi: AuthApiService,
    private router:  Router) {}

  onSubmit(): void {
    if (this.form.password !== this.form.confirmPassword) {
      this.errorMessage = 'Passwords do not match.';
      return;
    }
    if (this.form.password.length < 8) {
      this.errorMessage =
        'Password must be at least 8 characters.';
      return;
    }
    this.isLoading    = true;
    this.errorMessage = '';

    this.authApi.register(this.form).subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.success) {
          this.successMsg =
            'Registration successful! Please login.';
          setTimeout(() =>
            this.router.navigate(['/login']), 2000);
        } else {
          this.errorMessage = res.message;
        }
      },
      error: (err) => {
        this.isLoading    = false;
        this.errorMessage =
          err.error?.message ?? 'Registration failed.';
      }
    });
  }
}