# FreezerAnalysis Interface Direction

## World

An Italian match-day control sheet: the disciplined structure of a range officer's schedule combined with a competitor's pocket field notebook. The UI treats dates, stages, uncertainty, and revisions as working evidence rather than promotional metrics.

## First viewport

The opening screen shows the next action and the user's match ledger together. A compact header identifies the active account and connection state; the main column begins with the next match or an honest empty state, then a readable list of imported competitions and their revision/completeness. Import is a real action in this first view, not a decorative hero.

## User path

Import and review results, set the private shooter profile, inspect the estimate and its diagnostics, edit stage notes, freeze a reproducible snapshot, then switch to a high-contrast field view that remains useful offline. Verification and parameter proposals stay visibly separate from the frozen baseline.

## Signature interaction

The stage ledger can move from preparation to field use without losing its structure: stage order, start turn, and reconnaissance updates become a separate rendering context while the frozen estimate remains fixed. Offline field actions queue with stable operation IDs and reconcile visibly after reconnect.

## Materials and palette

Warm paper (`#f4f1e8`), deep ink (`#17211e`), slate (`#52605a`), and a restrained signal vermilion (`#a94720`) distinguish action and attention. Thin ruled lines and tabular numerals reference printed competition sheets; no texture is used behind dense data.

## Typography and layout

Use the system sans stack for reliable field rendering, with tabular numerals for scores and times. Desktop uses a narrow navigation rail and broad working ledger; mobile uses a compact top bar and persistent bottom navigation. Stage and event rows are semantic lists/tables, not nested cards. Main actions and form controls meet 44 CSS-pixel touch targets.

## Honest risk

An unavailable estimate must not resemble a result. Provisional imports, unresolved identities, conflicts, queued field events, and offline state use explicit words and icons in addition to color. The interface never claims original-Python parity or successful verification without evidence.
