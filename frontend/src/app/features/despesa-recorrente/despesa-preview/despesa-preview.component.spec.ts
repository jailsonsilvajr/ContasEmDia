import { TestBed } from '@angular/core/testing';
import { describe, it, expect, beforeEach } from 'vitest';

import { DespesaPreviewComponent } from './despesa-preview.component';

describe('DespesaPreviewComponent', () => {
  let fixture: ReturnType<typeof TestBed.createComponent<DespesaPreviewComponent>>;
  let root: HTMLElement;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [DespesaPreviewComponent],
    });
    fixture = TestBed.createComponent(DespesaPreviewComponent);
    root = fixture.nativeElement as HTMLElement;
  });

  it('renders exactly whatever is passed via its inputs, with no logic of its own', () => {
    fixture.componentRef.setInput('nome', 'Netflix');
    fixture.componentRef.setInput('categoriaLabel', 'Assinaturas');
    fixture.componentRef.setInput('catColor', '#7A5AF8');
    fixture.componentRef.setInput('valorFmt', 'R$ 39,90');
    fixture.componentRef.setInput('diaLabel', 'Dia 15');
    fixture.componentRef.setInput('statusHelperLabel', 'Ativa — a ocorrência deste mês será gerada automaticamente.');
    fixture.detectChanges();

    expect(root.textContent).toContain('Netflix');
    expect(root.textContent).toContain('Assinaturas');
    expect(root.textContent).toContain('R$ 39,90');
    expect(root.textContent).toContain('Dia 15');
    expect(root.textContent).toContain('Ativa — a ocorrência deste mês será gerada automaticamente.');
  });
});
