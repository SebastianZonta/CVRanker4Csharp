# Research: parámetros y ponderación v1 para rankear CVs sin LLM

Ticket: `.scratch/cv-ranker/issues/03-parametros-pesos.md` · Fecha: 2026-09-11
Pregunta: ¿qué parámetros/ponderación funcionan sin LLM (skills, años experiencia, educación, idiomas, keywords, recencia)? Comparar similitud textual BM25/TF-IDF + cobertura must-have/nice-to-have + reglas con pesos ajustables, proponer set v1 y cómo calibrarlo sin histórico etiquetado.

## 1. Hallazgos (con fuente primaria por afirmación)

### 1.1 Similitud textual: BM25 > TF-IDF puro, con defaults conocidos
- TF-IDF = TF × IDF con `IDF = log(N/DF)`; premia frecuencia del término y penaliza términos comunes. (Fuente: KMW Technology, "Understanding TF-IDF and BM-25", derivación TF→IDF paso a paso.)
- BM25 conserva la estructura TF×IDF pero sustituye TF por TF con **saturación** `TF/(TF+k·(1-b+b·dl/adl))` y usa IDF probabilístico; así evita que repetir un keyword infle el score linealmente y **normaliza por longitud** (CVs largos vs cortos). (Fuente: KMW Technology, secciones "Term Saturation" y "Document Length".)
- Defaults universales: **k1=1.2, b=0.75**. (Fuentes primarias: javadoc `BM25Similarity` de Apache Lucene 9.12.1 — constructores y `idf = log(1+(docCount-docFreq+0.5)/(docFreq+0.5))`; Elastic "Practical BM25 Part 3": "default values of b=0.75 and k1=1.2 work pretty well for most corpuses"; IBM Content Search docs: BM25 = TF-IDF + saturación + normalización de longitud.)
- Rango de experimentación documentado: b ∈ [0,1] (óptimos típicos 0.3–0.9), k1 ∈ [0,3] (óptimos típicos 0.5–2.0); no hay valores universalmente óptimos, se calibran por corpus con Rank Eval. (Fuente: Elastic Part 3, citando Lipani et al. 2015; Taylor et al. 2006; Trotman et al. 2014.)
- Evidencia en dominio CV↔oferta: **BM25 es baseline fuerte de ranking** en datasets reales de person-job fit (Intellipro/AliYun): con encoder pequeño, BM25 supera a BERT/RawEmbed/DPGNN en 1 de 4 tareas de ranking (MAP/nDCG@10); los métodos neuronales solo lo superan con encoders grandes + contrastive learning. Conclusión: sin LLM/entrenamiento, BM25 es el techo razonable. (Fuente: Yu et al., "ConFit: Improving Resume-Job Matching…", arXiv:2401.16349, Tablas 3–4, §4.5; métricas MAP y nDCG@10.)
- Decisión para v1: **BM25 (k1=1.2, b=0.75) como señal textual base**, no TF-IDF puro. En C# equivale a Lucene.NET `BM25Similarity` con esos defaults. TF-IDF+coseno queda como fallback solo si Lucene.NET no estuviera disponible.

### 1.2 Qué categorías pesan en la práctica (motores ATS empresariales)
- Los motores ATS extraen entidades estructuradas y puntúan por **categorías con peso configurable por oferta**: skills, títulos, educación, certificaciones, nivel de gestión, industrias, idiomas. **Skills domina (30–65%)** por ser la señal más objetiva y accionable; títulos 15–30%; educación 5–20%; certificaciones 5–15%; gestión 5–25%; industrias 5–15%; idiomas 0–10%. (Fuente: Resume Optimizer Pro, "Resume Matching Explained", abr-2026 — tabla de 7 categorías y pesos típicos; los % son rangos observados de configuración por oferta, no un estándar formal.)
- Dentro de skills: **hard skills a peso completo, soft skills a mitad**; **recencia (rol actual = 1.0×, roles antiguos decaen) y duración** computadas desde experiencia fechada; **doble presencia** (sección skills + historial laboral) da bonus aditivo. (Fuente: ibíd., tabla "How ROP scores each ATS category".)
- Regla de colocación: un skill **dentro de una entrada laboral fechada es evidencia** (permite recencia+duración); en sección plana de skills es solo declaración. (Fuente: ibíd., § "Work History Versus the Skills Section".)
- Umbrales orientativos de callback (estudios citados 2024–2025, mercado 2026 más exigente): ≥70% alineamiento ≈ 2.5× callbacks (Resumly.ai 2025); CVs adaptados 11.7% vs 4.2% genéricos (Wellfound, 15k aplicaciones 2024); mediana de score 48, 51% bajo 50 (ResumeAdapter 2026). (Fuente: ibíd., §§ score-to-callback; cifras de terceros, tratar como orden de magnitud, no garantía.)
- 92% de ATS **rankean, no auto-rechazan**: el score ordena la cola del reclutador. (Fuente: ibíd., citando Enhancv 2025.)

