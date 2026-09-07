import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';

import { App } from './app';
import { routes } from './app.routes';
import { PainelMensalDespesasComponent } from './features/painel-mensal-despesas/painel-mensal-despesas.component';
import { CadastroDespesaRecorrenteComponent } from './features/despesa-recorrente/cadastro-despesa-recorrente/cadastro-despesa-recorrente.component';

describe('App', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter(routes)],
    }).compileComponents();
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('renders the painel mensal despesas screen at the root route', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/', PainelMensalDespesasComponent);

    httpMock.expectOne('/api/v1/occurrences').flush({
      success: true,
      data: { referencePeriod: { year: 2026, month: 8 }, occurrences: [] },
      errors: null,
    });
    harness.detectChanges();

    expect(harness.routeNativeElement?.textContent).toContain('ContasEmDia');
  });

  it('renders the cadastro de despesa recorrente screen at /despesas/nova', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/despesas/nova', CadastroDespesaRecorrenteComponent);
    harness.detectChanges();

    expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toContain('Nova despesa recorrente');
  });
});
