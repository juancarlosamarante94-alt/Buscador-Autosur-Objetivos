# Buscador Autosur Objetivos

Aplicación Windows independiente basada en Buscador Autosur v1.2.8 (commit `10643a08b71e14f9640ec83b4708e4d7af41fefb`). Conserva Vehículos, Clientes y Turnero Renault y agrega Objetivos de Repuestos. No contiene el conversor Nissan.

## Instalación

En Windows, ejecutar `Buscador_VIN_Instalable/Instalar Buscador VIN.cmd`. Requiere Windows PowerShell 5.1 y .NET Framework 4.7.2 o superior. No requiere Python ni Excel. En la primera instalación copia las bases de la instalación original si están disponibles. Esas bases no se publican en GitHub.

La aplicación nueva se instala en `%LOCALAPPDATA%/Buscador Autosur Objetivos/app`, con acceso directo propio. Historial, objetivos y calendario se guardan fuera de la carpeta reemplazable, en `datos/objetivos.xml`, con respaldo de la versión anterior. La desinstalación conserva los datos. La aplicación original no se elimina ni se cambia.

## Reglas de ventas

- Octubre = 27 de septiembre al 26 de octubre, inclusive; mismo criterio para todos los meses.
- Cada carga es el reporte **acumulado completo desde el 27 hasta su fecha de corte**. Reemplaza el período, conserva objetivos y demás períodos, y preserva todas las líneas legítimas. Repetir una carga no suma ventas.
- Fechas fuera del período seleccionado se ignoran por defecto. Una opción explícita permite importarlas en sus propios períodos; cada período importado reemplaza su acumulado y debe estar completo dentro del archivo.
- Fecha D, tipo H/I, letra J, comprobante K, sector Q, venta CA, utilidad a costo de reposición CG. Se excluyen fila final y separadores sin fecha ni comprobante.
- CA y CG mantienen sus signos. Margen = suma CG / suma CA. Los porcentajes de las líneas no se promedian.
- Interno, Garantía y Cortesía se incluyen por defecto como en la planilla. Parámetros permite excluir cada categoría en venta, utilidad, tickets y avance, para todos los períodos de esta aplicación.
- El desglose Interno/Garantía/Cortesía ya pertenece al sector Q. No vuelve a sumarse al total. La clasificación usa H, sin deducir que un comprobante interno sea garantía.
- Tickets = documentos distintos por concesionario + sucursal + tipo + letra + número. Notas de crédito y débito ajustan importes y ticket promedio en su fecha de emisión sin modificar el número de tickets. Comprobantes internos y garantías cuentan cuando están incluidos, incluso si su importe es cero.
- Ticket promedio = venta neta del sector / tickets. Si no hay tickets o venta para un cociente, se muestra sin dato.
- Objetivo en pesos por período, distribución Taller en % y Mostrador como complemento a 100 %, margen único editable.
- Avance no se limita a 100 %; se muestra importe faltante o superado, por sector y total.

## Pantalla de Objetivos · versión 1.3.2

La pestaña Objetivos usa la cabecera Autosur en gris oscuro y amarillo, con cuatro tarjetas para venta, utilidad, margen y avance. Las tarjetas respetan la selección de fechas y sector; el avance y la proyección usan el corte acumulado guardado. Las barras comparan el acumulado de Total, Taller y Mostrador con sus objetivos, incluso por encima del 100 %. La marca vertical indica el objetivo y el color representa la venta.

**Cargar reporte** importa el acumulado. **Editar objetivos** permite cambiar el objetivo en pesos, la distribución y el margen, además de incluir o excluir categorías. **Más filtros** muestra Desde, Hasta, Reporte hasta, Guardar corte y Calendario nacional. **Ver desglose de Interno, Garantía y Cortesía** abre la tabla con venta, utilidad y comprobantes por categoría. En ventanas pequeñas se puede desplazar la pantalla.

La interfaz se valida con datos sintéticos, incluyendo objetivos superados en ambas bocas y total. Las capturas de la release son de demostración. El botón Buscar actualizaciones sigue disponible en la cabecera.

## Filtros, calendario y comparación

Indicadores, tickets, desglose y facturación diaria respetan fechas y sector. Las barras de objetivos siempre muestran el período acumulado completo en las tres bocas, y se identifican como tales. La proyección respeta el sector seleccionado.

Guardar `Reporte hasta` con la fecha hasta la que se descargó el acumulado, aunque ese día no haya ventas. No se infiere que la fecha de la última venta sea necesariamente el corte.

Calendario nacional: se incluye 2026 y se consultan automáticamente años faltantes en ArgentinaDatos. Se puede revisar o corregir contra https://www.argentina.gob.ar/feriados y guardar fechas nacionales por año. El calendario 2026 fue consultado el 14/10/2026, e incluye el feriado nacional extraordinario del 09/11/2026. Se excluyen sábados, domingos y las fechas registradas. No se excluyen automáticamente días no laborables opcionales ni feriados provinciales. Si falta el calendario de algún año necesario se conserva la venta real, pero no se inventa una proyección.

Proyección = venta acumulada / días laborables transcurridos × días laborables del período. Necesario diario = faltante / días laborables restantes. Comparación parcial: igual cantidad de días laborables disponibles en ambos períodos. El período anterior se etiqueta incompleto si no tiene corte al día 26. La cifra previa no se presenta como cierre completo en ese caso.

## Publicación

Destino previsto: `juancarlosamarante94-alt/Buscador-Autosur-Objetivos`. El código está en ese repositorio independiente; nunca en `Buscador-Autosur`. Las actualizaciones consultan solo el repositorio nuevo. Los repositorios privados usan el acceso de lectura cifrado configurado dentro de la app.

El workflow de Windows compila y ejecuta las pruebas antes de generar el paquete. Publica la release al actualizar main después de pasar las pruebas. No incluye datos de ventas, clientes ni vehículos.

## Actualizar desde la aplicación

El botón **Buscar actualizaciones** consulta la última release del repositorio propio, compara versiones, descarga el paquete, verifica su integridad si GitHub publica el digest, valida que corresponda a la app, cierra la instancia y ejecuta el instalador. Se conservan los datos y objetivos. Cada aplicación usa su propio repositorio, paquete y carpeta de datos.

Los repositorios son privados. En **Acceso GitHub** se configura una sola vez un token con permiso **Contents: Read-only** limitado al repositorio de esta app. El token se cifra con DPAPI para el usuario de Windows, fuera de la carpeta de instalación; nunca se publica. La app funciona sin token para consultas y reportes; solo las actualizaciones privadas lo requieren. Si el token vence o se revoca, se vuelve a configurar. No se guardan credenciales en código ni en releases.

El workflow de Windows valida cálculos, interfaz y almacenamiento cifrado; publica la release de la versión indicada en Actualizaciones.cs después de completar las pruebas. Las actualizaciones no modifican datos de otra app.