### 1.3 Must-have / nice-to-have + reglas (lo que la literatura respalda)
- Patrón estándar: **requisitos explícitos de la oferta** (skills requeridos, años mínimos, titulación, certificaciones nombradas, idiomas) extraídos como constraints + score ponderado. (Fuentes: guía RAG de screening — "extract explicit constraints: required skills, minimum years experience"; Impress.ai — "define weighted metrics for skills, education, experience".)
- Paper NCAIDT'25 P11 (Smart Resume Screening): score agregado con **pesos elegidos por grid search** sobre (skill, experiencia, educación, certificación). Respalda v1 como suma ponderada calibrada por búsqueda en rejilla, no pesos mágicos. (Fuente: índice NCAIDT'25; PDF no descargable — claim limitado al título/abstract indexado.)
- ConFit §5.2 (análisis de errores): ~20% de errores son "unsuitable" (falta requisito tipo "4+ años con Docker/K8s") y se mitigarían **combinando el modelo con feature engineering keyword tipo BM25**. Respalda arquitectura híbrida: BM25 + reglas de cobertura. (Fuente: ConFit §5.2.)

### 1.4 Métricas y evaluación sin histórico
- Métricas estándar de ranking: **MAP, nDCG@10** (ConFit §4.4, siguiendo Karpukhin et al. 2020 / Yang et al. 2022); para screening operativo: **Precision@k / Recall@k / MRR** (guía RAG §9; Evidently: P/R@K y F-beta@K).
- Sin histórico etiquetado: Elastic recomienda **Rank Eval API** — construir juicios de relevancia sintéticos (pares oferta↔CV anotados por RRHH) y evaluar incrementos de b/k1 y de pesos. (Fuente: Elastic Part 3.)

## 2. Comparativa de enfoques (para este repo)

| Enfoque | Pros | Contras | Veredicto v1 |
|---|---|---|---|
| TF-IDF + coseno | Simple, implementable en ~50 líneas C# sin dependencias | Sin saturación ni normalización de longitud; CVs largos/verborreicos inflan score | Fallback solo |
| **BM25 (k1=1.2, b=0.75)** | Saturación + longitud; default probado; BM25Similarity existe en Lucene.NET; baseline fuerte en CV-ranking real | Requiere índice por oferta (N=20–200 CVs; IDF ruidoso con N pequeño) | **Señal textual base** |
| Cobertura must-have/nice-to-have (conjuntos) | Explicable ("cubre 4/5 must"), auditable, configurable por oferta | Requiere parseo de skills/entidades + normalización de sinónimos | **Señal principal junto a BM25** |
| Reglas (años exp, educación, idiomas, recencia) | Capturan lo que el texto plano no pondera (recencia, duración, seniority) | Más código de extracción (fechas, títulos, niveles) | **Capa de ajuste, pesos pequeños** |
| Embeddings ONNX locales / ML entrenado | Mejor semántica ("React"≈"frontend") | Fuera de alcance v1 (sin GPU, sin entreno); embeddings sin calibrar no son explicables | v2, no v1 |

Nota sobre N pequeño: con 20–200 CVs por oferta, el IDF intra-oferta es inestable. Mitigación: IDF con **smoothing + floor** (como Lucene `log(1+…)`), stopwords EN, stemming ligero, y que el peso BM25 no domine (ver §3).

## 3. Set v1 propuesto (pesos iniciales, suma 100, ajustables por oferta)

Score(CV) = 100 × Σ wᵢ · fᵢ, cada fᵢ ∈ [0,1] normalizado por min-max intra-oferta (o z-score→sigmoide si hay outliers).

| # | Parámetro fᵢ | Qué mide (v1, extraíble sin LLM) | Peso inicial wᵢ | Fuente/justificación |
|---|---|---|---|---|
| 1 | **Cobertura must-have** | \|must ∩ CV\| / \|must\| (matching exacto + sinónimos curados; hard skills peso 1, soft 0.5) | **0.35** | Skills domina 30–65% (ROP); must-have es la parte exigible |
| 2 | **Similitud BM25 oferta↔CV** | BM25(CV, query=texto oferta+must/nice), k1=1.2 b=0.75, normalizado intra-oferta | **0.25** | Baseline fuerte en ConFit; Elastic/Lucene defaults |
| 3 | **Cobertura nice-to-have** | \|nice ∩ CV\| / \|nice\| (mismo matching que 1) | **0.15** | Complemento de must; evita que BM25 lo diluya |
| 4 | **Experiencia (años + recencia)** | años relevantes vs pedidos (cap 1) × 0.6 + recencia (skill en rol actual=1, decae 0.15/año) × 0.4 | **0.12** | Recencia/duración señal explícita (ROP); error "4+ años" (ConFit §5.2) |
| 5 | **Educación** | 1 si titulación ≥ mínima pedida (mapeo: PhD>Master>Bachelor>FP/None); 0.5 si relacionada sin nivel; 0 si oferta no exige (no penaliza) | **0.05** | Rango 5–20% (ROP); bonus sin penalización si no se exige |
| 6 | **Idiomas** | 1 si cubre idioma(s) pedidos; 0.5 parcial; 1 por defecto si la oferta no pide (no penaliza) | **0.03** | Rango 0–10% (ROP); CVs en inglés → casi siempre 1 |
| 7 | **Título/seniority match** | 1 título exacto rol actual; 0.6 familia+seniority; 0.3 familia; señales gestión si rol manager | **0.05** | 2ª categoría (15–30% en ATS); en v1 peso bajo porque títulos históricos son ruidosos |

