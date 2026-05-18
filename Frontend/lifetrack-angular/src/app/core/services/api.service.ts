import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { LoginResponse } from './auth.service';

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data:    T;
  errors:  string[];
}

export interface UserDto {
  userID:   number;
  name:     string;
  email:    string;
  phone:    string;
  roleID:   number;
  roleName: string;
  isActive: boolean;
}

export interface RoleDto {
  roleID:   number;
  roleName: string;
}

export interface PatientDto {
  patientID:        number;
  name:             string;
  dob:              string;
  contactInfo:      string;
  email:            string;
  enrollmentStatus: string;
  enrolledBy?:      number;
}

export interface PhaseDto {
  phaseNumber: number;
  startDate:   string;
  endDate:     string;
}

export interface ProtocolDto {
  protocolID:     number;
  title:          string;
  phase:          string;
  startDate:      string;
  endDate:        string;
  status:         string;
  investigatorID?: number;
  phasesJson?:    string;
  phases?:        PhaseDto[];
}

export interface SiteDto {
  siteID:         number;
  name:           string;
  location:       string;
  investigatorID: number;
  protocolID?:    number;
  status:         string;
}

export interface VisitDto {
  visitID:    number;
  patientID:  number;
  protocolID: number;
  visitDate:  string;
  status:     string;
  notes:      string;
}

export interface NotificationDto {
  notificationID: number;
  userID:         number;
  message:        string;
  category:       string;
  status:         string;
  createdDate:    string;
}

const BASE = 'http://localhost:5000';

@Injectable({ providedIn: 'root' })
export class AuthApiService {
  private url = `${BASE}/api/auth`;
  constructor(private http: HttpClient) {}

  login(email: string, password: string):
    Observable<ApiResponse<LoginResponse>> {
    return this.http.post<ApiResponse<LoginResponse>>(
      `${this.url}/login`, { email, password });
  }

  register(data: any): Observable<ApiResponse<UserDto>> {
    return this.http.post<ApiResponse<UserDto>>(
      `${this.url}/register`, data);
  }

  createStaff(data: any): Observable<ApiResponse<UserDto>> {
    return this.http.post<ApiResponse<UserDto>>(
      `${this.url}/create-staff`, data);
  }
}

@Injectable({ providedIn: 'root' })
export class UserApiService {
  private url = `${BASE}/api/users`;
  constructor(private http: HttpClient) {}

  getAll(): Observable<ApiResponse<UserDto[]>> {
    return this.http.get<ApiResponse<UserDto[]>>(this.url);
  }

  toggle(id: number): Observable<ApiResponse<UserDto>> {
    return this.http.patch<ApiResponse<UserDto>>(
      `${this.url}/${id}/toggle`, {});
  }

  delete(id: number): Observable<ApiResponse<boolean>> {
    return this.http.delete<ApiResponse<boolean>>(
      `${this.url}/${id}`);
  }
}

@Injectable({ providedIn: 'root' })
export class RoleApiService {
  private url = `${BASE}/api/roles`;
  constructor(private http: HttpClient) {}

  getAll(): Observable<ApiResponse<RoleDto[]>> {
    return this.http.get<ApiResponse<RoleDto[]>>(this.url);
  }

  assign(userId: number, roleID: number, roleName: string):
    Observable<ApiResponse<RoleDto>> {
    return this.http.post<ApiResponse<RoleDto>>(
      `${this.url}/assign`, { userId, roleID, roleName });
  }
}

@Injectable({ providedIn: 'root' })
export class PatientApiService {
  private url = `${BASE}/api/patients`;
  constructor(private http: HttpClient) {}

  getAll(): Observable<ApiResponse<PatientDto[]>> {
    return this.http.get<ApiResponse<PatientDto[]>>(this.url);
  }

  enroll(data: any): Observable<ApiResponse<PatientDto>> {
    return this.http.post<ApiResponse<PatientDto>>(
      `${this.url}/enroll`, data);
  }

  updateStatus(id: number, status: string):
    Observable<ApiResponse<PatientDto>> {
    return this.http.patch<ApiResponse<PatientDto>>(
      `${this.url}/${id}/status`,
      { enrollmentStatus: status });
  }

