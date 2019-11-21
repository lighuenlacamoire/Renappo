using iTextSharp;
using iTextSharp.text;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
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
using X7Renappo.Soap;

namespace X7Renappo.Negocio
{
    public class Recursos
    {
        private static readonly ILog log = LogManager.GetLogger("X7-Prove - ConsultaPadron: ");

        private static System.Xml.XmlDocument doc = new System.Xml.XmlDocument();

        private static System.Xml.XmlNode node = doc.CreateNode(XmlNodeType.Element, SoapException.DetailElementName.Name, SoapException.DetailElementName.Namespace);

        private static SoapError soapError = new SoapError();
        private static string result = "";
        private static string faultFactor = "Renappo";
        private static string faultDetail = "Error inesperado revise el log para mayor detalle";

        public Recursos()
        {

        }


        public Proveedor consultarPadronRest(string cuit)
        {
            log.Info("Cuit Ingresado " + cuit);
            cuit = Funciones.ConvertToCUIT(cuit);

            Certificacion[] certificaciones = new Certificacion[0];
            Proveedor proveedor = new Proveedor();
            IT_Mensaje mensaje = new IT_Mensaje();

            string usuarioProxy = ConfigurationManager.AppSettings["CPA_Proxy_Usuario"];
            string passwdProxy = ConfigurationManager.AppSettings["CPA_Proxy_Passwd"];
            string domainProxy = ConfigurationManager.AppSettings["CPA_Proxy_Dominio"];

            string WSApiEndpoint = ConfigurationManager.AppSettings["WS_ApiEndpoint"];
            string WSApiParameter = ConfigurationManager.AppSettings["WS_ApiParameter"];
            Uri WSUriApi = new Uri(WSApiEndpoint + "?" + WSApiParameter + "=" + cuit);

            var client = new RestClient(WSApiEndpoint);
            client.Proxy = Funciones.CrearProxy();

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3 |
                                                   SecurityProtocolType.Tls | SecurityProtocolType.Tls11;

            ServicePointManager.ServerCertificateValidationCallback += RemoteCertificateValidationCallback;

            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(delegate { return true; });

            mensaje.Consulta = cuit;
            mensaje.Mensaje = string.Empty;

            var request = new RestRequest("?" + WSApiParameter + "=" + cuit, Method.GET);
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            string content = string.Empty;

            log.Info("Invocacion al Endpoint de Renappo " + WSApiEndpoint + " con el siguiente parametro y valor " + WSApiParameter + "=" + cuit);
            timer.Start();
            var response = client.Execute(request);
            timer.Stop();
            long tiempo = timer.ElapsedMilliseconds / 1000;
            log.Info("Tiempo Renappo - invocacion: "+ tiempo+ "segundos");

            timer.Restart();
            if (response != null && !string.IsNullOrEmpty(response.Content) && response.StatusCode == HttpStatusCode.OK)
            {
                log.Info("Respuesta Renappo - Servicio : " + Environment.NewLine + JValue.Parse(response.Content)?.ToString(Newtonsoft.Json.Formatting.Indented));

                log.Info("Parseando el la respuesta previamente obtenida");
                certificaciones = JsonConvert.DeserializeObject<Certificacion[]>(response.Content);
            }
            else
            {
                mensaje.Tipo = "E";
                log.Info(
                    "Respuesta Renappo - Servicio : " +
                    Environment.NewLine +
                    "Estado - " + response?.StatusCode +
                    Environment.NewLine +
                    "Descripcion" + response?.StatusDescription);
            }

            if (certificaciones != null && certificaciones.Any())
            {
                if (certificaciones.Any(x => x.Fallo))
                {
                    mensaje.Mensaje = certificaciones.FirstOrDefault(x => x.Fallo)?.Mensaje;
                    mensaje.Tipo = "E";
                    proveedor.Detalle = null;
                    proveedor.Actividades = null;
                }
                else
                {
                    string mensajeVacio = "No se pudo obtener información";
                    mensaje.Tipo = "S";

                    IT_Detalle detalle = new IT_Detalle();
                    List<IT_Actividad> actividades = new List<IT_Actividad>();

                    foreach (var certificacion in certificaciones)
                    {
                        detalle.RazonSocial = !string.IsNullOrEmpty(certificacion.RazonSocial) ? certificacion.RazonSocial : mensajeVacio;
                        detalle.Intermediario = !string.IsNullOrEmpty(certificacion.Medio) ? certificacion.Medio : mensajeVacio;
                        detalle.Habilitado = !string.IsNullOrEmpty(certificacion.Habilitado) ? certificacion.Habilitado : mensajeVacio;
                        detalle.FechaVigenciaDesde = Funciones.ConvertToFechaFormato(Funciones.ConvertToFechaVigencia(certificacion.FechaVigencia, Parametros.FechaVigencia.Desde), null);
                        detalle.FechaVigenciaHasta = Funciones.ConvertToFechaFormato(Funciones.ConvertToFechaVigencia(certificacion.FechaVigencia, Parametros.FechaVigencia.Hasta), null);

                        #region Tarifario
                        if(!string.IsNullOrEmpty(certificacion.Tarifario))
                        {
                            var tarifario = SubirArchivoRest(certificacion.Tarifario);
                            if(tarifario != null && !tarifario.Error && !string.IsNullOrEmpty(tarifario.Id))
                            {
                                detalle.Tarifario = tarifario.Id;
                            }
                            else
                            {
                                mensaje.Tipo = "W";
                                mensaje.Mensaje = mensaje.Mensaje + tarifario.Detalle + "; ";
                                detalle.Tarifario = null;
                            }
                        }
                        else
                        {
                            detalle.Tarifario = mensajeVacio;
                        }
                        #endregion

                        #region Cobertura Geografica
                        if (!string.IsNullOrEmpty(certificacion.CoberturaGeografica))
                        {
                            var cobertura = SubirArchivoRest(certificacion.CoberturaGeografica);
                            if (cobertura != null && !cobertura.Error && !string.IsNullOrEmpty(cobertura.Id))
                            {
                                detalle.CoberturaGeografica = cobertura.Id;
                            }
                            else
                            {
                                mensaje.Tipo = "W";
                                mensaje.Mensaje = mensaje.Mensaje + cobertura.Detalle + "; ";
                                detalle.CoberturaGeografica = null;
                            }
                        }
                        else
                        {
                            detalle.CoberturaGeografica = mensajeVacio;
                        }
                        #endregion

                        if (!string.IsNullOrEmpty(certificacion.Certificado))
                        {
                            log.Info("Parseando el campo certificado para obtener la/s actividad/es del Proveedor");
                            var elementos = XElement.Parse(certificacion.Certificado);

                            if (elementos.HasElements)
                            {
                                foreach (var elemento in elementos.Elements())
                                {
                                    var elementosTexto = elemento.Descendants()?.Where(x => x.FirstNode != null && x.FirstNode.NodeType == XmlNodeType.Text);

                                    if (elementosTexto != null && elementosTexto.Any())
                                    {
                                        var actividad = new IT_Actividad();
                                        actividad.Actividad = elementosTexto?.FirstOrDefault(x => x.Parent.Value.Contains("CERTIFICADO HABILITANTE") && x.Name == "strong").Value;
                                        actividad.Fecha = Funciones.ConvertToFechaFormato(elementosTexto?.FirstOrDefault(x => x.Value.Contains("Fecha:"))?.Value, "Fecha:");
                                        actividad.Vencimiento = Funciones.ConvertToFechaFormato(elementosTexto?.FirstOrDefault(x => x.Value.Contains("Vencimiento:"))?.Value, "Vencimiento:");
                                        actividad.Certificado = elementosTexto?.FirstOrDefault(x => x.Value.Contains("RENAPPO")).Value.Trim() ?? mensajeVacio;

                                        actividades.Add(actividad);
                                    }
                                }
                            }
                            proveedor.Actividades = actividades.ToArray();
                        }
                        else
                        {
                            log.Info("No se pudo obtener la/s actividad/es del Proveedor");
                        }
                    }
                    proveedor.Detalle = detalle;
                }
            }

            proveedor.Mensaje = mensaje;

            timer.Stop();
            tiempo = timer.ElapsedMilliseconds / 1000;
            log.Info("Tiempo Servicio - procesamiento: " + tiempo + "segundos");

            return proveedor;
        }

