import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';

import { UploadZoneComponent } from '../components/upload-zone.component';
import { DocumentSummary } from '../data/document.models';
import { DocumentsService } from '../data/documents.service';
import { DocumentsPageComponent } from './documents-page.component';

function summary(over: Partial<DocumentSummary> = {}): DocumentSummary {
  return {
    id: '1',
    fileName: 'x.pdf',
    status: 'Completed',
    createdAt: '2026-01-01T00:00:00Z',
    processedAt: null,
    content: null,
    summary: null,
    summaryGeneratedAt: null,
    ...over,
  };
}

function makeDocsStub() {
  return {
    visible: signal<readonly DocumentSummary[]>([]),
    loading: signal(false),
    failed: signal(false),
    currentSearch: signal(''),
    liveDisconnected: signal(false),
    connectStreams: vi.fn(),
    disconnectStreams: vi.fn(),
    load: vi.fn(),
    upload: vi.fn(() => Promise.resolve()),
    remove: vi.fn(),
    search: vi.fn(),
    clearSearch: vi.fn(),
  };
}

describe('DocumentsPageComponent', () => {
  let docs: ReturnType<typeof makeDocsStub>;

  beforeEach(async () => {
    docs = makeDocsStub();
    await TestBed.configureTestingModule({
      imports: [DocumentsPageComponent],
      providers: [{ provide: DocumentsService, useValue: docs }],
    }).compileComponents();
  });
  afterEach(() => vi.restoreAllMocks());

  function create(): ComponentFixture<DocumentsPageComponent> {
    const f = TestBed.createComponent(DocumentsPageComponent);
    f.detectChanges();
    return f;
  }

  it('connects SSE streams and loads documents on init', () => {
    create();
    expect(docs.connectStreams).toHaveBeenCalled();
    expect(docs.load).toHaveBeenCalled();
  });

  it('disconnects the streams on destroy', () => {
    create().destroy();
    expect(docs.disconnectStreams).toHaveBeenCalled();
  });

  it('shows a loading spinner while loading', () => {
    docs.loading.set(true);
    const f = create();
    expect(f.nativeElement.querySelector('.spinner-border')).not.toBeNull();
  });

  it('shows the default empty-state message', () => {
    const f = create();
    expect(f.nativeElement.querySelector('[data-testid="empty-state"]').textContent).toContain(
      'No documents uploaded yet',
    );
  });

  it('shows the no-match empty-state message while searching', () => {
    docs.currentSearch.set('q');
    const f = create();
    expect(f.nativeElement.querySelector('[data-testid="empty-state"]').textContent).toContain(
      'No documents match your search',
    );
  });

  it('renders a card per visible document', () => {
    docs.visible.set([summary({ id: '1', fileName: 'x.pdf' })]);
    const f = create();
    expect(f.nativeElement.querySelectorAll('[data-testid="document-card"]').length).toBe(1);
  });

  it('uploads every file emitted by the upload zone, sequentially', async () => {
    const f = create();
    const a = new File(['x'], 'a.pdf', { type: 'application/pdf' });
    const b = new File(['x'], 'b.pdf', { type: 'application/pdf' });
    const zone = f.debugElement.query(By.directive(UploadZoneComponent))
      .componentInstance as UploadZoneComponent;
    zone.filesSelected.emit([a, b]);
    await f.whenStable();
    expect(docs.upload).toHaveBeenCalledTimes(2);
    expect(docs.upload).toHaveBeenLastCalledWith(b);
  });

  it('removes a document when a card emits delete', () => {
    docs.visible.set([summary({ id: 'c1', fileName: 'card.pdf' })]);
    const f = create();
    (f.nativeElement.querySelector('[data-testid="doc-delete"]') as HTMLButtonElement).click();
    expect(docs.remove).toHaveBeenCalledWith('c1');
  });

  it('refresh triggers a reload', () => {
    const f = create();
    docs.load.mockClear();
    (f.nativeElement.querySelector('[data-testid="refresh-btn"]') as HTMLButtonElement).click();
    expect(docs.load).toHaveBeenCalled();
  });

  it('search button passes the entered term to the service', () => {
    const f = create();
    const input = f.nativeElement.querySelector('[data-testid="search-input"]') as HTMLInputElement;
    input.value = 'hello';
    input.dispatchEvent(new Event('input'));
    (f.nativeElement.querySelector('[data-testid="search-btn"]') as HTMLButtonElement).click();
    expect(docs.search).toHaveBeenCalledWith('hello');
  });

  it('pressing Enter in the search box searches', () => {
    const f = create();
    const input = f.nativeElement.querySelector('[data-testid="search-input"]') as HTMLInputElement;
    input.value = 'kw';
    input.dispatchEvent(new Event('input'));
    input.dispatchEvent(new KeyboardEvent('keyup', { key: 'Enter' }));
    expect(docs.search).toHaveBeenCalledWith('kw');
  });

  it('clear resets the term and clears the search', () => {
    const f = create();
    (f.nativeElement.querySelector('[data-testid="clear-btn"]') as HTMLButtonElement).click();
    expect(docs.clearSearch).toHaveBeenCalled();
  });
});
