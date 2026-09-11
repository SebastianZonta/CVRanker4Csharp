Type: task
Status: resolved

## Question

Inventario de la app actual para fijar la integración: versión .NET, estructura solución / nuevo `.csproj`, schema de la job description guardada (campos, texto libre vs estructurado), dónde vive el listado de CVs/PDFs, cómo se despliega (on-premise, sin GPU), y restricciones de dependencias NuGet.

Registrar hechos que los demás tickets necesitan: versión .NET, ejemplo real (anonimizado) de JD, ruta de integración, y si los PDFs son nativos o escaneados.

## Answer

- .NET 10, nuevo `CVRanker.csproj` en la solución, con dependencias NuGet permitidas (MIT/Apache: PdfPig, Lucene.NET OK).
- JD = bloque de párrafos en texto libre (lo típico de una JD), sin must-have/nice-to-have estructurados → hay que añadirlos (lo cierra [Oferta y UX](05-oferta-ux.md)).
- CVs = blob storage Azure; en esta etapa se levantan por path para el spike/piloto.
- Despliegue on-premise CPU confirmado; ranking en vista RRHH + endpoint nuevo.
