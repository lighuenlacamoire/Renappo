using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace RenappoCertificacion.Models
{
    public class Certificacion
    {
        [JsonProperty("cuit")]
        public string Cuit { get; set; }

        [JsonProperty("razon_social")]
        public string RazonSocial { get; set; }

        [JsonProperty("medio")]
        public string Intermediario { get; set; }

        [JsonProperty("habilitado")]
        public string Habilitado { get; set; }

        [JsonProperty("vigencia")]
        public string FechaVigencia { get; set; }

        [JsonProperty("tarifario")]
        public string Tarifario { get; set; }

        [JsonProperty("certificado")]
        public string Certificado { get; set; }

    }
}