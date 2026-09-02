# Research: Cadastro de Despesa Recorrente (Frontend)

**Feature**: `002-cadastro-despesa-recorrente` | **Date**: 2026-09-02

This feature has no `NEEDS CLARIFICATION` markers left in the Technical
Context — the spec's own Clarifications session already resolved the three
open product questions (name length limit, synchronous preview, no client
timeout). The research below resolves the remaining *technical* decisions
needed to fill in the Technical Context and Project Structure sections,
building on the component/state/contract design already worked out in
[`refinements/frontend/cadastro-despesa-recorrente.md`](../../refinements/frontend/cadastro-despesa-recorrente.md).

## 1. Frontend project does not exist yet

**Decision**: This feature creates a brand-new Angular v22 workspace at
`frontend/` (repository root, sibling of `backend/`), scaffolded with the
Angular CLI defaults.

**Rationale**: `git status`/repo inspection at planning time shows only
`backend/` (Domain + Domain.Tests projects), `design/`, and `refinements/`
exist — no `frontend/` directory. The refinement document already proposes
this exact path (`frontend/src/app/features/despesa-recorrente/...`),
consistent with Constitution Principle VII (feature-folder organization).

**Alternatives considered**: Placing the app under a generic `client/` or
`web/` folder — rejected, no such convention exists in this repo and
`frontend/` mirrors the existing `backend/` naming.

## 2. Test runner: Vitest

**Decision**: Use Vitest, via the Angular CLI's current default
(`ng new` sets `testRunner: vitest`, builder `@angular/build:unit-test`).

**Rationale**: Constitution Principle II allows "Vitest or Jasmine/Karma,
whichever is configured for the project." Since this is a greenfield
workspace, the config that gets created is whatever `ng new` produces today,
and Angular's CLI has made Vitest the stable, production-ready default for
new projects. Adopting the CLI default avoids fighting the tool.

**Alternatives considered**: Jasmine/Karma (the long-standing prior
default) — rejected; it is no longer what `ng new` scaffolds by default, and
picking it would mean extra manual reconfiguration for no functional
benefit on a new project.

## 3. Currency parsing & display formatting

**Decision**: A single `valorFmt` computed signal formats the parsed number
with `Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })`.
Parsing raw typed text into a `number` (for both validation and display) is
a small hand-written helper, not a library.

**Rationale**: The refinement document already rules out the design
prototype's manual `formatBRL` string function in favor of
`Intl.NumberFormat`/`CurrencyPipe`. `Intl.NumberFormat` is chosen over
`CurrencyPipe` specifically because the formatted value is consumed by a
`computed()` used by the preview card (a plain string binding), keeping all
pt-BR formatting logic in one place rather than splitting it between a
template pipe and the validation/parsing logic that also needs the parsed
number.

**Alternatives considered**: `CurrencyPipe` in the template — rejected for
the reason above (would duplicate the formatting rule); a third-party
money-formatting library — rejected per Constitution Principle V (YAGNI) and
the AI Agent Guardrails (no unjustified new dependency).

## 4. Date input parsing (`dd/mm/aaaa`)

**Decision**: Hand-rolled parsing of the three numeric parts followed by
constructing a `Date`/`DateOnly`-equivalent check that the resulting
calendar date round-trips (rejects e.g. `31/02/2026`), with no date library.

**Rationale**: FR-006 only requires "a valid calendar date," not date
arithmetic, timezone handling, or locale-aware formatting elsewhere in this
feature. Constitution Principle V (Simplicity) disfavors adding a dependency
for a single validity check.

**Alternatives considered**: `date-fns`/`luxon` — rejected as
disproportionate to the single validation rule needed; would also trigger
the AI Agent Guardrails' new-dependency approval step for no real benefit.

## 5. Mapping `400` field errors to inline messages

