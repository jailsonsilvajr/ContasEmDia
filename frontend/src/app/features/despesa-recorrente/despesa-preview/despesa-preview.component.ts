import { Component, input } from '@angular/core';

@Component({
  selector: 'app-despesa-preview',
  imports: [],
  templateUrl: './despesa-preview.component.html',
})
export class DespesaPreviewComponent {
  readonly nome = input.required<string>();
  readonly categoriaLabel = input.required<string>();
  readonly catColor = input.required<string>();
  readonly valorFmt = input.required<string>();
  readonly diaLabel = input.required<string>();
  readonly statusHelperLabel = input.required<string>();
}
