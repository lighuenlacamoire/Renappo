using iTextSharp;
using iTextSharp.text;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web;
using System.Web.Services.Protocols;
using System.Xml;
using System.Xml.Linq;
using X7Renappo.Models;

namespace X7Renappo.Negocio
{
    public class Recursos
    {
        private static readonly ILog log = LogManager.GetLogger("obtenerCertificado");

        public Recursos()
        {

        }

        public Proveedor consultarPadron(string cuit)
        {
            cuit = Funciones.ConvertToCUIT(cuit);

            Certificacion[] certificaciones = new Certificacion[0];
            Proveedor proveedor = new Proveedor();

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3 |
                                                   SecurityProtocolType.Tls | SecurityProtocolType.Tls11;

            ServicePointManager.ServerCertificateValidationCallback += RemoteCertificateValidationCallback;

            HttpWebRequest request = Funciones.GenerarRequest("https://renappo.argentina.gob.ar/apiAnses/proveedor.php?cuit=" + cuit);

            string content = string.Empty;

            try
            {

                WebResponse response = (HttpWebResponse)request.GetResponse();

                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    content = reader.ReadToEnd();
                }

                certificaciones = JsonConvert.DeserializeObject<Certificacion[]>(content);

                if (certificaciones != null && certificaciones.Any())
                {
                    IT_Detalle detalle = new IT_Detalle();
                    List<IT_Actividad> actividades = new List<IT_Actividad>();

                    foreach (var certificacion in certificaciones)
                    {
                        detalle.RazonSocial = certificacion.RazonSocial;
                        detalle.Intermediario = certificacion.Medio;
                        detalle.Habilitado = certificacion.Habilitado;
                        detalle.FechaVigenciaDesde = Funciones.ConvertToFechaFormato(Funciones.ConvertToFechaVigencia(certificacion.FechaVigencia, Parametros.FechaVigencia.Desde), null);
                        detalle.FechaVigenciaHasta = Funciones.ConvertToFechaFormato(Funciones.ConvertToFechaVigencia(certificacion.FechaVigencia, Parametros.FechaVigencia.Hasta), null);

                        detalle.Tarifario = SubirArchivo(certificacion.Tarifario);
                        detalle.CoberturaGeografica = SubirArchivo(certificacion.CoberturaGeografica);

                        if (!string.IsNullOrEmpty(certificacion.Certificado))
                        {
                            var elementos = XElement.Parse(certificacion.Certificado);

                            if (elementos.HasElements)
                            {
                                foreach (var elemento in elementos.Elements())
                                {
                                    var elementosTexto = elemento.Descendants()?.Where(x => x.FirstNode != null && x.FirstNode.NodeType == XmlNodeType.Text);

                                    var actividad = new IT_Actividad();
                                    actividad.Actividad = elementosTexto?.FirstOrDefault(x => x.Parent.Value.Contains("CERTIFICADO HABILITANTE") && x.Name == "strong").Value;
                                    actividad.Fecha = Funciones.ConvertToFechaFormato(elementosTexto?.FirstOrDefault(x => x.Value.Contains("Fecha:"))?.Value, "Fecha:");
                                    actividad.Vencimiento = Funciones.ConvertToFechaFormato(elementosTexto?.FirstOrDefault(x => x.Value.Contains("Vencimiento:"))?.Value, "Vencimiento:");
                                    actividad.Certificado = elementosTexto?.FirstOrDefault(x => x.Value.Contains("RENAPPO")).Value.Trim();

                                    actividades.Add(actividad);
                                }
                            }
                        }
                    }

                    proveedor.Detalle = detalle;
                    proveedor.Actividades = actividades.ToArray();
                }

            }
            catch (WebException exp)
            {
                if (exp.Response != null)
                {
                    using (StreamReader sr = new StreamReader(exp.Response.GetResponseStream()))
                    {

                        content = sr.ReadToEnd();
                    }
                }
                else
                {
                    content = exp.Message;
                }

                throw new Exception(content);
            }

            return proveedor;
        }

