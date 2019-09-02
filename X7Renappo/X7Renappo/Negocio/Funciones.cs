using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Text.RegularExpressions;
using System.Web;

namespace X7Renappo.Negocio
{
    public static class Funciones
    {
        public static HttpWebRequest GenerarRequest(string endpoint)
        {
            HttpWebRequest request = WebRequest.Create(new Uri(endpoint)) as HttpWebRequest;

            //request.ContentType = "application/json; charset=utf-8";
            //request.Accept = "application/json";
            request.Method = "GET";
            request.KeepAlive = true;
            request.PreAuthenticate = true;
            request.AuthenticationLevel = AuthenticationLevel.MutualAuthRequested;
            request.Proxy = CrearProxy();
            request.Credentials = CredentialCache.DefaultCredentials;

            return request;
        }
        public static WebProxy CrearProxy()
        {
            string usuarioProxy = ConfigurationManager.AppSettings["CPA_Proxy_Usuario"];
            string passwdProxy = ConfigurationManager.AppSettings["CPA_Proxy_Passwd"];
            string urlProxy = ConfigurationManager.AppSettings["CPA_Proxy_URL"];
            string domainProxy = ConfigurationManager.AppSettings["CPA_Proxy_Dominio"];

            WebProxy Proxy = new WebProxy(new Uri(urlProxy), false);
            Proxy.Address = new Uri(urlProxy);
            Proxy.BypassProxyOnLocal = false;
            Proxy.UseDefaultCredentials = true;
            Proxy.Credentials = new NetworkCredential(usuarioProxy, passwdProxy, domainProxy);

            return Proxy;
        }

        public static string ConvertToCUIT(string cuit)
        {
            if (string.IsNullOrEmpty(cuit))
            {
                throw new Exception("No se ha enviado el cuit");
            }

            if (cuit.Length != 11)
            {
                throw new Exception("El cuit ingresado debe poseer una longitud de 11 digitos y sin guiones");
            }


            cuit = Regex.Replace(cuit, @"^\b[0-9]\d{1}", @"$&-");

            cuit = Regex.Replace(cuit, @"^\b[0-9]\d{1}-[0-9]\d{7}", @"$&-");

            Regex rgx = new Regex(@"^[0-9]\d{1}-[0-9]\d{7}-[0-9]$/");

            string[] validarCuit = cuit.Split('-');

            if (validarCuit != null && validarCuit.Any()
                && validarCuit.Count() == 3
                && validarCuit[0].Length == 2
                && validarCuit[1].Length == 8
                && validarCuit[2].Length == 1)
            {
                return cuit;
            }
            else
            {
                throw new Exception("El cuit ingresado no es valido, recuerde que el mismo deben ser 11 digitos sin guiones");
            }
        }

        public static string ConvertToFechaVigencia(string fecha, Parametros.FechaVigencia vigencia)
        {
            string formatoFechaValor = @"[0-9]\d{3}-[0-9]\d{1}-[0-9]\d{1}";
            string formatoFechaSeparador = @"[\s][-][\s]";

            if (!string.IsNullOrEmpty(fecha) && Parametros.FormatoFechaVigencia.IsMatch(fecha))
            {
                string fechavigencia = fecha;

                switch (vigencia)
                {
                    case Parametros.FechaVigencia.Desde:
                        {
                            fechavigencia = Regex.Replace(fecha, @"" + formatoFechaSeparador + formatoFechaValor + "$", @"");
                            break;
                        }
                    case Parametros.FechaVigencia.Hasta:
                        {
                            fechavigencia = Regex.Replace(fecha, @"^" + formatoFechaValor + formatoFechaSeparador, @"");
                            break;
                        }
                    default:
                        break;
                }

                return fechavigencia;
            }
            else
            {
                return fecha;
            }
        }

        public static string ConvertToFechaFormato(string fecha, string reemplazar)
        {
            if (!string.IsNullOrEmpty(fecha) && !string.IsNullOrEmpty(reemplazar))
            {
                fecha = fecha.Replace(reemplazar, "");
                fecha = fecha.Trim();
            }

            if (!string.IsNullOrEmpty(fecha) && Parametros.FormatoFecha.IsMatch(fecha))
            {
                var parts = fecha.Split('-');

                if (parts != null && parts.Any() && parts.Length == 3)
                {
                    return parts[2] + "/" + parts[1] + "/" + parts[0];
                }
            }
            return fecha;
        }

        public static string Cuit1 = "30-50009859-3";
        public static string Cuit2 = "30-59895662-2";
    }

    public static class Parametros
    {
        public static Regex FormatoFecha = new Regex(@"^[0-9]\d{3}[-][0-9]\d{1}[-][0-9]\d{1}$");
        public static Regex FormatoFechaVigencia = new Regex(@"^[0-9]\d{3}[-][0-9]\d{1}[-][0-9]\d{1}[\s][-][\s][0-9]\d{3}[-][0-9]\d{1}[-][0-9]\d{1}$");//"2019-06-25 - 2019-12-22"
        public enum FechaVigencia
        {
            Desde,
            Hasta
        }
    }
}