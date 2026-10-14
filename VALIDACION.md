# Verificación de la primera implementación

Base: tag v1.2.8 del repositorio original, commit 10643a08b71e14f9640ec83b4708e4d7af41fefb.

- Compilación completa C# con Mono 6.8 / referencias .NET Framework: correcta. Las advertencias de tipos duplicados provienen de System.Memory y System.ValueTuple de la base original y el runtime de Mono.
- 17 comprobaciones automáticas aprobadas: cierres 26/27, cruce anual, febrero, feriados, exclusión de totales, signos, tickets, filtros por categoría, margen ponderado, ticket neto, ceros, carga repetida, objetivos persistidos, calendario, sobreperformance y respaldo.
- Lectura real Autosur: 1.212 líneas, CA 63.379.113,22 y CG 13.980.834,47. Coincide con total fuente, incluyendo todas sus categorías.
- Lectura real Surisan: 352 líneas, CA 30.082.614,26 y CG 8.864.318,40. Coincide con total fuente. Se usó solo para comprobar compatibilidad del lector; no se incorporaron datos de Surisan a la app Autosur.
- El lector soporta tanto celdas XLSX con coordenadas como celdas secuenciales sin coordenadas. Los reportes reales usan la segunda variante.
- Sin cambios en Turnero.cs: conservación del conversor Renault.
- Fuentes de datos comerciales, clientes y vehículos ausentes del repositorio y paquete.

Validado en Windows Server mediante GitHub Actions: compilación .NET Framework, 17 pruebas del motor y apertura/renderizado del dashboard con una comprobación de avance al 120 %. La captura usa datos ficticios. Pendiente: ejecución del instalador en el equipo del usuario, impresión y exportación del turnero.

El código se publicó mediante el complemento GitHub en el repositorio privado Buscador-Autosur-Objetivos. No se publicó una release ni se modificó el repositorio original.
