# Research 04 — Evaluación piloto del rankeador de CVs

Pregunta origen: `.scratch/cv-ranker/issues/04-evaluacion-piloto.md` —
¿cómo evaluar el rankeador sin (o con) histórico? Protocolo piloto (3 ofertas),
métricas P@10 / MRR / nDCG, revisión ciega RRHH, criterio de éxito mínimo
(contratado en top-10 + ahorro de triaje), y qué significa el "Si" de Q8.

Fecha: 2026-09-11. Método: fuentes primarias (libro IR Stanford, paper nDCG
original, reportes NIST/TREC, código trec_eval, paper Buckley & Voorhees SIGIR'04).

## 1. Gist (respuesta corta)

- **Sin histórico: la única evaluación válida es revisión ciega prospectiva.**
  Armar 3 ofertas piloto ya cerradas (con contratado conocido pero oculto al
  sistema), rankear 20–200 CVs por oferta, y que RRHH juzgue el top-k sin ver
  scores. Métricas: P@10 + MRR como primarias (binarias, baratas), nDCG@10
  solo si RRHH acepta juicios graduados (0–3). Criterio de éxito mínimo:
  **contratado en top-10 en ≥2/3 ofertas + reducción medible del tiempo de
  triaje** (pasar de ~20–30 CVs/hora manual a revisar solo top-10/20).
- **Con histórico: solo reutilizar si cada oferta guarda (a) el pool completo
  de CVs recibidos, (b) la job description exacta usada, y (c) el outcome por
  candidato** (contratado / shortlist / descartado). Si falta cualquiera de
  los tres, el histórico NO es reutilizable como qrels y se vuelve al
  protocolo ciego.
- **El "Si" de Q8 no basta.** Hay que repreguntar con checklist de 3 ítems
  (pool + JD + outcome). La literatura TREC muestra que reutilizar juicios
  incompletos/sesgados subestima sistemas nuevos (Buckley & Voorhees 2004).

## 2. Métricas (definiciones primarias)

### P@k (Precision at k)
- Fracción de relevantes entre los top-k. No requiere estimar el total de
  relevantes; es la métrica más intuitiva para RRHH ("de los 10 primeros,
  ¿cuántos valían la pena?").
- Fuente: Manning, Raghavan & Schütze, *Introduction to Information
  Retrieval*, cap. 8 (ed. online Stanford): "measuring precision at fixed low
  levels of retrieved results, such as 10 or 30 documents… referred to as
  'Precision at k'". Advierte: "it is the least stable of the commonly used
  evaluation measures and… does not average well, since the total number of
  relevant documents has a strong influence" — por eso se promedia sobre las
  3 ofertas y se acompaña de MRR.
- Implementación referencia: `trec_eval -m P.10` (repo oficial
  `usnistgov/trec_eval`).

### MRR (Mean Reciprocal Rank)
- Media sobre queries de 1/rank del **primer** relevante (1 si rank 1, 1/2 si
  rank 2, 0 si ninguno). Mide "¿cuánto hay que bajar para encontrar al primer
  candidato bueno?".
- Fuente: Voorhees, *The TREC-8 Question Answering Track Report* (NIST):
  "An individual question received a score equal to the reciprocal of the
  rank at which the first correct response was returned, or 0 if none of the
  five responses [was correct]… The score for a run was the mean of the
  individual questions' reciprocal ranks."
- Para CVs: adaptado como "rank del contratado / del primer candidato que
  RRHH marca apto". Barato y muy sensible al éxito mínimo (contratado en
  top-10 ⇒ RR ≥ 0.1).
- Implementación referencia: `trec_eval -m recip_rank` (`m_recip_rank.c`).

### nDCG@k (Normalized Discounted Cumulative Gain)
- Para relevancia **graduada** (p. ej. 0=descartado, 1=potencial, 2=entrevistable,
  3=contratable). DCG = suma de `gain / log(pos)` con gain típico `2^rel − 1`;
  nDCG = DCG / DCG del ranking ideal (∈ [0,1], comparable entre ofertas).
- Fuente original: Järvelin & Kekäläinen, "Cumulated gain-based evaluation of
  IR techniques", *ACM TOIS* 20(4), 2002. Fuente operativa: Manning et al.
  cap. 8, fórmula `NDCG(Q,k) = 1/|Q| Σ Z_kj Σ (2^R(j,m) − 1)/log2(1+m)`
  con `Z_kj` normalizando el ranking perfecto a 1.
- Regla práctica: **solo usar nDCG si RRHH acepta escala graduada**; si solo
  hay binario (apto/no), nDCG@10 ≈ P@10 con descuento por posición y no aporta
  suficiente para justificar el coste. TREC Deep Learning track usa NDCG@10
  como métrica principal con juicios de 4 niveles (Craswell et al. 2019).
- Implementación referencia: `trec_eval -m ndcg_cut.10`.

### Métricas de negocio (triaje)
- **Tasa contratado-en-top-10**: binario por oferta (sí/no). Es el criterio
  de éxito mínimo, no una métrica IR.
- **Ahorro de triaje**: minutos por oferta (cronometrar revisión manual vs
  revisión solo top-10/20). Referencia de orden de magnitud: SHRM/Eddy citan
  ~23 h de screening por contratación; triaje manual rinde ~20–30 CVs/hora
  (fuentes secundarias — usar solo como baseline local, medir tiempos propios
  en el piloto).

## 3. Protocolo piloto (sin histórico — caso por defecto)

Asume 20–200 PDFs en inglés por oferta, JD del sistema, on-premise sin GPU.

1. **Elegir 3 ofertas cerradas diversas** (distinto rol/volumen; ≥1 con
   ≥50 CVs). Requisito: conocer al contratado pero **ocultarlo al equipo
   técnico** durante el ranking.
2. **Congelar inputs**: JD exacta + set completo de CVs por oferta. Registrar
   versión del scorer y pesos (auditoría + reproducibilidad).
3. **Rankear ciego**: el sistema produce top-20 por oferta con top-3 razones
   visibles (requisito de explicabilidad del mapa). No reordenar a mano.
4. **Juicio ciego RRHH**: para cada oferta, RRHH evalúa el pool — como mínimo
   el top-20 del sistema + contratado + muestra aleatoria de 10 no-top —
   sin ver scores ni orden del sistema (orden aleatorio). Escala mínima:
   binaria (apto/no apto); recomendada: 0–3 si hay tiempo.
5. **Calcular**: por oferta P@10, RR (del contratado y del primer apto),
   nDCG@10 solo si hubo escala graduada; luego media sobre 3 ofertas.
   Reportar también tasa contratado-en-top-10 (x/3).
6. **Medir tiempo**: cronometrar triaje manual histórico (o re-triaje de una
   oferta) vs revisión del top-10/20 del sistema. Registrar minutos ahorrados.
7. **Decisión**: ver §4.

Coste estimado: juzgar ~30 CVs × 3 ofertas ≈ 90 juicios; a 3–5 min/CV ≈
5–8 h RRHH totales. Asumible para un piloto.

## 4. Criterio de éxito mínimo (propuesto)

- **Puerta 1 (utilidad)**: contratado en top-10 en **≥2 de 3** ofertas, O
  MRR ≥ 0.2 medio (equivale a primer apto en promedio dentro del top-5).
- **Puerta 2 (eficiencia)**: ahorro de triaje **≥50%** del tiempo de
  preselección en ≥2 ofertas (revisar 10–20 en vez de 20–200).
- **Regla**: GO a v1 si se cumplen ambas puertas; CONDICIONAL si solo una
  (ajustar pesos/parámetros y repetir 1 oferta); NO-GO si ninguna.
- Justificación: umbrales deliberadamente modestos porque n=3 no da
  significancia estadística (Manning cap. 8: los scores varían más entre
  queries que entre sistemas; se necesitan decenas de queries para comparar
  sistemas con potencia). El piloto mide **viabilidad operativa**, no
  superioridad algorítmica.

## 5. Si SÍ existe histórico (qué pedir y qué hacer)

El "Si" de Q8 solo cuenta como histórico reutilizable si existen los 3:

1. **Pool completo por oferta**: todos los CVs recibidos (no solo
   shortlist). Sin esto hay sesgo de pooling: los no-juzgados se asumen no
   relevantes y el sistema nuevo se subestima.
2. **JD exacta versionada** por oferta (la que vio el recruiter, no la
   reescrita a posteriori).
3. **Outcome por candidato**: contratado / llegó a entrevista / descartado,
   con fecha.

- **Si faltan**: tratar como caso sin histórico (§3). No reconstruir qrels de
  memoria (juicios imperfectos; Buckley & Voorhees §5 muestran que alteran
  menos que la incompletitud pero siguen sesgando).
- **Si existen**: construir qrels TREC (formato `qid 0 docid rel` con rel
  0/1, o 0–3 si hay señal graduada) y evaluar con `trec_eval` offline. Con
  juicios incompletos preferir **bpref** (Buckley & Voorhees, SIGIR'04:
  "Adding additional unjudged documents… can have no effect on bpref…
  makes it the preferred measure… with incomplete relevance judgments").
  `trec_eval` lo implementa (`m_bpref.c`).
- **Pooling para reutilizar a futuro**: desde v1, guardar por oferta
  (JD + ranking completo + juicios RRHH del top-k). Cada piloto amplía el
  pool y reduce el coste del siguiente (práctica estándar TREC: pooling a
  profundidad k, Sparck Jones & van Rijsbergen 1975; Voorhees & Harman).

## 6. Revisión ciega y sesgo (nota)

- Anonimizar antes del juicio RRHH: nombre, foto, dirección, año de
  graduación (práctica estándar "blind resume review": retirar identificadores
  para reducir sesgo consciente/inconsciente — fuente secundaria Intervue/JDP;
  evidencia experimental de discriminación por edad en CVs: PMC 2017, estudio
  con 610 profesionales RRHH).
- El scorer v1 (params propuestos: skills, experiencia, educación, keywords)
  **no debe usar** señales demográficas; auditar que ningún parámetro las
  proxie (p. ej. año de graduación ⇒ edad). Detalle en ticket 06, no aquí.

## 7. Fuentes (primarias primero)

1. Manning, Raghavan & Schütze — *Introduction to Information Retrieval*,
   cap. 8, ed. online Stanford (`nlp.stanford.edu/IR-book/html/htmledition/
   evaluation-of-ranked-retrieval-results-1.html`): P@k, MAP/R-precision,
   fórmula nDCG, inestabilidad de P@k, necesidad de queries amplias.
2. Järvelin & Kekäläinen — "Cumulated gain-based evaluation of IR
   techniques", *ACM TOIS* 20(4) 2002 (`dl.acm.org/doi/10.1145/582415.582418`):
   definición original DCG/nDCG, relevancia graduada.
3. Voorhees — *The TREC-8 Question Answering Track Report* (NIST,
   `tsapps.nist.gov/publication/get_pdf.cfm?pub_id=151495`): definición MRR
   (recíproco del rank del primer correcto, media sobre preguntas).
4. NIST — `usnistgov/trec_eval` (GitHub): herramienta estándar; medidas
   `P.*`, `recip_rank`, `ndcg_cut`, `bpref`; uso `trec_eval -q -c -M1000
   qrels run`.
5. Buckley & Voorhees — "Retrieval Evaluation with Incomplete Information",
   SIGIR'04 (`tsapps.nist.gov/publication/get_pdf.cfm?pub_id=150469`):
   MAP/P@10/R-prec no robustos a juicios incompletos; bpref preferido;
   pooling y reusabilidad.
6. Craswell et al. — "Overview of the TREC 2019 Deep Learning Track"
   (arXiv 2003.07820): NDCG@10 como métrica principal con juicios de 4
   niveles.
7. PMC 2017 — "Implicit Age Cues in Resumes" (`pmc.ncbi.nlm.nih.gov/articles/
   PMC5554369/`): evidencia de sesgo por edad en revisión de CVs (apoya
   anonimización).
8. Secundarias (solo contexto, no afirmaciones técnicas): Intervue/JDP
   "blind resume review"; SHRM/Eddy cifra ~23 h screening por hire y
   ~20–30 CVs/hora manual — usar como orden de magnitud, medir baseline
   propio.
