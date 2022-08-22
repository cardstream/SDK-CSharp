using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace Gateway
{
    public class Gateway
    {
        private string merchantID;
        private string merchantSecret;
        private string merchantPwd;
        private string directUrl;
        private string hostedUrl;
        private string proxyUrl;

        public const int RC_SUCCESS = 0;    // Transaction successful
        public const int RC_DO_NOT_HONOR = 5;    // Transaction declined
        public const int RC_NO_REASON_TO_DECLINE = 85;   // Verification successful

        public const int RC_3DS_AUTHENTICATION_REQUIRED = 0x1010A;

        // Non 3DS merchantId = 10001, with secret Circle4Take40Idea
        // 3DS merchantId = 100856, with secret Threeds2Test60System


        public Gateway(string merchantID = "100856", string merchantSecret = "Circle4Take40Idea",
        string directUrl = "https://gateway.example.com/direct/",
        string hostedUrl = "https://gateway.example.com/hosted/",
        string proxyUrl = null)
        {
            this.merchantID = merchantID;
            this.merchantSecret = merchantSecret;
            this.directUrl = directUrl;
            this.hostedUrl = hostedUrl;
            this.proxyUrl = proxyUrl;
        }


        ///<summary>
        /// Send request to Gateway using HTTP Direct API.
        ///
        /// The method will send a request to the Gateway using the HTTP Direct API.
        /// 
        ///  The request will use the following Gateway properties unless alternative
        ///  values are provided in the request dictionary.
        /// hostedUrl          - Gateway Hosted API Endpoint
        /// merchantID         - Merchant Account Id
        /// merchantPwd        - Merchant Account Password
        /// merchantSecret     - Merchant Account Secret
        ///
        /// The method will sign the request and also call verifySignature to 
        /// check any response.
        ///
        /// The method will throw an exception if it is unable to send the request
        /// or receive the response.
        ///
        ///The method does not attempt to validate any request fields.
        ///
        /// The prepareRequest method called within will throw an exception if there
        /// key fields are missing, the method does not attempt to validate any request 
        /// fields.
        ///
        /// </summary>
        /// <param name="request"> Request data </params>
        /// <param name="options">  Not currently used </params>
        public Dictionary<string, string> DirectRequest(Dictionary<string, string> requestFlattened,
            Dictionary<string, string> options = null)
        {
            string secret;
            string directUrl;
            string hostedUrl;

            PrepareRequest(requestFlattened, options, out secret, out directUrl, out hostedUrl);

            if (!string.IsNullOrEmpty(secret))
            {
                requestFlattened["signature"] = Sign(requestFlattened, secret);
            }

            var handler = new HttpClientHandler();
            if (!string.IsNullOrEmpty(proxyUrl))
            {
                handler.UseProxy = true;
                handler.Proxy = new WebProxy(this.proxyUrl);
            }

            // In Console applications in .Net Framework version 4.7.2 this is not required. In the context of a 
            // ASP.NET web application with the same version, it is required. 
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;


            using (var httpClient = new HttpClient(handler))
            using (var requestMessage = new HttpRequestMessage(
                    new HttpMethod("POST"), directUrl))
            {

                requestMessage.Content = new StringContent(GetUrlEncodedBody(requestFlattened));

                requestMessage.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded");

                // Using an async request but immediately blocking while we wait for its response
                var httpResponse = httpClient.SendAsync(requestMessage);
                var result = httpResponse.Result;
                var resultString = result.Content.ReadAsStringAsync().Result;

                var responseNvc = HttpUtility.ParseQueryString(resultString);
                var response = responseNvc.AllKeys
                    .ToDictionary(k => k, k => responseNvc[k]);

                VerifyResponse(response, secret);

                return response;
            }
        }



        ///<summary>
        /// Send request to Gateway using HTTP Direct API.
        ///
        /// The method will send a request to the Gateway using the HTTP Direct API.
        /// 
        ///  The request will use the following Gateway properties unless alternative
        ///  values are provided in the request dictionary.
        /// hostedUrl          - Gateway Hosted API Endpoint
        /// merchantID         - Merchant Account Id
        /// merchantPwd        - Merchant Account Password
        /// merchantSecret     - Merchant Account Secret
        ///
        /// The method will sign the request and also call verifySignature to 
        /// check any response.
        ///
        /// The method will throw an exception if it is unable to send the request
        /// or receive the response.
        ///
        ///The method does not attempt to validate any request fields.
        ///
        /// The prepareRequest method called within will throw an exception if there
        /// key fields are missing, the method does not attempt to validate any request 
        /// fields.
        ///
        /// </summary>
        /// <param name="request"> Request data </params>
        /// <param name="options">  Not currently used </params>
        public Dictionary<string, object> DirectRequest(Dictionary<string, object> request,
            Dictionary<string, string> options = null)
        {
            string secret;
            string directUrl;
            string hostedUrl;

            PrepareRequest(request, options, out secret, out directUrl, out hostedUrl);

            var requestFlattened = NestedDictionaryAdapter(request);

            if (!string.IsNullOrEmpty(secret))
            {
                requestFlattened["signature"] = Sign(requestFlattened, secret);
            }

            var handler = new HttpClientHandler();
            if (!string.IsNullOrEmpty(proxyUrl))
            {
                handler.UseProxy = true;
                handler.Proxy = new WebProxy(this.proxyUrl);
            }

            using (var httpClient = new HttpClient(handler))
            using (var requestMessage = new HttpRequestMessage(
                    new HttpMethod("POST"), directUrl))
            {

                requestMessage.Content = new StringContent(GetUrlEncodedBody(requestFlattened));

                requestMessage.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded");

                // Using an async request but immediately blocking while we wait for its response
                var httpResponse = httpClient.SendAsync(requestMessage);
                var result = httpResponse.Result;
                var resultString = result.Content.ReadAsStringAsync().Result;

                var responseNvc = HttpUtility.ParseQueryString(resultString);
                var response = responseNvc.AllKeys
                    .ToDictionary(k => k, k => responseNvc[k]);

                VerifyResponse(response, secret);

                return NestedDictionaryAdapter(response);
            }
        }


        ///<summary>
        /// Create a form that can then be used to send the request to the gateway
        /// using the HTTP Hosted API.
        /// 
        /// The request will use the following Gateway properties unless alternative
        /// values are provided in the request;
        /// hostedUrl          - Gateway Hosted API Endpoint
        /// merchantID         - Merchant Account Id
        /// merchantPwd        - Merchant Account Password
        /// merchantSecret'    - Merchant Account Secret

        ///  The method accepts the following options in the options dictionary
        /// formAttrs     - HTML form attributes string
        /// submitAttrs   - HTML submit button attributes string
        /// submitImage   - URL of image to use as the Submit button
        /// submitHtml    - HTML to show on the Submit button
        /// submitText    - Text to show on the Submit button

        /// 'submitImage', 'submitHtml' and 'submitText' are mutually exclusive
        /// options and will be checked for in that order. If none are provided
        /// the submitText='Pay Now' is assumed.

        /// The prepareRequest method called within will throw an exception if there
        /// key fields are missing, the method does not attempt to validate any request 
        /// fields.

        /// </summary>
        /// <param name="request"> Dictionary<string, string> Request data </params>
        /// <param name="options"> Not currently used </params>
        public string HostedRequest(Dictionary<string, string> request,
                   Dictionary<string, string> options = null)
        {
            string secret;
            string directUrl;
            string hostedUrl;

            PrepareRequest(request, null, out secret, out directUrl, out hostedUrl);        

            if (!request.ContainsKey("redirectURL"))
            {
                // RedirectURL is used to send the user back to your site following
                // a transaction. It must be set according to your environment.  
                request["redirectURL"] = "www.example.net";
            }

            if (!string.IsNullOrEmpty(secret))
            {
                request["signature"] = Sign(request, secret);
            }

            var ret = new StringBuilder();
            var action = WebUtility.HtmlEncode(this.hostedUrl);
            string formAttrs = "";

            if (options != null)
            {
                formAttrs = options.ContainsKey("formAttrs") ? options["formAttrs"] : "";
                ret.Append($@"<form method=""post"" {formAttrs} action=""{action}"" /> \n");
            } 
            else
            {
                ret.Append($@"<form method=""post"" action=""{action}"" /> \n");
            }


            foreach (var name in request.Keys)
            {
                ret.Append(FieldToHtml(name, request[name]));
            }

            string submitAttrs = "";
            string submitElement = "";
            
            if (options != null)
            {
                submitAttrs = options.ContainsKey("submitAttrs") ? options["submitAttrs"] : "";
                ret.Append(submitAttrs);
            }      
                        
            if (options != null)
            {
                if (options.ContainsKey("submitImage"))
                {
                    submitElement = $"<input {submitAttrs}  type=\"image\" src=\""
                              + WebUtility.HtmlEncode(options["submitImage"]) + "\" />\n";
                }
                else if (options.ContainsKey("submitHtml"))
                {
                    submitElement = $"<button type=\"submit\" {submitAttrs} >"
                              + options["submitHtml"] + "</button>\n";
                }
                else if (options.ContainsKey("submitText"))
                {
                    submitElement = $"<input {submitAttrs} type=\"submit\" value=\""
                   + (options.ContainsKey("submitText") ? WebUtility.HtmlEncode(options["submitText"]) : "Pay Now")
                   + "\" />\n";
                }
            }
            else
            {
                submitElement = $"<input {submitAttrs} type=\"submit\" value=\"Pay now\" />\n";
            }

            ret.Append(submitElement + "</form>\n");

            return ret.ToString();
        }


        ///<summary>
        /// Prepare a request for sending to the Gateway.
        /// 
        /// The method will extract the following configuration properties from the
        /// request if they are present;
        ///   merchantSecret
        ///   directUrl
        ///   hostedUrl
        ///
        ///  The method will throw an exception is the request doesn't contain an
        /// 'action' element or a 'merchantID' element (and none could be inserted).
        /// </summary>
        /// <param name="request"> Dictionary<string, string> Request data </params>
        /// <param name="options"> Not currently used </params>
        /// <param name="secret"> The Merchant Secret </params>
        /// <param name="directUrl"> The URL for direct integrations </params>
        /// <param name="hostedUrl"> The URL for hosted integrations </params>
        private void PrepareRequest(Dictionary<string, string> request,
                                    Dictionary<string, string> options,
                                    out string secret,
                                    out string directUrl,
                                    out string hostedUrl)
        {

            if (request == null || request.Count == 0)
            {
                throw new ArgumentException("Request must be provided.");
            }

            if (!request.ContainsKey("action"))
            {
                throw new ArgumentException("Request must contain an 'action'");
            }

            // Insert 'merchantID' if doesn't exist and default is available
            if (!request.ContainsKey("merchantID"))
            {
                if (this.merchantID == null)
                {
                    // MerchantID must be set
                    throw new ArgumentException("MerchantID not set in either request or the class");
                }
                request["merchantID"] = this.merchantID;
            }

            // Insert 'merchantPwd' if doesn't exist and default is available
            if (!request.ContainsKey("merchantPwd") && merchantPwd != null)
            {
                request["merchantPwd"] = merchantPwd;
            }

            if (request.ContainsKey("merchantSecret"))
            {
                secret = request["merchandSecret"];
                request.Remove("merchantSecret");
            }
            else
            {
                secret = merchantSecret;
            }

            if (request.ContainsKey("hostedUrl"))
            {
                hostedUrl = request["hostedUrl"];
                request.Remove("hostedUrl");
            }
            else
            {
                hostedUrl = this.hostedUrl;
            }

            if (request.ContainsKey("directUrl"))
            {
                directUrl = request["directUrl"];
                request.Remove("directUrl");
            }
            else
            {
                directUrl = this.directUrl;
            }


            // Remove items we don't want to send in the request
            // (they may be there if a previous response is sent)
            var removeKeys = new string[] {
                "responseCode",
                "responseMessage",
                "responseStatus",
                "state",
                "signature",
                "merchantAlias",
                "merchantID2"
            };

            foreach (var key in removeKeys)
            {
                request.Remove(key); // Doesn't error if key not present. 
            }
        }



        ///<summary>
        /// Prepare a request for sending to the Gateway.
        /// 
        /// The method will extract the following configuration properties from the
        /// request if they are present;
        ///   merchantSecret
        ///   directUrl
        ///   hostedUrl
        ///
        ///  The method will throw an exception is the request doesn't contain an
        /// 'action' element or a 'merchantID' element (and none could be inserted).
        /// </summary>
        /// <param name="request"> Dictionary<string, string> Request data </params>
        /// <param name="options"> Not currently used </params>
        /// <param name="secret"> The Merchant Secret </params>
        /// <param name="directUrl"> The URL for direct integrations </params>
        /// <param name="hostedUrl"> The URL for hosted integrations </params>
        private void PrepareRequest(Dictionary<string, object> request,
                                    Dictionary<string, string> options,
                                    out string secret,
                                    out string directUrl,
                                    out string hostedUrl)
        {

            if (request == null || request.Count == 0)
            {
                throw new ArgumentException("Request must be provided.");
            }

            if (!request.ContainsKey("action"))
            {
                throw new ArgumentException("Request must contain an 'action'");
            }

            // Insert 'merchantID' if doesn't exist and default is available
            if (!request.ContainsKey("merchantID"))
            {
                if (this.merchantID == null)
                {
                    // MerchantID must be set
                    throw new ArgumentException("MerchantID not set in either request or the class");
                }
                request["merchantID"] = this.merchantID;
            }

            // Insert 'merchantPwd' if doesn't exist and default is available
            if (!request.ContainsKey("merchantPwd") && merchantPwd != null)
            {
                request["merchantPwd"] = merchantPwd;
            }

            if (request.ContainsKey("merchantSecret"))
            {
                secret = (string)request["merchandSecret"];
                request.Remove("merchantSecret");
            }
            else
            {
                secret = merchantSecret;
            }

            if (request.ContainsKey("hostedUrl"))
            {
                hostedUrl = (string)request["hostedUrl"];
                request.Remove("hostedUrl");
            }
            else
            {
                hostedUrl = this.hostedUrl;
            }

            if (request.ContainsKey("directUrl"))
            {
                directUrl = (string)request["directUrl"];
                request.Remove("directUrl");
            }
            else
            {
                directUrl = this.directUrl;
            }


            // Remove items we don't want to send in the request
            // (they may be there if a previous response is sent)
            var removeKeys = new string[] {
                "responseCode",
                "responseMessage",
                "responseStatus",
                "state",
                "signature",
                "merchantAlias",
                "merchantID2"
            };

            foreach (var key in removeKeys)
            {
                request.Remove(key); // Doesn't error if key not present. 
            }
        }

        private static string GetUrlEncodedBody(IEnumerable<KeyValuePair<string, string>> fields)
        {
            var rtn = string.Join("&",
                fields.Select(f => string.Format("{0}={1}", WebUtility.UrlEncode(f.Key), WebUtility.UrlEncode(f.Value))));

            return rtn.Replace("!", "%21").Replace("*", "%2A").Replace("(", "%28").Replace(")", "%29");
        }

        ///<summary>
        /// Sign the given array of data.
        /// 
        /// This method will return the correct signature for the dictionary
        /// </summary>
        /// <param name="fields"> Dictionary<string, string> containing the fields to be signed. </params>
        /// <param name="secret"> secret used to create the signature </params>
        /// <returns>
        /// Signature calculated from the provided fields.
        /// </returns>
        public string Sign(Dictionary<string, string> fields, string secret = null, IEnumerable<string> partial = null)
        {
            var partialStr = "";

            if (partial != null)
            {
                fields = fields.Where(kvp => partial.Contains(kvp.Key))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                partialStr = "|" + string.Join(",", partial);

            }

            if (secret == null && merchantSecret != null)
            {
                secret = merchantSecret;
            }

            // HttpUtility returns UPPERCASE percent encoded characeters
            var encodedFields = fields.OrderBy(f => (
                f.Key.Contains("[") ? f.Key.Replace("[", "0").Substring(0, f.Key.IndexOf("[")) : f.Key),
                StringComparer.Ordinal);
            var encodedBody = GetUrlEncodedBody(encodedFields);

            encodedBody = encodedBody.Replace("%0D", "");

            var bytes = Encoding.UTF8.GetBytes(encodedBody + secret);

            string signature;

            using (var hash = SHA512.Create())
            {
                var hashedInputBytes = hash.ComputeHash(bytes);

                signature = BitConverter.ToString(hashedInputBytes).Replace("-", "").ToLower();
            }

            return signature + partialStr;
        }


        /// <summary>
        /// Collect browser device information.
        ///
        /// The method will return a self submitting HTML form designed to provided
        /// the browser device details in the following integration fields;
        ///   + 'deviceChannel'            - Fixed value 'browser',
        ///   + 'deviceIdentity'            - Browser's UserAgent string
        ///   + 'deviceTimeZone'            - Browser's timezone
        ///   + 'deviceCapabilities'        - Browser's capabilities
        ///   + 'deviceScreenResolution'    - Browser's screen resolution (widthxheightxcolour-depth)
        ///   + 'deviceAcceptContent'    - Browser's accepted content types
        ///   + 'deviceAcceptEncoding'    - Browser's accepted encoding methods
        ///   + 'deviceAcceptLanguage'    - Browser's accepted languages
        ///   + 'deviceAcceptCharset'    - Browser's accepted character sets
        ///
        /// The above fields will be submitted as child elements of a 'browserInfo'
        /// parent field.
        ///
        /// The method accepts the following options;
        ///   + 'formAttrs'        - HTML form attributes string
        ///   + 'formData'        - associative array of additional post data
        ///
        ///
        /// The method returns the HTML fragment that needs including in order to
        /// render the HTML form.
        ///
        /// The browser must suport JavaScript in order to obtain the details and
        /// submit the form.
        ///
        /// @param    array    $options    options (or null)
        /// @return    string                request HTML form.
        ///
        /// @throws    InvalidArgumentException    invalid request data
        public string CollectBrowserInfo(Dictionary<string, object> options, Dictionary<string, string> env)
        {

            if (options == null)
            {
                options = new Dictionary<string, object>();
            }

            var formAttrs = new StringBuilder(@"id=""collectBrowserInfo"" method=""post"" action=""?"" ");

            if (options.ContainsKey("formAttrs") && (!string.IsNullOrEmpty(options["formAttrs"] as string)))
            {
                formAttrs.Append(options["formAttrs"]);
            }

            var deviceData = new Dictionary<string, string>()
            {
                { "deviceChannel", "browser" },
                { "deviceIdentity", (env.ContainsKey("HTTP_USER_AGENT") ? WebUtility.HtmlEncode(env["HTTP_USER_AGENT"]) : null) },
                { "deviceTimeZone", "0" },
                { "deviceCapabilities", "" },
                { "deviceScreenResolution", "1x1x1" },
                { "deviceAcceptContent", (env.ContainsKey("HTTP_ACCEPT") ? WebUtility.HtmlEncode(env["HTTP_ACCEPT"]) : null) },
                { "deviceAcceptEncoding", (env.ContainsKey("HTTP_ACCEPT_ENCODING") ? WebUtility.HtmlEncode(env["HTTP_ACCEPT_ENCODING"]) : null) },
                { "deviceAcceptLanguage", (env.ContainsKey("HTTP_ACCEPT_LANGUAGE") ? WebUtility.HtmlEncode(env["HTTP_ACCEPT_LANGUAGE"]) : null)}
            };


            var formFields = FieldToHtml("browserInfo", deviceData);

            if (options.ContainsKey("formData"))
            {
                var formFieldsSb = new StringBuilder();
                var formData = options["formData"] as Dictionary<string, string>;
                foreach (var name in formData.Keys)
                {
                    formFieldsSb.Append(FieldToHtml(name, formData[name]));
                }
            }

            var ret = $@"
				<form {formAttrs}>

                    {formFields}
				</form>
				<script>
					var screen_width = (window && window.screen ? window.screen.width : ""0"");
					var screen_height = (window && window.screen ? window.screen.height : ""0"");
					var screen_depth = (window && window.screen ? window.screen.colorDepth : ""0"");
					var identity = (window && window.navigator ? window.navigator.userAgent : """");
					var language = (window && window.navigator ? (window.navigator.language ? window.navigator.language : window.navigator.browserLanguage) : '');
					var timezone = (new Date()).getTimezoneOffset();
					var java = (window && window.navigator ? navigator.javaEnabled() : false);
					var fields = document.forms.collectBrowserInfo.elements;
					fields['browserInfo[deviceIdentity]'].value = identity;
					fields['browserInfo[deviceTimeZone]'].value = timezone;
					fields['browserInfo[deviceCapabilities]'].value = 'javascript' + (java ? ',java' : '');
					fields['browserInfo[deviceAcceptLanguage]'].value = language;
					fields['browserInfo[deviceScreenResolution]'].value = screen_width + 'x' + screen_height + 'x' + screen_depth;
					window.setTimeout('document.forms.collectBrowserInfo.submit()', 0);
				</script>
				";

            return ret;
        }


        ///<summary>
        /// Verify the any response.
        /// 
        /// This method will verify that the response is present, contains a response
        /// code and is correctly signed.
        /// </summary>
        /// <param name="response"> Dictionary<string, string> containing the fields the server has responded with. </params>
        /// <param name="secret"> secret used to create the signature </params>
        /// <returns>
        /// Boolean true if the signature is correct
        /// </returns>
        public bool VerifyResponse(Dictionary<string, string> response, string secret = null)
        {
            if (response == null || response.Count == 0)
            {
                throw new ArgumentException("Invalid response from Gateway");
            }

            if (string.IsNullOrEmpty(secret))
            {
                secret = merchantSecret;
            }

            IEnumerable<string> fields = null;
            string signature = null;
            if (response.ContainsKey("signature"))
            {
                signature = (string)response["signature"];
                response.Remove("signature");

                if (!string.IsNullOrEmpty(secret)
                    && !string.IsNullOrEmpty(signature)
                    && signature.IndexOf("|") != -1)
                {
                    var split = signature.Split('|');
                    signature = split.First();
                    fields = split.Skip(1).First().Split(',');
                }
            }

            // We display three suitable different exception messages to help show
            // secret mismatches between ourselves and the Gateway without giving
            // too much away if the messages are displayed to the Cardholder.
            if (string.IsNullOrEmpty(secret) && !string.IsNullOrEmpty(signature))
            {
                // Signature present when not expected (Gateway has a secret but we don't)
                throw new InvalidOperationException("Incorrectly signed response from Payment Gateway (1)");
            }
            else if (!string.IsNullOrEmpty(secret) && string.IsNullOrEmpty(signature))
            {
                // Signature missing when one expected (We have a secret but the Gateway doesn't)
                throw new InvalidOperationException("Incorrectly signed response from Payment Gateway (2)");
            }
            else if (!string.IsNullOrEmpty(secret) && Sign(response, secret, fields) != signature)
            {
                // Signature mismatch
                throw new InvalidOperationException("Incorrectly signed response from Payment Gateway");
            }

            response.Add("signature", signature);
            return true;
        }

        public static string FieldToHtml(string name, object value)
        {
            if (value.GetType() == typeof(string))
            {
                return FieldToHtml(name, (string)value);
            }
            else if (value.GetType() == typeof(Dictionary<string, string>))
            {
                return FieldToHtml(name, (Dictionary<string, string>)value);
            }
            else
            {
                throw new NotImplementedException("Invalid type passed to FieldToHtml");
            }
        }

        public static string FieldToHtml(string name, Dictionary<string, string> value)
        {
            var sb = new StringBuilder();

            foreach (var k in value.Keys)
            {
                sb.Append(FieldToHtml(name + $"[{k}]", value[k]));
            }

            return sb.ToString();
        }

        public static string FieldToHtml(string name, string value)
        {
            value = WebUtility.HtmlEncode(value);
            return $"<input type=\"hidden\" name=\"{name}\" value=\"{value}\" />\n";
        }

        private static Dictionary<string, object> NestedDictionaryAdapter(Dictionary<string, string> inDict)
        {
            var rtn = new Dictionary<string, object>();

            foreach (var k in inDict.Keys)
            {
                if (k.Contains("[") && k.IndexOf("]") > k.IndexOf("["))
                {
                    var splt = k.Split('[');
                    var outerKey = splt[0];
                    var innerKey = splt[1].Trim(']');


                    if (!rtn.ContainsKey(outerKey))
                    {
                        rtn[outerKey] = new Dictionary<string, string>();
                    }

                    (rtn[outerKey] as Dictionary<string, string>)[innerKey] = inDict[k];
                }
                else
                {
                    rtn[k] = inDict[k];
                }
            }

            return rtn;
        }

        private static Dictionary<string, string> NestedDictionaryAdapter(Dictionary<string, object> inDict)
        {
            var rtn = new Dictionary<string, string>();

            foreach (var k in inDict.Keys)
            {
                if (inDict[k].GetType() == typeof(string))
                {
                    rtn[k] = (string)inDict[k];
                }
                else if (inDict[k].GetType() == typeof(Dictionary<string, string>))
                {
                    var nestedD = (Dictionary<string, string>)inDict[k];
                    foreach (var nestedK in nestedD.Keys)
                    {
                        rtn[$"{k}[{nestedK}"] = nestedD[nestedK];
                    }
                }
            }

            return rtn;
        }
    }
}