Reglas de veto (no pesan, filtran o marcan): **must-have = 0 → flag "no cubre must"** (no auto-rechazo, baja a cola — coherente con "ATS rankean, no rechazan"); permiso de trabajo/ubicación solo si la oferta lo declara hard.
Explicabilidad: guardar por CV los top-3 contribuyentes (p.ej. "must 4/5 (+0.28)", "BM25 p75 (+0.15)", "5 años vs 3 pedidos (+0.10)").

Sensibilidad conocida: +8–15 pts al incrustar hard skills faltantes en historial fechado; +8–12 título exacto; +4–7 doble presencia; +3–6 señales de gestión (cifras ROP, direccionales).

## 4. Cómo calibrar sin histórico etiquetado (protocolo)

1. **Semilla sintética (día 0)**: 3 ofertas piloto × 20–40 CVs reales anonimados; RRHH marca top-10 ciego por oferta (juicio de relevancia binario/graduado). Es el "Rank Eval set" (Elastic Part 3).
2. **Grid search grueso**: variar (w_must, w_bm25, w_nice) en pasos 0.05 con w_exp+edu+idioma+título fijos; objetivo **nDCG@10 + Precision@10** (métricas ConFit §4.4 / Evidently P/R@K). Elegir el punto con mejor media en las 3 ofertas (no por oferta, evita overfit).
3. **Ajuste fino**: b ∈ {0.5,0.75,0.9}, k1 ∈ {0.8,1.2,1.5} solo si el paso 2 deja empates; CVs son longitud media-uniforme → b≈0.5–0.75 esperado.
4. **Validación**: revisión ciega A/B (pesos viejos vs nuevos, orden aleatorizado) + métrica negocio del mapa: **contratado en top-10 + ahorro triaje**. Recalibrar cada ~10 ofertas; pesos siempre editables por oferta ( Absolute requirement del ticket).
5. **Antisesgo**: pesos nunca sobre nombre/género/foto/edad; loguear distribución de scores por cohorte si hay datos.

## 5. Fuentes (primarias, por orden de peso probatorio)
1. Apache Lucene 9.12.1 javadoc `BM25Similarity` — defaults k1=1.2/b=0.75, fórmula IDF. https://lucene.apache.org/core/9_12_1/core/org/apache/lucene/search/similarities/BM25Similarity.html
2. Elastic, "Practical BM25 Part 3: Picking b and k1" (Connelly, 2018) — defaults, rangos b 0.3–0.9 / k1 0.5–2.0, Rank Eval. https://www.elastic.co/blog/practical-bm25-part-3-considerations-for-picking-b-and-k1-in-elasticsearch
3. Yu et al., "ConFit…" arXiv:2401.16349 — BM25 baseline fuerte en ranking CV↔oferta, métricas MAP/nDCG@10, errores "unsuitable"→híbrido keyword. https://arxiv.org/html/2401.16349v1
4. KMW Technology, "Understanding TF-IDF and BM-25" (Seitz, 2020) — derivación saturación/longitud. https://kmwllc.com/index.php/2020/03/20/understanding-tf-idf-and-bm-25/
5. IBM Content Search docs, "Document retrieval and ranking" — BM25 = TF-IDF + saturación + normalización. https://www.ibm.com/docs/en/content-cortex/5.7.0?topic=domain-document-retrieval-ranking
6. Resume Optimizer Pro, "Resume Matching Explained" (Hamui, abr-2026) — 7 categorías, rangos de peso, recencia/duración, rank-no-reject. https://resumeoptimizerpro.com/blog/resume-matching-explained (fuente comercial; rangos direccionales, estudios 2024–25 citados dentro)
7. Robertson et al., "Okapi at TREC-3" (1994) vía cita Lucene/ConFit — origen BM25. (No consultado full-text; citado a través de 1 y 3.)
8. NCAIDT'25 P11 "Smart Resume Screening and Matching System" — agregado ponderado skill/exp/edu/cert vía grid search. https://ncaidt.ganitara.com/ncaidt25/papers/P11.pdf (índice; PDF no accesible — claim limitado)

## 6. Límites de esta nota
- Pesos ROP (6) son práctica comercial observada, no estándar académico; los pesos v1 son **priors razonados**, no óptimos medidos — el §4 existe precisamente por eso.
- N=20–200 CVs/oferta hace el IDF ruidoso; v1 lo compensa con smoothing y peso BM25 0.25 (no dominante).
- Sinónimos ("K8s"↔"Kubernetes") requieren lista curada por oferta en v1; embeddings locales quedan para v2.
