Type: research
Status: resolved

## Question

¿Cómo evaluar el rankeador sin (o con) histórico? Definir protocolo piloto: 3 ofertas, métricas (P@10, MRR, nDCG si hay juicios, o revisión ciega RRHH), si el "Si" de Q8 significa que existe histórico reutilizable, y criterio de éxito mínimo (contratado en top-10 + ahorro triaje).

## Answer

Sin histórico (decidido: revisión ciega): piloto prospectivo sobre 3 ofertas cerradas, contratado oculto al equipo técnico. Congelar JD+CVs+versión scorer, rankear ciego, juicio ciego RRHH (top-20+contratado+10 aleatorios, binario mínimo / 0–3 recomendado). Métricas: P@10 + MRR primarias, nDCG@10 si graduado; `trec_eval`, bpref si incompleto. Éxito: contratado top-10 en ≥2/3 (o MRR≥0.2) + ahorro triaje ≥50%; n=3 mide viabilidad, no significancia. Coste ~90 juicios ≈5–8h RRHH. Nota en `research/04-evaluacion-piloto.md`.
