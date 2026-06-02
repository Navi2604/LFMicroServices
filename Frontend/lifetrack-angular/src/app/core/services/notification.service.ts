// notification.service.ts
// Central service — call notify() after any key event to create a notification
// Category options: 'Enrollment' | 'Visit' | 'Protocol' | 'Alert' | 'System'

import { Injectable } from '@angular/core';
import { NotificationApiService } from './api.service';

@Injectable({ providedIn: 'root' })
export class NotificationService {

  constructor(private notifApi: NotificationApiService) {}

  // Fire and forget — notifications are non-critical, errors are swallowed
  notify(userID: number, message: string, category: string): void {
    if (!userID || userID <= 0) return;
    this.notifApi.create(userID, message, category).subscribe({
      next:  () => {},
      error: () => {}
    });
  }

  // Convenience methods per category
  enrollment(userID: number, message: string): void {
    this.notify(userID, message, 'Enrollment');
  }
  visit(userID: number, message: string): void {
    this.notify(userID, message, 'Visit');
  }
  protocol(userID: number, message: string): void {
    this.notify(userID, message, 'Protocol');
  }
  alert(userID: number, message: string): void {
    this.notify(userID, message, 'Alert');
  }
  system(userID: number, message: string): void {
    this.notify(userID, message, 'System');
  }
}