        public Proveedor consultarPadron(string cuit)
        {
            log.Info("Cuit Ingresado " + cuit);
            cuit = Funciones.ConvertToCUIT(cuit);

            Certificacion[] certificaciones = new Certificacion[0];
            Proveedor proveedor = new Proveedor();

            string usuarioProxy = ConfigurationManager.AppSettings["CPA_Proxy_Usuario"];
            string passwdProxy = ConfigurationManager.AppSettings["CPA_Proxy_Passwd"];
            string domainProxy = ConfigurationManager.AppSettings["CPA_Proxy_Dominio"];

            string WSApiEndpoint = ConfigurationManager.AppSettings["WS_ApiEndpoint"];
            string WSApiParameter = ConfigurationManager.AppSettings["WS_ApiParameter"];
            Uri WSUriApi = new Uri(WSApiEndpoint + "?" + WSApiParameter + "=" + cuit);

            log.Debug("Seteando PROXY.");
            string urlProxy = ConfigurationManager.AppSettings["CPA_Proxy_URL"];

            WebProxy Proxy = new WebProxy(new Uri(urlProxy));
            Proxy.BypassProxyOnLocal = false;
            Proxy.UseDefaultCredentials = false;
            Proxy.Credentials = new NetworkCredential(usuarioProxy, passwdProxy, domainProxy);

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3 |
                                                   SecurityProtocolType.Tls | SecurityProtocolType.Tls11;

            ServicePointManager.ServerCertificateValidationCallback += RemoteCertificateValidationCallback;

            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(delegate { return true; });

            log.Info("Invocacion al Endpoint de Renappo " + WSApiEndpoint + " con el siguiente parametro y valor " + WSApiParameter + "=" + cuit);
            //HttpWebRequest request = Funciones.GenerarRequest(WSApiEndpoint + "?" + WSApiParameter + "=" + cuit);
            HttpWebRequest request = WebRequest.Create(WSUriApi) as HttpWebRequest;
            request.ContentType = "text/html; charset=utf-8";
            //request.ContentType = "application/x-www-form-urlencoded";
            //request.Accept = "application/json";
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3";
            request.Method = WebRequestMethods.Http.Get;
            //request.KeepAlive = true;
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/77.0.3865.90 Safari/537.36";
            request.PreAuthenticate = true;
            //request.AuthenticationLevel = AuthenticationLevel.MutualAuthRequested;
            request.Proxy = Proxy;
            //request.Credentials = CredentialCache.DefaultCredentials;

            string content = string.Empty;

            WebResponse response = (HttpWebResponse)request.GetResponse();

            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                content = reader.ReadToEnd();
            }

