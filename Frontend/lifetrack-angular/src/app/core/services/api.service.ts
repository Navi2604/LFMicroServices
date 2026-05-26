import { Injectable } from '@angular/core';
import { HttpClient,HttpParams } from '@angular/common/http';
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
  email?:         string;
  contact?:       string;
  investigatorID: number;
  protocolID?:    number;
  status:         string;
}

export interface SiteProtocolDto {
  siteProtocolID:     number;
  siteID:             number;
  siteName:           string;
  location:           string;
  siteLocation:       string;
  protocolID:         number;
  protocolTitle:      string;
  investigatorID:     number;
  investigatorName:   string;
  investigatorEmail:  string;
  investigatorContact: string;
  initiationDate:     string;
  status:             string;
}

export interface NotificationDto {
  notificationID: number;
  userID:         number;
  message:        string;
  category:       string;
  status:         string;
  createdDate:    string;
}

export interface VisitDto {
  visitID:      number;
  enrollmentID: number;
  patientID:    number;
  protocolID:   number;
  visitDate:    string;
  status:       string;
  notes:        string;
}

// ─── Adverse Event DTOs ───────────────────────────────────────────
export interface AdverseEventDto {
  eventID:      number;
  patientID:    number;
  patientName:  string;
  protocolID:   number;
  description:  string;
  severity:     string;
  status:       string;
  reportedDate: string;
}
export interface CreateAdverseEventRequest {
  patientID:   number;
  protocolID:  number;
  description: string;
  severity:    string;
}
export interface AdverseEventFilter {
  patientID?:  number;
  protocolID?: number;
  severity?:   string;
  status?:     string;
}

// ─── Deviation DTOs ───────────────────────────────────────────────
export interface DeviationDto {
  deviationID:    number;
  siteProtocolID: number;
  description:    string;
  severity:       string;
  status:         string;
}
export interface CreateDeviationRequest {
  siteProtocolID: number;
  description:    string;
  severity:       string;
}
export interface DeviationFilter {
  siteProtocolID?: number;
  severity?:       string;
  status?:         string;
}

// ─── KPI Report DTOs ──────────────────────────────────────────────
export interface KPIReportDto {
  reportID:       number;
  protocolID:     number;
  protocolTitle:  string;
  scope:          string;
  enrollmentRate: number;
  dropoutRate:    number;
  aeCount:        number;
  generatedDate:  string;
}
export interface CreateKPIReportRequest {
  protocolID:     number;
  scope:          string;
  enrollmentRate: number;
  dropoutRate:    number;
  aeCount:        number;
}
export interface KPIReportFilter {
  protocolID?: number;
  fromDate?:   string;
  toDate?:     string;
}

export interface VisitDto {
  visitID:    number;
  patientID:  number;
  protocolID: number;
  visitDate:  string;
  status:     string;
  notes:      string;
}

// ─── Document DTOs ────────────────────────────────────────────
export interface DocumentDto {
  documentID:    number;
  protocolID:    number;
  protocolTitle: string;
  uploadedBy:    number;
  uploaderName:  string;
  title:         string;
  type:          string;
  version:       string;
  fileURL:       string;
  status:        string;
  uploadedAt:    string;
  reviewNotes?:  string;
  reviewedBy?:   number;
  reviewerName?: string;
  reviewedAt?:   string;
}
export interface CreateDocumentRequest {
  protocolID: number;
  title:      string;
  type:       string;
  version:    string;
  fileURL:    string;
}
export interface ReviewDocumentRequest {
  approve:      boolean;
  reviewNotes?: string;
}
export interface DocumentFilter {
  protocolID?: number;
  type?:       string;
  status?:     string;
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

