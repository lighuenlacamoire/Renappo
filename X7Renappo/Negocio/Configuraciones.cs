using log4net;
using System;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Web;
using System.Web.Services.Protocols;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace X7Renappo.Configuraciones
{
    public class SoapLoggerExtension : SoapExtension
    {
        private static readonly ILog log = LogManager.GetLogger("X7-Prove - ConsultaPadron: ");

        private static string DigiWebEndpoint = ConfigurationManager.AppSettings["DigiWebEndpoint"];
        /// <summary>
        /// The old stream coming into this extension when we make a soap request.  See ChainStream.
        /// </summary>
        private Stream oldStream;

        /// <summary>
        /// The new stream which will hold our copy of the old stream when swapping places.
        /// </summary>
        private Stream newStream;

        /// <inheritdoc />
        /// <summary>
        /// The chain stream when overridden in a derived class, allows a SOAP extension access to the memory buffer containing the SOAP request or response.
        /// </summary>
        /// <param name="stream">
        /// The stream.
        /// </param>
        /// <returns>
        /// The <see cref="T:System.IO.Stream" />.
        /// </returns>
        public override Stream ChainStream(Stream stream)
        {
            //Logger.Debug("ChainStream");
            this.oldStream = stream;
            this.newStream = new MemoryStream();
            return this.newStream;
        }

        /// <inheritdoc />
        /// <summary>
        /// The get initializer, see documentation reference.
        /// </summary>
        /// <param name="methodInfo">
        /// The method info.
        /// </param>
        /// <param name="attribute">
        /// The attribute.
        /// </param>
        /// <returns>
        /// The <see cref="T:System.Object" />.
        /// </returns>
        public override object GetInitializer(LogicalMethodInfo methodInfo, SoapExtensionAttribute attribute)
        {
            //Logger.Debug("GetInitializer(LogicalMethodInfo methodInfo, SoapExtensionAttribute attribute)");
            return null;
        }

        /// <inheritdoc />
        /// <summary>
        /// Another get initializer, see documentation reference.
        /// </summary>
        /// <param name="serviceType">
        /// The service type.
        /// </param>
        /// <returns>
        /// The <see cref="T:System.Object" />.
        /// </returns>
        public override object GetInitializer(Type serviceType)
        {
            //Logger.Debug("GetInitializer(Type serviceType)");
            return null;
        }

        /// <inheritdoc />
        /// <summary>
        /// The initialize, see documentation reference.
        /// </summary>
        /// <param name="initializer">
        /// The initializer.
        /// </param>
        public override void Initialize(object initializer)
        {
            //Logger.Debug("Initialize");
        }


        /// <inheritdoc />
        /// <summary>
        /// ProcessMessage processes the soap message.
        /// </summary>
        /// <param name="message">
        /// The incoming soap message.
        /// </param>
        public override void ProcessMessage(SoapMessage message)
        {
            // Log some informational items for reviewing in this sample application.
            //Logger.Debug("Stage : " + message.Stage);

            switch (message.Stage)
            {
                case SoapMessageStage.BeforeSerialize:
                    break;
                case SoapMessageStage.AfterSerialize:
                    this.WriteOutput(message);
                    break;
                case SoapMessageStage.BeforeDeserialize:
                    this.WriteInput(message);
                    break;
                case SoapMessageStage.AfterDeserialize:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Write the output of the outgoing soap request.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        public void WriteOutput(SoapMessage message)
        {
            this.newStream.Position = 0;
            var reader = new StreamReader(this.newStream);
            var requestXml = reader.ReadToEnd();
            string salida = System.Web.HttpContext.Current.Request.Url.AbsoluteUri == message.Url ? "Respuesta Servicio - SAP : " : "Consulta Servicio - Renappo : ";

            if(message.Url == DigiWebEndpoint)
            {
                salida = "Consulta Servicio - Digiweb : ";
            }
            // Trimming the end of the string b/c some of my requests and responses had newlines :-(
            log.Info(salida + Environment.NewLine + this.PrettyXml(requestXml));

            this.newStream.Position = 0;
            CopyStream(this.newStream, this.oldStream);
            this.newStream.Position = 0;
        }

        /// <summary>
        /// Write the input of the incoming soap response.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        public void WriteInput(SoapMessage message)
        {
            CopyStream(this.oldStream, this.newStream);
            this.newStream.Position = 0;
            var reader = new StreamReader(this.newStream);
            var requestXml = reader.ReadToEnd();
            string entrada = System.Web.HttpContext.Current.Request.Url.AbsoluteUri == message.Url ? "Consulta SAP - Servicio : " : "Respuesta Renappo - Servicio : ";

            if (message.Url == DigiWebEndpoint)
            {
                entrada = "Respuesta Digiweb - Servicio : ";
            }
            // Trimming the end of the string b/c some of my requests and responses had newlines :-(
            log.Info(entrada + Environment.NewLine + this.PrettyXml(requestXml));
            this.newStream.Position = 0;
        }

        /// <summary>
        /// Copy Stream puts the contents of the toStream into the fromStream.
        /// We are swapping the oldStream and newStream so we can get the request 
        /// and response from the soap message.
        /// </summary>
        /// <param name="fromStream">
        /// The from stream, the value we want to copy to the toStream.
        /// </param>
        /// <param name="toStream">
        /// The to stream, which is change to the value of the fromStream.
        /// </param>
        private static void CopyStream(Stream fromStream, Stream toStream)
        {
            //Logger.Debug("CopyStream");
            try
            {
                var sr = new StreamReader(fromStream);
                var sw = new StreamWriter(toStream);
                sw.WriteLine(sr.ReadToEnd());
                sw.Flush();
            }
            catch (Exception ex)
            {
                var message = "CopyStream failed because: " + ex.Message;
                log.Error(message, ex);
            }
        }

        /// <summary>
        /// Format the Xml to be pretty for humans.
        /// </summary>
        /// <param name="requestXml">
        /// The request Xml.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public string PrettyXml(string requestXml)
        {
            if (!string.IsNullOrEmpty(requestXml))
            {
                return XDocument.Parse(requestXml).ToString();
            }
            return null;
        }
    }

}