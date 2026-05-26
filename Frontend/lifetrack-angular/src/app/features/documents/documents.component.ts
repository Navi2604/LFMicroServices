import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SidebarComponent } from '../../shared/sidebar.component';
import { AuthService } from '../../core/services/auth.service';
import {
  DocumentApiService, DocumentDto, CreateDocumentRequest, ReviewDocumentRequest,
  ProtocolApiService, ProtocolDto
} from '../../core/services/api.service';

type StatusTab = 'all' | 'Draft' | 'Under Review' | 'Approved' | 'Rejected' | 'Superseded' | 'Archived';

@Component({
  selector: 'app-documents',
  standalone: true,
  imports: [CommonModule, FormsModule, SidebarComponent],
  templateUrl: './documents.component.html'
})
export class DocumentsComponent implements OnInit {

  docs:      DocumentDto[] = [];
  filtered:  DocumentDto[] = [];
  protocols: ProtocolDto[] = [];

  isLoading   = false;
  userRole    = '';
  unreadCount = 0;
  errorMsg    = '';

  canCreate = false; // Admin, CTM
  canReview = false; // RO, Admin

  // Filters
  activeTab:       StatusTab = 'all';
  typeFilter:      string    = 'all';
  protocolFilter:  number | '' = '';

  // Document types list
  readonly docTypes = [
    { value: 'Protocol',              label: 'Protocol'              },
    { value: 'Amendment',             label: 'Amendment'             },
    { value: 'InformedConsentForm',   label: 'Informed Consent Form' },
    { value: 'InvestigatorBrochure',  label: 'Investigator Brochure' },
    { value: 'SafetyReport',          label: 'Safety Report'         },
    { value: 'MonitoringReport',      label: 'Monitoring Report'     },
    { value: 'RegulatorySubmission',  label: 'Regulatory Submission' }
  ];

  // ── Create modal ──────────────────────────────────────────────
  showCreateModal = false;
  isCreating      = false;
  createError     = '';
  createForm: CreateDocumentRequest = {
    protocolID: 0, title: '', type: '', version: '', fileURL: ''
  };

  // ── View modal ────────────────────────────────────────────────
  showViewModal = false;
  viewDoc:      DocumentDto | null = null;

  // ── Review modal ──────────────────────────────────────────────
  showReviewModal  = false;
  reviewDoc:       DocumentDto | null = null;
  reviewApprove    = true;
  reviewNotes      = '';
  isReviewing      = false;
  reviewError      = '';
  reviewSuccess    = '';

  // ── Row-level loading flags ───────────────────────────────────
  submitting: { [id: number]: boolean } = {};
  deleting:   { [id: number]: boolean } = {};
  archiving:  { [id: number]: boolean } = {};

  constructor(
    private docApi:      DocumentApiService,
    private protocolApi: ProtocolApiService,
    private authService: AuthService,
    private cdr:         ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.userRole  = this.authService.getRole();
    this.canCreate = ['Admin', 'ClinicalTrialManager'].includes(this.userRole);
    this.canReview = ['RegulatoryOfficer'].includes(this.userRole);
    this.loadProtocols();
    this.load();
  }

  // ── Data ──────────────────────────────────────────────────────

  load(): void {
    this.isLoading = true;
    this.errorMsg  = '';
    this.docApi.getAll().subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.success) {
          this.docs = res.data ?? [];
          this.applyFilter();
        } else {
          this.errorMsg = res.message;
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMsg  = err.error?.message ?? 'Failed to load documents.';
        this.cdr.detectChanges();
      }
    });
  }

  loadProtocols(): void {
    this.protocolApi.getAll().subscribe({
      next: (res) => {
        if (res.success) {
          this.protocols = res.data ?? [];
          this.cdr.detectChanges();
        }
      }
    });
  }

  // ── Filtering ─────────────────────────────────────────────────

  switchTab(tab: StatusTab): void { this.activeTab = tab; this.applyFilter(); }
  onTypeChange():     void { this.applyFilter(); }
  onProtocolChange(): void { this.applyFilter(); }

