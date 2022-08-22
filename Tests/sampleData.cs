using System;
using System.Collections.Generic;
using System.Linq;

namespace Gateway
{
    class sampleData
    {
        public static Dictionary<string, string> SendInitialRequest()
        {
            var defaultFields = GetInitialForm();

            return defaultFields.Concat(GetExampleBrowserData())
                                        .ToLookup(x => x.Key, x => x.Value)
                                        .ToDictionary(x => x.Key, g => g.First());
        }

        private static Dictionary<string, string> GetInitialForm()
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
              {"remoteAddress", "10.10.10.10"},
              {"merchantCategoryCode", "5411"},
              {"threeDSVersion", "2"},

              // Notice: This must be set correctly. Customers will be directed
              // here following 3DS authorisation
              {"threeDSRedirectURL", "https://example.net/returnUrl?acs=1"}
                };
        }

        private static Dictionary<string, string> GetExampleBrowserData()
        {
            return new Dictionary<string, string>{
            {"deviceChannel", "browser"},
            {"deviceIdentity", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:81.0) Gecko/20100101 Firefox/81.0"},
            {"deviceTimeZone", "-60"},
            {"deviceCapabilities", "javascript"},
            {"deviceScreenResolution", "1920x1080x24"},
            {"deviceAcceptContent", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8"},
            {"deviceAcceptEncoding", "gzip, deflate, br"},
            {"deviceAcceptLanguage", "en-GB"}
        };
        }

        public static Dictionary<string, string> RealWorldExampleResponse() {
            return new Dictionary<string, string> {
            {"__wafRequestID", "2020-11-11T23:22:21Z|94ba931b01|86.155.21.11|lz8a3h1maU"},
            {"action", "SALE"},
            {"addressCheckPref", "not known,not checked,matched,not matched,partially matched"},
            {"amount", "1001"},
            {"amountRetained", "0"},
            {"avscv2CheckEnabled", "Y"},
            {"caEnabled", "N"},
            {"cardCVVMandatory", "Y"},
            {"cardExpiryDate", "1220"},
            {"cardExpiryMonth", "12"},
            {"cardExpiryYear", "20"},
            {"cardFlags", "4128772"},
            {"cardIssuer", "Unknown"},
            {"cardIssuerCountry", "Unknown"},
            {"cardIssuerCountryCode", "XXX"},
            {"cardNumberMask", "401200******1112"},
            {"cardNumberValid", "Y"},
            {"cardScheme", "Visa"},
            {"cardSchemeCode", "VC"},
            {"cardType", "Visa Credit"},
            {"cardTypeCode", "VC"},
            {"cftEnabled", "N"},
            {"countryCode", "826"},
            {"currencyCode", "826"},
            {"currencyExponent", "2"},
            {"customerAddress", "16 Test Street"},
            {"customerEmail", "test@testcustomer.com"},
            {"customerName", "Test Customer"},
            {"customerPostcode", "TE15 5ST"},
            {"customerReceiptsRequired", "N"},
            {"cv2CheckPref", "not known,not checked,matched,not matched,partially matched"},
            {"deviceAcceptContent", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8"},
            {"deviceAcceptEncoding", "gzip, deflate, br"},
            {"deviceAcceptLanguage", "en-GB"},
            {"deviceCapabilities", "javascript"},
            {"deviceChannel", "browser"},
            {"deviceIdentity", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:81.0) Gecko/20100101 Firefox/81.0"},
            {"deviceScreenResolution", "1920x1080x24"},
            {"deviceTimeZone", "-60"},
            {"eReceiptsEnabled", "N"},
            {"initiator", "consumer"},
            {"merchantCategoryCode", "5411"},
            {"merchantID", "100856"},
            {"notifyEmail", "test@example.com"},
            {"orderRef", "Test purchase"},
            {"paymentMethod", "card"},
            {"postcodeCheckPref", "not known,not checked,matched,not matched,partially matched"},
            {"processMerchantID", "100856"},
            {"remoteAddress", "10.10.10.10"},
            {"requestID", "5fac722e12914"},
            {"requestMerchantID", "100856"},
            {"responseCode", "65802"},
            {"responseMessage", "3DS AUTHENTICATION REQUIRED"},
            {"responseStatus", "2"},
            {"riskCheckEnabled", "N"},
            {"rtsEnabled", "N"},
            {"signature", "523f04c3bf9b0f1fe7dbeca103ab026a11b956ff3c4a474a4b6a1ec4266d93da4c5b8eec615ab0d00de18a2d64136cb0d3a3080c89e5c8dbec9b399d08a98eb8" },
            {"state", "received"},
            {"surchargeEnabled", "N"},
            {"threeDSCheck", "not checked"},
            {"threeDSCheckPref", "authenticated,not authenticated,attempted authentication"},
            {"threeDSEnabled", "Y"},
            {"threeDSEnrolled", "Y"},
            {"threeDSRedirectURL", "https://example.net/returnUrl?acs=1"},
            {"threeDSRef", "UDNLRVk6dHJhbnNhY3Rpb25JRD03ODY5MiZtZXJjaGFudElEPTEwMDg1NiZfX2xpZmVfXz0xNjA1MTM4NzQy"},
            {"threeDSRequest[threeDSMethodData]", "eyJ0aHJlZURTTWV0aG9kTm90aWZpY2F0aW9uVVJMIjoiaHR0cHM6Ly9leGFtcGxlLm5ldC9yZXR1cm5Vcmw_YWNzPTEmdGhyZWVEU0Fjc1Jlc3BvbnNlPW1ldGhvZCIsInRocmVlRFNTZXJ2ZXJUcmFuc0lEIjoiZWMxZDQ1MzMtM2MzMi00NWJiLTgwNDMtMWZmNjg5MzIzMzEwIn0"},
            {"threeDSResponseCode", "65802"},
            {"threeDSResponseMessage", "3DS AUTHENTICATION REQUIRED"},
            {"threeDSURL", "https://acs.3ds-pit.com/?method"},
            {"threeDSVETimestamp", "2020-11-11 23:22:22"},
            {"threeDSVersion", "2.1.0"},
            {"threeDSXID", "ec1d4533-3c32-45bb-8043-1ff689323310"},
            {"timestamp", "2020-11-11 23:22:22"},
            {"transactionID", "78692"},
            {"transactionUnique", "C7COZKLK1OYXJPQ7"},
            {"type", "1"},
            {"vcsResponseCode", "0"},
            {"vcsResponseMessage", "Success - no velocity check rules applied"},
            {"xref", "20111123JP22RG22RM32VVY"}
                };
        }

        public static string RandomString()
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, 16)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static Dictionary<string, string> GetEnvDictionary()
        {
            return new Dictionary<string, string>()
            {
                { "HTTP_USER_AGENT", "Not Firefox" },
                { "HTTP_ACCEPT", "Anything" },
                { "HTTP_ACCEPT_ENCODING", "UTF-eight" },
                { "HTTP_ACCEPT_LANGUAGE", "Colloquial" }
            };
        }
    }
}
