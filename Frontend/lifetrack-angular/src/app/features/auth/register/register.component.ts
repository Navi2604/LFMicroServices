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
    this.errorMessage = '';

    // ✅ VALIDATION 1: Name
    if (!this.form.name || this.form.name.trim().length < 2) {
      this.errorMessage = 'Name must be at least 2 characters.';
      return;
    }

    // ✅ VALIDATION 2: Email format
    if (!this.form.email || !this.isValidEmail(this.form.email)) {
      this.errorMessage = 'Please enter a valid email address.';
      return;
    }

    // ✅ VALIDATION 3: DOB
    if (!this.form.dob) {
      this.errorMessage = 'Date of birth is required.';
      return;
    }

    const age = this.calculateAge(this.form.dob);
    if (age < 18) {
      this.errorMessage = 'You must be at least 18 years old to register.';
      return;
    }

    if (age > 120) {
      this.errorMessage = 'Please enter a valid date of birth.';
      return;
    }

    const dobDate = new Date(this.form.dob);
    if (dobDate > new Date()) {
      this.errorMessage = 'Date of birth cannot be in the future.';
      return;
    }

    // ✅ VALIDATION 4: Contact (10 digits)
    if (!this.form.contactInfo || this.form.contactInfo.trim() === '') {
      this.errorMessage = 'Contact number is required.';
      return;
    }

    if (!/^\d{10}$/.test(this.form.contactInfo)) {
      this.errorMessage = 'Contact must be exactly 10 digits (numbers only).';
      return;
    }

    // ✅ VALIDATION 5: Password strength
    if (!this.form.password || this.form.password.length < 8) {
      this.errorMessage = 'Password must be at least 8 characters.';
      return;
    }

    if (!this.isPasswordStrong(this.form.password)) {
      this.errorMessage = 'Password must contain: uppercase, lowercase, number, and special character.';
      return;
    }

    // ✅ VALIDATION 6: Passwords match
    if (this.form.password !== this.form.confirmPassword) {
      this.errorMessage = 'Passwords do not match.';
      return;
    }

    // All validations passed
    this.isLoading = true;

    this.authApi.register(this.form).subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.success) {
          this.successMsg = 'Registration successful! Please login.';
          setTimeout(() => this.router.navigate(['/login']), 2000);
        } else {
          this.errorMessage = res.message;
        }
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err.error?.message ?? 'Registration failed.';
      }
    });
  }

  private isValidEmail(email: string): boolean {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
  }

  private calculateAge(dob: string): number {
    const birthDate = new Date(dob);
    const today = new Date();
    let age = today.getFullYear() - birthDate.getFullYear();
    const monthDiff = today.getMonth() - birthDate.getMonth();
    
    if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birthDate.getDate())) {
      age--;
    }
    
    return age;
  }

  private isPasswordStrong(password: string): boolean {
    const hasUppercase = /[A-Z]/.test(password);
    const hasLowercase = /[a-z]/.test(password);
    const hasNumber = /\d/.test(password);
    const hasSpecial = /[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(password);
    
    return hasUppercase && hasLowercase && hasNumber && hasSpecial;
  }

  onlyNumbers(event: KeyboardEvent): boolean {
    const charCode = event.which ? event.which : event.keyCode;
    if (charCode > 31 && (charCode < 48 || charCode > 57)) {
      event.preventDefault();
      return false;
    }
    return true;
  }

  onPasteContact(event: ClipboardEvent): void {
    event.preventDefault();
    const pastedText = event.clipboardData?.getData('text') || '';
    const numbersOnly = pastedText.replace(/[^0-9]/g, '').substring(0, 10);
    this.form.contactInfo = numbersOnly;
  }
}