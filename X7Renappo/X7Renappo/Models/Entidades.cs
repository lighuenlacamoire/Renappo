
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
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

        [JsonProperty("certificado")]
        public string Certificado { get; set; }

        [JsonProperty("notaCoberturaGeografica")]
        public string CoberturaGeografica { get; set; }

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
        public IT_Detalle Detalle { get; set; }

        public IT_Actividad[] Actividad { get; set; }
    }

    public class IT_Detalle
    {
        public string RazonSocial { get; set; }

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

   
}