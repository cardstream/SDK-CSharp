using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Gateway;


namespace SampleCode.Controllers
{
    public class HomeController : Controller
    {
        private static string threeDSRef;

        public ActionResult Index()
        {

            var gateway = new Gateway.Gateway();
            var req = System.Web.HttpContext.Current.Request;

            // After the customer as responded to the ACS server, it returns the user
            // to the URL we provide, which includes acs=1 as a GET variable. 
            // This code renames all the submitted form inputs, and also breaks out of the frame. 
            // (In dynamic languages, this is effectively `responseCode = POST`. In C# we can 
            // use string manipulation instead. 
            if (req.QueryString.AllKeys.Contains("acs"))
            {
                var fields = new Dictionary<string, string>();

                foreach (var formItem in req.Form.AllKeys)
                {
                    fields.Add("threeDSResponse[" + formItem + "]", req.Form[formItem]);
                }

                ViewBag.GatewayHtml = SilentPost(req.Url.AbsoluteUri.Replace("acs=1",""), fields, "_parent");

                return View();
            }

            if (req.HttpMethod == "GET")
            {
                // Only the first request to this page/controller is a GET, that means this 
                // must be the first request made. 
                // The first step is to collect the customer's browser data. The SDK provides 
                // a helper form for this purpose. 
                ViewBag.GatewayHtml = gateway.CollectBrowserInfo(null, GetEnvironmentData());
            }
            else
            {
                // If any of the POST form keys start with threeDSResponse we have had a 
                // response from the ACS server (after it has been wrapped by the section 
                // of code above.)
                if (AnyKeyStartsWith(req.Form, "threeDSResponse"))
                {
                    var fields = new Dictionary<string, string>
                    {
                        { "action", "SALE" }
                    };

                    foreach (string item in req.Form)
                    {
                        if (item.StartsWith("threeDSResponse")) 
                        {
                            fields.Add(item, req.Form[item]);
                        }
                    }

                    fields.Add("threeDSRef", threeDSRef);

                    // Send the threeDSresponse to the server, along with threeDSRef previously stored. 
                    // The request will generally look like:
                    // threeDSResponse[AnyKeyFromACS] => value from the ACS
                    // threeDSRef => threeDSRef previously sent by the gateway. 
                    // action => SALE
                    // MerchantID => 100856
                    ViewBag.GatewayHtml = ProcessResponseFields(gateway.DirectRequest(fields, null));
                } 
                else // We don't have a threeDSResponse, but this is a POST request. 
                {    // This is the first request send to the Gateway, which may then request the user
                     // be redirected to the ACS server. 
                     
                    var remoteAddress = req.UserHostAddress;

                    if (remoteAddress == "::1")
                    {
                        // In development environments we often see the IPv6 localhost address.
                        // The Gateway requires an IPv4 address. 
                        remoteAddress = "8.8.8.8";
                    }

                    // Please note that this value MUST be HTTPS. 
                    // In development, this may be achieved using Apache as a reverse proxy and 
                    // changing this value. 
                    var thisUrl = req.Url.AbsoluteUri;

                    var requestFields = GetInitialForm(thisUrl, remoteAddress);

                    foreach (string item in req.Form)
                    {
                        if (item.StartsWith("browserInfo["))
                        {
                            // remove the browserInfo[ and the trailing ]
                            requestFields.Add(item.Substring(12, item.Length - 13), req.Form[item]);
                        }
                    }

                    ViewBag.GatewayHtml = ProcessResponseFields(gateway.DirectRequest(requestFields, null));
                }
            }


            return View();

        }

        ///<summary>
        /// Send request to Gateway using HTTP Direct API.
        ///

        /// </summary>
        /// <param name="request"> Request data </params>
        private string ProcessResponseFields(Dictionary<string, string> responseFields)
        {
            switch (responseFields["responseCode"])
            {
                case "65802":
                    threeDSRef = responseFields["threeDSRef"];
                    return ShowFrameForThreeDS(responseFields, System.Web.HttpContext.Current.Request.Url.AbsoluteUri);
                case "0":
                    return "Payment Successful";
                default:
                    return "Failed to take payment: " + responseFields["responseMessage"];
            }
        }
        
        private bool AnyKeyStartsWith(NameValueCollection haystack, string needle)
        {
            foreach (string hay in haystack)
            {
                if (hay.StartsWith(needle))
                {
                    return true;
                }
            }

            return false;
        }

