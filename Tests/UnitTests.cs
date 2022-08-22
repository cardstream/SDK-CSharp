using Gateway;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Gateway
{
    public class Tests
    {
        Gateway gateway;

        [SetUp]
        public void Setup()
        {
            gateway = new Gateway("1", "pass");
        }

        [Test]
        public void TestDirectRequest()
        {
            var g = new Gateway();

            var result = g.DirectRequest(DictionaryTypeAdapter(sampleData.SendInitialRequest()), null);

            Assert.AreEqual(result["responseCode"] as string, "65802");
        }

        [Test]
        public void TestSigning()
        {

            var testSample = new Dictionary<string, string>{
                {"a" , "one"}, {"b" , "two"}};
            Assert.True(gateway.Sign(testSample).StartsWith("86cdc"));


            testSample = new Dictionary<string, string>{
                {"a" , "one"}, {"b" , "New lines! %0D %0D%0A"}};
            Assert.True(gateway.Sign(testSample).StartsWith("cf50d"));
            
            testSample = new Dictionary<string, string>{
                {"a" , "one"}, {"b" , "strange \"'?& symbols "}};
            Assert.True(gateway.Sign(testSample).StartsWith("7c952"));


            testSample = new Dictionary<string, string>{
                {"a" , "one"}, {"b" , "a £ sign"}};
            Assert.True(gateway.Sign(testSample).StartsWith("13637"));

            testSample = new Dictionary<string, string>{
                {"a" , "one"}, {"b" , "aa ** stars"}};
            Assert.True(gateway.Sign(testSample).StartsWith("47a2b1"));

            testSample = new Dictionary<string, string>{
                {"a" , "one"}, {"b" , "newline \n characater"}};
            Assert.True(gateway.Sign(testSample).StartsWith("19582"));

            testSample = new Dictionary<string, string>{
                {"a[aa]" , "12"}, {"a[bb]" , "13"}, {"a1" , "0"}, {"aa" , "1"}, {"aZ" , "2"}};
            Assert.True(gateway.Sign(testSample).StartsWith("4aeaa")); 
        }

        [Test]
        public void TestSignWithKey()
        {
            var g = new Gateway("1", "incorrectPassword");

            var testSample = new Dictionary<string, string>{
                {"a" , "one"}, {"b" , "two"}};

            Assert.True(g.Sign(testSample, "pass").StartsWith("86cdc"));
        }


        [Test]
        public void TestSignWithPartial()
        {
            var testSample = new Dictionary<string, string>{
                {"a" , "one"}, {"b" , "two" }, {"junk", "junk"}};

            var result = gateway.Sign(testSample, "pass", new List<string>() { "a", "b" }); 
            Assert.True(result.StartsWith("86cdc"));
            Assert.True(result.EndsWith("|a,b"));
        }

        [Test]
        public void TestVerifyRealData()
        {
            Assert.True(gateway.VerifyResponse(sampleData.RealWorldExampleResponse()));
        }

        [Test]
        public void TestCollectBrowserData()
        {
            var result = gateway.CollectBrowserInfo(new Dictionary<string, object>(), sampleData.GetEnvDictionary());
            Assert.True(result.Length > 10);
            Assert.True(result.Contains("browserInfo[deviceAcceptLanguage]\" value=\"Colloquial"));
        }

        [Test]
        public void TestHostedForm()
        {
            var options = new Dictionary<string, string>();
            options["formAttrs"] = "-formAttrs-";
            var hostedForm = gateway.HostedRequest(DictionaryTypeAdapter(sampleData.SendInitialRequest()), options);

            // formAttrs in the correct place when provided
            Assert.True(hostedForm.Contains("<form method=\"post\" -formAttrs- action="));

            options.Clear();
            options["submitImage"] = "example.jpg";
            hostedForm = gateway.HostedRequest(DictionaryTypeAdapter(sampleData.SendInitialRequest()), options);
            Assert.True(hostedForm.Contains("<input   type=\"image\" src=\"example.jpg\" />"));

            options.Clear();
            options["submitHtml"] = "Some <b> HTML </b>";
            hostedForm = gateway.HostedRequest(DictionaryTypeAdapter(sampleData.SendInitialRequest()), options);
            Assert.True(hostedForm.Contains("<button type=\"submit\"  >Some <b> HTML </b></button>"));

            options.Clear();
            options["submitText"] = "Some text";
            hostedForm = gateway.HostedRequest(DictionaryTypeAdapter(sampleData.SendInitialRequest()), options);
            Assert.True(hostedForm.Contains("<input  type=\"submit\" value=\"Some text\" />"));
        }

        public static Dictionary<string, object> DictionaryTypeAdapter(Dictionary<string, string> inDict)
        {
            var rtn = new Dictionary<string, object>();

            foreach (var (k,v) in inDict)
            {
                rtn[k] = v;
            }

            return rtn;
        }


    }
}
