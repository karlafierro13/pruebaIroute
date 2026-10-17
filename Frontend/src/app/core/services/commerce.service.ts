import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  ProcessDateSummary,
  ProcessResult,
  QuarantineRecord,
  UpdateQuarantineRequest,
  UpdateQuarantineResult,
  UploadResult,
} from '../models/commerce.models';

@Injectable({ providedIn: 'root' })
export class CommerceService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/commerce`;

  upload(file: File): Observable<UploadResult> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.http.post<UploadResult>(`${this.baseUrl}/upload`, formData);
  }

  process(processDate: string): Observable<ProcessResult> {
    return this.http.post<ProcessResult>(`${this.baseUrl}/process`, { processDate });
  }

  getProcessDates(): Observable<ProcessDateSummary[]> {
    return this.http.get<ProcessDateSummary[]>(`${this.baseUrl}/process-dates`);
  }

  getQuarantine(processDate?: string | null): Observable<QuarantineRecord[]> {
    const params = processDate ? new HttpParams().set('processDate', processDate) : undefined;
    return this.http.get<QuarantineRecord[]>(`${this.baseUrl}/quarantine`, { params });
  }

  updateQuarantine(id: number, request: UpdateQuarantineRequest): Observable<UpdateQuarantineResult> {
    return this.http.put<UpdateQuarantineResult>(`${this.baseUrl}/quarantine/${id}`, request);
  }

  deleteQuarantine(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/quarantine/${id}`);
  }
}
