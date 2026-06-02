// src/app/core/pipes/ist-date.pipe.ts
// Usage in templates:  {{ someDate | istDate }}
//                      {{ someDate | istDate:'dd MMM yyyy, HH:mm' }}

import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'istDate', standalone: true })
export class IstDatePipe implements PipeTransform {

  transform(value: string | Date | null | undefined, format = 'dd MMM yyyy, HH:mm'): string {
    if (!value) return '—';

    const utc = typeof value === 'string' ? new Date(value) : value;
    if (isNaN(utc.getTime())) return '—';

    // IST = UTC + 5h 30m
    const ist = new Date(utc.getTime() + (5 * 60 + 30) * 60 * 1000);

    return this.formatDate(ist, format);
  }

  private formatDate(d: Date, format: string): string {
    const pad   = (n: number) => String(n).padStart(2, '0');
    const MONTHS_SHORT = ['Jan','Feb','Mar','Apr','May','Jun',
                          'Jul','Aug','Sep','Oct','Nov','Dec'];
    const MONTHS_LONG  = ['January','February','March','April','May','June',
                          'July','August','September','October','November','December'];

    return format
      .replace('yyyy', String(d.getFullYear()))
      .replace('yy',   String(d.getFullYear()).slice(-2))
      .replace('MMMM', MONTHS_LONG[d.getMonth()])
      .replace('MMM',  MONTHS_SHORT[d.getMonth()])
      .replace('MM',   pad(d.getMonth() + 1))
      .replace('M',    String(d.getMonth() + 1))
      .replace('dd',   pad(d.getDate()))
      .replace('d',    String(d.getDate()))
      .replace('HH',   pad(d.getHours()))
      .replace('H',    String(d.getHours()))
      .replace('hh',   pad(d.getHours() % 12 || 12))
      .replace('mm',   pad(d.getMinutes()))
      .replace('ss',   pad(d.getSeconds()));
  }
}