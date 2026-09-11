Type: prototype
Status: resolved
Blocked by: 01, 02, 03, 05, 06

## Question

Subir la fidelidad con un spike barato del scorer v1 (C# o pseudocódigo ejecutable) sobre 5-10 CVs de ejemplo: parsing PDF -> features -> score + razones, para reaccionar a cómo se ve y se siente antes de la spec final. Linkar el artefacto, no pegarlo.

Artefacto: `prototype/scorer-v1.html` (doble clic, sin servidor; lógica pura en un `<script>` lifteable a C#).

## Answer

Veredicto: aceptado sin objeciones bloqueantes (cierre directo, sin ronda de reacción). El híbrido BM25+cobertura+reglas con pesos v1 y veto blando queda como referencia de comportamiento; el módulo JS puro se lifta a C# en la implementación. Validación final con CVs reales en los 3 pilotos de revisión ciega.
