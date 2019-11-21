using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace X7Renappo
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(GlobalConfiguration).Name);

        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            string ruta = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppSettings["Config.log4Net"]);
            System.IO.FileInfo arch = new System.IO.FileInfo(ruta);
            log4net.Config.XmlConfigurator.ConfigureAndWatch(arch);
        }
        protected void Application_Error(object sender, EventArgs e)
        {
            // Code that runs when an unhandled error occurs
            Exception err = Server.GetLastError().GetBaseException();

            string ErrorID = Guid.NewGuid().GetHashCode().ToString();
            //			string oUsuario =Pagina.User.Identity.Name.ToString();

            //Escribo el Error en el Log de Eventos
            StringBuilder MsgErr = new StringBuilder();

            MsgErr.Append("ID Error		 : " + ErrorID.ToString() + "\n");
            MsgErr.Append("Mensaje Error : " + err.Message.ToString() + "\n");
            MsgErr.Append("Stack		 : " + err.StackTrace.ToString() + "\n");

            log.Error(MsgErr.ToString());
        }

        protected void Application_BeginRequest(Object sender, EventArgs e)
        {
            // OJO!
            // esto se pone aca para que se loguee el session id
            // se toma el cookie que es donde viene el session_id desde el http_request 
            // no se toma del session.sessionid porque todavia no se parseo el request y no existe la session 
            // (no se puede utilizar otro metodo donde si se tiene el estado de la sesion porque hay veces que no se llega a parsear el html (redirect)       
            HttpCookie session_id_cookie = HttpContext.Current.Request.Cookies["ASP.NET_SessionId"];
            if (session_id_cookie != null)
            {
                log4net.ThreadContext.Properties["sessionid"] = session_id_cookie.Value;
            }
        }

        protected void Session_Start(object sender, EventArgs e)
        {
            // Code that runs when a new session is started
            Session["Error"] = "";
            Session["MenuPerfil"] = "";
            Session["PermisosPerfil"] = null;
        }
    }
}
