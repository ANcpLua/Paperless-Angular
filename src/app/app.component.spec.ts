import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { AppComponent } from './app.component';
import { DocumentsService } from './features/documents/data/documents.service';

describe('AppComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [
        provideRouter([]),
        { provide: DocumentsService, useValue: { liveDisconnected: signal(false) } },
      ],
    }).compileComponents();
  });
  afterEach(() => vi.restoreAllMocks());

  it('renders the app shell and the notifications host', () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    const host = fixture.nativeElement as HTMLElement;
    expect(host.querySelector('app-shell')).not.toBeNull();
    expect(host.querySelector('app-notifications')).not.toBeNull();
  });
});
