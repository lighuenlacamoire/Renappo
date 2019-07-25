using RenappoCertificacion.Negocio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Services;
using System.Xml;
using System.Xml.Linq;

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
                ////ASCIIEncoding encoder = new ASCIIEncoding();
                ////byte[] data = encoder.GetBytes(cuit); // a json object, or xml, whatever...

                //ASCIIEncoding encoding = new ASCIIEncoding();
                //string SampleXml = "<DataRequest xmlns=\"YourNamespaceHere\">" +
                //                                "<ID>" +
                //                                "</ID>" +
                //                                "<Data>" +
                //                                "</Data>" +
                //                            "</DataRequest>";

                //string postData = SampleXml.ToString();
                //byte[] data = encoding.GetBytes(postData);

                ////XmlDocument document = new XmlDocument();
                ////document.LoadXml();

                //HttpWebRequest request = WebRequest.Create(new Uri("https://localhost:44392/generarInformeDeGastosC75.asmx")) as HttpWebRequest;
                //request.Accept = "text/xml";
                //request.Method = "POST";
                //request.Headers.Add("SOAPAction", "https://ws-si.mecon.gov.ar/ws/informeDeGastosMsg/generarInformeDeGastosPortType");
                ////request.ContentType = "text/xml; charset=utf-8";
                //request.ContentType = "application/xml";
                //request.ContentLength = data.Length;                            

                //request.GetRequestStream().Write(data, 0, data.Length);

                //HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                ///////////////////

                //HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri("https://localhost:44392/generarInformeDeGastosC75.asmx"));
                //request.Method = "POST";
                //request.ContentType = "application/xml";
                //request.Accept = "application/xml";
                //request.Headers.Add("SOAPAction", "https://ws-si.mecon.gov.ar/ws/informeDeGastosMsg/generarInformeDeGastosPortType");
                //request.ProtocolVersion = HttpVersion.Version11;

                //XElement redmineRequestXML =
                //   new XElement("generarInformeDeGastosPortType",
                //    new XElement("generarInformeDeGastos", "talvez")
                //);

                //byte[] bytes = Encoding.UTF8.GetBytes(redmineRequestXML.ToString());

                //request.ContentLength = bytes.Length;

                //using (Stream putStream = request.GetRequestStream())
                //{
                //    putStream.Write(bytes, 0, bytes.Length);
                //}

                string aa = string.Empty;

                //// Log the response from Redmine RESTful service
                //using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                //using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                //{
                //    aa= reader.ReadToEnd();
                //}

                /////////////////////

                string soap =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<soapenv:Envelope 
   xmlns:inf=""https://ws-si.mecon.gov.ar/ws/informeDeGastosMsg"" 
   xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soapenv:Body>
    <inf:generarInformeDeGastosPortType>
      <inf:generarInformeDeGastos>jkdhsjk</inf:generarInformeDeGastos>
    </inf:generarInformeDeGastosPortType>
  </soapenv:Body>
</soapenv:Envelope>";

                HttpWebRequest request = WebRequest.Create("https://localhost:44392/generarInformeDeGastosC75.asmx") as HttpWebRequest;
                request.Headers.Add("SOAPAction", "https://ws-si.mecon.gov.ar/ws/informeDeGastosMsg/generarInformeDeGastosPortType");
                request.ContentType = "text/xml;charset=\"utf-8\"";
                request.Accept = "text/xml";
                request.Method = "POST";

                using (Stream stm = request.GetRequestStream())
                {
                    using (StreamWriter stmw = new StreamWriter(stm))
                    {
                        stmw.Write(soap);
                    }
                }

                WebResponse response = request.GetResponse();

                //Stream responseStream = response.GetResponseStream();
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    aa= reader.ReadToEnd();
                }

                if (!string.IsNullOrEmpty(aa))
                {
                    Console.WriteLine("eeee");
                }


                return _recursos.obtenerCertificado(cuit);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
