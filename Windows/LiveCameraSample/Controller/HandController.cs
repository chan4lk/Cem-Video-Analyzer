using System;
using System.Net;
using System.Net.Http;

namespace LiveCameraSample.Controller
{
    public class HandController
    {
        public static void MiddleFinger(string ip)
        {
            try
            {
                Random random = new Random(356473);
                var address = $"http://{ip}/ajax_inputs&LED0=1&nocache={1 + random.NextDouble()}";
                WebRequest request = WebRequest.Create(address);
                request.Credentials = CredentialCache.DefaultCredentials;
                WebResponse response = request.GetResponse();
            }
            catch (Exception)
            {

              
            }
           
        }

        public static void Shake(string ip)
        {
            try
            {
                Random random = new Random(356473);
                var address = $"http://{ip}/ajax_inputs&LED1=1&nocache={1 + random.NextDouble()}";
                WebRequest request = WebRequest.Create(address);
                request.Credentials = CredentialCache.DefaultCredentials;
                WebResponse response = request.GetResponse();
            }
            catch (Exception)
            {

                
            }
           
        }
    }
}
