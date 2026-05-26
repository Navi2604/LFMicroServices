// login.component.ts
import { Component, AfterViewInit, ElementRef, ViewChild, NgZone } from '@angular/core';
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
export class LoginComponent implements AfterViewInit {
  email     = '';
  password  = '';
  errorMsg  = '';
  isLoading = false;
  loginType = 'staff';

  // Direct DOM references — used to fight browser autofill
  // which writes to DOM values bypassing [(ngModel)]
  @ViewChild('emailInput')    emailRef!:    ElementRef<HTMLInputElement>;
  @ViewChild('passwordInput') passwordRef!: ElementRef<HTMLInputElement>;

  // Track whether the user has manually typed — once they do,
  // we stop wiping the field
  private userTypedEmail    = false;
  private userTypedPassword = false;
  private observer!: MutationObserver;

  constructor(
    private authApi:     AuthApiService,
    private authService: AuthService,
    private router:      Router,
    private zone:        NgZone
  ) {}

  ngAfterViewInit(): void {
    // Wipe autofill immediately after render, then watch for
    // any late autofill injections (Chrome fires these at 100ms, 500ms, 1s)
    this.wipeAutofill();
    this.watchAutofill();
  }

  // Called on tab switch — reset everything
  switchTab(type: 'staff' | 'patient'): void {
    this.loginType        = type;
    this.errorMsg         = '';
    this.userTypedEmail   = false;
    this.userTypedPassword = false;
    this.wipeAutofill();
  }

  // Mark that user intentionally typed in this field
  onEmailInput(): void    { this.userTypedEmail    = true; }
  onPasswordInput(): void { this.userTypedPassword = true; }

  // Wipe both fields at DOM level + Angular model
  private wipeAutofill(): void {
    this.email    = '';
    this.password = '';
    // Multiple delays to catch all Chrome autofill waves
    [0, 50, 150, 300, 600, 1000].forEach(ms => {
      setTimeout(() => {
        if (!this.userTypedEmail && this.emailRef?.nativeElement) {
          this.emailRef.nativeElement.value = '';
          this.email = '';
        }
        if (!this.userTypedPassword && this.passwordRef?.nativeElement) {
          this.passwordRef.nativeElement.value = '';
          this.password = '';
        }
      }, ms);
    });
  }

  // MutationObserver watches the input DOM node directly.
  // If browser autofill changes the value and user hasn't typed yet, wipe it.
  private watchAutofill(): void {
    const check = (el: HTMLInputElement | undefined, isPassword: boolean) => {
      if (!el) return;
      this.observer = new MutationObserver(() => {
        this.zone.run(() => {
          if (isPassword && !this.userTypedPassword && el.value) {
            el.value      = '';
            this.password = '';
          } else if (!isPassword && !this.userTypedEmail && el.value) {
            el.value   = '';
            this.email = '';
          }
        });
      });
      this.observer.observe(el, {
        attributes: true,
        attributeFilter: ['value']
      });
    };

    check(this.emailRef?.nativeElement,    false);
    check(this.passwordRef?.nativeElement, true);
  }

  login(): void {
    // Read directly from DOM — autofill may have bypassed ngModel
    const emailVal    = this.emailRef?.nativeElement?.value    || this.email;
    const passwordVal = this.passwordRef?.nativeElement?.value || this.password;

    if (!emailVal || !passwordVal) {
      this.errorMsg = 'Email and password are required.';
      return;
    }

    this.isLoading = true;
    this.errorMsg  = '';

    if (this.loginType === 'patient') {
      this.authApi.loginPatient(emailVal, passwordVal).subscribe({
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
          this.errorMsg =
            err.error?.message ||
            err.error?.Message ||
            err.message ||
            'Login failed. Please try again.';
        }
      });

    } else {
      this.authApi.login(emailVal, passwordVal).subscribe({
        next: r => {
          this.isLoading = false;
          if (r.success) {
            this.authService.setUser(r.data);
            const role=this.authService.getRole();
            this.router.navigate(
              role === 'RegulatoryOfficer' ? ['/compliance-dashboard'] : ['/dashboard']
            );
          } else {
            this.errorMsg = r.message || 'Invalid email or password.';
          }
        },
        error: err => {
          this.isLoading = false;
          this.errorMsg =
            err.error?.message ||
            err.error?.Message ||
            err.message ||
            'Login failed. Please try again.';
        }
      });
    }
  }
}