  loginPatient(email: string, password: string):
    Observable<ApiResponse<LoginResponse>> {
    return this.http.post<ApiResponse<LoginResponse>>(
      `${this.url}/login-patient`, { email, password });
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

  update(id: number, data: { name: string; email: string; phone: string; roleID: number }): Observable<ApiResponse<UserDto>> {
    return this.http.put<ApiResponse<UserDto>>(`${this.url}/${id}`, data);
  }

  toggle(id: number): Observable<ApiResponse<UserDto>> {
    return this.http.patch<ApiResponse<UserDto>>(
      `${this.url}/${id}/toggle`, {});
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

  archive(id: number): Observable<ApiResponse<boolean>> {
    return this.http.patch<ApiResponse<boolean>>(`${this.url}/${id}/archive`, {});
  }

  unarchive(id: number): Observable<ApiResponse<boolean>> {
    return this.http.patch<ApiResponse<boolean>>(`${this.url}/${id}/unarchive`, {});
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
    return this.http.post<ApiResponse<SiteDto>>(this.url, data);
  }

  update(id: number, data: any): Observable<ApiResponse<SiteDto>> {
    return this.http.put<ApiResponse<SiteDto>>(`${this.url}/${id}`, data);
  }

  delete(id: number): Observable<ApiResponse<boolean>> {
    return this.http.delete<ApiResponse<boolean>>(`${this.url}/${id}`);
  }
}

// ✅ FIXED: VisitApiService now uses port 5007 for Visit Service
@Injectable({ providedIn: 'root' })
export class VisitApiService {
  private url = 'http://localhost:5007/api/visits';  // ✅ CHANGED: Port 5007
  constructor(private http: HttpClient) {}

  getAll(params?: any): Observable<ApiResponse<VisitDto[]>> {
    return this.http.get<ApiResponse<VisitDto[]>>(this.url, { params });
  }

  create(data: any): Observable<ApiResponse<VisitDto>> {
    return this.http.post<ApiResponse<VisitDto>>(
      this.url, data);
  }

  update(id: number, data: any): Observable<ApiResponse<VisitDto>> {
    return this.http.put<ApiResponse<VisitDto>>(`${this.url}/${id}`, data);
  }

  updateStatus(id: number, status: string): Observable<ApiResponse<boolean>> {
    return this.http.patch<ApiResponse<boolean>>(`${this.url}/${id}/status`, { Status: status });
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
export class EnrollmentApiService {
  private url = `${BASE}/api/enrollments`;
  constructor(private http: HttpClient) {}

  getAll(params?: any): Observable<ApiResponse<any[]>> {
    return this.http.get<ApiResponse<any[]>>(this.url, { params });
  }

  create(data: any): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(this.url, data);
  }

  updateStatus(id: number, data: any): Observable<ApiResponse<any>> {
    return this.http.patch<ApiResponse<any>>(`${this.url}/${id}/status`, data);
  }

  respond(id: number, accept: boolean): Observable<ApiResponse<boolean>> {
    return this.http.patch<ApiResponse<boolean>>(`${this.url}/${id}/respond`, { accept });
  }
}

@Injectable({ providedIn: 'root' })
export class SiteProtocolApiService {
  private url = `${BASE}/api/site-protocols`;
  constructor(private http: HttpClient) {}

  getAll(params?: any): Observable<ApiResponse<SiteProtocolDto[]>> {
    return this.http.get<ApiResponse<SiteProtocolDto[]>>(this.url, { params });
  }

  getById(id: number): Observable<ApiResponse<SiteProtocolDto>> {
    return this.http.get<ApiResponse<SiteProtocolDto>>(`${this.url}/${id}`);
  }

  create(data: any): Observable<ApiResponse<SiteProtocolDto>> {
    return this.http.post<ApiResponse<SiteProtocolDto>>(this.url, data);
  }

  updateStatus(id: number, status: string): Observable<ApiResponse<boolean>> {
    return this.http.patch<ApiResponse<boolean>>(`${this.url}/${id}/status`, { status });
  }

  delete(id: number): Observable<ApiResponse<boolean>> {
    return this.http.delete<ApiResponse<boolean>>(`${this.url}/${id}`);
  }
}

@Injectable({ providedIn: 'root' })
export class NotificationApiService {
  private url = `${BASE}/api/notifications`;
  constructor(private http: HttpClient) {}

  getAll(params?: any): Observable<ApiResponse<NotificationDto[]>> {
    return this.http.get<ApiResponse<NotificationDto[]>>(this.url, { params });
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

@Injectable({ providedIn: 'root' })
export class DocumentApiService {
  private url = `${BASE}/api/documents`;
  constructor(private http: HttpClient) {}

  getAll(filter?: DocumentFilter): Observable<ApiResponse<DocumentDto[]>> {
    let params = new HttpParams();
    if (filter?.protocolID) params = params.set('protocolID', filter.protocolID);
    if (filter?.type)       params = params.set('type',       filter.type);
    if (filter?.status)     params = params.set('status',     filter.status);
    return this.http.get<ApiResponse<DocumentDto[]>>(this.url, { params });
  }

  getById(id: number): Observable<ApiResponse<DocumentDto>> {
    return this.http.get<ApiResponse<DocumentDto>>(`${this.url}/${id}`);
  }

  create(data: CreateDocumentRequest): Observable<ApiResponse<DocumentDto>> {
    return this.http.post<ApiResponse<DocumentDto>>(this.url, data);
  }

  submitForReview(id: number): Observable<ApiResponse<boolean>> {
    return this.http.patch<ApiResponse<boolean>>(`${this.url}/${id}/submit`, {});
  }

  review(id: number, data: ReviewDocumentRequest): Observable<ApiResponse<DocumentDto>> {
    return this.http.patch<ApiResponse<DocumentDto>>(`${this.url}/${id}/review`, data);
  }

  delete(id: number): Observable<ApiResponse<boolean>> {
    return this.http.delete<ApiResponse<boolean>>(`${this.url}/${id}`);
  }
  archive(id: number): Observable<ApiResponse<boolean>> {
  return this.http.patch<ApiResponse<boolean>>(`${this.url}/${id}/archive`, {});
}
}

@Injectable({ providedIn: 'root' })
export class AdverseEventApiService {
  private url = `${BASE}/api/adverse-events`;
  constructor(private http: HttpClient) {}

  getAll(filter?: AdverseEventFilter): Observable<ApiResponse<AdverseEventDto[]>> {
    let params = new HttpParams();
    if (filter?.patientID  != null) params = params.set('patientID',  filter.patientID);
    if (filter?.protocolID != null) params = params.set('protocolID', filter.protocolID);
    if (filter?.severity)           params = params.set('severity',   filter.severity);
    if (filter?.status)             params = params.set('status',     filter.status);
    return this.http.get<ApiResponse<AdverseEventDto[]>>(this.url, { params });
  }
  create(data: CreateAdverseEventRequest): Observable<ApiResponse<AdverseEventDto>> {
    return this.http.post<ApiResponse<AdverseEventDto>>(this.url, data);
  }
  updateStatus(id: number, status: string): Observable<ApiResponse<boolean>> {
    return this.http.patch<ApiResponse<boolean>>(`${this.url}/${id}/status`, { status });
  }
  delete(id: number): Observable<ApiResponse<boolean>> {
    return this.http.delete<ApiResponse<boolean>>(`${this.url}/${id}`);
  }
}

@Injectable({ providedIn: 'root' })
export class DeviationApiService {
  private url = `${BASE}/api/deviations`;
  constructor(private http: HttpClient) {}

  getAll(filter?: DeviationFilter): Observable<ApiResponse<DeviationDto[]>> {
    let params = new HttpParams();
    if (filter?.siteProtocolID != null) params = params.set('siteProtocolID', filter.siteProtocolID);
    if (filter?.severity)              params = params.set('severity',       filter.severity);
    if (filter?.status)                params = params.set('status',         filter.status);
    return this.http.get<ApiResponse<DeviationDto[]>>(this.url, { params });
  }
  getById(id: number): Observable<ApiResponse<DeviationDto>> {
    return this.http.get<ApiResponse<DeviationDto>>(`${this.url}/${id}`);
  }
  create(data: CreateDeviationRequest): Observable<ApiResponse<DeviationDto>> {
    return this.http.post<ApiResponse<DeviationDto>>(this.url, data);
  }
  updateStatus(id: number, status: string): Observable<ApiResponse<boolean>> {
    return this.http.patch<ApiResponse<boolean>>(`${this.url}/${id}/status`, { status });
  }
  delete(id: number): Observable<ApiResponse<boolean>> {
    return this.http.delete<ApiResponse<boolean>>(`${this.url}/${id}`);
  }
}

@Injectable({ providedIn: 'root' })
export class KPIReportApiService {
  private url = `${BASE}/api/kpi-reports`;
  constructor(private http: HttpClient) {}

  getAll(filter?: KPIReportFilter): Observable<ApiResponse<KPIReportDto[]>> {
    let params = new HttpParams();
    if (filter?.protocolID) params = params.set('protocolID', filter.protocolID);
    if (filter?.fromDate)   params = params.set('fromDate',   filter.fromDate);
    if (filter?.toDate)     params = params.set('toDate',     filter.toDate);
    return this.http.get<ApiResponse<KPIReportDto[]>>(this.url, { params });
  }
  create(data: CreateKPIReportRequest): Observable<ApiResponse<KPIReportDto>> {
    return this.http.post<ApiResponse<KPIReportDto>>(this.url, data);
  }
  delete(id: number): Observable<ApiResponse<boolean>> {
    return this.http.delete<ApiResponse<boolean>>(`${this.url}/${id}`);
  }
}