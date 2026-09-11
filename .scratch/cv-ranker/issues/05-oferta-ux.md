Type: grilling
Status: resolved
Blocked by: 02, 03

## Question

Definir con RRHH/usuario la forma de la oferta y la UX del ranking: ¿la JD del sistema basta como query o hay que añadir must-have / nice-to-have y pesos editables? ¿Qué ve RRHH (score continuo vs 1..N, filtros, top-3 razones por candidato)? Cerrar spec de entrada/salida del rankeador.

## Answer

- Entrada: JD texto libre + listas must-have/nice-to-have editables por oferta; pesos defaults v1 con ajuste avanzado opcional.
- Salida: orden 1..N con score 0-100 + top-3 razones + flag si falta must-have; filtros por must completo y keyword.
- Flujo: botón "Rankear" que congela snapshot (JD+CVs+versión/pesos); re-rank solo manual (compatible con revisión ciega).