**Decision**: A small local lookup translates each `{ field, message }` item
in the `400` envelope's `errors` array into the corresponding client field's
error-message signal, by matching `field` against the known API field names
(`name`, `category`, `monthlyAmount`, `dueDay`, `startDate`). Any error entry
whose `field` doesn't match one of these, or any non-`400`/network failure,
falls back to the generic error banner (`formStatus = 'error'`) per FR-016.

**Rationale**: Directly matches the refinement document's accepted
assumption ("mapear os erros do envelope 400 para as mesmas mensagens
inline... caindo no banner genérico apenas para erros que não apontam um
campo conhecido"). No new dependency or abstraction is needed — it's a
5-branch lookup.

**Alternatives considered**: A generic error-mapping library/framework —
rejected as unjustified complexity for 5 known fields (Principle V).

## 6. Validating the flow without a live backend

**Decision**: Automated tests use `HttpTestingController`
(`@angular/common/http/testing`, part of the Angular framework already
mandated by the constitution — no new dependency) to simulate `201`, `400`,
and network-failure responses for `POST /api/recurring-expenses`, since no
such endpoint exists yet (`backend/` currently contains only the `Domain`
project — see [`specs/001-despesa-recorrente-domain`](../001-despesa-recorrente-domain/)).
The manual quickstart smoke test can exercise every client-only behavior
(validation, preview, submit-blocking) plus the network-failure path (by
pointing at an unreachable URL), but **cannot** manually exercise a real
`201`/`400` response until a future backend feature implements the
Application/API layer described in [`contracts/api-contract.md`](contracts/api-contract.md).

**Rationale**: Matches Constitution Principle I (frontend consumes the
system only through HTTP APIs — never a mock database or direct backend
call) while being honest about what a feature with no backend counterpart
yet can and cannot prove manually. This is the same forward-looking-contract
pattern already used between this feature and
`specs/001-despesa-recorrente-domain`.

**Alternatives considered**: Standing up a throwaway in-memory HTTP stub
(e.g., `angular-in-memory-web-api`) for manual testing — rejected as a new
dependency to satisfy a manual smoke test only, not required by any
acceptance scenario; `HttpTestingController` already gives full automated
coverage of every response shape.

## 7. New dependency introduced by this feature: Tailwind CSS

**Decision**: Install Tailwind CSS following the official Angular guide
(`angular.dev/guide/tailwind`): `tailwindcss`, `@tailwindcss/postcss`, and
`postcss` as dev dependencies, a `.postcssrc.json`, and
`@import "tailwindcss";` in `frontend/src/styles.css`.

**Rationale**: Constitution Principle VII already mandates Tailwind as the
project's default styling technology — this feature is simply the first one
to scaffold the frontend workspace, so it's also the first to add the
dependency. Per the constitution's **AI Agent Guardrails**, this must be
named and justified in the plan (done here) before it is added to
`package.json`, and requires explicit human validation of this plan update
before implementation proceeds.

**Alternatives considered**: Hand-written SCSS per Principle VII's
escape hatch — rejected; nothing in this screen's styling is "demonstrably
insufficient" for Tailwind utilities, so the default applies.

## 8. New dependency introduced during implementation: `@angular-eslint/schematics`

**Decision**: Run `ng add @angular-eslint/schematics` against the scaffolded
`frontend/` workspace, accepting the dev dependencies it installs
(`angular-eslint`, `eslint`, `typescript-eslint`) and the `eslint.config.js`
+ `lint` architect target it generates.

**Rationale**: This plan originally assumed `ng new` would scaffold an
ESLint config, per Constitution Principle III's mandate. During `/speckit-implement`
(T004), the actual Angular v22 CLI output showed no `eslint.config.js` and
no `lint` target in `angular.json` — `ng new` no longer includes linting by
default. `ng add @angular-eslint/schematics` is the CLI-maintained,
first-party way to add strict TypeScript-aware linting to an existing
Angular standalone-component workspace, avoiding a hand-rolled ESLint
config that would drift from Angular's own recommended rules.

**Alternatives considered**: Hand-writing `eslint.config.js` directly
against `typescript-eslint` without the Angular-specific plugin — rejected;
would miss Angular-specific rules (template binding checks, standalone
component conventions) that `angular-eslint` provides for free and keeps in
sync with the framework version.

## 9. New dependency introduced during implementation: `@vitest/coverage-v8`

**Decision**: Install `@vitest/coverage-v8` as a dev dependency and run
`ng test --coverage` (T044) to measure and report line coverage on the
feature's critical paths.

**Rationale**: Constitution Principle II requires frontend unit tests to
meet "the project's minimum line coverage threshold," but no numeric
threshold is documented anywhere in this repo yet — this is the first
frontend feature, so there is no prior baseline to check against. Producing
an actual coverage number (rather than asserting compliance by test-count
alone) needs a coverage provider; `@vitest/coverage-v8` is Vitest's
first-party, V8-based provider and requires no additional configuration
beyond enabling it.

**Alternatives considered**: `@vitest/coverage-istanbul` — rejected in
favor of the V8-based provider, which needs no source instrumentation step
and is Vitest's documented default recommendation.

## 10. Enforcing the 100-character Nome limit natively (spec update)

**Decision**: The `nome` `<input>` in
`cadastro-despesa-recorrente.component.html` MUST carry a native
`maxlength="100"` attribute, so the browser itself refuses further typed or
pasted characters once the field reaches 100. `nomeError`'s existing
length check (`> 100 characters`) stays in place as a defense-in-depth
backstop (e.g. against programmatic value assignment that bypasses the
DOM's own input handling), not as the primary enforcement mechanism.

**Rationale**: The spec's Clarifications session (Q5) resolved this
explicitly — the field "DEVE impedir, via atributo de limite do input" — a
native HTML attribute is exactly what's being asked for, and evergreen
browsers already enforce `maxlength` on both typed input and paste, so no
custom `(input)`/`(paste)` truncation logic is needed. This was not yet
reflected in `data-model.md` when the frontend was first implemented
(confirmed absent from the current `cadastro-despesa-recorrente.component.html`
as of this plan update) and must be added during the next implementation
pass.

**Alternatives considered**: Truncating the value in the existing
`onNomeInput($event)` handler — rejected as redundant extra logic for
something the native `maxlength` attribute already guarantees, and it would
still need `maxlength` on the element anyway for non-JS-mediated paste
behavior in some browsers.

## 11. Reproducing design tokens (colors, typography) via Tailwind v4 `@theme` (spec update)

**Decision**: Define the design's color/typography tokens once, globally, in
`frontend/src/styles.css`, using Tailwind v4's CSS-first `@theme` directive
(the project already uses the v4 `@import "tailwindcss";` form — see §7 —
not a `tailwind.config.js`-based v3 setup):

```css
@import "tailwindcss";
@import url('https://fonts.googleapis.com/css2?family=Sora:wght@600;700;800&family=Public+Sans:wght@400;500;600;700&display=swap');

@theme {
  --color-accent: #2E6FF2;
  --color-accent-hover: #1B4FCB;
  --font-heading: "Sora", sans-serif;
  --font-sans: "Public Sans", system-ui, sans-serif;
}
```

This makes `bg-accent`, `text-accent`, `border-accent`, `font-heading`, and
`font-sans` (the latter overriding Tailwind's default `sans` stack app-wide)
available as ordinary utility classes everywhere in the app, generated by
Tailwind from the token — not a value copy-pasted per component. The
component templates then use `bg-accent`/`text-accent`/`border-accent` (and
equivalents for the design's other fixed hexes — error red `#D92D20`,
success green `#0F7B4E`, borders `#D0D5DD`/`#E4E7EC`, muted text `#667085` —
added to the same `@theme` block under descriptive names, e.g.
`--color-danger`, `--color-success`, `--color-border`, `--color-muted`)
instead of Tailwind's generic default palette (`blue-600`, `red-600`,
`slate-*`) the current implementation uses, and instead of any inline
`style="color: #2E6FF2"` copied from `design/Cadastro.dc.html`.

**Rationale**: Spec Clarification #6 requires strict fidelity to the
design's exact colors/typography via Tailwind tokens, "nunca copiando
estilos inline literalmente"; Clarification #9 additionally requires the
accent color specifically to come from "um token de tema compartilhado da
aplicação," not a value fixed/duplicated inside this feature. Since this is
the first frontend feature and no shared theme file exists yet (Project
Structure decision: this feature is the only frontend code so far), this
plan establishes that shared token at the application root (`styles.css`,
loaded by every future feature) rather than inside
`features/despesa-recorrente/`, so later features reuse the same token
instead of redefining it. Tailwind v4's `@theme` is the framework's own
mechanism for this — no new dependency, no `tailwind.config.js` needed
alongside the already-adopted CSS-first setup.

**Alternatives considered**: Hardcoding `#2E6FF2` via Tailwind's arbitrary-
value syntax (`bg-[#2E6FF2]`) directly in the feature's component templates
— rejected; satisfies "use Tailwind, not inline styles" but not "shared
theme token, not a value fixed/duplicated in this feature" (Clarification
#9). A `tailwind.config.js` `theme.extend` block — rejected; this project's
Tailwind v4 setup is already CSS-first (`@import "tailwindcss";`, no config
file present in `frontend/`), and mixing in a JS config file for tokens
alone would be an unjustified second configuration mechanism (Principle V).

## 12. Responsive/mobile layout strategy (spec update)

**Decision**: The two-column layout (form + preview card side by side) uses
Tailwind's responsive prefixes to *start* single-column (`flex flex-col
gap-6`) and switch to the design's side-by-side arrangement only from a
`lg:` breakpoint up (`lg:flex-row lg:items-start lg:gap-8`), rather than
relying on the design prototype's bare `flex-wrap` (which only reflows once
the row runs out of horizontal space and doesn't guarantee usability at
narrow widths). Field pairs the design lays out side-by-side on one row
(Categoria/Valor, Dia/Data de início) similarly collapse to one column below
`sm:` (`grid grid-cols-1 sm:grid-cols-2 gap-3.5`). No new dependency —
Tailwind's existing responsive-prefix system already covers this.

**Rationale**: Spec Clarification #8 and FR-019/SC-006 require the screen to
stay usable, WCAG 2.1 AA-compliant, and free of horizontal scroll from
360px viewport width up, explicitly going beyond the design's own
wide-viewport-only `flex-wrap` behavior (spec Edge Cases). Starting the
layout single-column and opting into the wide two-column arrangement above a
breakpoint (mobile-first, Tailwind's default responsive direction)
guarantees narrow viewports never depend on wrap timing.

**Alternatives considered**: Keeping the design's plain `flex-wrap` and
adding a `min-width` per column (as in the prototype's inline
`min-width: 340px` / `min-width: 280px`) — rejected; at 360px a `340px`-plus
form column alone would already force horizontal scroll, violating SC-006.
A dedicated mobile stylesheet/breakpoint file — rejected as unjustified
complexity (Principle V) when Tailwind's responsive prefixes already express
this in the same utility classes.

## 13. Application header confirmed out of scope (spec update)

**Decision**: `CadastroDespesaRecorrenteComponent`'s template does not
render the "ContasEmDia" logo/icon header block shown at the top of
`design/Cadastro.dc.html` (lines 31–37 of that file). The component tree
starts at the "Nova despesa recorrente" `<h1>` and its subtitle.

**Rationale**: Spec Clarification #7 places that header in a shared
application shell/layout outside this feature's scope; this feature's
Project Structure (no routing, no shell component) confirms no such shell
exists yet to render it. Documenting the boundary here prevents a future
implementation pass from copying the header block along with the rest of
the design file's markup.

**Alternatives considered**: Implementing a minimal local copy of the
header inside this feature "for completeness" — rejected; spec Clarification
#7 explicitly assigns it to a future shared shell, and duplicating it here
would need to be deleted/reconciled once that shell exists (Principle V).