            if (!string.IsNullOrEmpty(content) && content.Length > 0 && !content.StartsWith("[{"))
            {
                throw new Exception(content);
            }

            log.Info("Respuesta del Endpoint " + WSApiEndpoint + ": " + Environment.NewLine + content);

            log.Info("Parseando el la respuesta previamente obtenida");
            certificaciones = JsonConvert.DeserializeObject<Certificacion[]>(content);

            if (certificaciones != null && certificaciones.Any())
            {
                if (certificaciones.Any(x => x.Fallo))
                {
                    throw new Exception(certificaciones.FirstOrDefault(x => x.Fallo)?.Mensaje);
                }

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
                        log.Info("Parseando el campo certificado para obtener la/s actividad/es del Proveedor");
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

            return proveedor;
        }

        private IT_Archivo SubirArchivoRest(string url)
        {
            var archivo = new IT_Archivo();
            archivo.Id = string.Empty;
            archivo.Error = false;
            archivo.Detalle = string.Empty;
            archivo.Url = url;
            archivo.Extension = string.Empty;

            try
            {

                string content = string.Empty;
                byte[] oFileToSave = null;

                log.Info("Se descargara el certificado con la siguiente url " + url);

                var client = new RestClient();
                client.Proxy = Funciones.CrearProxy();
                client.BaseUrl = new Uri(url);

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3 |
                                                       SecurityProtocolType.Tls | SecurityProtocolType.Tls11;

                ServicePointManager.ServerCertificateValidationCallback += RemoteCertificateValidationCallback;

                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(delegate { return true; });

                log.Info("Invocando a la descarga del archivo con la siguiente url " + url);
                var request = new RestRequest();
                request.Method = Method.GET;

                log.Info("Obteniendo la respuesta del Endpoint " + url);
                oFileToSave = client.DownloadData(request);

                archivo.Nombre = Path.GetFileName(url);
                string filename = archivo.Nombre;

                archivo.Extension = CheckFileExtension(oFileToSave);

                if(string.IsNullOrEmpty(archivo.Extension))
                {
                    throw new Exception("El archivo no es valido "+ archivo.Nombre);
                }

                filename = filename.Substring(0, filename.IndexOf(".")) + "." + archivo.Extension;
                log.Info("Iniciando Configuracion de Digiweb para subir el archivo previamente descargado");

                string digiwebEndpoint = ConfigurationManager.AppSettings["DigiWebEndpoint"];
                string digiDocCodigoSistema = ConfigurationManager.AppSettings["DigiDocCodigoSistema"];
                string digiDocCodigoExterno = ConfigurationManager.AppSettings["DigiDocCodigoExterno"];
                string digiDocCodigoId = ConfigurationManager.AppSettings["DigiDocCodigoId"];

                DigiWeb.DigitalizacionServicio service = new DigiWeb.DigitalizacionServicio();
                service.Credentials = CredentialCache.DefaultCredentials;
                service.Url = digiwebEndpoint;

                log.Info("Invocando a Digiweb para obtener la ruta de subida del archivo");
                string oRuta = service.CalcularRutaSistema(digiDocCodigoSistema);

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

                log.Info("Invocando a Digiweb para la subida logica del archivo");
                service.GuardarEDocumentoV2(oEDocumentoOriginal);

                oGuid_A_Traer = oEDocumentoOriginal.Id;

                log.Info("Inicio subida fisica del archivo nombre " + filename + " a la siguiente ruta " + oRuta);
                File.WriteAllBytes(oRuta + "\\" + filename, oFileToSave);

                log.Info("Invocando a Digiweb para verificar que se haya hecho la subida del archivo");
                var oEDocumentoSubido = service.TraerEDocumento(oGuid_A_Traer);

                if (oEDocumentoSubido != null)
                {
                    archivo.Id = Convert.ToString(oGuid_A_Traer);
                    log.Info("Se ha subido el archivo de nombre " + filename + " con el Id " + archivo.Id);
                }
                else
                {
                    throw new Exception("No se ha podido recuperar el archivo de nombre " + filename);
                }
            }
            catch (Exception ex)
            {
                archivo.Id = string.Empty;
                archivo.Error = true;
                archivo.Detalle = "Fallo la subida del archivo " + (archivo.Nombre ?? archivo.Url);
                log.Error("Fallo la subida del archivo de la url " + url);
                log.Error("Motivo del fallo: " + ex.Message);
                Console.WriteLine(ex.Message);
            }

            return archivo;
        }