applyFilter(): void {
  let result = [...this.docs];

  if (this.activeTab === 'all') {
    // All tab excludes Archived — they're soft-removed from active view
    result = result.filter(d => d.status !== 'Archived');
  } else {
    result = result.filter(d => d.status === this.activeTab);
  }

  if (this.typeFilter !== 'all')
    result = result.filter(d => d.type === this.typeFilter);
  if (this.protocolFilter)
    result = result.filter(d => d.protocolID === Number(this.protocolFilter));

  result.sort((a, b) =>
    new Date(b.uploadedAt).getTime() - new Date(a.uploadedAt).getTime()
  );
  this.filtered = result;
}

  countByStatus(status: string): number {
    return this.docs.filter(d => d.status === status).length;
  }

  // ── Submit for Review (inline) ────────────────────────────────

  submitForReview(doc: DocumentDto): void {
    if (!confirm(`Submit "${doc.title}" for regulatory review? It will be sent to the Regulatory Officer.`))
      return;
    this.submitting[doc.documentID] = true;
    this.docApi.submitForReview(doc.documentID).subscribe({
      next: (res) => {
        this.submitting[doc.documentID] = false;
        if (res.success) {
          const idx = this.docs.findIndex(d => d.documentID === doc.documentID);
          if (idx > -1) this.docs[idx] = { ...this.docs[idx], status: 'Under Review' };
          this.applyFilter();
        } else {
          alert(res.message || 'Failed to submit.');
        }
        this.cdr.detectChanges();
      },
      error: () => {
        this.submitting[doc.documentID] = false;
        alert('Failed to submit for review.');
        this.cdr.detectChanges();
      }
    });
  }

  // ── Delete (inline) ───────────────────────────────────────────

  deleteDoc(doc: DocumentDto): void {
    if (!confirm(`Permanently delete "${doc.title}"? This cannot be undone.`))
      return;
    this.deleting[doc.documentID] = true;
    this.docApi.delete(doc.documentID).subscribe({
      next: (res) => {
        this.deleting[doc.documentID] = false;
        if (res.success) {
          this.docs = this.docs.filter(d => d.documentID !== doc.documentID);
          this.applyFilter();
        } else {
          alert(res.message || 'Failed to delete.');
        }
        this.cdr.detectChanges();
      },
      error: () => {
        this.deleting[doc.documentID] = false;
        alert('Failed to delete.');
        this.cdr.detectChanges();
      }
    });
  }

  archiveDoc(doc: DocumentDto): void {
  if (!confirm(
    `Archive "${doc.title}"?\n\n` +
    `Archived documents are hidden from active views but preserved in the audit trail. ` +
    `This cannot be undone.`
  )) return;

  this.archiving[doc.documentID] = true;

  this.docApi.archive(doc.documentID).subscribe({
    next: (res: any) => {
      this.archiving[doc.documentID] = false;
      if (res.success) {
        // Update status in local list
        const idx = this.docs.findIndex(d => d.documentID === doc.documentID);
        if (idx > -1) this.docs[idx] = { ...this.docs[idx], status: 'Archived' };
        this.applyFilter();
      } else {
        alert(res.message || 'Failed to archive document.');
      }
      this.cdr.detectChanges();
    },
    error: () => {
      this.archiving[doc.documentID] = false;
      alert('Failed to archive document.');
      this.cdr.detectChanges();
    }
  });
}

  // ── Create modal ──────────────────────────────────────────────

  openCreateModal(): void {
    this.createForm  = { protocolID: 0, title: '', type: '', version: '', fileURL: '' };
    this.createError = '';
    this.isCreating  = false;
    this.showCreateModal = true;
  }

  closeCreateModal(): void {
    this.showCreateModal = false;
    this.createError = '';
  }

  isCreateFormValid(): boolean {
    return this.createForm.protocolID > 0 &&
           !!this.createForm.title.trim() &&
           !!this.createForm.type &&
           !!this.createForm.version.trim() &&
           !!this.createForm.fileURL.trim();
  }

  submitCreate(): void {
    if (!this.isCreateFormValid() || this.isCreating) return;
    this.isCreating  = true;
    this.createError = '';
    this.docApi.create(this.createForm).subscribe({
      next: (res) => {
        this.isCreating = false;
        if (res.success) {
          this.docs.unshift(res.data);
          this.applyFilter();
          this.closeCreateModal();
        } else {
          this.createError = res.message || 'Failed to create document.';
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isCreating  = false;
        this.createError = err.error?.message ?? 'Failed to create document.';
        this.cdr.detectChanges();
      }
    });
  }

  // ── View modal ────────────────────────────────────────────────

  openView(doc: DocumentDto): void {
    this.viewDoc      = doc;
    this.showViewModal = true;
  }

  closeViewModal(): void {
    this.showViewModal = false;
    this.viewDoc       = null;
  }

  // ── Review modal ──────────────────────────────────────────────

  openReview(doc: DocumentDto): void {
    this.reviewDoc     = doc;
    this.reviewApprove = true;
    this.reviewNotes   = '';
    this.reviewError   = '';
    this.reviewSuccess = '';
    this.isReviewing   = false;
    this.showReviewModal = true;
  }

  closeReviewModal(): void {
    this.showReviewModal = false;
    this.reviewDoc       = null;
  }

  submitReview(): void {
    if (!this.reviewDoc || this.isReviewing) return;
    if (!this.reviewApprove && !this.reviewNotes.trim()) {
      this.reviewError = 'Review notes are required when rejecting a document.';
      return;
    }
    this.isReviewing  = true;
    this.reviewError  = '';
    const payload: ReviewDocumentRequest = {
      approve:     this.reviewApprove,
      reviewNotes: this.reviewNotes.trim() || undefined
    };
    this.docApi.review(this.reviewDoc.documentID, payload).subscribe({
      next: (res) => {
        this.isReviewing = false;
        if (res.success) {
          const idx = this.docs.findIndex(d => d.documentID === this.reviewDoc!.documentID);
          if (idx > -1) this.docs[idx] = res.data;
          this.applyFilter();
          this.reviewSuccess = this.reviewApprove
            ? '✅ Document approved. Uploader has been notified.'
            : '❌ Document rejected. Uploader has been notified.';
          setTimeout(() => this.closeReviewModal(), 1400);
        } else {
          this.reviewError = res.message || 'Failed to submit review.';
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isReviewing = false;
        this.reviewError = err.error?.message ?? 'Failed to submit review.';
        this.cdr.detectChanges();
      }
    });
  }

  // ── Helpers ───────────────────────────────────────────────────

getStatusClass(status: string): string {
  switch (status) {
    case 'Draft':        return 'doc-pill doc-pill-gray';
    case 'Under Review': return 'doc-pill doc-pill-amber';
    case 'Approved':     return 'doc-pill doc-pill-green';
    case 'Rejected':     return 'doc-pill doc-pill-red';
    case 'Superseded':   return 'doc-pill doc-pill-muted';
    case 'Archived':     return 'doc-pill doc-pill-archived'; // ← new
    default:             return 'doc-pill doc-pill-gray';
  }
}

  getTypelabel(type: string): string {
    return this.docTypes.find(t => t.value === type)?.label ?? type;
  }

  truncate(text: string, n = 40): string {
    if (!text) return '';
    return text.length > n ? text.substring(0, n) + '…' : text;
  }
}