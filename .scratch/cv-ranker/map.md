## Destination

Spec técnica lista para implementar de un rankeador de CVs 1..N sin APIs de pago LLM, integrado como nuevo `.csproj` C# en la app de gestión de entrevistas/selección, rankeando PDFs en inglés (20-200 por oferta) contra la job description del sistema.

## Notes

- Dominio: selección / entrevistas; términos a afinar: candidato, CV, oferta / job description, parámetros, ranking.
- Stack: C# in-app .NET 10, nuevo `CVRanker.csproj`, dependencias permitidas; Python último recurso; prohibidas APIs de pago; on-premise CPU.
- Entrada (decidido): JD texto + must-have/nice-to-have editables; pesos defaults v1 ajustables. Salida: 1..N + score 0-100 + top-3 razones + flag must; filtros must+keyword. Flujo: botón Rankear con snapshot.
- Parámetros v1 (decidido): must 0.35 · BM25 0.25 · nice 0.15 · exp 0.12 · edu 0.05 · título 0.05 · idiomas 0.03.
- Explicabilidad (decidido): score + desglose colapsable + top-3 + flag must; anonimizar nombre/foto/edad/género/dirección; PDF con banner; snapshot auditable + disclaimer + riesgo documentado.
- Evaluación: revisión ciega 3 pilotos; P@10+MRR (+nDCG si graduado); éxito contratado top-10 ≥2/3 + ahorro ≥50%.
- Skills a consultar por sesión: `grilling`, `domain-modeling`; para tickets AFK: `research`; para HITL con artefacto: `prototype`.

## Decisions so far

- [Stack C# sin LLM](issues/02-csharp-stack.md): PdfPig + BM25 (propio o Lucene.NET BM25Similarity) + scoring por secciones; ML.NET no en v1; ONNX MiniLM v1.1; iText descartado AGPL.
- [Parámetros y pesos v1](issues/03-parametros-pesos.md): must 0.35 · BM25 0.25 · nice 0.15 · exp 0.12 · edu 0.05 · título 0.05 · idiomas 0.03; veto blando; calibración por grid en 3 pilotos.
- [Evaluación piloto](issues/04-evaluacion-piloto.md): revisión ciega 3 ofertas, P@10+MRR (+nDCG si graduado), éxito contratado top-10 ≥2/3 + ahorro ≥50%.
- [Inventario app](issues/01-app-inventory.md): .NET 10 + csproj nuevo, JD texto libre sin listas, CVs blob Azure (spike por path), on-premise CPU.
- [Oferta y UX](issues/05-oferta-ux.md): listas editables + pesos defaults ajustables; salida 1..N+score+top-3+flag; botón Rankear con snapshot.
- [Explicabilidad y sesgo](issues/06-explicabilidad-sesgo.md): desglose colapsable + anonimización + snapshot + disclaimer.
- [Prototipo scorer v1](issues/07-prototipo-scorer.md): aceptado; `prototype/scorer-v1.html` como referencia; lógica a liftar a C#.

<!-- una línea por ticket cerrado con gist + link -->

## Not yet specified

<!-- niebla despejada; solo queda el prototipo -->

## Out of scope

- Cualquier LLM vía API de pago.
- Microservicio Python en producción salvo que C# se demuestre inviable (última instancia).
- Reentrenar/fine-tunear modelos con GPU; v1 sin ML entrenado.
