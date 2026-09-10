import type { Routes } from '@angular/router';

import { PainelMensalDespesasComponent } from './features/painel-mensal-despesas/painel-mensal-despesas.component';
import { CadastroDespesaRecorrenteComponent } from './features/despesa-recorrente/cadastro-despesa-recorrente/cadastro-despesa-recorrente.component';
import { EditarDespesaRecorrenteComponent } from './features/despesa-recorrente/editar-despesa-recorrente/editar-despesa-recorrente.component';

export const routes: Routes = [
  { path: '', component: PainelMensalDespesasComponent },
  { path: 'despesas/nova', component: CadastroDespesaRecorrenteComponent },
  { path: 'despesas/:id/editar', component: EditarDespesaRecorrenteComponent },
];
