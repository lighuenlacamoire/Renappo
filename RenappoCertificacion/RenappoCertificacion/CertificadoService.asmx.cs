using RenappoCertificacion.Negocio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;

namespace RenappoCertificacion
{
    /// <summary>
    /// Descripción breve de CertificadoService
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    public class CertificadoService : System.Web.Services.WebService
    {
        private Recursos _recursos = new Recursos();

        [WebMethod]
        public Models.Certificacion obtenerCertificado(string cuit)
        {
            try
            {
                return _recursos.obtenerCertificado(cuit);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
