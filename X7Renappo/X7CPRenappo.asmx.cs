using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.Web.Services;
using System.Web.Services.Protocols;
using X7Renappo.Negocio;

namespace X7Renappo
{
    /// <summary>
    /// Descripción breve de X7CPRenappo
    /// </summary>
    [WebService(Namespace = "https://anses.gov.ar/ws/renappo",Name = "X7CPRenappo", Description = "Consulta de Proveedores Renappo")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // Para permitir que se llame a este servicio web desde un script, usando ASP.NET AJAX, quite la marca de comentario de la línea siguiente. 
    // [System.Web.Script.Services.ScriptService]
    public class X7CPRenappo : System.Web.Services.WebService
    {
        private Recursos _recursos = new Recursos();
        private SoapException soapException = new SoapException();

        [WebMethod(Description = "Consulta de Proveedores Renappo")]
        [SoapDocumentMethod(Action = "/consultarPadron")]
        public Models.Proveedor consultarPadron(string CUIT)
        {
            try
            {
                return _recursos.consultarPadronRest(CUIT);
            }
            catch (WebException exp)
            {
                throw _recursos.manejoWebError(exp);
            }
            catch (Exception ex)
            {
                throw _recursos.manejoError(ex);
            }
        }
    }
}
