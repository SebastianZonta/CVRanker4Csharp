Type: research
Status: resolved

## Question

¿Qué stack C# on-premise sin APIs de pago permite rankear 20-200 PDFs en inglés contra una job description? Comparar TF-IDF / BM25 / Lucene.NET / ML.NET / embeddings locales ONNX sin coste, más extracción texto PDF en C# (PdfPig, iText, PdfSharp, OCR si hace falta), con trade-offs, licencias y recomendación de combinación v1.

## Answer

Stack v1: PdfPig (extracción) + BM25 propio (~50 líneas) o Lucene.NET 4.8 beta con BM25Similarity + scoring ponderado por secciones + highlighter para top-3 razones. ML.NET descartado en v1 (necesita etiquetado, no trae ranking). Embeddings ONNX all-MiniLM-L6-v2 como mejora v1.1, no base. iTextSharp descartado por AGPL; PdfSharp solo generación; Tesseract solo fallback escaneados.

Detalle completo en el reporte del subagente (ses_f6d7cf3b6ffeOA9bfMmxRTo50h): BM25>TF-IDF por longitud; Lucene.NET Apache 2.0 beta estable, `BM25Similarity`, `StandardAnalyzer`, `RAMDirectory`, Highlighter; ML.NET `FeaturizeText` solo con histórico; ONNX `Microsoft.ML.OnnxRuntime` + MiniLM 384dim ~80MB ~5ms CPU; PdfPig Apache 2.0 `ContentOrderTextExtractor` + `GetWords`; iText AGPL/copyleft; OCR solo si página sin texto.

Recomendación concreta: 1) PdfPig texto+palabras, 2) BM25 por campo (skills×3, must-have filtro duro, experiencia/educación×2, keywords×1) o Lucene RAMDirectory por oferta, 3) score descompuesto + top-3 términos, 4) diferir ONNX re-rank v1.1, ML.NET con histórico, Tesseract fallback.

Fuentes: lucenenet.apache.org, github.com/apache/lucenenet, learn.microsoft.com (ML.NET), github.com/UglyToad/PdfPig, itextpdf AGPL, huggingface all-MiniLM-L6-v2, nuget Lucene.Net/PdfPig (ver reporte para URLs exactas).