        private string CheckFileExtension(byte[] buffer)
        {
            foreach (KeyValuePair<byte[], string> Sequences in Constancts.SupportedExtensions)
            {
                if(buffer.Take(Sequences.Key.Length).SequenceEqual(Sequences.Key))
                {
                    return Sequences.Value;
                }
            }

            return null;
        }


        private string SubirArchivo(string url)
        {
            string Id = string.Empty;
            try
            {

                string content = string.Empty;
                byte[] oFileToSave = null;

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3 |
                                                       SecurityProtocolType.Tls | SecurityProtocolType.Tls11;

                ServicePointManager.ServerCertificateValidationCallback += RemoteCertificateValidationCallback;

                log.Info("Se descargara el certificado con la siguiente url " + url);
                HttpWebRequest request = Funciones.GenerarRequest(url);

                log.Info("Invocando a la descarga del archivo con la siguiente url " + url);
                WebResponse response = (HttpWebResponse)request.GetResponse();

                log.Info("Obteniendo la respuesta del Endpoint " + url);
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    oFileToSave = reader.CurrentEncoding.GetBytes(reader.ReadToEnd());
                }

                log.Info("Iniciando Configuracion de Digiweb para subir el archivo previamente descargado");

                string digiwebEndpoint = ConfigurationManager.AppSettings["DigiWebEndpoint"];
                string digiDocCodigoSistema = ConfigurationManager.AppSettings["DigiDocCodigoSistema"];
                string digiDocCodigoExterno = ConfigurationManager.AppSettings["DigiDocCodigoExterno"];
                string digiDocCodigoId = ConfigurationManager.AppSettings["DigiDocCodigoId"];

                DigiWeb.DigitalizacionServicio service = new DigiWeb.DigitalizacionServicio();
                service.Credentials = CredentialCache.DefaultCredentials;
                service.Url = digiwebEndpoint;

                log.Info("Invocando a Digiweb para obtener la ruta de subida del archivo");
                string oRuta = service.CalcularRutaSistema(digiDocCodigoSistema);
                string filename = string.Empty;
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

                log.Info("Invocando a Digiweb para la subida logica del archivo");
                service.GuardarEDocumentoV2(oEDocumentoOriginal);

                oGuid_A_Traer = oEDocumentoOriginal.Id;

                log.Info("Inicio subida fisica del archivo nombre " + filename + " a la siguiente ruta " + oRuta);
                File.WriteAllBytes(oRuta + "\\" + filename, oFileToSave);

                log.Info("Invocando a Digiweb para verificar que se haya hecho la subida del archivo");
                var oEDocumentoSubido = service.TraerEDocumento(oGuid_A_Traer);

                if (oEDocumentoSubido != null)
                {
                    Id = Convert.ToString(oGuid_A_Traer);
                    log.Info("Se ha subido el archivo de nombre " + filename + " con el Id " + Id);
                }
            }
            catch (Exception ex)
            {
                log.Error("Fallo la subida del archivo de la url " + url);
                log.Error("Motivo del fallo: " + ex.Message);
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
        public SoapException manejoWebError(WebException exp)
        {
            WebResponse response = exp.Response;
            faultDetail = exp.InnerException != null ? exp.InnerException.Message : exp.Message;

            log.Error("Error en la ejecucion : " + faultDetail);

            if (response != null)
            {
                using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                {
                    result = sr.ReadToEnd();
                }
            }
            else
            {
                result = faultDetail;
            }

            log.Error("Error: " + result);
            soapError = Funciones.HandleWebException(result);

            if (soapError != null)
            {
                faultFactor = "Renappo";
                faultDetail = soapError.ErrorCode + " " + soapError.ErrorDescription;
            }

            node.InnerText = faultDetail;
            return new SoapException(faultDetail, SoapException.ServerFaultCode, faultFactor, node);
        }

        public SoapException manejoError(Exception ex)
        {
            faultDetail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;

            node.InnerText = faultDetail;
            return new SoapException(faultDetail, SoapException.ServerFaultCode, faultFactor, node);
        }

        public SoapException manejoError2(Exception ex, string Username, string Metodo)
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