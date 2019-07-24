using Newtonsoft.Json;
using RenappoCertificacion.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;

namespace RenappoCertificacion.Negocio
{
    public class Recursos
    {
        public Recursos()
        {

        }

        public Certificacion obtenerCertificado(string cuit)
        {
            Certificacion certificado = new Certificacion();
            //ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3 |
                                                   SecurityProtocolType.Tls | SecurityProtocolType.Tls11;

            var request = WebRequest.Create(new Uri("https://renappo.argentina.gob.ar/apiAnses/proveedor.php?cuit="+ cuit)) as HttpWebRequest;

            request.Method = "GET";
            //request.UserAgent = RequestConstants.UserAgentValue;
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;

            string content = string.Empty;

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                using (var stream = response.GetResponseStream())
                {
                    using (var sr = new StreamReader(stream))
                    {
                        content = sr.ReadToEnd();
                    }
                }
            }
            certificado = JsonConvert.DeserializeObject<Certificacion>(content);
            
            return certificado;



            //using (var client = new HttpClient())
            //{
            //    //Passing service base url  
            //    client.BaseAddress = new Uri("https://renappo.argentina.gob.ar");

            //    client.DefaultRequestHeaders.Clear();
            //    //Define request data format  
            //    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            //    //Sending request to find web api REST service resource GetAllEmployees using HttpClient  
            //    HttpResponseMessage Res = client.GetAsync("apiAnses/proveedor.php?cuit=30-50009859-3").Result;

            //    //Checking the response is successful or not which is sent using HttpClient  
            //    if (Res.IsSuccessStatusCode)
            //    {
            //        //Storing the response details recieved from web api   
            //        var EmpResponse = Res.Content.ReadAsStringAsync().Result;

            //        //Deserializing the response recieved from web api and storing into the Employee list  
            //        response = JsonConvert.DeserializeObject<Certificado>(EmpResponse);

            //    }
            //    //returning the employee list to view  
            //    return response;
            //}

        }

    }
}