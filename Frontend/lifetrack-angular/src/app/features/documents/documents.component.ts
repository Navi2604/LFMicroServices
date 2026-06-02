import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SidebarComponent } from '../../shared/sidebar.component';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import {
  DocumentApiService, DocumentDto, CreateDocumentRequest, ReviewDocumentRequest,
  ProtocolApiService, ProtocolDto, SiteProtocolApiService
} from '../../core/services/api.service';

type StatusTab = 'all' | 'Draft' | 'Under Review' | 'Approved' | 'Rejected';

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
  isInvestigator   = false;
  myDocProtocolIds: Set<number> = new Set(); // investigator's assigned protocol IDs

  // Filters
  activeTab:       StatusTab = 'all';
  typeFilter:      string    = 'all';
  titleSearch:     string = '';   // search by document title
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

  constructor(
    private docApi:      DocumentApiService,
    private protocolApi: ProtocolApiService,
    private authService: AuthService,
    private notifSvc:       NotificationService,
    private siteProtocolApi: SiteProtocolApiService,
    private cdr:             ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.userRole  = this.authService.getRole();
    this.canCreate = ['Admin', 'ClinicalTrialManager'].includes(this.userRole);
    this.canReview = ['RegulatoryOfficer'].includes(this.userRole);
    this.isInvestigator = this.userRole === 'Investigator';
    if (this.isInvestigator) {
      // Fetch assigned protocols first, then load docs
      const uid = this.authService.getUserId();
      this.siteProtocolApi.getAll({ investigatorID: uid }).subscribe(r => {
        if (r.success) {
          this.myDocProtocolIds = new Set((r.data ?? []).map((sp: any) => sp.protocolID as number));
        }
        this.loadProtocols();
        this.load();
      });
    } else {
      this.loadProtocols();
      this.load();
    }
  }

  // ── Data ──────────────────────────────────────────────────────

  load(): void {
    this.isLoading = true;
    this.errorMsg  = '';
    this.docApi.getAll().subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.success) {
          let docs = res.data ?? [];
          if (this.isInvestigator) {
            // Only Approved docs AND only from their assigned protocols
            docs = docs.filter((d: any) =>
              d.status === 'Approved' &&
              (this.myDocProtocolIds.size === 0 || this.myDocProtocolIds.has(d.protocolID))
            );
            // Filter protocol dropdown to only matching protocols
            const visibleProtoIds = new Set(docs.map((d: any) => d.protocolID));
            this.protocols = this.protocols.filter(
              (p: any) => visibleProtoIds.has(p.protocolID)
            );
          }
          this.docs = docs;
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
          // docs already reloaded after siteProtocols fetch in ngOnInit
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

  if (this.isInvestigator) {
    // Investigator always sees Approved only — no tab/status filter needed
    result = result.filter(d => d.status === 'Approved');
  } else if (this.activeTab === 'all') {
    // All tab — show everything except Archived (not used) and Superseded
    result = result.filter(d => d.status !== 'Archived' && d.status !== 'Superseded');
  } else {
    result = result.filter(d => d.status === this.activeTab);
  }

  if (this.typeFilter !== 'all')
    result = result.filter(d => d.type === this.typeFilter);
  if (this.titleSearch.trim()) {
    const q = this.titleSearch.toLowerCase();
    result = result.filter(d => (d.title || '').toLowerCase().includes(q));
  }
  if (this.protocolFilter)
    result = result.filter(d => d.protocolID === Number(this.protocolFilter));

  result.sort((a, b) =>
    new Date(b.uploadedAt).getTime() - new Date(a.uploadedAt).getTime()
  );
  this.filtered = result;
  this.currentPage = 1;
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
          this.notifSvc.system(this.authService.getUserId(),
            `Document "${doc.title}" has been submitted for regulatory review.`);
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
          // Notify the uploader
          const uid = this.reviewDoc?.uploadedBy ?? 0;
          if (this.reviewApprove && uid)
            this.notifSvc.system(uid, `Your document "${this.reviewDoc?.title}" has been approved.`);
          else if (!this.reviewApprove && uid)
            this.notifSvc.alert(uid, `Your document "${this.reviewDoc?.title}" has been rejected. Please revise and resubmit.`);
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

  // ── Pagination ────────────────────────────────────────────────────
  currentPage         = 1;
  pageSize            = 10;
  itemsPerPageOptions = [5, 10, 20, 50];

  get paginated(): any[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.filtered.slice(start, start + this.pageSize);
  }
  get totalPages(): number {
    return Math.max(1, Math.ceil(this.filtered.length / this.pageSize));
  }
  getPageNumbers(): number[] {
    const range = 2;
    const pages: number[] = [];
    for (let i = Math.max(1, this.currentPage - range);
             i <= Math.min(this.totalPages, this.currentPage + range); i++) {
      pages.push(i);
    }
    return pages;
  }
  nextPage():                void { if (this.currentPage < this.totalPages) this.currentPage++; }
  prevPage():                void { if (this.currentPage > 1) this.currentPage--; }
  goToPage(p: number):       void { this.currentPage = p; }
  changePageSize(s: number): void { this.pageSize = +s; this.currentPage = 1; }

  // ── Helpers ───────────────────────────────────────────────────

getStatusClass(status: string): string {
  switch (status) {
    case 'Draft':        return 'doc-pill doc-pill-gray';
    case 'Under Review': return 'doc-pill doc-pill-amber';
    case 'Approved':     return 'doc-pill doc-pill-green';
    case 'Rejected':     return 'doc-pill doc-pill-red';
    case 'Superseded':   return 'doc-pill doc-pill-muted';
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