        private Dictionary<string, string> GetEnvironmentData()
        {
            var rtn = new Dictionary<string, string>();

            rtn.Add("HTTP_USER_AGENT", System.Web.HttpContext.Current.Request.UserAgent);
            rtn.Add("HTTP_ACCEPT", String.Join(",", System.Web.HttpContext.Current.Request.AcceptTypes));
            rtn.Add("HTTP_ACCEPT_ENCODING", System.Web.HttpContext.Current.Request.Headers.GetValues("Accept-Encoding")[0]);
            rtn.Add("HTTP_ACCEPT_LANGUAGE", System.Web.HttpContext.Current.Request.Headers.GetValues("Accept-Language")[0]);

            return rtn;

        }

        ///<summary>
        /// Send request to Gateway using HTTP Direct API.
        ///

        /// </summary>
        /// <param name="request"> Request data </params>
        private string SilentPost(string url, Dictionary<string, string> fields, string target = "_self")
        {
            var rtn = new StringBuilder($@"<form id=""silentPost"" action=""{url}"" method=""post"" target=""{target}"">");

            foreach (var f in fields)
            {
                rtn.AppendLine($@"<input type=""hidden"" name=""{f.Key}"" value=""{f.Value}"" /> ");
            }

            rtn.AppendLine(@"
                <noscript><input type=""submit"" value=""Continue""></noscript>
                </form >
                <script >
                            window.setTimeout('document.forms.silentPost.submit()', 0);
                </script > ");

            return rtn.ToString();
        }

        private string ShowFrameForThreeDS(Dictionary<string, string> fieldsFromServer, string realUrl) {

            //Send request to the ACS server displaying response in an IFRAME
            //Render an IFRAME to show the ACS challenge (hidden for fingerprint method)

            var style = fieldsFromServer.Keys.Contains("threeDSRequest[threeDSMethodData]") ? "display: none;" : "";

            //rtn = f'<iframe name="threeds_acs" style="height:420px; width:420px; {style}"></iframe>\n\n'

            var formFields = new Dictionary<string, string>();

            formFields.Add("devUrl", realUrl);

            foreach (var field in fieldsFromServer)
            {
                if (field.Key.StartsWith("threeDSRequest["))
                {
                    formFields.Add(field.Key.Substring(15, field.Key.Length - 16), field.Value);
                }
            }
            return $"<iframe name=\"threeds_acs\" style=\"height: 420px; width: 420px; {style}\"></iframe>" +
                SilentPost(fieldsFromServer["threeDSURL"], formFields, "threeds_acs");

        }

        private static Dictionary<string, string> GetInitialForm(string url, string remoteAddress)
        {
            return new Dictionary<string, string>{
              {"merchantID", "100856"},
              {"action", "SALE"},
              {"type", "1"},

              //Notice: This isn't required by the gateway, but it is strongly recommended. 
              {"transactionUnique", RandomString() },

              {"countryCode", "826"},
              {"currencyCode", "826"},
              {"amount", "1001"},
              {"cardNumber", "4012001037141112"},
              {"cardExpiryMonth", "12"},
              {"cardExpiryYear", "20"},
              {"cardCVV", "083"},
              {"customerName", "Test Customer"},
              {"customerEmail","test@testcustomer.com"},
              {"customerAddress", "16 Test Street"},
              {"customerPostcode", "TE15 5ST"},
              {"orderRef", "Test purchase"},
    
                // remoteAddress, merchantCategoryCode, threeDSVersion and, threeDSRedirectURL 
                // fields are mandatory for 3DS v2

                // Notice: This must be the card holder's IP address, i.e, 
                // Request.UserHostAddress if you're using ASP.net.
                // For compatibility reasons, it must be an IPv4 address. 
              {"remoteAddress", remoteAddress},
              {"merchantCategoryCode", "5411"},
              {"threeDSVersion", "2"},

              // Notice: This must be set correctly. Customers will be directed
              // here following 3DS authorisation
              {"threeDSRedirectURL", url + "?acs=1"},


            // Requests can carry arbitrary data in addition to the standard fields. 
            // The below keys contain a variety of symbols which may cause issues with 
            // signature calculation. 

            /*
                {"MerchantData[AnyKey]", "This can be any data"},
                {"MerchantData[SecondValue]", "Symbols: ! \" # $ % & ' () * + , - . / 0 1 2"},
                {"MerchantData[C]", "Nested Arrays should not be sorted"},
                {"MerchantData[MoreSymbols]", ": ; < = > ? @ A B [ \\ ] ^ _ ` a b c { | } ~ "}
            */
                };
            
        }

        public static string RandomString()
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, 16)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
