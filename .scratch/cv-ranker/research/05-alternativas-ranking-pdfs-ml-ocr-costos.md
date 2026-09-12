# Alternativas al rankeo de PDFs del proyecto (BM25-lite + reglas + PdfPig) — con ML, OCR y costos

Fecha de consulta de precios y docs: 2026-09-12. Todas las cifras remiten a su página oficial de pricing (sección Fuentes).

## 1. Enfoque actual del repo (lo que ya existe)

- **Scorer** `src/CVRanker.Domain/Scoring/Bm25Ranker.cs`: BM25-lite sin IDF, `k1=1.2, b=0.75`, normalización intra-oferta por máximo, score 0–100 como media ponderada de 7 features (Must, Bm25, Nice, Experience, Education, Title, Languages), veto suave (candidatos con `must=0` van a la cola, nunca se rechazan), `scorer-v1` congelado en el snapshot.
- **Features de regla**: `ExperienceYears/HasDegree/IsSenior/HasLanguage` + cobertura must/nice por tokens (con trim de `.` final del lado oferta, p. ej. `C#.` → `c#`).
- **Extracción**: `src/CVRanker.Infrastructure/Pdf/PdfPigTextExtractor.cs` con `UglyToad.PdfPig` (prerelease `1.7.0-custom-5` en NuGet) a nivel de palabra (`page.GetWords()`); páginas sin palabras → flag `NeedsOcr` (solo detección, sin OCR integrado — ver `test/CVRanker.Tests/PdfPortTests.cs`: `cv-scanned.pdf` → `NeedsOcr=true`).
- **Persistencia/auditoría**: `RankingSnapshot.Create` (freeze en el agregado) + `InMemorySnapshotStore`; PDFs como `<PdfDirectory>/<ref>.pdf`.
- **Calibración**: `src/CVRanker.Domain/Pilot/WeightCalibrator.cs` — grid grueso (paso 0.05 sobre must/BM25/nice hasta 0.6, objetivo media nDCG@10+P@10 sobre grades HR 0–3); k1/b solo como desempate en rejilla pequeña. Es calibración de pesos lineales, no learning-to-rank.

Respuesta corta: **no, no es la única forma**. Es el extremo barato/auditable del espectro. Abajo las alternativas por capas.

## 2. Alternativas de ranking (tabla)

| # | Alternativa | Cómo encaja en este repo | Pros | Contras | Complejidad | Auditabilidad |
|---|-------------|--------------------------|------|---------|-------------|---------------|
| A | **BM25 completo con IDF** (añadir `log(1+(N-df+0.5)/(df+0.5))` sobre el corpus de la oferta) | Cambio de ~10 líneas en `Bm25Ranker` | Penaliza términos ubicuos ("engineer", "experience"); estándar TREC/Lucene; gratis | Con N pequeño (20–200 CVs) el IDF es ruidoso; poco cambio vs lite | Baja | Total (determinista, mismo snapshot) |
| B | **TF-IDF clásico** | Sustituir `RawBm25` por tf·idf + coseno | Simple, explicable, baseline de IR | Peor saturación de tf y normalización de longitud que BM25 | Baja | Total |
| C | **Lucene.NET** (índice invertido + `BM25Similarity` k1=1.2 b=0.75 por defecto + analizadores, highlight, facetas) | Indexar textos extraídos; la oferta = query; must/nice = filtros/boosts | Motor probado, analizadores ES/EN, persistencia, escala a miles de CVs | Dependencia nativa pesada; versión 4.8 aún beta; hay que versionar índice en el snapshot | Media | Alta (scores explicables por término) |
| D | **Embeddings + vector search** (oferta y CV → vectores, coseno; `text-embedding-3-small/large`; store en **Qdrant** o **pgvector**) | Añadir columna/tabla de vectores + paso de embedding en ingesta; el rankeo combina coseno con features de regla | Capta sinonimia ("k8s"≈"kubernetes", "frontend"≈"React"); multilingüe | Caja menos explicable; coste por embedding; deriva si cambia el modelo (hay que fijar versión en snapshot) | Media | Media (guardar modelo+versión+distancia) |
| E | **Híbrido léxico+vector (RRF)** — p. ej. Azure AI Search `search` + `vectorQueries` fusionados por Reciprocal Rank Fusion, opcional semantic ranker | Delegar retrieval a Azure AI Search; el dominio conserva veto suave + snapshot | Mejor precision+recall que cada uno solo; filtros/facetas gratis | Acopla a servicio externo; coste fijo mensual; semantic ranker = caja negra parcial | Media-Alta | Media (guardar ranking de cada rama + pesos RRF) |
| F | **Learning-to-rank con ML.NET** (`RankingCatalog`, trainers LightGBM/FastTree, PFI) entrenado con grades HR 0–3 | Sustituir `WeightCalibrator` por un trainer LTR cuando haya cientos de juicios | Aprende pesos e interacciones reales de los datos del piloto | Necesita volumen de grades (≥ cientos); riesgo de overfit; modelo binario que versionar | Alta | Media-Baja (feature importance ayuda, pero no es regla legible) |
| G | **LLM rerank / extracción de features** (LLM extrae años/experiencia/skills a JSON; o reordena top-20 con razones) vía Azure OpenAI / OpenAI / Claude | Segunda etapa: BM25-lite filtra top-20 (barato) → LLM solo reordena/justifica | Mejor manejo de ambigüedad y razones en lenguaje natural | Coste por token, latencia, no-determinismo (fijar seed/versión/temperatura 0 y guardar prompt+respuesta en snapshot); riesgo de sesgo/PII | Media | Baja-Media (solo si se congela prompt+modelo+salida) |
| H | **Reglas + LLM híbrido** (recomendado): scorer actual como etapa 1, LLM solo para `NeedsOcr`, extracción de features y top-K rerank | Mínimo cambio arquitectónico: puertos `IFeatureExtractor` / `IReranker` en Application | Conserva auditoría y coste bajo; mejora donde BM25 sufre (escaneados, paráfrasis) | Dos caminos de código que mantener | Media | Alta en etapa 1, Media en etapa 2 |

