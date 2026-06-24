// Mirrors the PaperlessREST document contract (GET /api/v1/documents,
// POST /api/v1/documents, GET /api/v1/documents/search). Hand-typed against the
// live OpenAPI; swap for openapi-typescript generated types once `pnpm generate`
// is wired into the build.

export type DocumentStatus = 'Pending' | 'Completed' | 'Failed' | (string & {});

export interface DocumentSummary {
  id: string;
  fileName: string;
  status: DocumentStatus;
  createdAt: string;
  processedAt: string | null;
  content: string | null;
  summary: string | null;
  summaryGeneratedAt: string | null;
}

/** GET /api/v1/documents — cursor-paginated envelope. */
export interface PaginatedDocumentsResponse {
  items: DocumentSummary[];
  nextCursor: string | null;
  hasMore: boolean;
  count: number;
}

/** POST /api/v1/documents — 202 Accepted body. */
export interface UploadedDocument {
  id: string;
  fileName: string;
  status: DocumentStatus;
  createdAt: string;
}

/** GET /api/v1/documents/search — bare array of DocumentSearchResultDto. */
export interface DocumentSearchResult {
  id: string;
  fileName: string;
  status?: DocumentStatus;
  content?: string | null;
  createdAt?: string;
  processedAt?: string | null;
  summary?: string | null;
}

export function toSummary(
  doc: UploadedDocument | DocumentSearchResult,
): DocumentSummary {
  return {
    id: doc.id,
    fileName: doc.fileName,
    status: doc.status ?? 'Completed',
    createdAt: 'createdAt' in doc && doc.createdAt ? doc.createdAt : '',
    processedAt: 'processedAt' in doc ? (doc.processedAt ?? null) : null,
    content: 'content' in doc ? (doc.content ?? null) : null,
    summary: 'summary' in doc ? (doc.summary ?? null) : null,
    summaryGeneratedAt: null,
  };
}
