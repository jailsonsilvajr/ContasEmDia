import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { describe, it, expect, beforeEach, afterEach } from 'vitest';

import { CadastroDespesaRecorrenteComponent } from './cadastro-despesa-recorrente.component';
import type { CreateRecurringExpenseResponse } from '../despesa-recorrente.model';

function setInputValue(el: HTMLInputElement | HTMLTextAreaElement, value: string): void {
  el.value = value;
  el.dispatchEvent(new Event('input'));
}

function blur(el: HTMLElement): void {
  el.dispatchEvent(new Event('blur'));
}

function fillValidForm(root: HTMLElement): void {
  setInputValue(root.querySelector('[data-testid="nome-input"]')!, 'Aluguel');
  const categoriaSelect = root.querySelector<HTMLSelectElement>('[data-testid="categoria-select"]')!;
  categoriaSelect.value = 'Housing';
  categoriaSelect.dispatchEvent(new Event('change'));
  setInputValue(root.querySelector('[data-testid="valor-input"]')!, '1500,50');
  setInputValue(root.querySelector('[data-testid="dia-input"]')!, '5');
  setInputValue(root.querySelector('[data-testid="data-inicio-input"]')!, '01/09/2026');
}

describe('CadastroDespesaRecorrenteComponent', () => {
  let httpMock: HttpTestingController;
  let fixture: ReturnType<typeof TestBed.createComponent<CadastroDespesaRecorrenteComponent>>;
  let root: HTMLElement;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [CadastroDespesaRecorrenteComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    fixture = TestBed.createComponent(CadastroDespesaRecorrenteComponent);
    root = fixture.nativeElement as HTMLElement;
    httpMock = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('sends exactly one POST with the expected payload and shows the success confirmation with the submitted name on 201 (US1-1)', () => {
    fillValidForm(root);
    fixture.detectChanges();

    root.querySelector<HTMLButtonElement>('[data-testid="salvar-btn"]')!.click();
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/recurring-expenses');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      name: 'Aluguel',
      category: 'Housing',
      monthlyAmount: 1500.5,
      dueDay: 5,
      startDate: '2026-09-01',
      frequency: 'Monthly',
      status: 'Active',
      note: null,
    });

    const response: CreateRecurringExpenseResponse = {
      id: 'abc-123',
      name: 'Aluguel',
      category: 'Housing',
      monthlyAmount: 1500.5,
      dueDay: 5,
      startDate: '2026-09-01',
      frequency: 'Monthly',
      status: 'Active',
      note: null,
      occurrences: [],
    };
    req.flush(response, { status: 201, statusText: 'Created' });
    fixture.detectChanges();

    expect(fixture.componentInstance.formStatus()).toBe('success');
    expect(fixture.componentInstance.savedName()).toBe('Aluguel');
    expect(root.textContent).toContain('Aluguel');
  });

  it('resets every signal to its initial value when "Cadastrar outra despesa" is clicked after success (US1-2, FR-014)', () => {
    fillValidForm(root);
    fixture.detectChanges();
    root.querySelector<HTMLButtonElement>('[data-testid="salvar-btn"]')!.click();
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/recurring-expenses');
    req.flush(
      {
        id: 'abc-123',
        name: 'Aluguel',
        category: 'Housing',
        monthlyAmount: 1500.5,
        dueDay: 5,
        startDate: '2026-09-01',
        frequency: 'Monthly',
        status: 'Active',
        note: null,
        occurrences: [],
      },
      { status: 201, statusText: 'Created' },
    );
    fixture.detectChanges();

    root.querySelector<HTMLButtonElement>('[data-testid="nova-despesa-btn"]')!.click();
    fixture.detectChanges();

    const c = fixture.componentInstance;
    expect(c.nome()).toBe('');
    expect(c.categoria()).toBe('Housing');
    expect(c.valor()).toBe('');
    expect(c.dia()).toBe('');
    expect(c.dataInicio()).toBe('');
    expect(c.status()).toBe('ativa');
    expect(c.observacao()).toBe('');
    expect(c.formStatus()).toBe('idle');
    expect(c.savedName()).toBeNull();
    expect(c.submitErrorMessage()).toBeNull();
  });

  it('submits frequency: "Monthly" and status: "Active" when left untouched (US1-3, US1-4)', () => {
    setInputValue(root.querySelector('[data-testid="nome-input"]')!, 'Internet');
    setInputValue(root.querySelector('[data-testid="valor-input"]')!, '150');
    setInputValue(root.querySelector('[data-testid="dia-input"]')!, '10');
    setInputValue(root.querySelector('[data-testid="data-inicio-input"]')!, '01/09/2026');
    fixture.detectChanges();

    root.querySelector<HTMLButtonElement>('[data-testid="salvar-btn"]')!.click();
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/recurring-expenses');
    expect(req.request.body.frequency).toBe('Monthly');
    expect(req.request.body.status).toBe('Active');
    req.flush(
      {
        id: 'x',
        name: 'Internet',
        category: 'Housing',
        monthlyAmount: 150,
        dueDay: 10,
        startDate: '2026-09-01',
        frequency: 'Monthly',
        status: 'Active',
        note: null,
        occurrences: [],
      },
      { status: 201, statusText: 'Created' },
    );
  });

  it('updates the preview computed signals synchronously as fields change (US2-1..5, SC-003)', () => {
    const c = fixture.componentInstance;

    setInputValue(root.querySelector('[data-testid="nome-input"]')!, 'Netflix');
    expect(c.nomePreview()).toBe('Netflix');

    const categoriaSelect = root.querySelector<HTMLSelectElement>('[data-testid="categoria-select"]')!;
    categoriaSelect.value = 'Subscriptions';
    categoriaSelect.dispatchEvent(new Event('change'));
    expect(c.categoriaLabel()).toBe('Assinaturas');
    expect(c.catColor()).toBe('#7A5AF8');

    setInputValue(root.querySelector('[data-testid="valor-input"]')!, '39,90');
    expect(c.valorFmt()).toContain('39,90');

    setInputValue(root.querySelector('[data-testid="dia-input"]')!, '15');
    expect(c.diaLabel()).toBe('Dia 15');

    root.querySelector<HTMLButtonElement>('[data-testid="status-pausada-btn"]')!.click();
    expect(c.statusHelperLabel()).toContain('Pausada');

    root.querySelector<HTMLButtonElement>('[data-testid="status-ativa-btn"]')!.click();
    expect(c.statusHelperLabel()).toContain('Ativa');
  });

  it('shows "Nome da despesa" and "Dia --" placeholders when nome/dia are empty', () => {
    const c = fixture.componentInstance;
    expect(c.nomePreview()).toBe('Nome da despesa');
    expect(c.diaLabel()).toBe('Dia --');
  });

  it('carries a native maxlength="100" attribute on the nome input (spec Clarification #5, FR-002)', () => {
    const nomeInput = root.querySelector<HTMLInputElement>('[data-testid="nome-input"]')!;
    expect(nomeInput.getAttribute('maxlength')).toBe('100');
  });

  it('shows the required-field and max-length messages for nome on blur or submit (US3-1, US3-2, FR-002)', () => {
    const nomeInput = root.querySelector<HTMLInputElement>('[data-testid="nome-input"]')!;
    blur(nomeInput);
    fixture.detectChanges();
    expect(root.querySelector('[data-testid="nome-error"]')?.textContent).toContain('obrigat');

    setInputValue(nomeInput, 'a'.repeat(101));
    blur(nomeInput);
    fixture.detectChanges();
    expect(root.querySelector('[data-testid="nome-error"]')?.textContent).toContain('100');

    setInputValue(nomeInput, '');
    fixture.detectChanges();
    root.querySelector<HTMLButtonElement>('[data-testid="salvar-btn"]')!.click();
    fixture.detectChanges();
    expect(root.querySelector('[data-testid="nome-error"]')?.textContent).toContain('obrigat');
  });

  it('shows the corresponding message for valor <= 0, negative, or more than 2 decimals on blur or submit (US3-3, FR-004)', () => {
    const valorInput = root.querySelector<HTMLInputElement>('[data-testid="valor-input"]')!;
    setInputValue(valorInput, '0');
    blur(valorInput);
    fixture.detectChanges();
    expect(root.querySelector('[data-testid="valor-error"]')).toBeTruthy();

    setInputValue(valorInput, '-5');
    blur(valorInput);
    fixture.detectChanges();
    expect(root.querySelector('[data-testid="valor-error"]')).toBeTruthy();

    setInputValue(valorInput, '10,999');
    blur(valorInput);
    fixture.detectChanges();
    expect(root.querySelector('[data-testid="valor-error"]')).toBeTruthy();

    setInputValue(valorInput, '');
    fixture.detectChanges();
    root.querySelector<HTMLButtonElement>('[data-testid="salvar-btn"]')!.click();
    fixture.detectChanges();
    expect(root.querySelector('[data-testid="valor-error"]')).toBeTruthy();
  });

  it('shows the corresponding message for dia outside 1..31 on blur or submit (US3-4, FR-005)', () => {
    const diaInput = root.querySelector<HTMLInputElement>('[data-testid="dia-input"]')!;
    setInputValue(diaInput, '0');
    blur(diaInput);
    fixture.detectChanges();
    expect(root.querySelector('[data-testid="dia-error"]')).toBeTruthy();

    setInputValue(diaInput, '32');
    blur(diaInput);
    fixture.detectChanges();
    expect(root.querySelector('[data-testid="dia-error"]')).toBeTruthy();

    setInputValue(diaInput, '');
    fixture.detectChanges();
    root.querySelector<HTMLButtonElement>('[data-testid="salvar-btn"]')!.click();
    fixture.detectChanges();
    expect(root.querySelector('[data-testid="dia-error"]')).toBeTruthy();
  });

  it('shows the corresponding message for an invalid dataInicio on blur or submit (US3-5, FR-006)', () => {
    const dataInput = root.querySelector<HTMLInputElement>('[data-testid="data-inicio-input"]')!;
    setInputValue(dataInput, '31/02/2026');
    blur(dataInput);
    fixture.detectChanges();
    expect(root.querySelector('[data-testid="data-inicio-error"]')).toBeTruthy();

    setInputValue(dataInput, '');
    fixture.detectChanges();
    root.querySelector<HTMLButtonElement>('[data-testid="salvar-btn"]')!.click();
    fixture.detectChanges();
    expect(root.querySelector('[data-testid="data-inicio-error"]')).toBeTruthy();
  });

  it('reveals every invalid field at once and blocks submission when multiple fields are invalid (US3-6, FR-011, SC-002)', () => {
    root.querySelector<HTMLButtonElement>('[data-testid="salvar-btn"]')!.click();
    fixture.detectChanges();

    expect(root.querySelector('[data-testid="nome-error"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="valor-error"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="dia-error"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="data-inicio-error"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="corrigir-banner"]')).toBeTruthy();

    httpMock.expectNone('/api/recurring-expenses');
  });

  it('shows the error banner and retains all field values on a network/5xx failure (US4-1, FR-015, SC-004)', () => {
    fillValidForm(root);
    fixture.detectChanges();
    root.querySelector<HTMLButtonElement>('[data-testid="salvar-btn"]')!.click();
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/recurring-expenses');
    req.flush('Internal Server Error', { status: 500, statusText: 'Internal Server Error' });
    fixture.detectChanges();

    const c = fixture.componentInstance;
    expect(c.formStatus()).toBe('error');
    expect(root.querySelector('[data-testid="erro-banner"]')).toBeTruthy();
    expect(c.nome()).toBe('Aluguel');
    expect(c.valor()).toBe('1500,50');
    expect(c.dia()).toBe('5');
    expect(c.dataInicio()).toBe('01/09/2026');
  });

  it('resends the same payload when "Tentar novamente" is clicked, without requiring re-entry (US4-2, FR-017)', () => {
    fillValidForm(root);
    fixture.detectChanges();
    root.querySelector<HTMLButtonElement>('[data-testid="salvar-btn"]')!.click();
    fixture.detectChanges();

    const firstReq = httpMock.expectOne('/api/recurring-expenses');
    const firstBody = firstReq.request.body;
    firstReq.flush('Internal Server Error', { status: 500, statusText: 'Internal Server Error' });
    fixture.detectChanges();

    root.querySelector<HTMLButtonElement>('[data-testid="tentar-novamente-btn"]')!.click();
    fixture.detectChanges();

    const secondReq = httpMock.expectOne('/api/recurring-expenses');
    expect(secondReq.request.body).toEqual(firstBody);
    secondReq.flush(
      {
        id: 'x',
        name: 'Aluguel',
        category: 'Housing',
        monthlyAmount: 1500.5,
        dueDay: 5,
        startDate: '2026-09-01',
        frequency: 'Monthly',
        status: 'Active',
        note: null,
        occurrences: [],
      },
      { status: 201, statusText: 'Created' },
    );
  });

  it('populates the nome field inline error from a 400 field-error response (US4-3, FR-016)', () => {
    fillValidForm(root);
    fixture.detectChanges();
    root.querySelector<HTMLButtonElement>('[data-testid="salvar-btn"]')!.click();
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/recurring-expenses');
    req.flush(
      { errors: [{ field: 'name', message: 'Já existe uma despesa recorrente com esse nome.' }] },
      { status: 400, statusText: 'Bad Request' },
    );
    fixture.detectChanges();

    expect(root.querySelector('[data-testid="nome-error"]')?.textContent).toContain(
      'Já existe uma despesa recorrente com esse nome.',
    );
  });

  it('updates the observacao signal from the textarea and includes it (trimmed) in the payload (FR-009)', () => {
    fillValidForm(root);
    setInputValue(root.querySelector('[data-testid="observacao-textarea"]')!, '  Pagamento via cartão  ');
    fixture.detectChanges();

    expect(fixture.componentInstance.observacao()).toBe('  Pagamento via cartão  ');

    root.querySelector<HTMLButtonElement>('[data-testid="salvar-btn"]')!.click();
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/recurring-expenses');
    expect(req.request.body.note).toBe('Pagamento via cartão');
    req.flush(
      {
        id: 'x',
        name: 'Aluguel',
        category: 'Housing',
        monthlyAmount: 1500.5,
        dueDay: 5,
        startDate: '2026-09-01',
        frequency: 'Monthly',
        status: 'Active',
        note: 'Pagamento via cartão',
        occurrences: [],
      },
      { status: 201, statusText: 'Created' },
    );
  });

  it('sends exactly one request when "Salvar despesa" is double-clicked while loading (FR-013, Edge Cases)', () => {
    fillValidForm(root);
    fixture.detectChanges();

    const salvarBtn = root.querySelector<HTMLButtonElement>('[data-testid="salvar-btn"]')!;
    salvarBtn.click();
    fixture.detectChanges();
    salvarBtn.click();
    fixture.detectChanges();

    httpMock.expectOne('/api/recurring-expenses');
  });
});
