import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';

import { EditarDespesaRecorrenteComponent } from './editar-despesa-recorrente.component';
import { routes } from '../../../app.routes';
import type { ApiEnvelope, RecurringExpenseDetailResponse } from '../despesa-recorrente.model';

const DESPESA_ID = 'abc-123';

const DETAIL: RecurringExpenseDetailResponse = {
  id: DESPESA_ID,
  name: 'Aluguel',
  category: 'Housing',
  monthlyAmount: 1500,
  dueDay: 10,
  startDate: '2025-03-10',
  frequency: 'Monthly',
  status: 'Active',
  note: 'Contrato nº 1234',
};

function setInputValue(el: HTMLInputElement | HTMLTextAreaElement, value: string): void {
  el.value = value;
  el.dispatchEvent(new Event('input'));
}

describe('EditarDespesaRecorrenteComponent', () => {
  let httpMock: HttpTestingController;
  let fixture: ReturnType<typeof TestBed.createComponent<EditarDespesaRecorrenteComponent>>;
  let root: HTMLElement;

  function setUp(id: string = DESPESA_ID): void {
    TestBed.configureTestingModule({
      imports: [EditarDespesaRecorrenteComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter(routes),
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id }) } } },
      ],
    });
    fixture = TestBed.createComponent(EditarDespesaRecorrenteComponent);
    root = fixture.nativeElement as HTMLElement;
    httpMock = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  }

  function flushGet(body: ApiEnvelope<RecurringExpenseDetailResponse> = { success: true, data: DETAIL, errors: null }): void {
    const req = httpMock.expectOne(`/api/v1/recurring-expenses/${DESPESA_ID}`);
    expect(req.request.method).toBe('GET');
    req.flush(body);
    fixture.detectChanges();
  }

  afterEach(() => {
    httpMock.verify();
  });

  describe('loading and pre-filling (FR-003, FR-004)', () => {
    beforeEach(() => setUp());

    it('shows a loading state and no form fields before the GET resolves', () => {
      expect(root.querySelector('[data-testid="loading-view"]')).toBeTruthy();
      expect(root.querySelector('[data-testid="nome-input"]')).toBeFalsy();
      flushGet();
    });

    it('GETs the despesa by the route id and pre-fills every editable field on success', () => {
      flushGet();

      const c = fixture.componentInstance;
      expect(c.nome()).toBe('Aluguel');
      expect(c.categoria()).toBe('Housing');
      expect(c.valor()).toBe('1.500,00');
      expect(c.dia()).toBe('10');
      expect(c.dataInicio()).toBe('2025-03-10');
      expect(c.status()).toBe('ativa');
      expect(c.observacao()).toBe('Contrato nº 1234');
      expect(root.querySelector('[data-testid="loading-view"]')).toBeFalsy();
      expect(root.querySelector<HTMLInputElement>('[data-testid="nome-input"]')!.value).toBe('Aluguel');
    });
  });

  describe('not found and load-error states (FR-005, FR-006)', () => {
    beforeEach(() => setUp());

    it('shows a dedicated not-found message on a 404, with no form', () => {
      const req = httpMock.expectOne(`/api/v1/recurring-expenses/${DESPESA_ID}`);
      req.flush({ success: false, data: null, errors: null }, { status: 404, statusText: 'Not Found' });
      fixture.detectChanges();

      expect(root.querySelector('[data-testid="not-found-view"]')).toBeTruthy();
      expect(root.querySelector('[data-testid="nome-input"]')).toBeFalsy();
    });

    it('shows a load-error banner with retry on a network/5xx failure, and retrying re-issues the GET', () => {
      let req = httpMock.expectOne(`/api/v1/recurring-expenses/${DESPESA_ID}`);
      req.error(new ProgressEvent('error'), { status: 0, statusText: 'Unknown Error' });
      fixture.detectChanges();

      expect(root.querySelector('[data-testid="load-error-banner"]')).toBeTruthy();

      root.querySelector<HTMLButtonElement>('[data-testid="tentar-novamente-carregar-btn"]')!.click();
      fixture.detectChanges();

      req = httpMock.expectOne(`/api/v1/recurring-expenses/${DESPESA_ID}`);
      req.flush({ success: true, data: DETAIL, errors: null });
      fixture.detectChanges();

      expect(root.querySelector('[data-testid="load-error-banner"]')).toBeFalsy();
      expect(root.querySelector<HTMLInputElement>('[data-testid="nome-input"]')!.value).toBe('Aluguel');
    });
  });

  describe('editing and saving (FR-007-FR-012, FR-015, US1)', () => {
    beforeEach(() => {
      setUp();
      flushGet();
    });

    it('updates the preview in real time as fields change', () => {
      const c = fixture.componentInstance;
      setInputValue(root.querySelector('[data-testid="nome-input"]')!, 'Aluguel novo');
      expect(c.nomePreview()).toBe('Aluguel novo');
    });

    it('sends a PUT with the edited payload and shows a success confirmation without a "cadastrar outra despesa" action (FR-010)', () => {
      setInputValue(root.querySelector('[data-testid="valor-input"]')!, '160000');
      fixture.detectChanges();

      root.querySelector<HTMLButtonElement>('[data-testid="salvar-btn"]')!.click();
      fixture.detectChanges();

      expect(fixture.componentInstance.formStatus()).toBe('loading');
      expect(root.querySelector<HTMLButtonElement>('[data-testid="salvar-btn"]')!.textContent).toContain('Salvando');

      const req = httpMock.expectOne(`/api/v1/recurring-expenses/${DESPESA_ID}`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual({
        name: 'Aluguel',
        category: 'Housing',
        monthlyAmount: 1600,
        dueDay: 10,
        startDate: '2025-03-10',
        status: 'Active',
        note: 'Contrato nº 1234',
      });

      req.flush({ success: true, data: { ...DETAIL, monthlyAmount: 1600 }, errors: null });
      fixture.detectChanges();

      expect(fixture.componentInstance.formStatus()).toBe('success');
      expect(root.querySelector('[data-testid="success-view"]')).toBeTruthy();
      expect(root.textContent).toContain('Aluguel');
      expect(root.querySelector('[data-testid="nova-despesa-btn"]')).toBeFalsy();
      expect(root.querySelector('[data-testid="voltar-painel-sucesso-btn"]')).toBeTruthy();
    });

    it('blocks submission and reveals every invalid field at once when the form is invalid', () => {
      setInputValue(root.querySelector('[data-testid="nome-input"]')!, '');
      fixture.detectChanges();

      root.querySelector<HTMLButtonElement>('[data-testid="salvar-btn"]')!.click();
      fixture.detectChanges();

      expect(root.querySelector('[data-testid="nome-error"]')).toBeTruthy();
      expect(root.querySelector('[data-testid="corrigir-banner"]')).toBeTruthy();
      httpMock.expectNone(`/api/v1/recurring-expenses/${DESPESA_ID}`);
    });

    it('saving without changing any field still succeeds (FR-015, no-op)', () => {
      root.querySelector<HTMLButtonElement>('[data-testid="salvar-btn"]')!.click();
      fixture.detectChanges();

      const req = httpMock.expectOne(`/api/v1/recurring-expenses/${DESPESA_ID}`);
      req.flush({ success: true, data: DETAIL, errors: null });
      fixture.detectChanges();

      expect(fixture.componentInstance.formStatus()).toBe('success');
    });

    it('shows the persistent reactivation helper text while status is "Ativa", updating immediately on toggle (FR-016)', () => {
      const c = fixture.componentInstance;
      expect(c.statusHelperLabel().toLowerCase()).toContain('gerada');

      root.querySelector<HTMLButtonElement>('[data-testid="status-pausada-btn"]')!.click();
      fixture.detectChanges();
      expect(c.statusHelperLabel().toLowerCase()).toContain('pausada');

      root.querySelector<HTMLButtonElement>('[data-testid="status-ativa-btn"]')!.click();
      fixture.detectChanges();
      expect(c.statusHelperLabel().toLowerCase()).toContain('gerada');
    });
  });

  describe('save-failure handling (FR-011, FR-012, US4)', () => {
    beforeEach(() => {
      setUp();
      flushGet();
    });

    it('shows an error banner and preserves the edited field values on a network/5xx failure, and retries on click', () => {
      setInputValue(root.querySelector('[data-testid="valor-input"]')!, '160000');
      fixture.detectChanges();
      root.querySelector<HTMLButtonElement>('[data-testid="salvar-btn"]')!.click();
      fixture.detectChanges();

      let req = httpMock.expectOne(`/api/v1/recurring-expenses/${DESPESA_ID}`);
      req.flush('Internal Server Error', { status: 500, statusText: 'Internal Server Error' });
      fixture.detectChanges();

      expect(fixture.componentInstance.formStatus()).toBe('error');
      expect(root.querySelector('[data-testid="erro-banner"]')).toBeTruthy();
      expect(root.querySelector<HTMLInputElement>('[data-testid="valor-input"]')!.value).toBe('1.600,00');

      root.querySelector<HTMLButtonElement>('[data-testid="tentar-novamente-btn"]')!.click();
      fixture.detectChanges();

      req = httpMock.expectOne(`/api/v1/recurring-expenses/${DESPESA_ID}`);
      expect(req.request.body.monthlyAmount).toBe(1600);
      req.flush({ success: true, data: { ...DETAIL, monthlyAmount: 1600 }, errors: null });
    });

    it('maps a 400 field-validation error onto the corresponding field inline (FR-012)', () => {
      root.querySelector<HTMLButtonElement>('[data-testid="salvar-btn"]')!.click();
      fixture.detectChanges();

      const req = httpMock.expectOne(`/api/v1/recurring-expenses/${DESPESA_ID}`);
      req.flush(
        { success: false, data: null, errors: [{ field: 'name', message: 'Já existe uma despesa recorrente com esse nome.' }] },
        { status: 400, statusText: 'Bad Request' },
      );
      fixture.detectChanges();

      expect(root.querySelector('[data-testid="nome-error"]')?.textContent).toContain(
        'Já existe uma despesa recorrente com esse nome.',
      );
    });
  });

  describe('unsaved-changes confirmation (FR-013, FR-014, US3)', () => {
    beforeEach(() => {
      setUp();
      flushGet();
    });

    it('shows the exit-confirmation dialog when a field differs from its loaded value', () => {
      const router = TestBed.inject(Router);
      const navigateSpy = vi.spyOn(router, 'navigateByUrl');

      setInputValue(root.querySelector('[data-testid="nome-input"]')!, 'Aluguel novo');
      fixture.detectChanges();

      root.querySelector<HTMLButtonElement>('[data-testid="voltar-painel-btn"]')!.click();
      fixture.detectChanges();

      expect(navigateSpy).not.toHaveBeenCalled();
      expect(root.querySelector('[data-testid="exit-confirm-overlay"]')).toBeTruthy();
    });

    it('shows no confirmation when a changed field is manually reverted to its originally-loaded value (value-based comparison)', () => {
      const router = TestBed.inject(Router);
      const navigateSpy = vi.spyOn(router, 'navigateByUrl');

      const nomeInput = root.querySelector<HTMLInputElement>('[data-testid="nome-input"]')!;
      setInputValue(nomeInput, 'Aluguel novo');
      fixture.detectChanges();
      setInputValue(nomeInput, 'Aluguel');
      fixture.detectChanges();

      root.querySelector<HTMLButtonElement>('[data-testid="voltar-painel-btn"]')!.click();
      fixture.detectChanges();

      expect(navigateSpy).toHaveBeenCalledWith('/');
      expect(root.querySelector('[data-testid="exit-confirm-overlay"]')).toBeFalsy();
    });

    it('shows no confirmation when leaving without having changed anything', () => {
      const router = TestBed.inject(Router);
      const navigateSpy = vi.spyOn(router, 'navigateByUrl');

      root.querySelector<HTMLButtonElement>('[data-testid="voltar-painel-btn"]')!.click();
      fixture.detectChanges();

      expect(navigateSpy).toHaveBeenCalledWith('/');
    });
  });
});