Fuentes primarias: BM25 con defaults k1=1.2/b=0.75 e IDF `log(1+(docCount-docFreq+0.5)/(docFreq+0.5))` [Lucene BM25Similarity]; Lucene.NET es el port .NET con soporte .NET 8/6, v4.8.0 en beta [repo lucenenet]; ML.NET expone `RankingCatalog` con trainers/evaluadores y PFI [RankingCatalog API]; Semantic Kernel es el SDK OSS ligero C#/Python/Java para enchufar LLMs [SK overview]; pgvector aporta HNSW/IVFFlat + distancias L2/IP/coseno y receta de hybrid search con full-text + RRF [pgvector]; Azure AI Search combina `search`+`vectorQueries` con RRF y semantic ranker opcional [hybrid overview]; modelos de embeddings OpenAI con benchmarks MIRACL/MTEB [Foundry models doc].

## 3. OCR para PDFs escaneados (el repo hoy solo marca `NeedsOcr`)

| Opción | Qué es (fuente primaria) | Integración en este repo | Costo oficial |
|--------|--------------------------|--------------------------|---------------|
| **Tesseract** (OSS, Apache-2.0, LSTM, 100+ lenguas, salida texto/hOCR/PDF/TSV) | Motor OCR local [repo tesseract] | Nuevo `TesseractOcrExtractor : IOcrExtractor` invocado solo cuando `PagesWithoutText` no vacío; contrato: devolver texto por página + confianza; test con `cv-scanned.pdf` | **$0** (infra propia) |
| **Azure AI Document Intelligence** (`prebuilt-read` texto impreso/manuscrito, `prebuilt-layout` texto+tablas+estructura; SDK C#) | Servicio cloud [overview v4.0] | `DocumentIntelligenceOcrExtractor` vía SDK; guardar `modelId=prebuilt-read`, versión API y nº páginas en el snapshot | Pay-per-page S0 + free 500 págs/mes; ver tabla §5 [pricing Doc Intelligence] |
| **AWS Textract** (`DetectDocumentText` = OCR puro) | Servicio cloud con ejemplo resuelto en su pricing | Alternativa si el despliegue es AWS | **$0.0015/pág** primer 1M (us-west-2) [Textract pricing] |
| **Google Document AI** (Enterprise OCR processor) | Servicio cloud con tabla por procesador | Alternativa si el despliegue es GCP | **$1.50/1000 págs** (tramo 1K–5M; primeras 1000 gratis) [Doc AI pricing] |

Recomendación: implementar el puerto `IOcrExtractor` con **Tesseract primero** (costo cero, sin salida de datos PII), y dejar el adaptador cloud (Read/layout) como segunda implementación tras el puerto, conmutada por config cuando la calidad local no baste (tablas densas, manuscrito).

## 4. ML / LTR / LLM (detalle)

- **Calibración actual ≠ LTR.** El `WeightCalibrator` hace grid search sobre 3 pesos con objetivo nDCG@10+P@10 — correcto con pocos pilotos, pero no aprende interacciones ni usa todo el feature set. El paso a LTR real es ML.NET `RankingCatalog` (trainers + `Evaluate` + PFI) cuando haya cientos de grades; hasta entonces, no compensa.
- **Embeddings**: `text-embedding-3-small` ($0.02/1M tokens) para检索 y `3-large` ($0.13/1M) si se necesita más calidad multilingüe [OpenAI pricing]. En Azure OpenAI los mismos modelos se facturan por 1K tokens vía el medidor de embeddings [Azure OpenAI pricing]; como referencia de escala, Azure anuncia GPT-4o mini global a $0.15 in / $0.60 out por 1M [blog Azure].
- **LLM rerank**: usar siempre como **etapa 2 sobre top-20** del scorer determinista, con temperatura 0, modelo fijado y prompt+respuesta persistidos en el snapshot (así la auditoría sobrevive al no-determinismo). Precios de referencia: GPT-4o mini $0.15/$0.60, GPT-4o $2.50/$10.00 por 1M in/out [OpenAI pricing]; Claude Haiku 4.5 $1/$5, Sonnet 4.5 $3/$15, Sonnet 5 $2/$10, batch −50%, web search $10/1000 [Anthropic pricing].
- **Orquestación .NET**: Semantic Kernel como middleware para plugins (extractor, reranker) sin atar el dominio al proveedor [SK overview].

## 5. Costos 2026 — tabla comparativa y ejemplo

Supuesto común: **1000 CVs/mes de 1–2 páginas (~1500 págs) + 50 ofertas con rerank del top-20**.

| Componente | Precio oficial (consultado 2026-09-12) | Costo del ejemplo | Notas |
|------------|----------------------------------------|-------------------|-------|
| Scorer actual (BM25-lite + reglas, PdfPig) | $0 (cómputo propio; PdfPig OSS prerelease en NuGet) | **$0** | Solo VM/app que ya existe |
| Tesseract OCR | $0 (Apache-2.0) | **$0** | Solo escaneados (fracción del total) |
| AWS Textract `DetectDocumentText` | $0.0015/pág (primer 1M) [link] | 1500×$0.0015 = **$2.25** | Solo si se enruta lo escaneado (~10% ⇒ ~$0.23) |
| Google Document AI Enterprise OCR | $1.50/1000 págs, 1000 primeras gratis [link] | 500×$1.50/1000 = **$0.75** | Como Textract, solo escaneados |
| Azure Document Intelligence Read/Layout | S0 por 1000 págs + free 500 págs/mes [link] (cifra exacta por tramo, en East US, en la página) | **~$1–3** (verificar tramo Read en la página; orden de magnitud como Textract/Google) | Elegir `prebuilt-read` salvo tablas densas |
| Embeddings `text-embedding-3-small` | $0.02/1M tokens [link] | 1000 CVs×~800 tok = 0.8M ⇒ **~$0.02** | Una sola vez por CV (cachear vector) |
| LLM extracción (GPT-4o mini) | $0.15 in / $0.60 out por 1M [link] | 1000×(1200 in+300 out) ⇒ ~$0.18+$0.18 = **~$0.36** | Solo si se usa LLM para features |
| LLM rerank top-20 × 50 ofertas (GPT-4o mini) | idem | 1000 llamadas×(1500 in+200 out) ⇒ ~$0.23+$0.12 = **~$0.35** | Etapa 2 acotada |
| Idem rerank con Haiku 4.5 | $1/$5 por 1M [link] | 1.5M in + 0.2M out ⇒ **~$2.50** | Alternativa Anthropic |
| Azure AI Search (índice gestionado) | Tiers Free/Basic/S1…/L2 por SU-hora; Serverless por CU/h+GB/mes (Serverless dev factura desde 2026-09-13) [tiers] [pricing] | Free–Basic: **$0–~$75/mes** fijos (consultar tarifa SU en [pricing]; este volumen cabe en Basic 1 SU) | Solo compensa con miles de CVs o necesidad de facetas/semántico |
| Qdrant Cloud / self-host | Free 0.5 vCPU/1GB/4GB; Standard uso-horario; OSS auto-hospedado gratis [link] | **$0** (free tier o Docker propio sobra para 1000 vectores) | pgvector en Postgres existente también $0 marginal |

**Totales del ejemplo:**
- Solo repo actual: **$0/mes**.
- Repo + Tesseract + embeddings cacheados: **~$0.02/mes**.
- Repo + OCR cloud solo escaneados + LLM extracción + rerank GPT-4o mini: **~$1–3/mes**.
- Con Azure AI Search Basic dedicado: sumar el fijo del servicio (**dominante**, decenas USD/mes) — no recomendado a este volumen.

## 6. Recomendación por fases

1. **Fase 0 (ya)**: mantener BM25-lite; añadir IDF (opción A) tras el puerto como experimento de 1 commit, evaluado con `WeightCalibrator` + pilotos ciegos.
2. **Fase 1 (OCR)**: puerto `IOcrExtractor` + implementación Tesseract; adaptar `PdfPigTextExtractor` para delegar páginas vacías; test con `cv-scanned.pdf`; Document Intelligence como adaptador opcional.
3. **Fase 2 (recall semántico barato)**: embeddings `3-small` + pgvector/Qdrant self-host, combinado con BM25 (RRF manual); fijar versión del modelo en el snapshot.
4. **Fase 3 (solo si el piloto lo pide)**: LLM rerank top-20 + extracción de features a JSON, con prompt/modelo/respuesta congelados en el snapshot; empezar por GPT-4o mini o Haiku por costo.
5. **No hacer aún**: Azure AI Search dedicado ni LTR con ML.NET hasta tener escala (miles de CVs) o cientos de grades HR.

## Fuentes (primarias, consultadas 2026-09-12)

- Lucene `BM25Similarity` (defaults k1=1.2 b=0.75, fórmula IDF): https://lucene.apache.org/core/9_9_0/core/org/apache/lucene/search/similarities/BM25Similarity.html
- Apache Lucene.NET (repo oficial, port .NET, 4.8.0 beta): https://github.com/apache/lucenenet
- ML.NET qué es / cómo funciona: https://learn.microsoft.com/en-us/dotnet/machine-learning/how-does-mldotnet-work
- ML.NET `RankingCatalog` (API ref): https://learn.microsoft.com/en-us/dotnet/api/microsoft.ml.rankingcatalog?view=ml-dotnet
- Semantic Kernel overview: https://learn.microsoft.com/en-us/semantic-kernel/overview/
- Azure AI Search qué es: https://learn.microsoft.com/en-us/azure/search/search-what-is-azure-search
- Azure AI Search tiers y modelos Dedicated/Serverless: https://learn.microsoft.com/en-us/azure/search/search-sku-tier
- Azure AI Search pricing (tiers por SU, serverless): https://azure.microsoft.com/en-us/pricing/details/search/
- Azure hybrid search (RRF + semantic): https://learn.microsoft.com/en-us/azure/search/hybrid-search-overview
- Azure Document Intelligence overview (Read/Layout, SDK C#): https://learn.microsoft.com/en-us/azure/ai-services/document-intelligence/overview
- Azure Document Intelligence pricing (S0 por 1000 págs, free 500/mes): https://azure.microsoft.com/en-us/pricing/details/document-intelligence/
- Azure OpenAI pricing (GPT/embedding por 1M/1K tokens): https://azure.microsoft.com/en-us/pricing/details/azure-openai/
- Azure blog GPT-4o mini ($0.15 in / $0.60 out por 1M, global): https://azure.microsoft.com/en-us/blog/openais-fastest-model-gpt-4o-mini-is-now-available-on-azure-ai/
- Foundry models (embeddings ada/3-small/3-large, MIRACL/MTEB): https://learn.microsoft.com/en-us/azure/foundry/foundry-models/concepts/models-sold-directly-by-azure
- OpenAI platform pricing (GPT-4o mini $0.15/$0.60, GPT-4o $2.50/$10.00, embeddings $0.02/$0.10/$0.13 por 1M): https://platform.openai.com/docs/pricing
- Anthropic API pricing (Haiku $1/$5, Sonnet $3/$15, batch −50%): https://docs.anthropic.com/en/docs/about-claude/pricing
- AWS Textract pricing (Detect $0.0015/pág primer 1M): https://aws.amazon.com/textract/pricing/
- Google Document AI pricing (Enterprise OCR $1.50/1000): https://cloud.google.com/document-ai/pricing
- Qdrant Cloud pricing (free/standard/premium, OSS gratis): https://qdrant.tech/pricing/
- pgvector (HNSW/IVFFlat, distancias, hybrid): https://github.com/pgvector/pgvector
- Tesseract OCR (Apache-2.0, LSTM, 100+ lenguas): https://github.com/tesseract-ocr/tesseract
- UglyToad.PdfPig en NuGet (1.7.0-custom-5 prerelease): https://www.nuget.org/packages/UglyToad.PdfPig
- Repo local: `src/CVRanker.Domain/Scoring/Bm25Ranker.cs`, `src/CVRanker.Infrastructure/Pdf/PdfPigTextExtractor.cs`, `src/CVRanker.Domain/Pilot/WeightCalibrator.cs`, `test/CVRanker.Tests/PdfPortTests.cs`, `CONTEXT.md`, `docs/adr/0001-layered-architecture.md`