  delete(id: number): Observable<ApiResponse<boolean>> {
    return this.http.delete<ApiResponse<boolean>>(
      `${this.url}/${id}`);
  }
}

@Injectable({ providedIn: 'root' })
export class ProtocolApiService {
  private url = `${BASE}/api/protocols`;
  constructor(private http: HttpClient) {}

  getAll(): Observable<ApiResponse<ProtocolDto[]>> {
    return this.http.get<ApiResponse<ProtocolDto[]>>(this.url);
  }

  create(data: any): Observable<ApiResponse<ProtocolDto>> {
    return this.http.post<ApiResponse<ProtocolDto>>(
      this.url, data);
  }

  update(id: number, data: any):
    Observable<ApiResponse<ProtocolDto>> {
    return this.http.put<ApiResponse<ProtocolDto>>(
      `${this.url}/${id}`, data);
  }

  delete(id: number): Observable<ApiResponse<boolean>> {
    return this.http.delete<ApiResponse<boolean>>(
      `${this.url}/${id}`);
  }

  archive(id: number, data: any): Observable<ApiResponse<ProtocolDto>> {
    return this.http.put<ApiResponse<ProtocolDto>>(
      `${this.url}/${id}`, { ...data, status: 'Archived' });
  }
}

@Injectable({ providedIn: 'root' })
export class SiteApiService {
  private url = `${BASE}/api/sites`;
  constructor(private http: HttpClient) {}

  getAll(): Observable<ApiResponse<SiteDto[]>> {
    return this.http.get<ApiResponse<SiteDto[]>>(this.url);
  }

  create(data: any): Observable<ApiResponse<SiteDto>> {
    return this.http.post<ApiResponse<SiteDto>>(
      this.url, data);
  }

  delete(id: number): Observable<ApiResponse<boolean>> {
    return this.http.delete<ApiResponse<boolean>>(
      `${this.url}/${id}`);
  }
}

@Injectable({ providedIn: 'root' })
export class VisitApiService {
  private url = `${BASE}/api/visits`;
  constructor(private http: HttpClient) {}

  getAll(): Observable<ApiResponse<VisitDto[]>> {
    return this.http.get<ApiResponse<VisitDto[]>>(this.url);
  }

  create(data: any): Observable<ApiResponse<VisitDto>> {
    return this.http.post<ApiResponse<VisitDto>>(
      this.url, data);
  }

  delete(id: number): Observable<ApiResponse<boolean>> {
    return this.http.delete<ApiResponse<boolean>>(
      `${this.url}/${id}`);
  }
}

@Injectable({ providedIn: 'root' })
export class AuditApiService {
  private url = `${BASE}/api/audit`;
  constructor(private http: HttpClient) {}

  getLogs(params?: any): Observable<ApiResponse<any>> {
    return this.http.get<ApiResponse<any>>(
      `${this.url}/logs`, { params });
  }
}

@Injectable({ providedIn: 'root' })
export class NotificationApiService {
  private url = `${BASE}/api/notifications`;
  constructor(private http: HttpClient) {}

  getAll(params?: any): Observable<ApiResponse<NotificationDto[]>> {
    return this.http.get<ApiResponse<NotificationDto[]>>(this.url, { params });
  }

  getById(id: number): Observable<ApiResponse<NotificationDto>> {
    return this.http.get<ApiResponse<NotificationDto>>(`${this.url}/${id}`);
  }

  create(data: any): Observable<ApiResponse<NotificationDto>> {
    return this.http.post<ApiResponse<NotificationDto>>(this.url, data);
  }

  markAsRead(id: number): Observable<ApiResponse<boolean>> {
    return this.http.patch<ApiResponse<boolean>>(`${this.url}/${id}/read`, {});
  }

  markAllAsRead(userId: number): Observable<ApiResponse<boolean>> {
    return this.http.patch<ApiResponse<boolean>>(`${this.url}/read-all/${userId}`, {});
  }

  delete(id: number): Observable<ApiResponse<boolean>> {
    return this.http.delete<ApiResponse<boolean>>(`${this.url}/${id}`);
  }
}