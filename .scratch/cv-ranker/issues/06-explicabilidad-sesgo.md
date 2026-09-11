Type: grilling
Status: resolved
Blocked by: 03

## Question

Definir explicabilidad y resguardo anti-sesgo: ¿qué razones muestra cada ranking, qué datos sensibles se anonimizar (nombre, foto, edad, género), qué disclaimer / auditoría necesita RRHH, y qué queda documentado como riesgo?

## Answer

- Razones: score total + desglose por peso colapsable + top-3 coincidentes siempre visibles + flag must-faltante.
- Anonimización: ocultar nombre/foto/edad/género/dirección en ranking y revisión ciega; PDF accesible con banner "fase ciega"; esos campos nunca son features.
- Auditoría: snapshot por ranking (JD+pesos+versión+scores) + disclaimer "apoyo, no decisión automática" + riesgo de sesgo documentado.
