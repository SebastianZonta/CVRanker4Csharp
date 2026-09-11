Type: research
Status: resolved

## Question

¿Qué parámetros y ponderación funcionan sin LLM para rankear CVs (skills, años experiencia, educación, idiomas, keywords, recencia)? Comparar similitud textual (BM25/TF-IDF) + cobertura must-have/nice-to-have + reglas con pesos ajustables por oferta, y proponer set v1 con pesos iniciales y cómo calibrarlos sin histórico etiquetado.

## Answer

Híbrido: BM25 (k1=1.2, b=0.75) + cobertura must-have/nice-to-have + reglas (experiencia/recencia, educación, idiomas, título). Score = 100×Σw·f, f∈[0,1] normalizado intra-oferta: must-have 0.35 · BM25 0.25 · nice-to-have 0.15 · experiencia 0.12 · educación 0.05 · título/seniority 0.05 · idiomas 0.03. Veto blando (must=0 → cola, no rechazo). Top-3 contribuyentes como explicabilidad. Calibrar: 3 pilotos, juicio ciego top-10 RRHH, grid pasos 0.05 sobre (must,BM25,nice) objetivo nDCG@10+P@10, afinar b/k1 solo en empates, recalibrar cada ~10 ofertas. Nota completa en `research/03-parametros-pesos.md`.
