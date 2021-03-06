﻿
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using X7Renappo.Negocio;

namespace X7Renappo.Models
{
    public class Certificacion
    {
        [JsonProperty("cuit")]
        public string Cuit { get; set; }

        [JsonProperty("razon_social")]
        public string RazonSocial { get; set; }

        [JsonProperty("medio")]
        public string Medio { get; set; }

        [JsonProperty("habilitado")]
        public string Habilitado { get; set; }

        [JsonProperty("vigencia")]
        public string FechaVigencia { get; set; }

        [JsonProperty("tarifario")]
        public string Tarifario { get; set; }

        [JsonProperty("notaCoberturaGeografica")]
        public string CoberturaGeografica { get; set; }

        [JsonProperty("certificado")]
        public string Certificado { get; set; }

        [JsonProperty("Data")]
        public string Mensaje { get; set; }

        [JsonIgnore]
        public bool Fallo
        {
            get
            {
                return !(!string.IsNullOrEmpty(Cuit) && string.IsNullOrEmpty(Mensaje));
            }
        }
        
    }

    public class Proveedor
    {
        [System.Xml.Serialization.XmlElement("IT_Mensaje")]
        public IT_Mensaje Mensaje { get; set; }

        [System.Xml.Serialization.XmlElement("IT_Detalle")]
        public IT_Detalle Detalle { get; set; }

        [System.Xml.Serialization.XmlElement("IT_Actividad")]
        public IT_Actividad[] Actividades { get; set; }
    }

    public class IT_Mensaje
    {
        //Si renappo no devuelve valores, se enviara el aviso por el presente campo
        public string Mensaje { get; set; }

        //Tipo mensaje
        public string Tipo { get; set; }

        //Cuit que se consulto
        public string Consulta { get; set; }

    }

    public class IT_Detalle
    {
        public string RazonSocial { get; set; }//no se pudo recuyperar el detao

        public string Intermediario { get; set; }

        public string Habilitado { get; set; }

        public string FechaVigenciaDesde { get; set; }

        public string FechaVigenciaHasta { get; set; }

        public string Tarifario { get; set; }

        public string CoberturaGeografica { get; set; }
    }

    public class IT_Actividad
    {
        public string Actividad { get; set; }
        public string Fecha { get; set; }
        public string Vencimiento { get; set; }
        public string Certificado { get; set; }
    }

    public class IT_Archivo
    {
        public string Id { get; set; }
        public bool Error { get; set; }
        public string Detalle { get; set; }
        public string Nombre { get; set; }
        public string Url { get; set; }
        public string Extension { get; set; }
    }
       
}
namespace X7Renappo.Soap
{
    public class SoapError
    {
        public string Title { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorDescription { get; set; }
    }
}