# X7-PROVE (Proveedores Publicidad)
El presente proyecto es un intermediario entre el servicio de SAP y el servicio X7-PROVE de Renappo <br/>
Como en este servicio el circuito es el siguiente:

SAP --> X7-PROVE --> Renappo

### Requisitos
Para invocar al servicio precisara los siguientes items:
* Tener instalado el certificado X509 (para lo cual consulte la documentacion correspondiente)

### Invocacion
Para poder invocar al presente servicio debe hacerlo mediante la url <br/>
Desarrollo: [**http://ansesnegodesapp/X7-PROVE/X7CPRenappo.asmx**](http://ansesnegodesapp/X7-PROVE/X7CPRenappo.asmx) 

### Ejemplo
ejemplo de request en SoapUI

``` js
<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:ren="https://anses.gov.ar/ws/renappo">
   <soapenv:Header/>
   <soapenv:Body>
      <ren:consultarPadron>
         <ren:CUIT>30560956904</ren:CUIT>
      </ren:consultarPadron>
   </soapenv:Body>
</soapenv:Envelope>
```