        private string SubirArchivo(string url)
        {
            string Id = string.Empty;
            try
            {
                string digiwebEndpoint = ConfigurationManager.AppSettings["DigiWebEndpoint"];
                string digiDocCodigoSistema = ConfigurationManager.AppSettings["DigiDocCodigoSistema"];
                string digiDocCodigoExterno = ConfigurationManager.AppSettings["DigiDocCodigoExterno"];
                string digiDocCodigoId = ConfigurationManager.AppSettings["DigiDocCodigoId"];

                DigiWeb.DigitalizacionServicio service = new DigiWeb.DigitalizacionServicio();
                service.Credentials = CredentialCache.DefaultCredentials;
                service.Url = digiwebEndpoint;

                string oRuta = service.CalcularRutaSistema(digiDocCodigoSistema);
                string filename = string.Empty;
                byte[] oFileToSave = null;
                filename = Path.GetFileName(url);
                filename = filename.Substring(0, filename.IndexOf(".")) + ".pdf";

                Guid oGuid_A_Traer;

                DigiWeb.EDocumentoOriginal oEDocumentoOriginal = new DigiWeb.EDocumentoOriginal();
                oEDocumentoOriginal.Id = Guid.NewGuid();
                oEDocumentoOriginal.CodigoSistema = digiDocCodigoSistema;
                oEDocumentoOriginal.TipoEDocumentoId = Convert.ToInt32(digiDocCodigoId);
                oEDocumentoOriginal.EstadoEDocumentoId = 1;
                oEDocumentoOriginal.Entidad = "0";
                oEDocumentoOriginal.PreCuil = 0;
                oEDocumentoOriginal.NumeroDocumento = "0";
                oEDocumentoOriginal.DigitoVerificador = 0;
                oEDocumentoOriginal.TipoTramite = 0;
                oEDocumentoOriginal.Secuencia = 0;
                oEDocumentoOriginal.Nombre = filename;
                oEDocumentoOriginal.Ruta = oRuta + "\\" + filename;
                oEDocumentoOriginal.FechaIndexacion = DateTime.Now;
                oEDocumentoOriginal.CodigoExterno = digiDocCodigoExterno;

                service.GuardarEDocumentoV2(oEDocumentoOriginal);

                oGuid_A_Traer = oEDocumentoOriginal.Id;

                string content = string.Empty;

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3 |
                                                       SecurityProtocolType.Tls | SecurityProtocolType.Tls11;

                ServicePointManager.ServerCertificateValidationCallback += RemoteCertificateValidationCallback;

                HttpWebRequest request = Funciones.GenerarRequest(url);
                
                WebResponse response = (HttpWebResponse)request.GetResponse();

                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    oFileToSave = reader.CurrentEncoding.GetBytes(reader.ReadToEnd());
                }

                File.WriteAllBytes(oRuta + "\\" + filename, oFileToSave);

                var oEDocumentoSubido = service.TraerEDocumento(oGuid_A_Traer);

                if (oEDocumentoSubido != null)
                {
                    Id = Convert.ToString(oGuid_A_Traer);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return Id;
        }


        //protected string UploadFile()filename
        //{
        //    log.Debug("AltaFectura.ascx.cs - UploadFile() - Inicio.");
        //    if (oRta != string.Empty)
        //    {
        //        deleteImgFunction();
        //        oRta = string.Empty;
        //    }
        //    if (!FileUploadControl.HasFile)
        //    {
        //        StatusLabel.Text = "Upload status: Debe elegir un archivo a subir!";
        //        oRta = string.Empty;
        //        return oRta;
        //    }
        //    if (FileUploadControl.PostedFile.ContentType != "image/jpeg" && FileUploadControl.PostedFile.ContentType != "image/png" && FileUploadControl.PostedFile.ContentType != "application/pdf")
        //    {
        //        StatusLabel.Text = "Upload status: Solo archivos .jpg, .png y .pdf son aceptados!";
        //        oRta = string.Empty;
        //        return oRta;
        //    }
        //    if (FileUploadControl.PostedFile.ContentLength > 1048576)
        //    {
        //        StatusLabel.Text = "Upload status:Los Archivos pueden ser de hasta 1 MB !";
        //        oRta = string.Empty;
        //        return oRta;
        //    }
        //    try
        //    {
        //        log.Debug("AltaFectura.ascx.cs - UploadFile() - entra al try");
        //        DigiWeb.DigitalizacionServicio service = new DigiWeb.DigitalizacionServicio();
        //        service.Credentials = CredentialCache.DefaultCredentials;
        //        log.Debug("AltaFectura.ascx.cs - UploadFile() - antes de service.CalcularRutaSistema()");
        //        oRuta = service.CalcularRutaSistema("3W-SERVPUB");
        //        log.Debug("AltaFectura.ascx.cs - UploadFile() - paso service.CalcularRutaSistema() - ruta calculada: oRuta: " + oRuta.ToString());
        //        string filename = string.Empty;
        //        byte[] oFileToSave = null;
        //        filename = Path.GetFileName(FileUploadControl.FileName);
        //        filename = filename.Substring(0, filename.IndexOf(".")) + ".pdf";
        //        log.Debug("AltaFectura.ascx.cs - UploadFile() - filename: " + filename.ToString());
        //        if (FileUploadControl.PostedFile.ContentType == "image/jpeg" || FileUploadControl.PostedFile.ContentType == "image/png")
        //        {
        //            log.Debug("AltaFectura.ascx.cs - UploadFile() - es imagen: convertir a pdf y cargar a oFileToSave");
        //            Stream stream = new MemoryStream(FileUploadControl.FileBytes);
        //            oFileToSave = ConvertImageToPdf(filename, stream);
        //        }
        //        else
        //        {
        //            log.Debug("AltaFectura.ascx.cs - UploadFile() - es pdf: cargar a oFileToSave");
        //            oFileToSave = FileUploadControl.FileBytes;
        //        }
        //        #region Upload
        //        log.Debug("AltaFectura.ascx.cs - UploadFile() - convertir oFileToSave a Base64String");
        //        String oFileToSaveStr = Convert.ToBase64String(oFileToSave);
        //        ServPubSitio.AnsesUploadWS.DigitalizacionWS oDigitalizacionWS = new ServPubSitio.AnsesUploadWS.DigitalizacionWS();
        //        oDigitalizacionWS.Credentials = CredentialCache.DefaultCredentials;
        //        filename = filename.Insert(filename.IndexOf("."), "_" + DateTime.Now.Hour.ToString().PadLeft(2, '0') + DateTime.Now.Minute.ToString().PadLeft(2, '0') + DateTime.Now.Second.ToString().PadLeft(2, '0'));
        //        log.Debug("AltaFectura.ascx.cs - UploadFile() - volver a convertir filename: " + filename.ToString());

        //        //string oUploadFileRta = oDigitalizacionWS.UploadFile(oFileToSaveStr, oRuta + "\\", filename);

        //        log.Debug("AltaFectura.ascx.cs - UploadFile() - antes de usar PostedFile.SaveAs()");
        //        //Como lo utilizamos en BEFE/GESCOM
        //        FileUploadControl.PostedFile.SaveAs(oRuta + "\\" + filename);
        //        log.Debug("AltaFectura.ascx.cs - UploadFile() - después de usar PostedFile.SaveAs()");




        //        //log.Debug("AltaFectura.ascx.cs - UploadFile() - ruta completa para subir archivo: oUploadFileRta: " + oUploadFileRta.ToString());
        //        //if (oUploadFileRta == "OK") {
        //        //    //    imgPreview.Visible = true;
        //        //    //    imgDelete.Visible = true;
        //        //    //    // imgPreview.ImageUrl = oRuta + "\\" + filename;
        //        //    //    string oUrlRuta = "http://10.86.37.148/SERVPUB.SITE/Uploaded/";
        //        //    //    imgPreview.ImageUrl = oUrlRuta + filename;
        //        //    //    imgPreview.AlternateText = filename;
        //        //}
        //        #endregion
        //        Guid oGuid;
        //        oGuid = Guid.NewGuid();
        //        Guid oGuid_A_Traer;
        //        if (oPIdDigi != "")
        //        {
        //            DigiWeb.EDocumento oEDocumento = new DigiWeb.EDocumento();
        //            oEDocumento.Id = new Guid(oPIdDigi);
        //            oEDocumento.CodigoSistema = "3W-SERVPUB";
        //            oEDocumento.TipoEDocumentoId = 1054;
        //            oEDocumento.EstadoEDocumentoId = 1;
        //            oEDocumento.Entidad = "0";
        //            oEDocumento.PreCuil = 0;
        //            oEDocumento.NumeroDocumento = "0";
        //            oEDocumento.DigitoVerificador = 0;
        //            oEDocumento.TipoTramite = 0;
        //            oEDocumento.Secuencia = 0;
        //            oEDocumento.Nombre = filename;
        //            oEDocumento.Ruta = oRuta + "\\" + filename;
        //            oEDocumento.FechaIndexacion = DateTime.Now;
        //            log.Debug("AltaFectura.ascx.cs - UploadFile() - antes de service.ActualizarEDocumento - con oEDocumento.Id: " + oEDocumento.Id.ToString());
        //            service.ActualizarEDocumento(oEDocumento);
        //            oGuid_A_Traer = oEDocumento.Id;
        //        }
        //        else
        //        {
        //            DigiWeb.EDocumentoOriginal oEDocumentoOriginal = new DigiWeb.EDocumentoOriginal();
        //            oEDocumentoOriginal.Id = oGuid;
        //            oEDocumentoOriginal.CodigoSistema = "3W-SERVPUB";
        //            oEDocumentoOriginal.TipoEDocumentoId = 1054;
        //            oEDocumentoOriginal.EstadoEDocumentoId = 1;
        //            oEDocumentoOriginal.Entidad = "0";
        //            oEDocumentoOriginal.PreCuil = 0;
        //            oEDocumentoOriginal.NumeroDocumento = "0";
        //            oEDocumentoOriginal.DigitoVerificador = 0;
        //            oEDocumentoOriginal.TipoTramite = 0;
        //            oEDocumentoOriginal.Secuencia = 0;
        //            oEDocumentoOriginal.Nombre = filename;
        //            oEDocumentoOriginal.Ruta = oRuta + "\\" + filename;
        //            oEDocumentoOriginal.FechaIndexacion = DateTime.Now;
        //            log.Debug("AltaFectura.ascx.cs - UploadFile() - antes de service.GuardarEDocumentoV2 - con oGuid: " + oGuid.ToString());
        //            service.GuardarEDocumentoV2(oEDocumentoOriginal);
        //            oGuid_A_Traer = oEDocumentoOriginal.Id;
        //        }
        //        log.Debug("AltaFectura.ascx.cs - UploadFile() - pasó actualizar/guardar documento - antes de service.TraerEDocumento: oGuid_A_Traer: " + oGuid_A_Traer.ToString());
        //        DigiWeb.EDocumento oEDocumentoSubido = service.TraerEDocumento(oGuid_A_Traer);
        //        if (oEDocumentoSubido != null)
        //        {
        //            StatusLabel.Text = "Upload status: Documento Subido con Exito. " + oEDocumentoSubido.Nombre.ToString();
        //            log.Debug("AltaFectura.ascx.cs - UploadFile() - Documento Subido con Exito");
        //            imgPreview.Visible = true;
        //            imgDelete.Visible = true;
        //            //imgPreview.ImageUrl = oEDocumentoSubido.Ruta ;
        //            if (oEDocumentoSubido.Nombre.ToLower().Contains(".pdf") == true)
        //            {
        //                imgPreview.ImageUrl = "~/imagenes/Logo_PDF.png";
        //                imgPreview.ToolTip = Encriptar(oEDocumentoSubido.Ruta);
        //                imgPreview.AlternateText = Encriptar(oEDocumentoSubido.Ruta);
        //            }
        //            else
        //            {
        //                string oImageUrl = "verPdf.aspx" + "?Id=" + Encriptar(oEDocumentoSubido.Ruta);
        //                imgPreview.ImageUrl = oImageUrl;
        //                imgPreview.AlternateText = oEDocumentoSubido.Nombre;
        //            }
        //            imgPreview.ToolTip = oEDocumentoSubido.Nombre;
        //            oRta = oEDocumentoSubido.Id.ToString();
        //            return oRta;
        //        }
        //        else
        //        {
        //            log.Debug("AltaFectura.ascx.cs - UploadFile() - El documento no se pudo subir");
        //            ((PaginaBase)this.Page).Informar("No se pudo subir el documento");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        StatusLabel.Text = "Upload status: No se pudo subir el archivo. Ocurrió el siguiente error: " + ex.Message;
        //        log.Error("AltaFactura.ascx.cs - UploadFile() - error: " + ex.Message + " - " + ex.InnerException + " - " + ex.StackTrace);
        //        return string.Empty;
        //    }
        //    return oRta;
        //}

        public SoapException manejoError(Exception ex, string Username, string Metodo)
        {
            log.Error("Error " + Metodo + ": " + ex.Message);

            // Build the detail element of the SOAP fault.
            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();

            System.Xml.XmlNode node = doc.CreateNode(XmlNodeType.Element, SoapException.DetailElementName.Name, SoapException.DetailElementName.Namespace);

            System.Xml.XmlNode details = doc.CreateNode(XmlNodeType.Element, "Detalle", "http://z2.anses.gov.ar/");
            details.InnerText = "Metodo: " + Metodo + " Usuario - [" + Username + "] Mensaje: " + ex.Message;

            node.AppendChild(details);

            //Throw the exception    
            SoapException se = new SoapException("Error", SoapException.ClientFaultCode, "", ex);

            log.Error(Metodo, ex);

            return se;
        }
        public static bool RemoteCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
}