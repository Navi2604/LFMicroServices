import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject } from 'rxjs';

export interface LoginResponse {
  token:     string;
  userName:  string;
  email:     string;
  role:      string;
  userId:    number;
  expiresAt: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {

  private userSubject =
    new BehaviorSubject<LoginResponse | null>(
      this.loadFromStorage());

  currentUser$ = this.userSubject.asObservable();

  constructor(private router: Router) {}

  setUser(user: LoginResponse): void {
    localStorage.setItem('token', user.token);
    localStorage.setItem('user', JSON.stringify(user));
    this.userSubject.next(user);
  }

  logout(): void {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    this.userSubject.next(null);
    this.router.navigate(['/login']);
  }

  getToken(): string | null {
    return localStorage.getItem('token');
  }

  isLoggedIn(): boolean {
    const token = this.getToken();
    if (!token) return false;
    try {
      const payload = JSON.parse(
        atob(token.split('.')[1]));
      return payload.exp * 1000 > Date.now();
    } catch {
      return false;
    }
  }

  getRole(): string {
    return this.userSubject.value?.role ?? '';
  }

  getUserId(): number {
    return this.userSubject.value?.userId ?? 0;
  }

  getUserName(): string {
    return this.userSubject.value?.userName ?? '';
  }

  private loadFromStorage(): LoginResponse | null {
    const str = localStorage.getItem('user');
    return str ? JSON.parse(str) : null;
  }
}